// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Host
{
    internal interface IShutdownCancellationService : IWorkspaceService
    {
        /// <summary>
        /// Token signaled when the host starts to shut down.
        /// </summary>
        CancellationToken ShutdownToken { get; }
    }
}
