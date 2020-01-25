﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// Information related to pinned solution
    /// </summary>
    internal sealed class PinnedSolutionInfo
    {
        /// <summary>
        /// Unique ID for this pinned solution
        /// 
        /// This later used to find matching solution between VS and remote host
        /// </summary>
        public readonly int ScopeId;

        /// <summary>
        /// This indicates whether this scope is for primary branch or not (not forked solution)
        /// 
        /// Features like OOP will use this flag to see whether caching information related to this solution
        /// can benefit other requests or not
        /// </summary>
        public readonly bool FromPrimaryBranch;

        /// <summary>
        /// This indicates a Solution.WorkspaceVersion of this solution. remote host engine uses this version
        /// to decide whether caching this solution will benefit other requests or not
        /// </summary>
        public readonly int WorkspaceVersion;

        public readonly Checksum SolutionChecksum;

        public PinnedSolutionInfo(int scopeId, bool fromPrimaryBranch, int workspaceVersion, Checksum solutionChecksum)
        {
            ScopeId = scopeId;
            FromPrimaryBranch = fromPrimaryBranch;
            WorkspaceVersion = workspaceVersion;
            SolutionChecksum = solutionChecksum;
        }
    }

    /// <summary>
    /// checksum scope that one can use to pin assets in memory while working on remote host
    /// </summary>
    internal sealed class PinnedRemotableDataScope : IDisposable
    {
        private static int s_scopeId = 1;

        private readonly AssetStorages _storages;
        private readonly AssetStorages.Storage _storage;
        private bool _disposed;

        public readonly PinnedSolutionInfo SolutionInfo;

        private PinnedRemotableDataScope(
            AssetStorages storages,
            AssetStorages.Storage storage,
            PinnedSolutionInfo solutionInfo)
        {
            _storages = storages;
            _storage = storage;
            SolutionInfo = solutionInfo;
        }

        public Workspace Workspace => _storage.SolutionState.Workspace;
        public Checksum SolutionChecksum => SolutionInfo.SolutionChecksum;

        public static PinnedRemotableDataScope Create(
            AssetStorages storages,
            AssetStorages.Storage storage,
            Checksum solutionChecksum)
        {
            Contract.ThrowIfNull(solutionChecksum);

            var solutionInfo = new PinnedSolutionInfo(
                Interlocked.Increment(ref s_scopeId),
                storage.SolutionState.BranchId == storage.SolutionState.Workspace.PrimaryBranchId,
                storage.SolutionState.WorkspaceVersion,
                solutionChecksum);

            storages.RegisterSnapshot(solutionInfo.ScopeId, storage);

            return new PinnedRemotableDataScope(storages, storage, solutionInfo);
        }

        /// <summary>
        /// Add asset that is not part of solution to be part of this snapshot.
        /// 
        /// TODO: currently, this asset must be something <see cref="ISerializerService"/> can understand
        ///       this should be changed so that custom serializer can be discoverable by <see cref="RemotableData.Kind"/> 
        /// </summary>
        public void AddAdditionalAsset(CustomAsset asset)
        {
            _storage.AddAdditionalAsset(asset);
        }

        public async ValueTask<RemotableData?> GetRemotableDataAsync(Checksum checksum, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.PinnedRemotableDataScope_GetRemotableData, Checksum.GetChecksumLogInfo, checksum, cancellationToken))
            {
                return await _storages.GetRemotableDataAsync(SolutionInfo.ScopeId, checksum, cancellationToken).ConfigureAwait(false);
            }
        }

        public async ValueTask<IReadOnlyDictionary<Checksum, RemotableData>> GetRemotableDataAsync(IEnumerable<Checksum> checksums, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.PinnedRemotableDataScope_GetRemotableData, Checksum.GetChecksumsLogInfo, checksums, cancellationToken))
            {
                return await _storages.GetRemotableDataAsync(SolutionInfo.ScopeId, checksums, cancellationToken).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _storages.UnregisterSnapshot(SolutionInfo.ScopeId);
            }

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
