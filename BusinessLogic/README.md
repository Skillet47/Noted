# BusinessLogic Project

BusinessLogic is the core domain and persistence layer for Noted. It is a .NET 10 class library responsible for:

- Note models and type-specific behavior
- File-based persistence for notes
- Folder organization
- Trash and restore workflows
- Note update history and revert support
- Note template management
- Operation result handling for business operations

## Current Structure

```
BusinessLogic/
   Features/
      Notes/
         INoteManagement.cs
         NoteManagement.cs
         NoteManagement.Folders.cs
         NoteManagement.Trash.cs
         NoteSerializer.cs
      NoteTemplates/
         INoteTemplateManagement.cs
         NoteTemplate.cs
         NoteTemplateManagement.cs
   Models/
      OperationResult.cs
      Notes/
         Note.cs
         GeneralNote.cs
         IdeaNote.cs
         ReminderNote.cs
         TaskNote.cs
         NoteHistoryEntry.cs
   Shared/
      NoteFormat.cs
      NoteTag.cs
      NoteType.cs
      TaskStatus.cs
      PreviewSize.cs
   BusinessLogic.csproj
```

## Key Concepts

### Base Note Model

All notes inherit from `Note` and include:

- `Title` (required)
- `Content` (required)
- `CreatedAt` (required)
- `ModifiedAt` (required, mutable)
- `IsPinned`
- `Tag` (`NoteTag`) - displayed as visual indicators
- `Format` (`NoteFormat`: `PlainText`, `Markdown`, `RichText`)
- `OriginalFolder` (used for trash/restore metadata)
- `Type` (abstract `NoteType` implemented by derived types)

### Note Types

- `GeneralNote`: basic note with no specialized metadata
- `IdeaNote`: ideation note with `Stage` (`IdeaStage`) and stage prompts
- `ReminderNote`: `ReminderDateTime` and `Recurrence` (`None`, `Daily`, `Weekly`, `Monthly`)
- `TaskNote`: `Status` (`NoteTaskStatus`: `NotStarted`, `InProgress`, `Completed`)

### Note Management API

`INoteManagement` exposes operations for:

- Retrieving notes from root or subfolders
- Saving notes to root or subfolders
- Updating and deleting notes
- Reading note history (`GetNoteHistoryAsync`)
- Reverting to a history snapshot (`RevertNoteToHistoryAsync`)
- Moving notes to trash, restoring from trash, permanently deleting from trash
- Creating/deleting folders and listing subfolders

Most write operations return `OperationResult`.

### Template Management API

`INoteTemplateManagement` exposes operations for:

- Listing templates (`GetTemplatesAsync`)
- Loading a template by name (`GetTemplateAsync`)
- Creating/updating a template (`SaveTemplateAsync`)
- Deleting a template (`DeleteTemplateAsync`)

Templates are persisted in `templates.json` under the configured template folder. On first run, `GetTemplatesAsync` seeds a default `Groceries List` template if no file exists.

## Storage Model

### Supported File Types

`NoteSerializer` supports:

- `.txt` -> `PlainText`
- `.md` -> `Markdown`
- `.rtf` -> `RichText`

### File Naming

Saved note files are named:

`{SanitizedTitle}_{CreatedAt:yyyyMMddHHmmss}{extension}`

### Serialized Note File Format

Each note file stores metadata as lines followed by a delimiter and then full note content:

```
Line 0: Title
Line 1: CreatedAt (ISO 8601)
Line 2: ModifiedAt (ISO 8601)
Line 3: NoteType
Line 4: ReminderDateTime (Reminder only; empty otherwise)
Line 5: Recurrence (Reminder only; empty otherwise)
Line 6: IsPinned
Line 7: TaskStatus (Task only; empty otherwise)
Line 8: Tag
Line 9: Format
Line 10: IdeaStage (Idea only; empty otherwise)
Line 11: ---CONTENT---
Line 12+: Content (multi-line supported)
```

### History Metadata

When a note is updated, the previous snapshot is appended to:

- `{noteFileName}.history.json`

History entries include core note fields and type-specific metadata (reminder recurrence/date, task status, idea stage).

### Trash Metadata

When moving a note to trash, a companion metadata file is created:

- `{noteFileName}.folder`

This stores the original subfolder path (or empty for root) so restores can return notes to the correct location.

## Error Handling

BusinessLogic implements comprehensive error handling throughout all Features:

### Key Principles

- **Graceful Degradation**: Non-critical operations (like history tracking) don't fail the main operation
- **Input Validation**: All public methods validate parameters before processing
- **Specific Exception Handling**: Different handling for `IOException`, `UnauthorizedAccessException`, `JsonException`, etc.
- **Safe Defaults**: Type parsing uses `TryParse` with fallback values for backward compatibility
- **Cancellation Support**: Operations properly respect and rethrow `OperationCanceledException`

### OperationResult Pattern

Write operations return `OperationResult` for success/failure reporting:

```csharp
var result = await noteManager.SaveNoteAsync(note);
if (!result.Success)
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}
```

### Scope

- `NoteManagement`: History operations, folder operations, trash workflows, file I/O
- `NoteSerializer`: File reading/parsing, metadata deserialization, safe enum/date conversion
- `NoteTemplateManagement`: Template file I/O, JSON serialization
- All public methods include input validation and appropriate exception handling

## Usage Example

```csharp
using BusinessLogic.Core.Features.Notes;
using BusinessLogic.Features.NoteTemplates;
using BusinessLogic.Models.Notes;
using BusinessLogic.Shared;

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
      IsPinned = true
};

var saveResult = await noteManager.SaveNoteAsync(reminder);
if (!saveResult.Success)
{
      Console.WriteLine(saveResult.ErrorMessage);
      return;
}

var updated = new ReminderNote
{
      Title = reminder.Title,
      Content = reminder.Content + Environment.NewLine + "Set autopay reminder.",
      CreatedAt = reminder.CreatedAt,
      ModifiedAt = reminder.ModifiedAt,
      ReminderDateTime = reminder.ReminderDateTime,
      Recurrence = reminder.Recurrence,
      Format = reminder.Format,
      Tag = reminder.Tag,
      IsPinned = reminder.IsPinned
};

await noteManager.UpdateNoteAsync("Pay rent", updated);
var history = await noteManager.GetNoteHistoryAsync("Pay rent");

var templateSave = await templateManager.SaveTemplateAsync(new NoteTemplate
{
      Name = "Monthly Reminder",
      Title = "Reminder",
      Content = "- [ ] Action item",
      NoteType = NoteType.Reminder,
      Format = NoteFormat.Markdown,
      Recurrence = RecurrencePattern.Monthly
});
```

## Notes For Contributors

### Adding a New Note Type

1. Add enum value to `NoteType`.
2. Add a derived model in `Models/Notes/`.
3. Update serialization and deserialization in `Features/Notes/NoteSerializer.cs`.
4. Ensure history capture and restore support in `NoteHistoryEntry`.
5. Add or update tests in `Tests/Features/Notes/`.
6. Update UI handling in the Noted project.

### Adding New Persisted Metadata

1. Extend the relevant model.
2. Update note file serialization in `NoteSerializer` if note metadata is persisted in note files.
3. Update `NoteHistoryEntry` if metadata should round-trip through history.
4. Parse with safe defaults for backward compatibility.
5. Add regression tests for both new and old file layouts.

## Dependencies

- .NET 10.0
- No external runtime dependencies (BCL only)
