// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Stores solution snapshots available to remote services.
    /// </summary>
    internal partial class SolutionAssetStorage
    {
        private static int s_scopeId = 1;

        /// <summary>
        /// Map from solution checksum scope id to its associated <see cref="SolutionState"/>.
        /// </summary>
        private readonly ConcurrentDictionary<int, (SolutionState Solution, SolutionReplicationContext ReplicationContext)> _solutionStates = new(concurrencyLevel: 2, capacity: 10);

        public SolutionReplicationContext GetReplicationContext(int scopeId)
            => _solutionStates[scopeId].ReplicationContext;

        /// <summary>
        /// Adds given snapshot into the storage. This snapshot will be available within the returned <see cref="Scope"/>.
        /// </summary>
        internal ValueTask<Scope> StoreAssetsAsync(Solution solution, CancellationToken cancellationToken)
            => StoreAssetsAsync(solution, projectId: null, cancellationToken);

        /// <summary>
        /// Adds given snapshot into the storage. This snapshot will be available within the returned <see cref="Scope"/>.
        /// </summary>
        internal ValueTask<Scope> StoreAssetsAsync(Project project, CancellationToken cancellationToken)
            => StoreAssetsAsync(project.Solution, project.Id, cancellationToken);

        private async ValueTask<Scope> StoreAssetsAsync(Solution solution, ProjectId? projectId, CancellationToken cancellationToken)
        {
            var solutionState = solution.State;
            var solutionChecksum = projectId == null
                ? await solutionState.GetChecksumAsync(cancellationToken).ConfigureAwait(false)
                : await solutionState.GetChecksumAsync(projectId, cancellationToken).ConfigureAwait(false);
            var context = SolutionReplicationContext.Create();

            var id = Interlocked.Increment(ref s_scopeId);
            var solutionInfo = new PinnedSolutionInfo(
                id,
                fromPrimaryBranch: solutionState.BranchId == solutionState.Workspace.PrimaryBranchId,
                solutionState.WorkspaceVersion,
                solutionChecksum,
                projectId);

            Contract.ThrowIfFalse(_solutionStates.TryAdd(id, (solutionState, context)));

            return new Scope(this, solutionInfo);
        }

        /// <summary>
        /// Retrieve asset of a specified <paramref name="checksum"/> available within <paramref name="scopeId"/> scope from the storage.
        /// </summary>
        public async ValueTask<SolutionAsset?> GetAssetAsync(int scopeId, Checksum checksum, CancellationToken cancellationToken)
        {
            if (checksum == Checksum.Null)
            {
                // check nil case
                return SolutionAsset.Null;
            }

            var remotableData = await FindAssetAsync(_solutionStates[scopeId].Solution, checksum, cancellationToken).ConfigureAwait(false);
            if (remotableData != null)
            {
                return remotableData;
            }

            // if it reached here, it means things get cancelled. due to involving 2 processes,
            // current design can make slightly staled requests to running even when things cancelled.
            // if it is other case, remote host side will throw and close connection which will cause
            // vs to crash.
            // this should be changed once I address this design issue
            cancellationToken.ThrowIfCancellationRequested();

            return null;
        }

        /// <summary>
        /// Retrieve assets of specified <paramref name="checksums"/> available within <paramref name="scopeId"/> scope from the storage.
        /// </summary>
        public async ValueTask<IReadOnlyDictionary<Checksum, SolutionAsset>> GetAssetsAsync(int scopeId, IEnumerable<Checksum> checksums, CancellationToken cancellationToken)
        {
            using var checksumsToFind = Creator.CreateChecksumSet(checksums);

            var numberOfChecksumsToSearch = checksumsToFind.Object.Count;
            var result = new Dictionary<Checksum, SolutionAsset>(numberOfChecksumsToSearch);

            if (checksumsToFind.Object.Remove(Checksum.Null))
            {
                result[Checksum.Null] = SolutionAsset.Null;
            }

            await FindAssetsAsync(_solutionStates[scopeId].Solution, checksumsToFind.Object, result, cancellationToken).ConfigureAwait(false);
            if (result.Count == numberOfChecksumsToSearch)
            {
                // no checksum left to find
                Debug.Assert(checksumsToFind.Object.Count == 0);
                return result;
            }

            // if it reached here, it means things get cancelled. due to involving 2 processes,
            // current design can make slightly staled requests to running even when things cancelled.
            // if it is other case, remote host side will throw and close connection which will cause
            // vs to crash.
            // this should be changed once I address this design issue
            cancellationToken.ThrowIfCancellationRequested();

            return result;
        }

        /// <summary>
        /// Find an asset of the specified <paramref name="checksum"/> within <paramref name="solutionState"/>.
        /// </summary>
        private static async ValueTask<SolutionAsset?> FindAssetAsync(SolutionState solutionState, Checksum checksum, CancellationToken cancellationToken)
        {
            using var checksumPool = Creator.CreateChecksumSet(SpecializedCollections.SingletonEnumerable(checksum));
            using var resultPool = Creator.CreateResultSet();

            await FindAssetsAsync(solutionState, checksumPool.Object, resultPool.Object, cancellationToken).ConfigureAwait(false);

            if (resultPool.Object.Count == 1)
            {
                var (resultingChecksum, value) = resultPool.Object.First();
                Contract.ThrowIfFalse(checksum == resultingChecksum);

                return new SolutionAsset(checksum, value);
            }

            return null;
        }

        /// <summary>
        /// Find an assets of the specified <paramref name="remainingChecksumsToFind"/> within <paramref name="solutionState"/>.
        /// Once an asset of given checksum is found the corresponding asset is placed to <paramref name="result"/> and the checksum is removed from <paramref name="remainingChecksumsToFind"/>.
        /// </summary>
        private static async Task FindAssetsAsync(SolutionState solutionState, HashSet<Checksum> remainingChecksumsToFind, Dictionary<Checksum, SolutionAsset> result, CancellationToken cancellationToken)
        {
            using var resultPool = Creator.CreateResultSet();

            await FindAssetsAsync(solutionState, remainingChecksumsToFind, resultPool.Object, cancellationToken).ConfigureAwait(false);

            foreach (var (checksum, value) in resultPool.Object)
            {
                result[checksum] = new SolutionAsset(checksum, value);
            }
        }

        private static async Task FindAssetsAsync(SolutionState solutionState, HashSet<Checksum> remainingChecksumsToFind, Dictionary<Checksum, object> result, CancellationToken cancellationToken)
        {
            if (solutionState.TryGetStateChecksums(out var stateChecksums))
                await stateChecksums.FindAsync(solutionState, remainingChecksumsToFind, result, cancellationToken).ConfigureAwait(false);

            foreach (var projectId in solutionState.ProjectIds)
            {
                if (remainingChecksumsToFind.Count == 0)
                    break;

                if (solutionState.TryGetStateChecksums(projectId, out var checksums))
                    await checksums.FindAsync(solutionState, remainingChecksumsToFind, result, cancellationToken).ConfigureAwait(false);
            }
        }

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor
        {
            private readonly SolutionAssetStorage _solutionAssetStorage;

            internal TestAccessor(SolutionAssetStorage solutionAssetStorage)
            {
                _solutionAssetStorage = solutionAssetStorage;
            }

            public async ValueTask<SolutionAsset?> GetAssetAsync(Checksum checksum, CancellationToken cancellationToken)
            {
                foreach (var (scopeId, _) in _solutionAssetStorage._solutionStates)
                {
                    var data = await _solutionAssetStorage.GetAssetAsync(scopeId, checksum, cancellationToken).ConfigureAwait(false);
                    if (data != null)
                    {
                        return data;
                    }
                }

                return null;
            }
        }
    }
}
