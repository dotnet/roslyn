// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Notification;

internal interface INotificationService : IWorkspaceService
{
    /// <summary>
    /// Displays a message box with an OK button to the user.
    /// </summary>
    /// <param name="message">The message shown within the message box.</param>
    /// <param name="title">The title bar to be shown in the message box. May be ignored by some implementations.</param>
    /// <param name="severity">The severity of the message.</param>
    void SendNotification(
        string message,
        string title = null,
        NotificationSeverity severity = NotificationSeverity.Warning);

    /// <summary>
    /// Displays a message box with a yes/no question to the user.
    /// </summary>
    /// <param name="message">The message shown within the message box.</param>
    /// <param name="title">The title bar to be shown in the message box. May be ignored by some implementations.</param>
    /// <param name="severity">The severity of the message.</param>
    /// <returns>true if yes was clicked, false otherwise.</returns>
    bool ConfirmMessageBox(
        string message,
        string title = null,
        NotificationSeverity severity = NotificationSeverity.Warning);
}
