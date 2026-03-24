using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PocketLinks.Models;
using PocketLinks.ViewModels;

namespace PocketLinks.Views;

public partial class PopupWindow : Window
{
    public PopupWindow()
    {
        InitializeComponent();
        RenderTransform = new TranslateTransform();
    }

    public void ShowAtTray()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 8;
        Top = workArea.Bottom - Height - 8;

        // Position the transform at the start offset BEFORE showing,
        // so the very first rendered frame is already displaced.
        var transform = (TranslateTransform)RenderTransform;
        transform.BeginAnimation(TranslateTransform.YProperty, null); // clear previous
        transform.Y = 20;

        Show();
        Activate();

        // Slide up from the offset to 0
        var slideUp = new DoubleAnimation(20, 0, System.TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        transform.BeginAnimation(TranslateTransform.YProperty, slideUp);
    }

    private void Window_Deactivated(object sender, System.EventArgs e)
    {
        Hide();
        if (DataContext is MainViewModel vm && vm.IsEditMode)
            vm.ToggleEditModeCommand.Execute(null);
    }

    private void LinkTile_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not LinkItem item) return;
        if (DataContext is not MainViewModel vm) return;

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            vm.OpenLinkCommand.Execute(item);
        }
        else
        {
            vm.CopyLinkCommand.Execute(item);
        }
    }

    private void CopyLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not LinkItem item) return;
        if (DataContext is not MainViewModel vm) return;
        vm.CopyLinkCommand.Execute(item);
    }

    private void OpenLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not LinkItem item) return;
        if (DataContext is not MainViewModel vm) return;
        vm.OpenLinkCommand.Execute(item);
    }

    private void EditLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not LinkItem item) return;
        if (DataContext is not MainViewModel vm) return;

        var dialog = new AddEditLinkDialog(vm.Storage, item);
        dialog.Owner = this;
        dialog.ShowDialog();

        if (dialog.ViewModel.DialogResult)
            vm.RefreshCommand.Execute(null);
    }

    private void DeleteLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not LinkItem item) return;
        if (DataContext is not MainViewModel vm) return;
        vm.DeleteLinkCommand.Execute(item);
    }

    private void AddLink_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var dialog = new AddEditLinkDialog(vm.Storage);
        dialog.Owner = this;
        dialog.ShowDialog();

        if (dialog.ViewModel.DialogResult)
            vm.RefreshCommand.Execute(null);
    }
}
