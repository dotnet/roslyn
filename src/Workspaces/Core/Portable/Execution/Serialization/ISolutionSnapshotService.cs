// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// a service that lets one to create <see cref="SolutionSnapshot"/> that can be used to send over to other host
    /// </summary>
    internal interface ISolutionSnapshotService : IWorkspaceService
    {
        /// <summary>
        /// Add global <see cref="Asset"/>  which stays alive while host is alive.
        /// 
        /// this asset can be something that is not part of <see cref="SolutionSnapshot"/> 
        /// </summary>
        void AddGlobalAsset(object value, Asset asset, CancellationToken cancellationToken);

        /// <summary>
        /// Get saved global <see cref="Asset"/> associated with given <paramref name="value"/>
        /// </summary>
        Asset GetGlobalAsset(object value, CancellationToken cancellationToken);

        /// <summary>
        /// Create <see cref="SolutionSnapshot"/> from <see cref="Solution"/>.
        /// </summary>
        Task<SolutionSnapshot> CreateSnapshotAsync(Solution solution, CancellationToken cancellationToken);

        /// <summary>
        /// Get <see cref="ChecksumObject"/> corresponding to given <see cref="Checksum"/>. 
        /// </summary>
        Task<ChecksumObject> GetChecksumObjectAsync(Checksum checksum, CancellationToken cancellationToken);
    }

    /// <summary>
    /// a solution snapshot that one can use to get checksums to send over
    /// </summary>
    internal abstract class SolutionSnapshot : IDisposable
    {
        public readonly Workspace Workspace;
        public readonly SolutionSnapshotId Id;

        protected SolutionSnapshot(Workspace workspace, SolutionSnapshotId id)
        {
            Workspace = workspace;
            Id = id;
        }

        /// <summary>
        /// Add asset that is not part of solution to be part of this snapshot.
        /// 
        /// TODO: currently, this asset must be something <see cref="Serializer"/> can understand
        ///       this should be changed so that custom serializer can be discoverable by <see cref="ChecksumObject.Kind"/> 
        /// </summary>
        public abstract void AddAdditionalAsset(Asset asset, CancellationToken cancellationToken);
        public abstract void Dispose();
    }
}
