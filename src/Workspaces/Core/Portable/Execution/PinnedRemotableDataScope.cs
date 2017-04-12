// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// checksum scope that one can use to pin assets in memory while working on remote host
    /// </summary>
    internal class PinnedRemotableDataScope : IDisposable
    {
        private readonly AssetStorages _storages;
        private readonly AssetStorages.Storage _storage;

        public readonly Checksum SolutionChecksum;

        public PinnedRemotableDataScope(
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
        ///       this should be changed so that custom serializer can be discoverable by <see cref="RemotableData.Kind"/> 
        /// </summary>
        public void AddAdditionalAsset(CustomAsset asset, CancellationToken cancellationToken)
        {
            _storage.AddAdditionalAsset(asset, cancellationToken);
        }

        public RemotableData GetRemotableData(Checksum checksum, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.PinnedRemotableDataScope_GetRemotableData, Checksum.GetChecksumLogInfo, checksum, cancellationToken))
            {
                return _storages.GetRemotableData(this, checksum, cancellationToken);
            }
        }

        public IReadOnlyDictionary<Checksum, RemotableData> GetRemotableData(IEnumerable<Checksum> checksums, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.PinnedRemotableDataScope_GetRemotableData, Checksum.GetChecksumsLogInfo, checksums, cancellationToken))
            {
                return _storages.GetRemotableData(this, checksums, cancellationToken);
            }
        }

        public void Dispose()
        {
            _storages.UnregisterSnapshot(this);
            GC.SuppressFinalize(this);
        }

        ~PinnedRemotableDataScope()
        {
            if (!Environment.HasShutdownStarted)
            {
                Contract.Fail($@"Should have been disposed!");
            }
        }
    }
}
