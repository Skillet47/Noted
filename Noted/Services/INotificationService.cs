namespace Noted.Services;

/// <summary>
/// Interface for scheduling and managing local notifications.
/// Provides cross-platform notification functionality for reminder notes.
/// </summary>
/// <remarks>
/// <para>
/// Implementations handle platform-specific notification APIs:
/// <list type="bullet">
///     <item><b>Android:</b> Uses AlarmManager and BroadcastReceiver</item>
///     <item><b>iOS/macOS:</b> Uses UserNotifications framework</item>
///     <item><b>Windows:</b> Uses Windows App SDK notifications with timer-based scheduling</item>
/// </list>
/// </para>
/// <para>
/// <b>Usage:</b>
/// 1. Call <see cref="RequestPermissionAsync"/> at app startup to request notification permissions
/// 2. Use <see cref="ScheduleNotificationAsync"/> when creating/updating reminder notes
/// 3. Use <see cref="CancelNotificationAsync"/> when deleting or updating reminders
/// </para>
/// </remarks>
public interface INotificationService
{
    /// <summary>
    /// Requests permission from the user to display notifications.
    /// </summary>
    /// <returns>True if permission was granted; otherwise, false.</returns>
    /// <remarks>
    /// Should be called early in the app lifecycle (e.g., in OnInitializedAsync).
    /// On some platforms (Android 12 and below), this always returns true.
    /// </remarks>
    Task<bool> RequestPermissionAsync();

    /// <summary>
    /// Schedules a notification to be displayed at a specific time.
    /// </summary>
    /// <param name="id">Unique identifier for the notification (used for cancellation).</param>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification body text.</param>
    /// <param name="scheduledTime">When the notification should be displayed.</param>
    /// <remarks>
    /// If a notification with the same ID already exists, it will be replaced.
    /// Notifications scheduled for the past will be ignored.
    /// </remarks>
    Task ScheduleNotificationAsync(string id, string title, string message, DateTime scheduledTime);

    /// <summary>
    /// Cancels a previously scheduled notification.
    /// </summary>
    /// <param name="id">The unique identifier of the notification to cancel.</param>
    Task CancelNotificationAsync(string id);

    /// <summary>
    /// Cancels all scheduled notifications for the application.
    /// </summary>
    Task CancelAllNotificationsAsync();
}
