namespace Noted.Services;

public interface INotificationService
{
    Task<bool> RequestPermissionAsync();
    Task ScheduleNotificationAsync(string id, string title, string message, DateTime scheduledTime);
    Task CancelNotificationAsync(string id);
    Task CancelAllNotificationsAsync();
}
