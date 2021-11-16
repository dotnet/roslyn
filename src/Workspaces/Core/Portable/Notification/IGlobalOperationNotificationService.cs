﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
