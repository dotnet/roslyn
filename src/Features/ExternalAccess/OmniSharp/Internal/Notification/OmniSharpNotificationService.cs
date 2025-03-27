// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Notification;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Internal.Notification;

[ExportWorkspaceService(typeof(INotificationService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class OmniSharpNotificationService(
    IOmniSharpNotificationService omniSharpNotificationService) : INotificationService
{
    public bool ConfirmMessageBox(string message, string? title = null, NotificationSeverity severity = NotificationSeverity.Warning)
    {
        return omniSharpNotificationService.ConfirmMessageBox(message, title, (OmniSharpNotificationSeverity)severity);
    }

    public void SendNotification(string message, string? title = null, NotificationSeverity severity = NotificationSeverity.Warning)
    {
        omniSharpNotificationService.SendNotification(message, title, (OmniSharpNotificationSeverity)severity);
    }
}
