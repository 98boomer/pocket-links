using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PocketLinks.Models;
using PocketLinks.Services;

namespace PocketLinks.ViewModels;

public partial class AddEditLinkViewModel : ObservableObject
{
    private readonly LinkStorageService _storage;
    private readonly LinkItem? _existingItem;

    public AddEditLinkViewModel(LinkStorageService storage, LinkItem? existingItem = null)
    {
        _storage = storage;
        _existingItem = existingItem;

        // Load categories
        var cats = _storage.GetCategories();
        cats.Insert(0, ""); // "(None)" at top
        Categories = cats;

        if (_existingItem != null)
        {
            Name = _existingItem.Name;
            Target = _existingItem.Target;
            SelectedCategory = _existingItem.Category;
            IsEditing = true;
            WindowTitle = "Edit Link";
        }
    }

    public bool IsEditing { get; }
    public string WindowTitle { get; } = "Add Link";

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _target = string.Empty;

    [ObservableProperty]
    private string _selectedCategory = string.Empty;

    [ObservableProperty]
    private string _newCategory = string.Empty;

    [ObservableProperty]
    private bool _isNewCategory;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public List<string> Categories { get; }

    public bool DialogResult { get; private set; }

    public Action? CloseAction { get; set; }

    [RelayCommand]
    private void Browse()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select file or application",
            Filter = "All files (*.*)|*.*|Applications (*.exe)|*.exe",
            CheckFileExists = true
        };

        if (dlg.ShowDialog() == true)
        {
            Target = dlg.FileName;
            if (string.IsNullOrWhiteSpace(Name))
                Name = Path.GetFileNameWithoutExtension(dlg.FileName);
        }
    }

    [RelayCommand]
    private void Save()
    {
        // Validate
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Target))
        {
            ErrorMessage = "URL or path is required.";
            return;
        }

        var category = IsNewCategory ? NewCategory.Trim() : SelectedCategory;

        try
        {
            if (_existingItem != null)
            {
                _storage.EditLink(_existingItem, Name.Trim(), Target.Trim(), category);
            }
            else
            {
                _storage.AddLink(Name.Trim(), Target.Trim(), category);
            }

            DialogResult = true;
            CloseAction?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        CloseAction?.Invoke();
    }
}
