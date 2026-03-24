using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PocketLinks.Models;

public partial class LinkItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _target = string.Empty;

    [ObservableProperty]
    private string _category = string.Empty;

    /// <summary>
    /// Full path to the .url or .lnk file on disk.
    /// </summary>
    [ObservableProperty]
    private string _filePath = string.Empty;

    /// <summary>
    /// True for .url (web link), false for .lnk (local shortcut).
    /// </summary>
    [ObservableProperty]
    private bool _isUrl;

    /// <summary>
    /// Resolved icon for display. Null means use generic icon.
    /// </summary>
    [ObservableProperty]
    private System.Windows.Media.ImageSource? _iconSource;
}
