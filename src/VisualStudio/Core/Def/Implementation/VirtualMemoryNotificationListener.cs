﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// Listens to broadcast notifications from the Visual Studio Shell indicating that the application is running
    /// low on available virtual memory.
    /// </summary>
    [Export, Shared]
    internal sealed class VirtualMemoryNotificationListener : ForegroundThreadAffinitizedObject, IVsBroadcastMessageEvents
    {
        private WorkspaceCacheService _workspaceCacheService;
        private bool _alreadyLogged;

        [ImportingConstructor]
        private VirtualMemoryNotificationListener(
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspace workspace) : base(assertIsForeground: true)
        {
            _workspaceCacheService = workspace.Services.GetService<IWorkspaceCacheService>() as WorkspaceCacheService;
            if (_workspaceCacheService == null)
            {
                // No need to hook up the event.
                return;
            }

            var shell = (IVsShell)serviceProvider.GetService(typeof(SVsShell));

            // Note: We never unhook this event sink. It lives for the lifetime of the host.
            uint cookie;
            ErrorHandler.ThrowOnFailure(shell.AdviseBroadcastMessages(this, out cookie));
        }

        /// <summary>
        /// Called by the Visual Studio Shell to notify components of a broadcast message.
        /// </summary>
        /// <param name="msg">The message identifier.</param>
        /// <param name="wParam">First parameter associated with the message.</param>
        /// <param name="lParam">Second parameter associated with the message.</param>
        /// <returns>S_OK always.</returns>
        public int OnBroadcastMessage(uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case VSConstants.VSM_VIRTUALMEMORYLOW:
                case VSConstants.VSM_VIRTUALMEMORYCRITICAL:
                    {
                        if (!_alreadyLogged)
                        {
                            // record that we had hit critical memory barrier
                            Logger.Log(FunctionId.VirtualMemory_MemoryLow, KeyValueLogMessage.Create(m => m["Memory"] = msg));
                            _alreadyLogged = true;
                        }

                        _workspaceCacheService.FlushCaches();
                        break;
                    }
            }

            return VSConstants.S_OK;
        }
    }
}
