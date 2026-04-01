# BusinessLogic Project

BusinessLogic is the core domain and persistence layer for Noted. It is a .NET 10 class library responsible for:

- Note models and note-type specific behavior
- File-based persistence for notes
- Folder organization
- Trash and restore workflows
- Operation result handling for business operations

## Current Structure

```
BusinessLogic/
   Core/
      INoteManagement.cs
      NoteManagement.cs
      NoteManagement.Folders.cs
      NoteManagement.Trash.cs
      NoteSerializer.cs
      Enums/
         NoteFormat.cs
         NoteTag.cs
         NoteType.cs
         TaskStatus.cs
      Models/
         Note.cs
      Records/
         OperationResult.cs
   Notes/
      IdeaNote.cs
      ReminderNote.cs
      TaskNote.cs
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
- `Tag` (`NoteTag`)
- `Format` (`NoteFormat`: `PlainText`, `Markdown`, `RichText`)
- `OriginalFolder` (used for trash/restore metadata)
- `Type` (abstract `NoteType` implemented by derived types)

### Note Types

- `GeneralNote`: basic note with no specialized metadata
- `IdeaNote`: ideation note with `IdeaStage` and stage-based guidance via `GetFocusPrompt()`
- `ReminderNote`: includes `ReminderDateTime` and `Recurrence` (`None`, `Daily`, `Weekly`, `Monthly`)
- `TaskNote`: includes `Status` (`NoteTaskStatus`: `NotStarted`, `InProgress`, `Completed`)

### Operations Contract

`INoteManagement` exposes asynchronous operations for:

- Retrieving notes from root or subfolders
- Saving notes to root or subfolders
- Updating notes
- Deleting notes
- Moving notes to trash
- Restoring notes from trash
- Permanently deleting notes from trash
- Creating/deleting folders and listing subfolders

Most write operations return `OperationResult`.

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

### Trash Metadata

When moving a note to trash, a companion metadata file is created:

- `{noteFileName}.folder`

This stores the original subfolder path (or empty for root) so restores can return notes to the correct location.

## Usage Example

```csharp
using BusinessLogic.Core;
using BusinessLogic.Core.Enums;
using BusinessLogic.Notes;

var manager = new NoteManagement("/path/to/notes");

var note = new ReminderNote
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

var saveResult = await manager.SaveNoteAsync(note);
if (!saveResult)
{
      Console.WriteLine(saveResult.ErrorMessage);
      return;
}

var notes = await manager.RetrieveNotesAsync();

var updated = new ReminderNote
{
      Title = note.Title,
      Content = note.Content + Environment.NewLine + "Set autopay reminder.",
      CreatedAt = note.CreatedAt,
      ModifiedAt = note.ModifiedAt,
      ReminderDateTime = note.ReminderDateTime,
      Recurrence = note.Recurrence,
      Format = note.Format,
      Tag = note.Tag,
      IsPinned = note.IsPinned
};

await manager.UpdateNoteAsync("Pay rent", updated);
await manager.MoveNoteToTrashAsync("Pay rent", subfolderName: null);
await manager.RestoreNoteFromTrashAsync("Pay rent");
```

## Notes for Contributors

### Adding a New Note Type

1. Add enum value to `NoteType`.
2. Add a derived model in `Notes/`.
3. Update deserialization/serialization in `NoteSerializer`.
4. Add or update tests in the `Tests` project.
5. Update UI handling in the Noted project.

### Adding New Persisted Metadata

1. Extend the note model.
2. Add a new serialized line before the `---CONTENT---` delimiter.
3. Parse with safe defaults for backward compatibility.
4. Add regression tests for both new and old file layouts.

## Dependencies

- .NET 10.0
- No external runtime dependencies (BCL only)
