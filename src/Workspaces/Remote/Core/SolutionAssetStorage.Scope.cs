// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal partial class SolutionAssetStorage
{
    internal sealed partial class Scope : IDisposable
    {
        private readonly SolutionAssetStorage _storage;

        public readonly Checksum SolutionChecksum;
        public readonly SolutionState Solution;

        /// <summary>
        ///  Will be disposed from <see cref="DecreaseScopeRefCount(Scope)"/> when the last ref-count to this scope goes
        ///  away.
        /// </summary>
        public readonly SolutionReplicationContext ReplicationContext = new();

        /// <summary>
        /// Only safe to read write while <see cref="_gate"/> is held.
        /// </summary>
        public int RefCount = 1;

        public Scope(
            SolutionAssetStorage storage,
            Checksum solutionChecksum,
            SolutionState solution)
        {
            _storage = storage;
            SolutionChecksum = solutionChecksum;
            Solution = solution;
        }

        public void Dispose()
            => _storage.DecreaseScopeRefCount(this);

        /// <summary>
        /// Retrieve asset of a specified <paramref name="checksum"/> available within <see langword="this"/> from
        /// the storage.
        /// </summary>
        public async ValueTask<SolutionAsset> GetAssetAsync(Checksum checksum, CancellationToken cancellationToken)
        {
            if (checksum == Checksum.Null)
            {
                // check nil case
                return SolutionAsset.Null;
            }

            return await FindAssetAsync(checksum, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieve assets of specified <paramref name="checksums"/> available within <see langword="this"/> from
        /// the storage.
        /// </summary>
        public async ValueTask<IReadOnlyDictionary<Checksum, SolutionAsset>> GetAssetsAsync(
            Checksum[] checksums, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var checksumsToFind = Creator.CreateChecksumSet(checksums);

            var numberOfChecksumsToSearch = checksumsToFind.Object.Count;
            var result = new Dictionary<Checksum, SolutionAsset>(numberOfChecksumsToSearch);

            if (checksumsToFind.Object.Remove(Checksum.Null))
            {
                result[Checksum.Null] = SolutionAsset.Null;
            }

            await FindAssetsAsync(checksumsToFind.Object, result, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfTrue(result.Count != numberOfChecksumsToSearch);

            // no checksum left to find
            Debug.Assert(checksumsToFind.Object.Count == 0);
            return result;
        }

        /// <summary>
        /// Find an asset of the specified <paramref name="checksum"/> within <see langword="this"/>.
        /// </summary>
        private async ValueTask<SolutionAsset> FindAssetAsync(Checksum checksum, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var checksumPool = Creator.CreateChecksumSet(SpecializedCollections.SingletonEnumerable(checksum));
            using var resultPool = Creator.CreateResultSet();

            await FindAssetsAsync(checksumPool.Object, resultPool.Object, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfTrue(resultPool.Object.Count != 1);

            var (resultingChecksum, value) = resultPool.Object.First();
            Contract.ThrowIfFalse(checksum == resultingChecksum);

            return new SolutionAsset(checksum, value);
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
}
