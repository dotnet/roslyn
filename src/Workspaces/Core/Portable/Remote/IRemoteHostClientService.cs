// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        Task<RemoteHostClient> GetRemoteHostClientAsync(CancellationToken cancellationToken);
    }
}
