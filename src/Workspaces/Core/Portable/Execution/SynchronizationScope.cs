// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// checksum scope that one can use to pin assets in memory while working on remote host
    /// </summary>
    internal class SynchronizationScope : IDisposable
    {
        private readonly AssetStorages _storages;
        private readonly AssetStorages.Storage _storage;

        public readonly Checksum SolutionChecksum;

        public SynchronizationScope(
            AssetStorages storages,
            AssetStorages.Storage storage,
            Checksum solutionChecksum)
        {
            _storages = storages;
            _storage = storage;

            SolutionChecksum = solutionChecksum;

            _storages.RegisterSnapshot(this, storage);
        }

        public Workspace Workspace => _storage.SolutionState.Workspace;

        /// <summary>
        /// Add asset that is not part of solution to be part of this snapshot.
        /// 
        /// TODO: currently, this asset must be something <see cref="Serializer"/> can understand
        ///       this should be changed so that custom serializer can be discoverable by <see cref="SynchronizationObject.Kind"/> 
        /// </summary>
        public void AddAdditionalAsset(CustomAsset asset, CancellationToken cancellationToken)
        {
            _storage.AddAdditionalAsset(asset, cancellationToken);
        }

        public void Dispose()
        {
            _storages.UnregisterSnapshot(this);
        }
    }
}
