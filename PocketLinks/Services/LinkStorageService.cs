using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PocketLinks.Models;

namespace PocketLinks.Services;

public sealed class LinkStorageService : IDisposable
{
    private const string RootFolderName = "PocketLinks";
    private readonly string _rootPath;
    private readonly FileSystemWatcher _watcher;

    public event Action? LinksChanged;

    public LinkStorageService()
    {
        _rootPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            RootFolderName);

        Directory.CreateDirectory(_rootPath);

        _watcher = new FileSystemWatcher(_rootPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        _watcher.Created += OnFileChanged;
        _watcher.Deleted += OnFileChanged;
        _watcher.Renamed += OnFileChanged;
        _watcher.Changed += OnFileChanged;
    }

    public string RootPath => _rootPath;

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        LinksChanged?.Invoke();
    }

    // ── Read ──────────────────────────────────────────────────────

    public List<LinkItem> LoadAll()
    {
        var items = new List<LinkItem>();
        ScanDirectory(_rootPath, string.Empty, items);
        return items.OrderBy(i => i.Category).ThenBy(i => i.Name).ToList();
    }

    private void ScanDirectory(string directory, string category, List<LinkItem> items)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext is not (".url" or ".lnk")) continue;

            var item = ParseFile(file, category);
            if (item != null) items.Add(item);
        }

        foreach (var subDir in Directory.EnumerateDirectories(directory))
        {
            var dirName = Path.GetFileName(subDir);
            // Skip hidden/system folders and .icons cache
            if (dirName.StartsWith('.') || dirName.StartsWith('_')) continue;
            ScanDirectory(subDir, dirName, items);
        }
    }

    private LinkItem? ParseFile(string filePath, string category)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var name = Path.GetFileNameWithoutExtension(filePath);

        try
        {
            if (ext == ".url")
            {
                var target = ReadUrlFile(filePath);
                if (string.IsNullOrWhiteSpace(target)) return null;

                return new LinkItem
                {
                    Name = name,
                    Target = target,
                    Category = category,
                    FilePath = filePath,
                    IsUrl = true,
                    IconSource = GetGlobeIcon()
                };
            }
            else // .lnk
            {
                var target = ReadLnkFile(filePath);
                if (string.IsNullOrWhiteSpace(target)) return null;

                return new LinkItem
                {
                    Name = name,
                    Target = target,
                    Category = category,
                    FilePath = filePath,
                    IsUrl = false,
                    IconSource = ExtractIcon(target)
                };
            }
        }
        catch
        {
            // Silently skip corrupt files
            return null;
        }
    }

    private static string ReadUrlFile(string filePath)
    {
        foreach (var line in System.IO.File.ReadAllLines(filePath))
        {
            if (line.StartsWith("URL=", StringComparison.OrdinalIgnoreCase))
                return line.Substring(4).Trim();
        }
        return string.Empty;
    }

    private static string ReadLnkFile(string filePath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")!;
        dynamic shell = Activator.CreateInstance(shellType)!;
        try
        {
            dynamic shortcut = shell.CreateShortcut(filePath);
            try
            {
                return (string)shortcut.TargetPath;
            }
            finally
            {
                Marshal.FinalReleaseComObject(shortcut);
            }
        }
        finally
        {
            Marshal.FinalReleaseComObject(shell);
        }
    }

    // ── Write ─────────────────────────────────────────────────────

    public void AddLink(string name, string target, string category)
    {
        var dir = GetCategoryDirectory(category);
        Directory.CreateDirectory(dir);

        if (IsWebUrl(target))
        {
            var path = Path.Combine(dir, SanitizeFileName(name) + ".url");
            WriteUrlFile(path, target);
        }
        else
        {
            var path = Path.Combine(dir, SanitizeFileName(name) + ".lnk");
            WriteLnkFile(path, target);
        }
    }

    public void EditLink(LinkItem existing, string newName, string newTarget, string newCategory)
    {
        // Delete old file
        if (System.IO.File.Exists(existing.FilePath))
            System.IO.File.Delete(existing.FilePath);

        // Create new
        AddLink(newName, newTarget, newCategory);
    }

    public void DeleteLink(LinkItem item)
    {
        if (System.IO.File.Exists(item.FilePath))
            System.IO.File.Delete(item.FilePath);
    }

    private static void WriteUrlFile(string path, string url)
    {
        System.IO.File.WriteAllText(path, $"[InternetShortcut]\r\nURL={url}\r\n");
    }

    private static void WriteLnkFile(string path, string targetPath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")!;
        dynamic shell = Activator.CreateInstance(shellType)!;
        try
        {
            dynamic shortcut = shell.CreateShortcut(path);
            try
            {
                shortcut.TargetPath = targetPath;

                var dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir))
                    shortcut.WorkingDirectory = dir;

                shortcut.Save();
            }
            finally
            {
                Marshal.FinalReleaseComObject(shortcut);
            }
        }
        finally
        {
            Marshal.FinalReleaseComObject(shell);
        }
    }

    // ── Categories ────────────────────────────────────────────────

    public List<string> GetCategories()
    {
        return Directory.EnumerateDirectories(_rootPath)
            .Select(Path.GetFileName)
            .Where(n => !string.IsNullOrEmpty(n) && !n.StartsWith('.') && !n.StartsWith('_'))
            .OrderBy(n => n)
            .ToList()!;
    }

    public void CreateCategory(string name)
    {
        var dir = Path.Combine(_rootPath, SanitizeFileName(name));
        Directory.CreateDirectory(dir);
    }

    public void DeleteCategory(string name)
    {
        var dir = Path.Combine(_rootPath, SanitizeFileName(name));
        if (Directory.Exists(dir))
            Directory.Delete(dir, true);
    }

    public void RenameCategory(string oldName, string newName)
    {
        var oldDir = Path.Combine(_rootPath, SanitizeFileName(oldName));
        var newDir = Path.Combine(_rootPath, SanitizeFileName(newName));
        if (Directory.Exists(oldDir))
            Directory.Move(oldDir, newDir);
    }

    // ── Helpers ───────────────────────────────────────────────────

    private string GetCategoryDirectory(string category)
    {
        return string.IsNullOrWhiteSpace(category)
            ? _rootPath
            : Path.Combine(_rootPath, SanitizeFileName(category));
    }

    public static bool IsWebUrl(string target)
    {
        return target.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();
    }

    // ── Icons ─────────────────────────────────────────────────────

    private static ImageSource? ExtractIcon(string targetPath)
    {
        try
        {
            if (!System.IO.File.Exists(targetPath)) return null;

            using var icon = Icon.ExtractAssociatedIcon(targetPath);
            if (icon == null) return null;

            var source = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        catch
        {
            return null;
        }
    }

    private static ImageSource? _globeIcon;

    private static ImageSource GetGlobeIcon()
    {
        if (_globeIcon != null) return _globeIcon;

        // Draw a simple globe icon in code — no P/Invoke, no system DLL dependency
        const int size = 32;
        var visual = new System.Windows.Media.DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var accent = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));
            accent.Freeze();
            var pen = new System.Windows.Media.Pen(accent, 1.5);
            pen.Freeze();

            var center = new System.Windows.Point(size / 2.0, size / 2.0);
            double radius = size / 2.0 - 2;

            // Outer circle
            dc.DrawEllipse(null, pen, center, radius, radius);

            // Vertical ellipse (meridian)
            dc.DrawEllipse(null, pen, center, radius * 0.45, radius);

            // Horizontal line (equator)
            dc.DrawLine(pen, new System.Windows.Point(2, size / 2.0), new System.Windows.Point(size - 2, size / 2.0));

            // Two latitude lines
            dc.DrawLine(pen, new System.Windows.Point(5, size * 0.3), new System.Windows.Point(size - 5, size * 0.3));
            dc.DrawLine(pen, new System.Windows.Point(5, size * 0.7), new System.Windows.Point(size - 5, size * 0.7));
        }

        var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
            size, size, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
        rtb.Render(visual);
        rtb.Freeze();
        _globeIcon = rtb;
        return _globeIcon;
    }

    public void Dispose()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
    }
}
