// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// a service that lets one to create <see cref="ChecksumScope"/> that can be used to pin solution
    /// while working on remote host
    /// </summary>
    internal interface ISolutionChecksumService : IWorkspaceService
    {
        /// <summary>
        /// Add global <see cref="Asset"/> which stays alive while host is alive.
        /// 
        /// this asset can be something that is not part of <see cref="ChecksumScope"/> 
        /// 
        /// TODO: currently, this asset must be something <see cref="Serializer"/> can understand
        ///       this should be changed so that custom serializer can be discoverable by <see cref="ChecksumObject.Kind"/> 
        /// </summary>
        void AddGlobalAsset(object value, Asset asset, CancellationToken cancellationToken);

        /// <summary>
        /// Get saved global <see cref="Asset"/> associated with given <paramref name="value"/>
        /// </summary>
        Asset GetGlobalAsset(object value, CancellationToken cancellationToken);

        /// <summary>
        /// Remove saved global <see cref="Asset"/> associated with given <paramref name="value"/>
        /// </summary>
        void RemoveGlobalAsset(object value, CancellationToken cancellationToken);

        /// <summary>
        /// Create <see cref="ChecksumScope"/> from <see cref="Solution"/>.
        /// </summary>
        Task<ChecksumScope> CreateChecksumAsync(Solution solution, CancellationToken cancellationToken);

        /// <summary>
        /// Get <see cref="ChecksumObject"/> corresponding to given <see cref="Checksum"/>. 
        /// </summary>
        ChecksumObject GetChecksumObject(Checksum checksum, CancellationToken cancellationToken);

        /// <summary>
        /// Get <see cref="ChecksumObject"/>s corresponding to given <see cref="Checksum"/>s. 
        /// </summary>
        IReadOnlyDictionary<Checksum, ChecksumObject> GetChecksumObjects(IEnumerable<Checksum> checksums, CancellationToken cancellationToken);
    }
}
