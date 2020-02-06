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
    internal interface IRemoteHostClientService : IWorkspaceService
    {
        bool IsEnabled();

        /// <summary>
        /// Request new remote host. 
        /// 
        /// this is designed to be not disruptive to existing callers and to support scenarios where
        /// features required to reload user extension dlls without re-launching VS.
        /// 
        /// if someone requests new remote host, all new callers for <see cref="TryGetRemoteHostClientAsync(CancellationToken)"/> will
        /// receive a new remote host client that connects to a new remote host.
        /// 
        /// existing remoteHostClient will still remain connected to old host and that old host will eventually go away once all existing clients
        /// are done with their requests.
        /// 
        /// callers can subscribe to <see cref="RemoteHostClient.StatusChanged"/> event to see whether client is going away if
        /// caller is designed to hold onto a service for a while to react to remote host change.
        /// </summary>
        Task RequestNewRemoteHostAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Get <see cref="RemoteHostClient"/> to current RemoteHost
        /// </summary>
        Task<RemoteHostClient> TryGetRemoteHostClientAsync(CancellationToken cancellationToken);
    }
}
