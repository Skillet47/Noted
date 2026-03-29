using System.Text;
using BusinessLogic.Core.Enums;
using BusinessLogic.Core.Models;
using BusinessLogic.Notes;

namespace BusinessLogic.Core;

/// <summary>
/// Handles serialization and deserialization of notes to and from the file-based storage format.
/// </summary>
internal static class NoteSerializer
{
    internal const string ContentDelimiter = "---CONTENT---";
    internal const string OriginalFolderMetadataExtension = ".folder";
    internal static readonly Dictionary<string, NoteFormat> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".txt", NoteFormat.PlainText },
        { ".md", NoteFormat.Markdown },
        { ".rtf", NoteFormat.RichText }
    };

    internal static string GetFileExtension(NoteFormat format) => format switch
    {
        NoteFormat.PlainText => ".txt",
        NoteFormat.Markdown => ".md",
        NoteFormat.RichText => ".rtf",
        _ => ".txt"
    };

    internal static NoteFormat GetFormatFromExtension(string extension)
    {
        return SupportedExtensions.GetValueOrDefault(extension, NoteFormat.PlainText);
    }

    internal static async Task<Note?> ReadNoteFromFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken).ConfigureAwait(false);
        var fileExtension = Path.GetExtension(filePath);
        var formatFromExtension = GetFormatFromExtension(fileExtension);

        var delimiterIndex = Array.IndexOf(lines, ContentDelimiter);

        if (delimiterIndex < 0 || delimiterIndex < 4)
            return null;

        var title = lines[0];
        var createdAt = DateTime.Parse(lines[1]);
        var modifiedAt = DateTime.Parse(lines[2]);
        var noteType = Enum.Parse<NoteType>(lines[3]);

        var reminderDateTime = delimiterIndex >= 5 && !string.IsNullOrWhiteSpace(lines[4])
            ? DateTime.Parse(lines[4])
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
        var format = delimiterIndex >= 10 && Enum.TryParse<NoteFormat>(lines[9], out var parsedFormat)
            ? parsedFormat
            : formatFromExtension;
        var content = delimiterIndex + 1 < lines.Length
            ? string.Join(Environment.NewLine, lines.Skip(delimiterIndex + 1))
            : string.Empty;

        string? originalFolder = null;
        var metadataPath = filePath + OriginalFolderMetadataExtension;
        if (File.Exists(metadataPath))
        {
            originalFolder = await File.ReadAllTextAsync(metadataPath, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(originalFolder))
                originalFolder = null;
        }

        Note note = noteType switch
        {
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
                Format = format
            },
            _ => throw new InvalidOperationException($"Unknown note type: {noteType}")
        };

        note.OriginalFolder = originalFolder;
        return note;
    }

    internal static string BuildNoteFileContent(Note note)
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
        content.AppendLine(ContentDelimiter);
        content.Append(note.Content);

        return content.ToString();
    }
}
