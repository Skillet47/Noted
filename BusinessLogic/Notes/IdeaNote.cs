using BusinessLogic.Core;

namespace BusinessLogic.Notes;

public class IdeaNote : Note
{
	public override NoteType Type => NoteType.Idea;
}
