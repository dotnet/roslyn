// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// a service that lets one to create <see cref="PinnedRemotableDataScope"/> that can be used to pin solution
    /// while working on remote host
    /// </summary>
    internal interface IRemotableDataService : IWorkspaceService
    {
        /// <summary>
        /// Create <see cref="PinnedRemotableDataScope"/> from <see cref="Solution"/>.
        /// </summary>
        ValueTask<PinnedRemotableDataScope> CreatePinnedRemotableDataScopeAsync(Solution solution, CancellationToken cancellationToken);

        /// <summary>
        /// Get <see cref="RemotableData"/> corresponding to given <see cref="Checksum"/>. 
        /// </summary>
        ValueTask<RemotableData?> GetRemotableDataAsync(int scopeId, Checksum checksum, CancellationToken cancellationToken);

        /// <summary>
        /// Get <see cref="RemotableData"/>s corresponding to given <see cref="Checksum"/>s. 
        /// </summary>
        ValueTask<IReadOnlyDictionary<Checksum, RemotableData>> GetRemotableDataAsync(int scopeId, IEnumerable<Checksum> checksums, CancellationToken cancellationToken);
    }
}
