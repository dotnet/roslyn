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
        /// Lock over mutable state in this type.  Note: We could consider making this a SemaphoreSlim if the locking
        /// proves to be a problem. However, it would greatly complicate the implementation and consumption side due to
        /// the pattern around <c>await using</c> as well as <see cref="ReferenceCountedDisposable{T}"/> not supporting
        /// <see cref="IAsyncDisposable"/>.
        /// </summary>
        private readonly object _gate = new();

        /// <summary>
        /// Mapping from operation checksum to the scope for the syncing operation that we've created for it.
        /// Ref-counted so that if we have many concurrent calls going out from the host to the OOP side that we share
        /// the same storage here so that all OOP calls can safely call back into us and get the assets they need, even
        /// if individual calls get canceled.
        /// </summary>
        private readonly Dictionary<Checksum, ReferenceCountedDisposable<Scope>> _checksumToScope = new();

        /// <summary>
        /// Map from solution checksum scope id to its associated <see cref="SolutionState"/>.
        /// </summary>
        private readonly ConcurrentDictionary<int, (SolutionState Solution, SolutionReplicationContext ReplicationContext)> _solutionStates = new(concurrencyLevel: 2, capacity: 10);

        public SolutionReplicationContext GetReplicationContext(int scopeId)
            => _solutionStates[scopeId].ReplicationContext;

        /// <summary>
        /// Adds given snapshot into the storage. This snapshot will be available within the returned <see cref="Scope"/>.
        /// </summary>
        internal ValueTask<ReferenceCountedDisposable<Scope>> StoreAssetsAsync(Solution solution, CancellationToken cancellationToken)
            => StoreAssetsAsync(solution, projectId: null, cancellationToken);

        /// <summary>
        /// Adds given snapshot into the storage. This snapshot will be available within the returned <see cref="Scope"/>.
        /// </summary>
        internal ValueTask<ReferenceCountedDisposable<Scope>> StoreAssetsAsync(Project project, CancellationToken cancellationToken)
            => StoreAssetsAsync(project.Solution, project.Id, cancellationToken);

        private async ValueTask<ReferenceCountedDisposable<Scope>> StoreAssetsAsync(Solution solution, ProjectId? projectId, CancellationToken cancellationToken)
        {
            var solutionState = solution.State;
            var checksum = projectId == null
                ? await solutionState.GetChecksumAsync(cancellationToken).ConfigureAwait(false)
                : await solutionState.GetChecksumAsync(projectId, cancellationToken).ConfigureAwait(false);

            lock (_gate)
            {
                if (_checksumToScope.TryGetValue(checksum, out var refCountedScope))
                {
                    // Found a matching scope for this checksum.  See if we can up the refcount on it (i.e. it didn't
                    // concurrently drop to 0 just before this on another thread.  If so, we're all good and the scope
                    // can be shared.
                    var result = refCountedScope.TryAddReference();
                    if (result != null)
                        return result;

                    // Otherwise scope's refcount has dropped to zero externally, but we still got a concurrent call to
                    // do an operation with the same checksum.  We have to recreate a scope at this point.  Explicitly
                    // remove teh mapping here as we're going to update it below.  Note: when the scope itself calls
                    // back into us to clean it will check to ensure that the mapping still points to it, so there's no
                    // risk of both paths racing with each other.
                    Contract.ThrowIfFalse(_checksumToScope.Remove(checksum));
                }

                var id = Interlocked.Increment(ref s_scopeId);
                var solutionInfo = new PinnedSolutionInfo(
                    id,
                    fromPrimaryBranch: solutionState.BranchId == solutionState.Workspace.PrimaryBranchId,
                    solutionState.WorkspaceVersion,
                    checksum);

                Contract.ThrowIfFalse(_solutionStates.TryAdd(id, (solutionState, SolutionReplicationContext.Create())));

                refCountedScope = new ReferenceCountedDisposable<Scope>(new Scope(this, checksum, solutionInfo));
                _checksumToScope.Add(checksum, refCountedScope);

                return refCountedScope;
            }
        }

        private void DisposeScope(Scope scope)
        {
            lock (_gate)
            {
                // See if the checksum mapping is still pointing at this scope.  Definitely remove it in that scope has
                // been disposed and should not be used by anyone anymore.
                //
                // Note: we cannot assume the checksum mapping even has a mapping for this checksum, or that if it has a
                // mapping that it points to this scope.  Specifically we may get the following sequences of steps:
                //
                //  1. Feature A creates and stores a scope-I associated with checksum X.  Scope-I will have a refcount of 1.
                //  2. Feature A finishes their work and disposes the scope.  This will drop the refcount of the scope-I to 0.
                //  3. Concurrently, feature B calls in to get a scope for checksum X.  THey see the mapping from that checksum to
                //     scope with refcount 0.  They then remove that mapping and create a new mapping from checksum X to scope-J.
                //  4a. Scope-I's Dispose then gets run and calls into this method.  Due to '3' the checksum now points at scope-J.
                //  4b. Alternatively, Scope-J gets refcounted to 0, gets Disposed and then gets removed as well from the mapping
                //      Scope-I's Dispose then calls into this and sees nothing at all.
                if (_checksumToScope.TryGetValue(scope.Checksum, out var currentScopeMapping) &&
                    currentScopeMapping.Target?.SolutionInfo.ScopeId == scope.SolutionInfo.ScopeId)
                {
                    Contract.ThrowIfFalse(_checksumToScope.Remove(scope.Checksum));
                }
            }

            // We know at this point that absolutely no operations are in flight corresponding to this scope id.  So we
            // can also remove it from the states map.
            Contract.ThrowIfFalse(_solutionStates.TryRemove(scope.SolutionInfo.ScopeId, out var entry));
            entry.ReplicationContext.Dispose();
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

            if (!_solutionStates.ContainsKey(scopeId))
                throw new InvalidOperationException($"Request for scopeId '{scopeId}' that was not pinned on the host side.");

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
