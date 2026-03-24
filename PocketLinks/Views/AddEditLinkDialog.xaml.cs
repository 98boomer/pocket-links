using PocketLinks.Models;
using PocketLinks.Services;
using PocketLinks.ViewModels;
using System.Windows;
using System.Windows.Input;

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
        PreviewKeyDown += Dialog_PreviewKeyDown;
    }

    private void Dialog_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}
