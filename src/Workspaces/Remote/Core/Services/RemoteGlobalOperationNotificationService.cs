// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;

namespace Microsoft.CodeAnalysis.Remote.Services
{
    [ExportWorkspaceService(typeof(IGlobalOperationNotificationService), WorkspaceKind.RemoteWorkspace), Shared]
    internal class RemoteGlobalOperationNotificationService : IGlobalOperationNotificationService
    {
        public event EventHandler Started;
        public event EventHandler<GlobalOperationEventArgs> Stopped;

        [ImportingConstructor]
        public RemoteGlobalOperationNotificationService()
        {
        }

        public GlobalOperationRegistration Start(string operation)
        {
            // Currently not supported for anything on the remote side to start a global
            // operation.
            throw new NotSupportedException();
        }

        public void OnStarted()
            => this.Started?.Invoke(this, EventArgs.Empty);

        public void OnStopped(IReadOnlyList<string> operations, bool cancelled)
            => this.Stopped?.Invoke(this, new GlobalOperationEventArgs(operations, cancelled));
    }
}
