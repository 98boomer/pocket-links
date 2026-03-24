using PocketLinks.Models;
using PocketLinks.Services;
using PocketLinks.ViewModels;
using System.Windows;

namespace PocketLinks.Views;

public partial class AddEditLinkDialog : Window
{
    public AddEditLinkViewModel ViewModel { get; }

    public AddEditLinkDialog(LinkStorageService storage, LinkItem? existingItem = null)
    {
        ViewModel = new AddEditLinkViewModel(storage, existingItem);
        ViewModel.CloseAction = Close;
        DataContext = ViewModel;

        InitializeComponent();
    }
}
