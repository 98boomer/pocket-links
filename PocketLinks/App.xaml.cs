using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using PocketLinks.Helpers;
using PocketLinks.Services;
using PocketLinks.ViewModels;
using PocketLinks.Views;

namespace PocketLinks;

public partial class App : Application
{
    private static Mutex? _mutex;
    private TaskbarIcon? _trayIcon;
    private LinkStorageService? _storage;
    private MainViewModel? _viewModel;
    private PopupWindow? _popup;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Single-instance guard
        const string mutexName = "PocketLinks_SingleInstance_Mutex";
        _mutex = new Mutex(true, mutexName, out bool createdNew);

        if (!createdNew)
        {
            MessageBox.Show("Pocket Links is already running.", "Pocket Links",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // Sync accent color with Windows system theme
        ThemeHelper.SyncAccentColor(this);
        ThemeHelper.ApplyThemeColors(this, ThemeHelper.IsSystemDarkMode());

        // Initialize services
        _storage = new LinkStorageService();
        _viewModel = new MainViewModel(_storage);

        // Initialize tray icon (defined in App.xaml resources)
        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");

        // Sync the "Start with Windows" checkbox with actual registry state
        if (_trayIcon.ContextMenu?.Items.OfType<MenuItem>()
                .FirstOrDefault(m => m.Name == "StartupMenuItem") is MenuItem mi)
        {
            mi.IsChecked = IsStartupEnabled();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _storage?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }

    private void ShowPopup()
    {
        // Recreate the window if it was closed or the native handle is gone
        if (_popup == null || !_popup.IsHandleValid)
        {
            _popup = new PopupWindow { DataContext = _viewModel };
        }

        _viewModel?.RefreshCommand.Execute(null);
        _popup.ShowAtTray();
    }

    private void TrayIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e)
    {
        ShowPopup();
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        ShowPopup();
    }

    private void AddLink_Click(object sender, RoutedEventArgs e)
    {
        if (_storage == null) return;

        var dialog = new AddEditLinkDialog(_storage);
        dialog.ShowDialog();

        if (dialog.ViewModel.DialogResult)
            _viewModel?.RefreshCommand.Execute(null);
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (_storage == null) return;

        Process.Start(new ProcessStartInfo
        {
            FileName = _storage.RootPath,
            UseShellExecute = true
        });
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Shutdown();
    }

    private const string StartupRegKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValueName = "PocketLinks";

    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRegKey);
        return key?.GetValue(StartupValueName) != null;
    }

    private void Startup_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem mi) return;

        using var key = Registry.CurrentUser.OpenSubKey(StartupRegKey, writable: true);
        if (key == null) return;

        if (mi.IsChecked)
        {
            var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath != null)
                key.SetValue(StartupValueName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(StartupValueName, throwOnMissingValue: false);
        }
    }
}

