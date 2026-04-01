using BusinessLogic.Core.Enums;
using BusinessLogic.Core.Models;

namespace BusinessLogic.Notes;

/// <summary>
/// Represents the development stage of an idea.
/// </summary>
public enum IdeaStage
{
    Seed,
    Exploring,
    Validating,
    ReadyToBuild
}

/// <summary>
/// Represents a simple note for capturing ideas.
/// This is the most basic note type with no additional properties beyond the base <see cref="Note"/> class.
/// </summary>
/// <remarks>
/// Idea notes are useful for quick thoughts, brainstorming, or any content
/// that doesn't require reminders or task tracking.
/// </remarks>
public class IdeaNote : Note
{
    /// <inheritdoc/>
    public override NoteType Type => NoteType.Idea;

    /// <summary>
    /// Tracks where this idea currently sits in the ideation process.
    /// </summary>
    public IdeaStage Stage { get; init; } = IdeaStage.Seed;

    /// <summary>
    /// Returns a focused next-step prompt based on the current idea stage.
    /// </summary>
    public string GetFocusPrompt() => Stage switch
    {
        IdeaStage.Seed => "Describe the core problem this idea solves in one sentence.",
        IdeaStage.Exploring => "List 3 approaches and the trade-off for each.",
        IdeaStage.Validating => "Define the smallest experiment to test this idea.",
        IdeaStage.ReadyToBuild => "Outline the first implementation milestone and acceptance criteria.",
        _ => string.Empty
    };
}
