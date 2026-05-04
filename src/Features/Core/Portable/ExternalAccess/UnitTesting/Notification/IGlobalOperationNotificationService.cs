// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Notification;

/// <summary>
/// Optional interface that can be used to hear about when expensive global operations (like a 'build') occur in the
/// current host.
/// </summary>
internal interface IGlobalOperationNotificationService
{
    /// <summary>
    /// raised when global operation is started
    /// </summary>
    event EventHandler Started;

    /// <summary>
    /// raised when global operation is stopped
    /// </summary>
    event EventHandler Stopped;

    /// <summary>
    /// start new global operation
    /// </summary>
    IDisposable Start(string operation);
}
