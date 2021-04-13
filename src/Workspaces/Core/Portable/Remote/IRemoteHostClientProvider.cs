// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Returns a <see cref="RemoteHostClient"/> that a user can use to communicate with a remote host (i.e. ServiceHub) 
    /// </summary>
    internal interface IRemoteHostClientProvider : IWorkspaceService
    {
        /// <summary>
        /// Get <see cref="RemoteHostClient"/> to current RemoteHost
        /// </summary>
        Task<RemoteHostClient?> TryGetRemoteHostClientAsync(CancellationToken cancellationToken);
    }
}
