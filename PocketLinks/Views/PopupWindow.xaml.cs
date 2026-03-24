using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PocketLinks.Models;
using PocketLinks.ViewModels;

namespace PocketLinks.Views;

public partial class PopupWindow : Window
{
    private DateTime _lastShown;

    public PopupWindow()
    {
        InitializeComponent();
        RenderTransform = new TranslateTransform();
        PreviewKeyDown += Window_PreviewKeyDown;
    }

    /// <summary>
    /// Returns true if this window's native handle is still valid.
    /// After sleep/idle, WPF can silently lose the HWND.
    /// </summary>
    public bool IsHandleValid
    {
        get
        {
            try
            {
                var helper = new WindowInteropHelper(this);
                return helper.Handle != nint.Zero && IsLoaded;
            }
            catch { return false; }
        }
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

        _lastShown = DateTime.UtcNow;

        // Ensure window is visible and activated
        if (Visibility != Visibility.Visible)
            Show();

        Activate();

        // Win32 fallback — force foreground after sleep/idle
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != nint.Zero)
            NativeMethods.SetForegroundWindow(hwnd);

        // Slide up from the offset to 0
        var slideUp = new DoubleAnimation(20, 0, System.TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        transform.BeginAnimation(TranslateTransform.YProperty, slideUp);
    }

    private void Window_Deactivated(object sender, System.EventArgs e)
    {
        // Ignore deactivation during the show sequence (avoids instant hide race)
        if ((DateTime.UtcNow - _lastShown).TotalMilliseconds < 200)
            return;

        Hide();
        if (DataContext is MainViewModel vm && vm.IsEditMode)
            vm.ToggleEditModeCommand.Execute(null);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            if (DataContext is MainViewModel vm && vm.IsEditMode)
                vm.ToggleEditModeCommand.Execute(null);
            e.Handled = true;
        }
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
