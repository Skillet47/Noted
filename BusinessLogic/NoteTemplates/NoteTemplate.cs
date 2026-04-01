using BusinessLogic.Core.Enums;
using BusinessLogic.Notes;

namespace BusinessLogic.NoteTemplates;

public sealed class NoteTemplate
{
    public required string Name { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public NoteType NoteType { get; init; } = NoteType.General;
    public NoteTag Tag { get; init; } = NoteTag.None;
    public NoteFormat Format { get; init; } = NoteFormat.PlainText;
    public RecurrencePattern Recurrence { get; init; } = RecurrencePattern.None;
    public NoteTaskStatus TaskStatus { get; init; } = NoteTaskStatus.NotStarted;
    public IdeaStage IdeaStage { get; init; } = IdeaStage.Seed;
}
