using Android.App;
using Android.Content;
using AndroidX.Core.App;
using BusinessLogic.Notes;
using BusinessLogic.Core;
using Noted.Services;
using System;
using System.Linq;

namespace Noted.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class NotificationReceiver : BroadcastReceiver
{
    public const string ChannelId = "noted_reminders";
    public const string ChannelName = "Note Reminders";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null)
            return;

        var notificationId = intent.GetIntExtra("notificationId", 0);
        var title = intent.GetStringExtra("title") ?? "Reminder";
        var message = intent.GetStringExtra("message") ?? "";

        CreateNotificationChannel(context);
        ShowNotification(context, notificationId, title, message);

        // --- Recurring Reminder Logic ---
        try
        {
            // Access the note storage and notification service
            // (Assume a singleton or static accessor for these services)
            var noteManager = ServiceLocator.Get<NoteManagement>();
            var notificationService = ServiceLocator.Get<INotificationService>();
            var storageService = ServiceLocator.Get<StorageService>();

            // Find the reminder note by title in the current folder
            var notes = noteManager.RetrieveNotes(storageService.CurrentFolder);
            var reminderNote = notes.OfType<ReminderNote>().FirstOrDefault(n => n.Title == title);
            if (reminderNote != null && reminderNote.Recurrence != RecurrencePattern.None)
            {
                var next = reminderNote.GetNextOccurrence();
                if (next.HasValue && next.Value > DateTime.Now)
                {
                    reminderNote.ReminderDateTime = next.Value;
                    noteManager.UpdateNote(reminderNote.Title, reminderNote, storageService.CurrentFolder);
                    // Reschedule the next notification
                    notificationService.ScheduleNotificationAsync(
                        $"reminder_{reminderNote.Title}",
                        $"Reminder: {reminderNote.Title}",
                        reminderNote.Content,
                        next.Value
                    ).Wait();
                }
            }
        }
        catch { /* Swallow errors to avoid crashing the receiver */ }
    }

    private static void CreateNotificationChannel(Context context)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.High)
            {
                Description = "Notifications for note reminders"
            };
            channel.EnableVibration(true);
            channel.EnableLights(true);

            var notificationManager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
            notificationManager?.CreateNotificationChannel(channel);
        }
    }

    private static void ShowNotification(Context context, int notificationId, string title, string message)
    {
        var intent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName ?? "");
        var pendingIntent = PendingIntent.GetActivity(
            context,
            0,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var builder = new NotificationCompat.Builder(context, ChannelId)
            .SetContentTitle(title)
            .SetContentText(message)
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetAutoCancel(true)
            .SetContentIntent(pendingIntent)
            .SetPriority(NotificationCompat.PriorityHigh);

        var notificationManager = NotificationManagerCompat.From(context);
        notificationManager.Notify(notificationId, builder.Build());
    }
}
