# Noted

Noted is a cross-platform note-taking app built with .NET MAUI Blazor Hybrid.

Targets:

- Android
- iOS
- Mac Catalyst
- Windows (when building on Windows)

## Solution Layout

This repository contains three main projects:

- `Noted/` - MAUI Blazor Hybrid UI application
- `BusinessLogic/` - core domain and file persistence library
- `Tests/` - unit tests for BusinessLogic

High-level structure:

```
BusinessLogic/
  Features/
    Notes/
    NoteTemplates/
  Models/
  Shared/
Noted/
  Components/
  Services/
  Platforms/
  Resources/
  wwwroot/
Tests/
  Features/
    Notes/
    NoteTemplates/
```

## Core Features

- Multiple note types: General, Idea, Reminder, Task
- Reminder recurrence support: None, Daily, Weekly, Monthly
- Multiple content formats: Plain Text, Markdown, Rich Text
- Folder organization plus Trash/Restore workflows
- Pinning and color tags for prioritization with visual indicators
- Note history capture on updates with snapshot-based revert
- Reusable note templates (seeded with a default groceries template)
- Filtering, sorting, and search in the UI
- Custom storage location with live path updates
- Theme selection and global UI scaling
- Editor-style tabs for multi-note viewing
- Native platform-specific context menus for note actions
- Comprehensive error handling and graceful degradation

## UI Services (Noted Project)

- `StorageService`: note storage path, current folder, and preview size preferences; supports live path updates
- `FilterService`: filtering and sorting preferences
- `SidebarService`: selected folder/note, tab management (open notes, active tab), and refresh events; tracks pinned vs standard section selection
- `ThemeService`: selected theme and UI scale with global CSS transform scaling
- `NotificationService`: reminder notifications
- `MarkdownService`: Markdown to HTML rendering
- `RichTextService`: rich text to HTML rendering
- `INativeContextMenuService`: platform-native context menus for note actions (UIEditMenuInteraction on Mac Catalyst 16+)

Business logic is provided through `INoteManagement` and `INoteTemplateManagement` from the BusinessLogic project.

## BusinessLogic Snapshot

BusinessLogic exposes async APIs for:

- Retrieve, save, update, and delete notes (root or subfolder)
- Get note history and revert a note to a prior snapshot
- Move notes to trash, restore from trash, and permanently delete from trash
- Create/delete folders and list subfolders
- List, save, load, and delete note templates

For full details, see `BusinessLogic/README.md`.

## Prerequisites

- .NET 10 SDK
- MAUI workloads installed

Install MAUI workloads (if needed):

```bash
dotnet workload install maui
```

## Build And Run

From repository root:

```bash
# Restore solution
dotnet restore Noted.slnx

# Build core projects
dotnet build BusinessLogic/BusinessLogic.csproj
dotnet build Tests/BusinessLogicTests.csproj

# Build MAUI app for Android
dotnet build Noted/Noted.csproj -f net10.0-android

# Build MAUI app for Mac Catalyst (macOS)
dotnet build Noted/Noted.csproj -f net10.0-maccatalyst

# Build MAUI app for Windows (run on Windows)
dotnet build Noted/Noted.csproj -f net10.0-windows10.0.19041.0
```

Example run command (Mac Catalyst):

```bash
dotnet build Noted/Noted.csproj -t:Run -f net10.0-maccatalyst
```

## Testing

Run BusinessLogic tests:

```bash
dotnet test Tests/BusinessLogicTests.csproj
```

## Notes For Contributors

- Register new services in `Noted/MauiProgram.cs`.
- If adding persisted note metadata or note types, update serialization and tests.
- Keep `BusinessLogic/README.md` and this root README aligned when behavior changes.
