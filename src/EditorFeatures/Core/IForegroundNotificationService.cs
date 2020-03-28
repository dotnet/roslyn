// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// provide a way to call APIs from UI thread
    /// </summary>
    internal interface IForegroundNotificationService
    {
        void RegisterNotification(Action action, IAsyncToken asyncToken, CancellationToken cancellationToken = default);

        void RegisterNotification(Action action, int delayInMS, IAsyncToken asyncToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// if action return true, the service will call it back again when it has time.
        /// </summary>
        void RegisterNotification(Func<bool> action, IAsyncToken asyncToken, CancellationToken cancellationToken = default);

        void RegisterNotification(Func<bool> action, int delayInMS, IAsyncToken asyncToken, CancellationToken cancellationToken = default);
    }
}
