using BusinessLogic.Core;

namespace BusinessLogic.Notes;

public class TaskNote : Note
{
	public override NoteType Type => NoteType.Task;
}
