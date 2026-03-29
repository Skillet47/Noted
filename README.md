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
- `BusinessLogic/` - Core domain + file persistence library
- `Tests/` - Unit tests for BusinessLogic

High-level structure:

```
Noted/
  BusinessLogic/
    Core/
    Notes/
    README.md
  Noted/
    Components/
      Layout/
      Modals/
      Pages/
    Services/
    Platforms/
    Resources/
    wwwroot/
    MauiProgram.cs
  Tests/
```

## Core Features

- Multiple note types: Idea, Reminder, Task
- Reminder recurrence support: None, Daily, Weekly, Monthly
- Multiple content formats: Plain Text, Markdown, Rich Text
- Markdown preview and rendered note viewing
- Folder-based organization plus Trash/Restore workflows
- Pinning and color tags for prioritization
- Filtering, sorting, and search
- Custom storage location
- Theme selection and global UI scaling

## App Pages

- `/` - Welcome dashboard with note and folder stats
- `/note/new` and `/note/{title}` - Create/edit/view note flows
- `/settings` - Storage, UI scale, and theme configuration

## Services (UI Project)

- `StorageService`: manages note storage path, current folder, and preview size preferences
- `FilterService`: stores filtering and sorting preferences
- `SidebarService`: coordinates selected folder/note and refresh events
- `ThemeService`: manages selected theme and UI scale
- `NotificationService`: platform-specific reminder notifications
- `MarkdownService`: Markdown to HTML rendering
- `RichTextService`: rich text to HTML rendering

Business logic is provided through `INoteManagement` from the `BusinessLogic` project.

## BusinessLogic Snapshot

BusinessLogic handles note persistence and operations using async APIs:

- Retrieve, save, update, and delete notes
- Work with root and subfolders
- Move notes to trash, restore from trash, and permanently delete from trash
- Create/delete folders and list subfolders

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
# Restore all projects
dotnet restore Noted.slnx

# Build BusinessLogic and tests
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
- Keep component styling in co-located `.razor.css` files when possible.
- If adding new persisted note metadata or types, update serializer logic and tests.
- Keep `BusinessLogic/README.md` and this root README aligned when behavior changes.