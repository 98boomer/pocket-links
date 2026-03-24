# Pocket Links

A lightweight Windows system tray utility for storing and quickly accessing frequently used links.

![.NET 8](https://img.shields.io/badge/.NET-8.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)

## Features

- **System tray access** — Click the tray icon to pop up your link list
- **One-click copy** — Click any link to copy it to your clipboard (with rich HTML for pasting as a hyperlink in Outlook/Teams)
- **Ctrl+Click to open** — Hold Ctrl and click to open the link directly
- **Categories** — Organize links into groups via subfolders
- **Search** — Instantly filter your links by name, URL, or category
- **Light / Dark mode** — Toggle between themes with the ☀ / 🌙 button
- **Edit mode** — iOS-style edit button reveals delete controls on every link
- **OneDrive-friendly storage** — Links are stored as `.url` / `.lnk` files in `Documents\PocketLinks\`, so they sync automatically if your Documents folder is backed up to OneDrive

## Getting Started

### Prerequisites

- Windows 10/11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build & Run

```bash
dotnet build
dotnet run --project PocketLinks
```

### Publish a single-file executable

```bash
dotnet publish PocketLinks -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

The output will be in the `publish/` folder.

## Usage

1. Launch the app — it lives in the system tray (bottom-right of your taskbar)
2. **Left-click** the tray icon to open the popup
3. **Right-click** the tray icon for options (Add Link, Open Folder, Exit)
4. Inside the popup:
   - Click a link → copies to clipboard
   - Ctrl+Click a link → opens it
   - Right-click a link → Copy / Open / Edit / Delete
   - Right-click empty space → Add Link
   - ☀ / 🌙 button → toggle light/dark mode
   - **Edit** button → reveal minus buttons to quickly remove links

## How links are stored

Links are saved as standard Windows shortcut files inside `%USERPROFILE%\Documents\PocketLinks\`:

```
Documents/
└── PocketLinks/
    ├── My Website.url        ← uncategorized
    ├── Work/
    │   ├── Jira Board.url
    │   └── Wiki.url
    └── Tools/
        └── Calculator.lnk   ← file/app shortcut
```

- `.url` files for web links (plain INI format)
- `.lnk` files for local file/app shortcuts

## Tech Stack

- **C# / .NET 8 / WPF** — UI framework
- **Hardcodet.NotifyIcon.Wpf** — System tray integration
- **CommunityToolkit.Mvvm** — MVVM source generators
- **MVVM architecture** — clean separation of Models, ViewModels, Views, Services

## License

MIT
