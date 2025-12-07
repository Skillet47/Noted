using BusinessLogic.Core;

namespace BusinessLogic.Notes;

public class ReminderNote : Note
{
	public override NoteType Type => NoteType.Reminder;
	public required DateTime ReminderDateTime { get; set; }
}
