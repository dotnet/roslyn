// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Windows;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Notification
{
    [ExportWorkspaceServiceFactory(typeof(INotificationService), ServiceLayer.Editor)]
    [Shared]
    internal class EditorNotificationServiceFactory : IWorkspaceServiceFactory
    {
        private static readonly object s_gate = new object();

        private static EditorDialogService s_singleton;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditorNotificationServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            lock (s_gate)
            {
                s_singleton ??= new EditorDialogService();
            }

            return s_singleton;
        }

        private class EditorDialogService : INotificationService, INotificationServiceCallback
        {
            /// <summary>
            /// For testing purposes only.  If non-null, this callback will be invoked instead of showing a dialog.
            /// </summary>
            public Action<string, string, NotificationSeverity> NotificationCallback { get; set; }

            public void SendNotification(
                string message,
                string title = null,
                NotificationSeverity severity = NotificationSeverity.Warning)
            {
                var callback = NotificationCallback;
                if (callback != null)
                {
                    // invoke the callback
                    callback(message, title, severity);
                }
                else
                {
                    var image = SeverityToImage(severity);
                    MessageBox.Show(message, title, MessageBoxButton.OK, image);
                }
            }

            public bool ConfirmMessageBox(
                string message,
                string title = null,
                NotificationSeverity severity = NotificationSeverity.Warning)
            {
                var callback = NotificationCallback;
                if (callback != null)
                {
                    // invoke the callback and assume 'Yes' was clicked.  Since this is a test-only scenario, assuming yes should be fine.
                    callback(message, title, severity);
                    return true;
                }
                else
                {
                    var image = SeverityToImage(severity);
                    return MessageBox.Show(message, title, MessageBoxButton.YesNo, image) == MessageBoxResult.Yes;
                }
            }

            private static MessageBoxImage SeverityToImage(NotificationSeverity severity)
                => severity switch
                {
                    NotificationSeverity.Information => MessageBoxImage.Information,
                    NotificationSeverity.Warning => MessageBoxImage.Warning,
                    _ => MessageBoxImage.Error,
                };
        }
    }
}
