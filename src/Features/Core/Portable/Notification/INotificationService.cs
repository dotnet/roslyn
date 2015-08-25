// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Notification
{
    internal interface INotificationService : IWorkspaceService
    {
        void SendNotification(
            string message,
            string title = null,
            NotificationSeverity severity = NotificationSeverity.Warning);

        bool ConfirmMessageBox(
            string message,
            string title = null,
            NotificationSeverity severity = NotificationSeverity.Warning);
    }
}
