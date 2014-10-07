using System;

namespace Roslyn.Services.Notification
{
    internal interface INotificationServiceCallback
    {
        Action<string, string, NotificationSeverity> NotificationCallback { get; set; }
    }
}
