using System.Collections.Concurrent;

namespace Noted.Services;

/// <summary>
/// Cross-platform implementation of <see cref="INotificationService"/>.
/// Handles notification scheduling using platform-specific APIs via conditional compilation.
/// </summary>
/// <remarks>
/// <para>
/// <b>Platform-Specific Behavior:</b>
/// <list type="bullet">
///     <item><b>macOS:</b> Uses UNUserNotificationCenter for native scheduling</item>
///     <item><b>Windows:</b> Uses timer-based approach with Windows App SDK notifications</item>
/// </list>
/// </para>
/// <para>
/// <b>Limitations:</b>
/// <list type="bullet">
///     <item>Windows: Scheduled notifications use in-process timers (won't fire if app is closed)</item>
///     <item>All platforms: Notifications are stored in-memory and lost on app restart</item>
/// </list>
/// </para>
/// <para>
/// <b>Future Improvements:</b>
/// Consider persisting scheduled notifications to handle app restarts,
/// and using a persistent scheduling mechanism for background scheduling.
/// </para>
/// </remarks>
public class NotificationService : INotificationService
{
    private readonly ConcurrentDictionary<string, int> _scheduledNotifications = new();
    private int _notificationIdCounter = 1;

    /// <inheritdoc/>
    public async Task<bool> RequestPermissionAsync()
    {
#if MACCATALYST
        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.PostNotifications>();
        }
        return status == PermissionStatus.Granted;
#else
        // Windows and other platforms don't require explicit permission
        await Task.CompletedTask;
        return true;
#endif
    }

    /// <inheritdoc/>
    public async Task ScheduleNotificationAsync(string id, string title, string message, DateTime scheduledTime)
    {
        // Don't schedule notifications in the past
        if (scheduledTime <= DateTime.Now)
            return;

        // Cancel any existing notification with the same ID
        await CancelNotificationAsync(id);

        // Generate a unique numeric ID for platform APIs that require it
        var notificationId = _notificationIdCounter++;
        _scheduledNotifications[id] = notificationId;

#if MACCATALYST
        await ScheduleAppleNotificationAsync(id, title, message, scheduledTime);
#elif WINDOWS
        await ScheduleWindowsNotificationAsync(id, title, message, scheduledTime);
#else
        await Task.CompletedTask;
#endif
    }

    /// <inheritdoc/>
    public async Task CancelNotificationAsync(string id)
    {
        if (_scheduledNotifications.TryRemove(id, out var notificationId))
        {
#if MACCATALYST
            CancelAppleNotification(id);
#elif WINDOWS
            CancelWindowsNotification(id);
#endif
        }
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task CancelAllNotificationsAsync()
    {
        foreach (var id in _scheduledNotifications.Keys.ToList())
        {
            await CancelNotificationAsync(id);
        }
    }

#if MACCATALYST
    /// <summary>
    /// Schedules a notification on macOS using UNUserNotificationCenter.
    /// </summary>
    /// <remarks>
    /// Uses time interval trigger calculated from the current time.
    /// Notifications are handled natively by the system.
    /// </remarks>
    private async Task ScheduleAppleNotificationAsync(string id, string title, string message, DateTime scheduledTime)
    {
        var content = new UserNotifications.UNMutableNotificationContent
        {
            Title = title,
            Body = message,
            Sound = UserNotifications.UNNotificationSound.Default
        };

        // Calculate seconds until the scheduled time
        var trigger = UserNotifications.UNTimeIntervalNotificationTrigger.CreateTrigger(
            (scheduledTime - DateTime.Now).TotalSeconds, false);

        var request = UserNotifications.UNNotificationRequest.FromIdentifier(id, content, trigger);

        await UserNotifications.UNUserNotificationCenter.Current.AddNotificationRequestAsync(request);
    }

    private void CancelAppleNotification(string id)
    {
        UserNotifications.UNUserNotificationCenter.Current.RemovePendingNotificationRequests([id]);
    }
#endif

#if WINDOWS
    /// <summary>
    /// Schedules a notification on Windows using Windows App SDK.
    /// </summary>
    /// <remarks>
    /// Uses an in-process timer to show the notification at the scheduled time.
    /// Limitation: Notifications won't fire if the app is closed before the scheduled time.
    /// </remarks>
    private async Task ScheduleWindowsNotificationAsync(string id, string title, string message, DateTime scheduledTime)
    {
        await Task.CompletedTask;
        var notificationManager = Microsoft.Windows.AppNotifications.AppNotificationManager.Default;

        // Build toast notification XML payload
        var xmlPayload = $"""
            <toast>
                <visual>
                    <binding template="ToastGeneric">
                        <text>{System.Security.SecurityElement.Escape(title)}</text>
                        <text>{System.Security.SecurityElement.Escape(message)}</text>
                    </binding>
                </visual>
            </toast>
            """;

        var notification = new Microsoft.Windows.AppNotifications.AppNotification(xmlPayload)
        {
            Tag = id,
            Expiration = scheduledTime.AddMinutes(5)
        };

        // Use timer-based scheduling since Windows App SDK doesn't support
        // direct scheduled notifications for unpackaged apps
        var delay = scheduledTime - DateTime.Now;
        if (delay > TimeSpan.Zero)
        {
            _ = Task.Delay(delay).ContinueWith(_ =>
            {
                // Only show if the notification hasn't been canceled
                if (_scheduledNotifications.ContainsKey(id))
                {
                    notificationManager.Show(notification);
                }
            }, TaskScheduler.Default);
        }
    }

    private void CancelWindowsNotification(string id)
    {
        // Cancellation is handled by checking _scheduledNotifications before showing
        // The ID is already removed from _scheduledNotifications in CancelNotificationAsync
    }
#endif
}
