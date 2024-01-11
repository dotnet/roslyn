// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal partial class SolutionAssetStorage
{
    internal sealed partial class Scope(
        SolutionAssetStorage storage,
        Checksum solutionChecksum,
        SolutionCompilationState compilationState) : IDisposable
    {
        private readonly SolutionAssetStorage _storage = storage;

        public readonly Checksum SolutionChecksum = solutionChecksum;
        public readonly SolutionCompilationState CompilationState = compilationState;

        /// <summary>
        ///  Will be disposed from <see cref="DecreaseScopeRefCount(Scope)"/> when the last ref-count to this scope goes
        ///  away.
        /// </summary>
        public readonly SolutionReplicationContext ReplicationContext = new();

        /// <summary>
        /// Only safe to read write while <see cref="_gate"/> is held.
        /// </summary>
        public int RefCount = 1;

        public void Dispose()
            => _storage.DecreaseScopeRefCount(this);

        /// <summary>
        /// Retrieve assets of specified <paramref name="checksums"/> available within <see langword="this"/> from
        /// the storage.
        /// </summary>
        public async Task AddAssetsAsync(
            AssetHint assetHint,
            ImmutableArray<Checksum> checksums,
            Dictionary<Checksum, object> assetMap,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var obj = Creator.CreateChecksumSet(checksums);
            var checksumsToFind = obj.Object;

            var numberOfChecksumsToSearch = checksumsToFind.Count;
            Contract.ThrowIfTrue(checksumsToFind.Contains(Checksum.Null));

            await FindAssetsAsync(assetHint, checksumsToFind, assetMap, cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfTrue(checksumsToFind.Count > 0);
            Contract.ThrowIfTrue(assetMap.Count != numberOfChecksumsToSearch);
        }

        private async Task FindAssetsAsync(
            AssetHint assetHint, HashSet<Checksum> remainingChecksumsToFind, Dictionary<Checksum, object> result, CancellationToken cancellationToken)
        {
            var solutionState = this.CompilationState;
            if (solutionState.TryGetStateChecksums(out var stateChecksums))
                await stateChecksums.FindAsync(solutionState, assetHint, remainingChecksumsToFind, result, cancellationToken).ConfigureAwait(false);

            foreach (var projectId in solutionState.SolutionState.ProjectIds)
            {
                if (remainingChecksumsToFind.Count == 0)
                    break;

                if (solutionState.TryGetStateChecksums(projectId, out var checksums))
                    await checksums.FindAsync(solutionState, assetHint, remainingChecksumsToFind, result, cancellationToken).ConfigureAwait(false);
            }
        }

        public TestAccessor GetTestAccessor()
            => new(this);

        public readonly struct TestAccessor(Scope scope)
        {
            /// <summary>
            /// Retrieve asset of a specified <paramref name="checksum"/> available within <see langword="this"/> from
            /// the storage.
            /// </summary>
            public async ValueTask<object> GetAssetAsync(Checksum checksum, CancellationToken cancellationToken)
            {
                Contract.ThrowIfTrue(checksum == Checksum.Null);

                using var checksumPool = Creator.CreateChecksumSet(checksum);
                using var _ = Creator.CreateResultMap(out var resultPool);

                await scope.FindAssetsAsync(AssetHint.None, checksumPool.Object, resultPool, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfTrue(resultPool.Count != 1);

                var (resultingChecksum, value) = resultPool.First();
                Contract.ThrowIfFalse(checksum == resultingChecksum);

                return value;
            }
        }
    }
}
