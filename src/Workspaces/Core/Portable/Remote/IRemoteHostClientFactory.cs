// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
        Task<RemoteHostClient?> CreateAsync(Workspace workspace, CancellationToken cancellationToken);
    }
}
