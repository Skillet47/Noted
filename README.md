# Noted

Noted is a .NET MAUI Blazor Hybrid note-taking app for Android, iOS, Mac Catalyst, and Windows.

## Projects

- `Noted/` — UI app and platform integration
- `BusinessLogic/` — core note models, persistence, folder/trash/history workflows
- `Tests/` — unit tests for business logic

## Key Features

- General, Idea, Reminder, and Task note types
- Plain text, Markdown, and Rich Text content
- Folder organization, trash/restore, and note history
- Note templates with default seed data
- Filtering, sorting, search, and pin/tag support
- Theme selection and global UI scaling

## Prerequisites

- .NET 10 SDK
- MAUI workloads installed

Install MAUI workloads if needed:

```bash
dotnet workload install maui
```

## Build

From repository root:

```bash
dotnet restore Noted.slnx

dotnet build BusinessLogic/BusinessLogic.csproj
dotnet build Tests/BusinessLogicTests.csproj

dotnet build Noted/Noted.csproj -f net10.0-maccatalyst
```

## Run

Example (Mac Catalyst):

```bash
dotnet build Noted/Noted.csproj -t:Run -f net10.0-maccatalyst
```

## Test

```bash
dotnet test Tests/BusinessLogicTests.csproj
```

## Notes

- See `BusinessLogic/README.md` for the core library API and storage model.
