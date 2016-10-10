// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Create new <see cref="RemoteHostClient"/>.
    /// 
    /// <see cref="IRemoteHostClientFactory"/> will use this to create new <see cref="RemoteHostClient"/> 
    /// </summary>
    internal interface IRemoteHostClientFactory : IWorkspaceService
    {
        Task<RemoteHostClient> CreateAsync(Workspace workspace, CancellationToken cancellationToken);
    }
}