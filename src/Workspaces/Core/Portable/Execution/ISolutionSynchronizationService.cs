// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// a service that lets one to create <see cref="PinnedRemotableDataScope"/> that can be used to pin solution
    /// while working on remote host
    /// </summary>
    internal interface IRemotableDataService : IWorkspaceService
    {
        /// <summary>
        /// Add global <see cref="CustomAsset"/> which stays alive while host is alive.
        /// 
        /// this asset can be something that is not part of <see cref="PinnedRemotableDataScope"/> 
        /// 
        /// TODO: currently, this asset must be something <see cref="ISerializerService"/> can understand
        ///       this should be changed so that custom serializer can be discoverable by <see cref="RemotableData.Kind"/> 
        /// </summary>
        void AddGlobalAsset(object value, CustomAsset asset, CancellationToken cancellationToken);

        /// <summary>
        /// Get saved global <see cref="CustomAsset"/> associated with given <paramref name="value"/>
        /// </summary>
        CustomAsset GetGlobalAsset(object value, CancellationToken cancellationToken);

        /// <summary>
        /// Remove saved global <see cref="CustomAsset"/> associated with given <paramref name="value"/>
        /// </summary>
        void RemoveGlobalAsset(object value, CancellationToken cancellationToken);

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
