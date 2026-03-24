using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PocketLinks.Helpers;
using PocketLinks.Models;
using PocketLinks.Services;

namespace PocketLinks.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly LinkStorageService _storage;

    public MainViewModel(LinkStorageService storage)
    {
        _storage = storage;

        _linksView = CollectionViewSource.GetDefaultView(Links);
        _linksView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(LinkItem.Category)));
        _linksView.Filter = FilterLinks;

        _storage.LinksChanged += () =>
        {
            Application.Current?.Dispatcher.BeginInvoke(Refresh);
        };

        Refresh();
    }

    public ObservableCollection<LinkItem> Links { get; } = new();

    private readonly ICollectionView _linksView;
    public ICollectionView LinksView => _linksView;

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        _linksView.Refresh();
    }

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private bool _isDarkMode = ThemeHelper.IsSystemDarkMode();

    [RelayCommand]
    private void ToggleEditMode()
    {
        IsEditMode = !IsEditMode;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        ThemeHelper.ApplyThemeColors(Application.Current, IsDarkMode);
    }

    [RelayCommand]
    private void RemoveLink(LinkItem? item)
    {
        if (item == null) return;
        _storage.DeleteLink(item);
        Refresh();
    }

    private bool FilterLinks(object obj)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        if (obj is not LinkItem item) return false;

        return item.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.Target.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private void Refresh()
    {
        Links.Clear();
        foreach (var item in _storage.LoadAll())
            Links.Add(item);
        _linksView.Refresh();
    }

    [RelayCommand]
    private void CopyLink(LinkItem? item)
    {
        if (item == null) return;
        try
        {
            // Put both plain text AND rich HTML on the clipboard.
            // Rich-text apps (Outlook, Teams, Word) will paste the display name as a clickable link.
            // Plain-text apps (Notepad, terminal) will paste the raw URL.
            var dataObj = new DataObject();
            dataObj.SetText(item.Target, TextDataFormat.UnicodeText);

            if (Services.LinkStorageService.IsWebUrl(item.Target))
            {
                var escapedUrl = System.Security.SecurityElement.Escape(item.Target);
                var escapedName = System.Security.SecurityElement.Escape(item.Name);
                var html = BuildClipboardHtml($"<a href=\"{escapedUrl}\">{escapedName}</a>");
                dataObj.SetData(DataFormats.Html, html);
            }

            Clipboard.SetDataObject(dataObj, true);
            StatusMessage = $"Copied: {item.Name}";
        }
        catch
        {
            StatusMessage = "Failed to copy";
        }
    }

    /// <summary>
    /// Builds the CF_HTML clipboard format string that Windows requires.
    /// The header has byte-offset placeholders that must be filled in.
    /// </summary>
    private static string BuildClipboardHtml(string htmlFragment)
    {
        // CF_HTML clipboard format requires byte-offset headers.
        // We build with placeholder strings, measure offsets, then replace.
        const string ph0 = "<<S_HTML>>";
        const string ph1 = "<<E_HTML>>";
        const string ph2 = "<<S_FRAG>>";
        const string ph3 = "<<E_FRAG>>";

        var header =
            "Version:0.9\r\n" +
            $"StartHTML:{ph0}\r\n" +
            $"EndHTML:{ph1}\r\n" +
            $"StartFragment:{ph2}\r\n" +
            $"EndFragment:{ph3}\r\n";

        const string startFrag = "<!--StartFragment-->";
        const string endFrag = "<!--EndFragment-->";

        var sb = new System.Text.StringBuilder();
        sb.Append(header);

        // All placeholders are 10 chars, same as the final "0000000045" style values
        int startHtml = System.Text.Encoding.UTF8.GetByteCount(sb.ToString());
        sb.Append("<html><body>");
        sb.Append(startFrag);
        int fragStart = System.Text.Encoding.UTF8.GetByteCount(sb.ToString());
        sb.Append(htmlFragment);
        int fragEnd = System.Text.Encoding.UTF8.GetByteCount(sb.ToString());
        sb.Append(endFrag);
        sb.Append("</body></html>");
        int endHtml = System.Text.Encoding.UTF8.GetByteCount(sb.ToString());

        sb.Replace(ph0, $"{startHtml:D10}");
        sb.Replace(ph1, $"{endHtml:D10}");
        sb.Replace(ph2, $"{fragStart:D10}");
        sb.Replace(ph3, $"{fragEnd:D10}");

        return sb.ToString();
    }

    [RelayCommand]
    private void OpenLink(LinkItem? item)
    {
        if (item == null) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = item.Target,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DeleteLink(LinkItem? item)
    {
        if (item == null) return;

        var result = MessageBox.Show(
            $"Delete \"{item.Name}\"?",
            "Pocket Links",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _storage.DeleteLink(item);
            Refresh();
        }
    }

    public List<string> GetCategories() => _storage.GetCategories();
    public LinkStorageService Storage => _storage;
}
