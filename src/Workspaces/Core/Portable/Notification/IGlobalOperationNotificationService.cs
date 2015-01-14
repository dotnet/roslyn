// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Notification
{
    internal interface IGlobalOperationNotificationService : IWorkspaceService
    {
        /// <summary>
        /// raised when global operation is started
        /// </summary>
        event EventHandler Started;

        /// <summary>
        /// raised when global operation is stopped
        /// </summary>
        event EventHandler<GlobalOperationEventArgs> Stopped;

        /// <summary>
        /// start new global operation
        /// </summary>
        GlobalOperationRegistration Start(string operation);
    }
}
