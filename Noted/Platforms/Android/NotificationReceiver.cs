using Android.App;
using Android.Content;
using AndroidX.Core.App;

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
