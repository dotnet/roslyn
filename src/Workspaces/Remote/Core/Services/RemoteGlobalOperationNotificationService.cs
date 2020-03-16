﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;

namespace Microsoft.CodeAnalysis.Remote.Services
{
    [ExportWorkspaceService(typeof(IGlobalOperationNotificationService), WorkspaceKind.RemoteWorkspace), Shared]
    internal sealed class RemoteGlobalOperationNotificationService : IGlobalOperationNotificationService
    {
        public event EventHandler? Started;
        public event EventHandler<GlobalOperationEventArgs>? Stopped;

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
            => Started?.Invoke(this, EventArgs.Empty);

        public void OnStopped(IReadOnlyList<string> operations, bool cancelled)
            => Stopped?.Invoke(this, new GlobalOperationEventArgs(operations, cancelled));
    }
}
