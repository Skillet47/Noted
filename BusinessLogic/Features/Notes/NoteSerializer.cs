using BusinessLogic.Models.Notes;
using BusinessLogic.Shared;
using System.Text;

namespace BusinessLogic.Features.Notes;

/// <summary>
/// Handles serialization and deserialization of notes to and from the file-based storage format.
/// </summary>
public static class NoteSerializer
{
    public const string ContentDelimiter = "---CONTENT---";
    public const string OriginalFolderMetadataExtension = ".folder";
    public static readonly Dictionary<string, NoteFormat> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".txt", NoteFormat.PlainText },
        { ".md", NoteFormat.Markdown },
        { ".rtf", NoteFormat.RichText }
    };

    public static string GetFileExtension(NoteFormat format) => format switch
    {
        NoteFormat.PlainText => ".txt",
        NoteFormat.Markdown => ".md",
        NoteFormat.RichText => ".rtf",
        _ => ".txt"
    };

    public static NoteFormat GetFormatFromExtension(string extension)
    {
        return SupportedExtensions.GetValueOrDefault(extension, NoteFormat.PlainText);
    }

    public static async Task<Note?> ReadNoteFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken).ConfigureAwait(false);
            var fileExtension = Path.GetExtension(filePath);
            var formatFromExtension = GetFormatFromExtension(fileExtension);

            var delimiterIndex = Array.IndexOf(lines, ContentDelimiter);

            if (delimiterIndex < 0 || delimiterIndex < 4)
                return null;

            var title = lines[0];
            
            // Parse required date fields with error handling
            if (!DateTime.TryParse(lines[1], out var createdAt))
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Invalid CreatedAt date in note file: {filePath}");
                createdAt = DateTime.Now;
            }
            
            if (!DateTime.TryParse(lines[2], out var modifiedAt))
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Invalid ModifiedAt date in note file: {filePath}");
                modifiedAt = DateTime.Now;
            }

            if (!Enum.TryParse<NoteType>(lines[3], out var noteType))
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Invalid NoteType in note file: {filePath}, defaulting to General");
                noteType = NoteType.General;
            }

            var reminderDateTime = delimiterIndex >= 5 && !string.IsNullOrWhiteSpace(lines[4])
                ? (DateTime.TryParse(lines[4], out var parsedDateTime) ? parsedDateTime : DateTime.MinValue)
                : DateTime.MinValue;

            var recurrence = delimiterIndex >= 6 && Enum.TryParse<RecurrencePattern>(lines[5], out var parsedRecurrence)
                ? parsedRecurrence
                : RecurrencePattern.None;

            var isPinned = delimiterIndex >= 7 && bool.TryParse(lines[6], out var pinned) && pinned;
            var taskStatus = delimiterIndex >= 8 && Enum.TryParse<NoteTaskStatus>(lines[7], out var status)
                ? status
                : NoteTaskStatus.NotStarted;
            var tag = delimiterIndex >= 9 && Enum.TryParse<NoteTag>(lines[8], out var parsedTag)
                ? parsedTag
                : NoteTag.None;

            var format = formatFromExtension;
            var ideaStage = IdeaStage.Seed;

            if (delimiterIndex >= 10)
            {
                if (Enum.TryParse<NoteFormat>(lines[9], out var parsedFormat))
                {
                    format = parsedFormat;

                    if (delimiterIndex >= 11 && Enum.TryParse<IdeaStage>(lines[10], out var parsedStage))
                        ideaStage = parsedStage;
                }
                else
                {
                    if (Enum.TryParse<IdeaStage>(lines[9], out var parsedStage))
                        ideaStage = parsedStage;

                    if (delimiterIndex >= 11 && Enum.TryParse<NoteFormat>(lines[10], out var secondParsedFormat))
                        format = secondParsedFormat;
                }
            }

            var content = delimiterIndex + 1 < lines.Length
                ? string.Join(Environment.NewLine, lines.Skip(delimiterIndex + 1))
                : string.Empty;

            string? originalFolder = null;
            var metadataPath = filePath + OriginalFolderMetadataExtension;
            if (File.Exists(metadataPath))
            {
                try
                {
                    originalFolder = await File.ReadAllTextAsync(metadataPath, cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(originalFolder))
                        originalFolder = null;
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Failed to read metadata file for note: {filePath} - {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: No permission to read metadata file for note: {filePath} - {ex.Message}");
                }
            }


            Note note = noteType switch
            {
                NoteType.General => new GeneralNote
                {
                    Title = title,
                    Content = content,
                    CreatedAt = createdAt,
                    ModifiedAt = modifiedAt,
                    IsPinned = isPinned,
                    Tag = tag,
                    Format = format
                },
                NoteType.Reminder => new ReminderNote
                {
                    Title = title,
                    Content = content,
                    CreatedAt = createdAt,
                    ModifiedAt = modifiedAt,
                    IsPinned = isPinned,
                    Tag = tag,
                    Format = format,
                    ReminderDateTime = reminderDateTime,
                    Recurrence = recurrence
                },
                NoteType.Task => new TaskNote
                {
                    Title = title,
                    Content = content,
                    CreatedAt = createdAt,
                    ModifiedAt = modifiedAt,
                    IsPinned = isPinned,
                    Tag = tag,
                    Format = format,
                    Status = taskStatus
                },
                NoteType.Idea => new IdeaNote
                {
                    Title = title,
                    Content = content,
                    CreatedAt = createdAt,
                    ModifiedAt = modifiedAt,
                    IsPinned = isPinned,
                    Tag = tag,
                    Format = format,
                    Stage = ideaStage
                },
                _ => throw new InvalidOperationException($"Unknown note type: {noteType}")
            };

            note.OriginalFolder = originalFolder;
            return note;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: Failed to read note file: {filePath} - {ex.Message}");
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: No permission to read note file: {filePath} - {ex.Message}");
            return null;
        }
        catch (InvalidOperationException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: Unknown note type in file: {filePath} - {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: Failed to parse note file: {filePath} - {ex.Message}");
            return null;
        }
    }

    public static string BuildNoteFileContent(Note note)
    {
        if (note == null)
            throw new ArgumentNullException(nameof(note));

        if (string.IsNullOrWhiteSpace(note.Title))
            throw new ArgumentException("Note title cannot be empty.", nameof(note));

        try
        {
            var content = new StringBuilder();

            content.AppendLine(note.Title);
            content.AppendLine(note.CreatedAt.ToString("O"));
            content.AppendLine(note.ModifiedAt.ToString("O"));
            content.AppendLine(note.Type.ToString());

            if (note is ReminderNote reminder)
            {
                content.AppendLine(reminder.ReminderDateTime.ToString("O"));
                content.AppendLine(reminder.Recurrence.ToString());
            }
            else
            {
                content.AppendLine(string.Empty);
                content.AppendLine(string.Empty);
            }

            content.AppendLine(note.IsPinned.ToString());
            content.AppendLine(note is TaskNote task ? task.Status.ToString() : string.Empty);
            content.AppendLine(note.Tag.ToString());
            content.AppendLine(note.Format.ToString());
            content.AppendLine(note is IdeaNote idea ? idea.Stage.ToString() : string.Empty);
            content.AppendLine(ContentDelimiter);
            content.Append(note.Content);

            return content.ToString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: Failed to build note file content: {ex.Message}");
            throw;
        }
    }
}
