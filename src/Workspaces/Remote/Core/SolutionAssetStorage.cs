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

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// Stores solution snapshots available to remote services.
/// </summary>
internal partial class SolutionAssetStorage
{
    /// <summary>
    /// Lock over <see cref="_checksumToScope"/>.  Note: We could consider making this a SemaphoreSlim if
    /// the locking proves to be a problem. However, it would greatly complicate the implementation and consumption
    /// side due to the pattern around <c>await using</c>.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// Mapping from operation checksum to the scope for the syncing operation that we've created for it.
    /// Ref-counted so that if we have many concurrent calls going out from the host to the OOP side that we share
    /// the same storage here so that all OOP calls can safely call back into us and get the assets they need, even
    /// if individual calls get canceled.
    /// </summary>
    private readonly Dictionary<Checksum, Scope> _checksumToScope = new();

    public Scope GetScope(Checksum solutionChecksum)
    {
        lock (_gate)
        {
            if (!_checksumToScope.ContainsKey(solutionChecksum))
                throw new InvalidOperationException($"Request for solution-checksum '{solutionChecksum}' that was not pinned on the host side.");

            return _checksumToScope[solutionChecksum];
        }
    }

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
        var checksum = projectId == null
            ? await solutionState.GetChecksumAsync(cancellationToken).ConfigureAwait(false)
            : await solutionState.GetChecksumAsync(projectId, cancellationToken).ConfigureAwait(false);

        lock (_gate)
        {
            if (_checksumToScope.TryGetValue(checksum, out var scope))
            {
                Contract.ThrowIfTrue(scope.RefCount <= 0);
                scope.RefCount++;
                return scope;
            }

            var solutionInfo = new PinnedSolutionInfo(
                checksum,
                fromPrimaryBranch: solutionState.BranchId == solutionState.Workspace.PrimaryBranchId,
                solutionState.WorkspaceVersion);

            scope = new Scope(this, checksum, solutionInfo, solutionState);
            _checksumToScope[checksum] = scope;
            return scope;
        }
    }

    private void DecreaseScopeRefCount(Scope scope)
    {
        lock (_gate)
        {
            var checksum = scope.Checksum;
            var existingScope = _checksumToScope[checksum];
            Contract.ThrowIfTrue(existingScope != scope);

            Contract.ThrowIfTrue(scope.RefCount <= 0);
            scope.RefCount--;

            // If our refcount is still above 0, then nothing else to do at this point.
            if (scope.RefCount > 0)
                return;

            // Last ref went away, update our maps while under the lock, then cleanup its context data outside of the lock.
            _checksumToScope.Remove(checksum);
        }

        scope.ReplicationContext.Dispose();
    }

    internal partial class Scope
    {
        /// <summary>
        /// Retrieve asset of a specified <paramref name="checksum"/> available within <see langword="this"/> from
        /// the storage.
        /// </summary>
        public async ValueTask<SolutionAsset?> GetAssetAsync(Checksum checksum, CancellationToken cancellationToken)
        {
            if (checksum == Checksum.Null)
            {
                // check nil case
                return SolutionAsset.Null;
            }

            var remotableData = await FindAssetAsync(checksum, cancellationToken).ConfigureAwait(false);
            if (remotableData != null)
            {
                return remotableData;
            }

            // if it reached here, it means things get canceled. due to involving 2 processes,
            // current design can make slightly staled requests to running even when things canceled.
            // if it is other case, remote host side will throw and close connection which will cause
            // vs to crash.
            // this should be changed once I address this design issue
            cancellationToken.ThrowIfCancellationRequested();

            return null;
        }

        /// <summary>
        /// Retrieve assets of specified <paramref name="checksums"/> available within <see langword="this"/> from
        /// the storage.
        /// </summary>
        public async ValueTask<IReadOnlyDictionary<Checksum, SolutionAsset>> GetAssetsAsync(
            IEnumerable<Checksum> checksums, CancellationToken cancellationToken)
        {
            using var checksumsToFind = Creator.CreateChecksumSet(checksums);

            var numberOfChecksumsToSearch = checksumsToFind.Object.Count;
            var result = new Dictionary<Checksum, SolutionAsset>(numberOfChecksumsToSearch);

            if (checksumsToFind.Object.Remove(Checksum.Null))
            {
                result[Checksum.Null] = SolutionAsset.Null;
            }

            await FindAssetsAsync(checksumsToFind.Object, result, cancellationToken).ConfigureAwait(false);
            if (result.Count == numberOfChecksumsToSearch)
            {
                // no checksum left to find
                Debug.Assert(checksumsToFind.Object.Count == 0);
                return result;
            }

            // if it reached here, it means things get canceled. due to involving 2 processes,
            // current design can make slightly staled requests to running even when things canceled.
            // if it is other case, remote host side will throw and close connection which will cause
            // vs to crash.
            // this should be changed once I address this design issue
            cancellationToken.ThrowIfCancellationRequested();

            return result;
        }

        /// <summary>
        /// Find an asset of the specified <paramref name="checksum"/> within <see langword="this"/>.
        /// </summary>
        private async ValueTask<SolutionAsset?> FindAssetAsync(Checksum checksum, CancellationToken cancellationToken)
        {
            using var checksumPool = Creator.CreateChecksumSet(SpecializedCollections.SingletonEnumerable(checksum));
            using var resultPool = Creator.CreateResultSet();

            await FindAssetsAsync(checksumPool.Object, resultPool.Object, cancellationToken).ConfigureAwait(false);

            if (resultPool.Object.Count == 1)
            {
                var (resultingChecksum, value) = resultPool.Object.First();
                Contract.ThrowIfFalse(checksum == resultingChecksum);

                return new SolutionAsset(checksum, value);
            }

            return null;
        }

        /// <summary>
        /// Find an assets of the specified <paramref name="remainingChecksumsToFind"/> within <see
        /// langword="this"/>. Once an asset of given checksum is found the corresponding asset is placed to
        /// <paramref name="result"/> and the checksum is removed from <paramref name="remainingChecksumsToFind"/>.
        /// </summary>
        private async Task FindAssetsAsync(HashSet<Checksum> remainingChecksumsToFind, Dictionary<Checksum, SolutionAsset> result, CancellationToken cancellationToken)
        {
            using var resultPool = Creator.CreateResultSet();

            await FindAssetsAsync(remainingChecksumsToFind, resultPool.Object, cancellationToken).ConfigureAwait(false);

            foreach (var (checksum, value) in resultPool.Object)
            {
                result[checksum] = new SolutionAsset(checksum, value);
            }
        }

        private async Task FindAssetsAsync(HashSet<Checksum> remainingChecksumsToFind, Dictionary<Checksum, object> result, CancellationToken cancellationToken)
        {
            var solutionState = this.Solution;
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
            foreach (var (_, scope) in _solutionAssetStorage._checksumToScope)
            {
                var data = await scope.GetAssetAsync(checksum, cancellationToken).ConfigureAwait(false);
                if (data != null)
                {
                    return data;
                }
            }

            return null;
        }
    }
}
