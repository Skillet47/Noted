using System.Collections.Concurrent;

namespace Noted.Services
{
    /// <summary>
    /// Simple static service locator for platform code (e.g., NotificationReceiver).
    /// Register your services at app startup.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly ConcurrentDictionary<Type, object> Services = new();

        public static void Register<T>(T service) where T : class
        {
            Services[typeof(T)] = service ?? throw new ArgumentNullException(nameof(service));
        }

        public static T Get<T>() where T : class
        {
            if (Services.TryGetValue(typeof(T), out var service) && service is T typed)
                return typed;
            throw new InvalidOperationException($"Service of type {typeof(T)} is not registered.");
        }
    }
}
