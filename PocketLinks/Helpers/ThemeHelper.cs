using System;
using System.Windows;
using System.Windows.Media;

namespace PocketLinks.Helpers;

/// <summary>
/// Detects Windows light/dark mode and accent color, applies matching theme to the app.
/// </summary>
public static class ThemeHelper
{
    /// <summary>
    /// Detects whether the user is in Windows dark mode.
    /// </summary>
    public static bool IsSystemDarkMode()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var val = key?.GetValue("AppsUseLightTheme");
            return val is int i && i == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Reads the Windows accent color from the registry and updates the AccentBrush resource.
    /// </summary>
    public static void SyncAccentColor(Application app)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent");
            var val = key?.GetValue("AccentColorMenu");
            if (val is int abgr)
            {
                byte a = 255;
                byte b = (byte)((abgr >> 16) & 0xFF);
                byte g = (byte)((abgr >> 8) & 0xFF);
                byte r = (byte)(abgr & 0xFF);
                var color = Color.FromArgb(a, r, g, b);
                app.Resources["AccentBrush"] = new SolidColorBrush(color);
            }
        }
        catch { }
    }

    /// <summary>
    /// Applies light or dark mode colors.
    /// </summary>
    public static void ApplyThemeColors(Application app, bool isDark)
    {
        if (isDark)
        {
            app.Resources["PanelBackground"] = new SolidColorBrush(Color.FromRgb(0x2C, 0x2C, 0x2C));
            app.Resources["TileBackground"] = new SolidColorBrush(Color.FromRgb(0x38, 0x38, 0x38));
            app.Resources["TileHoverBackground"] = new SolidColorBrush(Color.FromRgb(0x45, 0x45, 0x45));
            app.Resources["TilePressedBackground"] = new SolidColorBrush(Color.FromRgb(0x50, 0x50, 0x50));
            app.Resources["TextPrimary"] = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
            app.Resources["TextSecondary"] = new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
            app.Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A));
            app.Resources["CategoryHeader"] = new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
            app.Resources["SearchBackground"] = new SolidColorBrush(Color.FromRgb(0x38, 0x38, 0x38));
        }
        else
        {
            app.Resources["PanelBackground"] = new SolidColorBrush(Color.FromRgb(0xF9, 0xF9, 0xF9));
            app.Resources["TileBackground"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            app.Resources["TileHoverBackground"] = new SolidColorBrush(Color.FromRgb(0xE9, 0xE9, 0xE9));
            app.Resources["TilePressedBackground"] = new SolidColorBrush(Color.FromRgb(0xDC, 0xDC, 0xDC));
            app.Resources["TextPrimary"] = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
            app.Resources["TextSecondary"] = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
            app.Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
            app.Resources["CategoryHeader"] = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
            app.Resources["SearchBackground"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
        }
    }
}
