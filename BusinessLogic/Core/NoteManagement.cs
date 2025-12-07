using BusinessLogic.Notes;
using System.Text;

namespace BusinessLogic.Core;

public class NoteManagement(string folderPath)
{
    private readonly string _folderPath = folderPath;

    public IEnumerable<Note> RetrieveNotes()
    {
        if (!Directory.Exists(_folderPath))
        {
            yield break;
        }

        foreach (var filePath in Directory.EnumerateFiles(_folderPath, "*.txt"))
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length >= 5)
            {
                var title = lines[0];
                var content = lines[1];
                var createdAt = DateTime.Parse(lines[2]);
                var modifiedAt = DateTime.Parse(lines[3]);
                var noteType = Enum.Parse<NoteType>(lines[4]);

                yield return noteType switch
                {
                    NoteType.Reminder => new ReminderNote
                    {
                        Title = title,
                        Content = content,
                        CreatedAt = createdAt,
                        ModifiedAt = modifiedAt,
                        ReminderDateTime = lines.Length >= 6 && !string.IsNullOrWhiteSpace(lines[5])
                            ? DateTime.Parse(lines[5])
                            : DateTime.MinValue
                    },
                    NoteType.Task => new TaskNote
                    {
                        Title = title,
                        Content = content,
                        CreatedAt = createdAt,
                        ModifiedAt = modifiedAt
                    },
                    NoteType.Idea => new IdeaNote
                    {
                        Title = title,
                        Content = content,
                        CreatedAt = createdAt,
                        ModifiedAt = modifiedAt
                    },
                    _ => throw new InvalidOperationException($"Unknown note type: {noteType}")
                };
            }
        }
    }

    public void SaveNote(Note note)
    {
        Directory.CreateDirectory(_folderPath);

        var fileName = $"{SanitizeFileName(note.Title)}_{note.CreatedAt:yyyyMMddHHmmss}.txt";
        var filePath = Path.Combine(_folderPath, fileName);

        var content = new StringBuilder();
        content.AppendLine(note.Title);
        content.AppendLine(note.Content);
        content.AppendLine(note.CreatedAt.ToString("O"));
        content.AppendLine(note.ModifiedAt.ToString("O"));
        content.AppendLine(note.Type.ToString());
        content.AppendLine(note is ReminderNote reminder ? reminder.ReminderDateTime.ToString("O") : string.Empty);

        File.WriteAllText(filePath, content.ToString());
    }

    public bool DeleteNote(string noteTitle)
    {
        if (!Directory.Exists(_folderPath))
        {
            return false;
        }

        foreach (var filePath in Directory.EnumerateFiles(_folderPath, "*.txt"))
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length > 0 && lines[0] == noteTitle)
            {
                File.Delete(filePath);
                return true;
            }
        }

        return false;
    }

    public bool UpdateNote(string originalTitle, Note updatedNote)
    {
        if (!Directory.Exists(_folderPath))
        {
            return false;
        }

        foreach (var filePath in Directory.EnumerateFiles(_folderPath, "*.txt"))
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length > 0 && lines[0] == originalTitle)
            {
                updatedNote.ModifiedAt = DateTime.Now;

                var content = new StringBuilder();
                content.AppendLine(updatedNote.Title);
                content.AppendLine(updatedNote.Content);
                content.AppendLine(updatedNote.CreatedAt.ToString("O"));
                content.AppendLine(updatedNote.ModifiedAt.ToString("O"));
                content.AppendLine(updatedNote.Type.ToString());
                content.AppendLine(updatedNote is ReminderNote reminder ? reminder.ReminderDateTime.ToString("O") : string.Empty);

                File.WriteAllText(filePath, content.ToString());
                return true;
            }
        }

        return false;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder(fileName);
        foreach (var c in invalidChars)
        {
            sanitized.Replace(c, '_');
        }
        return sanitized.ToString();
    }
}
