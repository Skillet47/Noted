# BusinessLogic

`BusinessLogic` is the core note library for Noted. It provides note models, persistence, folders, trash/history workflows, and template management.

## Structure

- `Features/Notes/` — note CRUD, folder/trash/history workflows, serialization
- `Features/NoteTemplates/` — template load/save/delete
- `Models/` — note models, history entries, operation results
- `Shared/` — enums and shared types

## Note Model

- Base `Note` includes title, content, created/modified timestamps, pin/tag state, format, and original folder metadata
- Supported note types:
  - `GeneralNote`
  - `IdeaNote`
  - `ReminderNote`
  - `TaskNote`

## APIs

- `INoteManagement`
  - list/save/update/delete notes
  - folder create/delete/list
  - trash/restore/permanent delete
  - history retrieval and revert
- `INoteTemplateManagement`
  - list/load/save/delete templates
  - seeds a default template on first use

## Persistence

- `NoteSerializer` supports `.txt`, `.md`, and `.rtf`
- Notes are stored as metadata lines plus content
- Updates create a `.history.json` snapshot file
- Trash uses a companion `.folder` file to preserve original location

## Build / Test

- .NET 10 SDK required
- Build from repo root or `BusinessLogic/BusinessLogic.csproj`
- Run tests with `dotnet test Tests/BusinessLogicTests.csproj`

## Quick Usage

```csharp
var noteManager = new NoteManagement("/path/to/notes");
var templateManager = new NoteTemplateManagement("/path/to/templates");

var reminder = new ReminderNote
{
    Title = "Pay rent",
    Content = "Due on the first business day",
    CreatedAt = DateTime.Now,
    ModifiedAt = DateTime.Now,
    ReminderDateTime = DateTime.Now.AddDays(2),
    Recurrence = RecurrencePattern.Monthly,
    Format = NoteFormat.Markdown,
    Tag = NoteTag.Red,
    IsPinned = true,
};

await noteManager.SaveNoteAsync(reminder);
```
