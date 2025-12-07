namespace BusinessLogic.Core;

public abstract class Note
{
	public required string Title { get; set; }
	public required string Content { get; set; }
	public required DateTime CreatedAt { get; set; }
	public required DateTime ModifiedAt { get; set; }
	public bool IsPinned { get; set; }
	public NoteTag Tag { get; set; } = NoteTag.None;
	public abstract NoteType Type { get; }
}
