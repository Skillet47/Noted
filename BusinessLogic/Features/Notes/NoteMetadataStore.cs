using Microsoft.Data.Sqlite;

namespace BusinessLogic.Features.Notes;

internal sealed class NoteMetadata
{
    public required string RelativePath { get; init; }
    public required string Title { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime ModifiedAt { get; init; }
    public required BusinessLogic.Shared.NoteType Type { get; init; }
    public bool IsPinned { get; init; }
    public BusinessLogic.Shared.NoteTag Tag { get; init; }
    public BusinessLogic.Shared.NoteFormat Format { get; init; }
    public DateTime ReminderDateTime { get; init; }
    public BusinessLogic.Models.Notes.RecurrencePattern Recurrence { get; init; }
    public BusinessLogic.Shared.NoteTaskStatus TaskStatus { get; init; }
    public BusinessLogic.Models.Notes.IdeaStage IdeaStage { get; init; }
    public string? OriginalFolder { get; init; }
}

internal sealed class NoteMetadataStore
{
    private const string DatabaseDirectory = ".noted";
    private const string DatabaseFileName = "notes.db";
    private readonly string _rootPath;

    public NoteMetadataStore(string rootPath) => _rootPath = rootPath;

    private string DatabasePath => Path.Combine(_rootPath, DatabaseDirectory, DatabaseFileName);

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.Combine(_rootPath, DatabaseDirectory));
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Notes (
                RelativePath TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                ModifiedAt TEXT NOT NULL,
                Type INTEGER NOT NULL,
                IsPinned INTEGER NOT NULL,
                Tag INTEGER NOT NULL,
                Format INTEGER NOT NULL,
                ReminderDateTime TEXT NOT NULL,
                Recurrence INTEGER NOT NULL,
                TaskStatus INTEGER NOT NULL,
                IdeaStage INTEGER NOT NULL,
                OriginalFolder TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Notes_Title ON Notes (Title);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<NoteMetadata?> GetAsync(string relativePath, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Notes WHERE RelativePath = $path";
        command.Parameters.AddWithValue("$path", relativePath);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? Read(reader) : null;
    }

    public async Task<NoteMetadata?> FindByTitleAsync(string relativeFolder, string title, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Notes WHERE Title = $title AND (RelativePath = $root OR RelativePath LIKE $prefix) LIMIT 1";
        command.Parameters.AddWithValue("$title", title);
        command.Parameters.AddWithValue("$root", string.IsNullOrEmpty(relativeFolder) ? "" : relativeFolder);
        command.Parameters.AddWithValue("$prefix", string.IsNullOrEmpty(relativeFolder) ? "%" : relativeFolder.TrimEnd('/') + "/%");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? Read(reader) : null;
    }

    public async Task<IReadOnlyList<NoteMetadata>> ListAsync(string relativeFolder, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Notes WHERE RelativePath = $root OR RelativePath LIKE $prefix";
        command.Parameters.AddWithValue("$root", string.IsNullOrEmpty(relativeFolder) ? "" : relativeFolder);
        command.Parameters.AddWithValue("$prefix", string.IsNullOrEmpty(relativeFolder) ? "%" : relativeFolder.TrimEnd('/') + "/%");
        var results = new List<NoteMetadata>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            results.Add(Read(reader));
        return results;
    }

    public async Task UpsertAsync(NoteMetadata metadata, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Notes VALUES ($path, $title, $created, $modified, $type, $pinned, $tag, $format, $reminder, $recurrence, $status, $stage, $folder)
            ON CONFLICT(RelativePath) DO UPDATE SET
                Title = excluded.Title, CreatedAt = excluded.CreatedAt, ModifiedAt = excluded.ModifiedAt,
                Type = excluded.Type, IsPinned = excluded.IsPinned, Tag = excluded.Tag, Format = excluded.Format,
                ReminderDateTime = excluded.ReminderDateTime, Recurrence = excluded.Recurrence,
                TaskStatus = excluded.TaskStatus, IdeaStage = excluded.IdeaStage, OriginalFolder = excluded.OriginalFolder;
            """;
        command.Parameters.AddWithValue("$path", metadata.RelativePath);
        command.Parameters.AddWithValue("$title", metadata.Title);
        command.Parameters.AddWithValue("$created", metadata.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$modified", metadata.ModifiedAt.ToString("O"));
        command.Parameters.AddWithValue("$type", (int)metadata.Type);
        command.Parameters.AddWithValue("$pinned", metadata.IsPinned ? 1 : 0);
        command.Parameters.AddWithValue("$tag", (int)metadata.Tag);
        command.Parameters.AddWithValue("$format", (int)metadata.Format);
        command.Parameters.AddWithValue("$reminder", metadata.ReminderDateTime.ToString("O"));
        command.Parameters.AddWithValue("$recurrence", (int)metadata.Recurrence);
        command.Parameters.AddWithValue("$status", (int)metadata.TaskStatus);
        command.Parameters.AddWithValue("$stage", (int)metadata.IdeaStage);
        command.Parameters.AddWithValue("$folder", (object?)metadata.OriginalFolder ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string relativePath, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Notes WHERE RelativePath = $path";
        command.Parameters.AddWithValue("$path", relativePath);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MoveAsync(string oldRelativePath, string newRelativePath, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Notes SET RelativePath = $newPath WHERE RelativePath = $oldPath";
        command.Parameters.AddWithValue("$oldPath", oldRelativePath);
        command.Parameters.AddWithValue("$newPath", newRelativePath);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateOriginalFolderAsync(string relativePath, string? originalFolder, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Notes SET OriginalFolder = $folder WHERE RelativePath = $path";
        command.Parameters.AddWithValue("$path", relativePath);
        command.Parameters.AddWithValue("$folder", (object?)originalFolder ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private static NoteMetadata Read(SqliteDataReader reader) => new()
    {
        RelativePath = reader.GetString(0),
        Title = reader.GetString(1),
        CreatedAt = DateTime.Parse(reader.GetString(2)),
        ModifiedAt = DateTime.Parse(reader.GetString(3)),
        Type = (BusinessLogic.Shared.NoteType)reader.GetInt32(4),
        IsPinned = reader.GetInt32(5) != 0,
        Tag = (BusinessLogic.Shared.NoteTag)reader.GetInt32(6),
        Format = (BusinessLogic.Shared.NoteFormat)reader.GetInt32(7),
        ReminderDateTime = DateTime.Parse(reader.GetString(8)),
        Recurrence = (BusinessLogic.Models.Notes.RecurrencePattern)reader.GetInt32(9),
        TaskStatus = (BusinessLogic.Shared.NoteTaskStatus)reader.GetInt32(10),
        IdeaStage = (BusinessLogic.Models.Notes.IdeaStage)reader.GetInt32(11),
        OriginalFolder = reader.IsDBNull(12) ? null : reader.GetString(12)
    };
}