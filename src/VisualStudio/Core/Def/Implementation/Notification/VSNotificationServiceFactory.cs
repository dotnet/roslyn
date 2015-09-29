// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Windows.Forms;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Notification
{
    [ExportWorkspaceServiceFactory(typeof(INotificationService), ServiceLayer.Host), Shared]
    internal class VSNotificationServiceFactory : IWorkspaceServiceFactory
    {
        private IVsUIShell _uiShellService;

        private static object s_gate = new object();

        private static VSDialogService s_singleton;

        [ImportingConstructor]
        public VSNotificationServiceFactory(SVsServiceProvider serviceProvider)
        {
            _uiShellService = (IVsUIShell)serviceProvider.GetService(typeof(SVsUIShell));
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            lock (s_gate)
            {
                if (s_singleton == null)
                {
                    s_singleton = new VSDialogService(_uiShellService);
                }
            }

            return s_singleton;
        }

        private class VSDialogService : INotificationService, INotificationServiceCallback
        {
            private IVsUIShell _uiShellService;

            /// <summary>
            /// For testing purposes only.  If non-null, this callback will be invoked instead of showing a dialog.
            /// </summary>
            public Action<string, string, NotificationSeverity> NotificationCallback { get; set; }

            public VSDialogService(IVsUIShell uiShellService)
            {
                _uiShellService = uiShellService;
            }

            public void SendNotification(
                string message,
                string title = null,
                NotificationSeverity severity = NotificationSeverity.Warning)
            {
                if (NotificationCallback != null)
                {
                    // invoke the callback
                    NotificationCallback(message, title, severity);
                }
                else
                {
                    _uiShellService.EnableModeless(0);
                    try
                    {
                        var icon = SeverityToIcon(severity);
                        int dialogResult;
                        _uiShellService.ShowMessageBox(
                            dwCompRole: 0, // unused, as per MSDN documentation
                            rclsidComp: Guid.Empty, // unused
                            pszTitle: null, // use a null title since the title just becomes another line in the regular message
                            pszText: message,
                            pszHelpFile: null,
                            dwHelpContextID: 0, // required to be 0, as per MSDN documentation
                            msgbtn: OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            msgdefbtn: OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                            msgicon: icon,
                            fSysAlert: 0, // Not system modal
                            pnResult: out dialogResult);
                    }
                    finally
                    {
                        // if ShowMessageBox() throws we need to ensure that the UI isn't forever stuck in a modal state
                        _uiShellService.EnableModeless(1);
                    }
                }
            }

            public bool ConfirmMessageBox(string message, string title = null, NotificationSeverity severity = NotificationSeverity.Warning)
            {
                _uiShellService.EnableModeless(0);
                try
                {
                    var icon = SeverityToIcon(severity);
                    int dialogResult;
                    _uiShellService.ShowMessageBox(
                        dwCompRole: 0, // unused, as per MSDN documentation
                        rclsidComp: Guid.Empty, // unused
                        pszTitle: null, // use a null title since the title just becomes another line in the regular message
                        pszText: message,
                        pszHelpFile: null,
                        dwHelpContextID: 0, // required to be 0, as per MSDN documentation
                        msgbtn: OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
                        msgdefbtn: OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                        msgicon: icon,
                        fSysAlert: 0, // Not system modal
                        pnResult: out dialogResult);

                    // The dialogResult is 6 when the Yes button is clicked.
                    return dialogResult == 6;
                }
                finally
                {
                    // if ShowMessageBox() throws we need to ensure that the UI isn't forever stuck in a modal state
                    _uiShellService.EnableModeless(1);
                }
            }

            private static OLEMSGICON SeverityToIcon(NotificationSeverity severity)
            {
                OLEMSGICON result;
                switch (severity)
                {
                    case NotificationSeverity.Information:
                        result = OLEMSGICON.OLEMSGICON_INFO;
                        break;
                    case NotificationSeverity.Warning:
                        result = OLEMSGICON.OLEMSGICON_WARNING;
                        break;
                    default:
                        // Error
                        result = OLEMSGICON.OLEMSGICON_CRITICAL;
                        break;
                }

                return result;
            }
        }
    }
}
