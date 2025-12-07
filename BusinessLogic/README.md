# BusinessLogic Project

This project contains the core business logic for the Noted application. It is a standalone .NET 10 class library that can be referenced by the UI project.

## Project Structure

```
BusinessLogic/
??? Core/                    # Core domain models and services
?   ??? Note.cs             # Abstract base class for all note types
?   ??? NoteType.cs         # Enum defining note categories (Reminder, Task, Idea)
?   ??? NoteTag.cs          # Enum for color-coded tags
?   ??? TaskStatus.cs       # Enum for task completion states
?   ??? NoteManagement.cs   # File-based CRUD operations for notes
??? Notes/                   # Concrete note type implementations
?   ??? ReminderNote.cs     # Note with reminder date/time
?   ??? TaskNote.cs         # Note with completion status
?   ??? IdeaNote.cs         # Simple note without extra properties
??? BusinessLogic.csproj    # Project file
```

## Key Components

### Note (Abstract Base Class)
The `Note` class defines the common properties shared by all note types:
- **Title**: The note heading (used as unique identifier for file operations)
- **Content**: The main body text
- **CreatedAt / ModifiedAt**: Timestamps for tracking changes
- **IsPinned**: Whether the note appears at the top of the list
- **Tag**: Color-coded categorization (None, Red, Orange, Yellow, Green, Blue, Purple)
- **Type**: Abstract property overridden by subclasses

### Note Types
| Type | Description | Additional Properties |
|------|-------------|----------------------|
| `IdeaNote` | Simple note for capturing thoughts | None |
| `ReminderNote` | Note with scheduled reminder | `ReminderDateTime` |
| `TaskNote` | Note with progress tracking | `Status` (NotStarted, InProgress, Completed) |

### NoteManagement
Handles file-based storage of notes. Each note is saved as a `.txt` file with the following format:

```
Line 0: Title
Line 1: Content
Line 2: CreatedAt (ISO 8601)
Line 3: ModifiedAt (ISO 8601)
Line 4: NoteType
Line 5: ReminderDateTime (for Reminder notes, empty otherwise)
Line 6: IsPinned
Line 7: TaskStatus (for Task notes, empty otherwise)
Line 8: Tag
```

**File Naming**: `{SanitizedTitle}_{CreatedAtTimestamp}.txt`

## Adding a New Note Type

1. **Add enum value** to `NoteType.cs`:
   ```csharp
   public enum NoteType
   {
       Reminder,
       Task,
       Idea,
       YourNewType  // Add here
   }
   ```

2. **Create the class** in the `Notes/` folder:
   ```csharp
   public class YourNewNote : Note
   {
       public override NoteType Type => NoteType.YourNewType;
       
       // Add type-specific properties
       public string CustomProperty { get; set; }
   }
   ```

3. **Update NoteManagement.cs**:
   - Add a case in `RetrieveNotes()` to deserialize the new type
   - Update `SaveNote()` and `UpdateNote()` to serialize any new properties

4. **Update the UI** (in Noted project):
   - Update `Notes.razor` to display type-specific fields
   - Update `NoteEditModal.razor` to allow editing type-specific properties

## Adding New Properties to Existing Notes

When adding new properties:
1. Add the property to the appropriate class
2. Add a new line in the file format (append to end for backward compatibility)
3. Update `RetrieveNotes()` to parse the new line with a fallback default
4. Update `SaveNote()` and `UpdateNote()` to write the new property

## Dependencies

- .NET 10.0
- No external NuGet packages (uses only BCL)

## Usage

```csharp
var noteManager = new NoteManagement("/path/to/notes/folder");

// Create a note
var note = new IdeaNote 
{
    Title = "My Idea",
    Content = "Something brilliant",
    CreatedAt = DateTime.Now,
    ModifiedAt = DateTime.Now
};
noteManager.SaveNote(note);

// Retrieve all notes
var notes = noteManager.RetrieveNotes().ToList();

// Update a note
note.Content = "Updated content";
noteManager.UpdateNote("My Idea", note);

// Delete a note
noteManager.DeleteNote("My Idea");
```
