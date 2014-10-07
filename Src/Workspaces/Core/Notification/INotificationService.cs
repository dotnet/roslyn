using Roslyn.Services.WorkspaceServices;

namespace Roslyn.Services.Notification
{
    public interface INotificationService : IWorkspaceService
    {
        void SendNotification(
            string message,
            string title = null,
            NotificationSeverity severity = NotificationSeverity.Warning);
    }
}
