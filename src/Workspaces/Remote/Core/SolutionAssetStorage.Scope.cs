// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
        SolutionState solution) : IDisposable
    {
        private readonly SolutionAssetStorage _storage = storage;

        public readonly Checksum SolutionChecksum = solutionChecksum;
        public readonly SolutionState Solution = solution;

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
            ProjectId? hintProject,
            ImmutableArray<Checksum> checksums,
            Dictionary<Checksum, object?> assetMap,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var checksumsToFind = Creator.CreateChecksumSet(checksums);

            var numberOfChecksumsToSearch = checksumsToFind.Object.Count;

            if (checksumsToFind.Object.Remove(Checksum.Null))
                assetMap[Checksum.Null] = null;

            await FindAssetsAsync(hintProject, checksumsToFind.Object, assetMap, cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfTrue(checksumsToFind.Object.Count > 0);
            Contract.ThrowIfTrue(assetMap.Count != numberOfChecksumsToSearch);
        }

        private async Task FindAssetsAsync(
            ProjectId? hintProject, HashSet<Checksum> remainingChecksumsToFind, Dictionary<Checksum, object?> result, CancellationToken cancellationToken)
        {
            var solutionState = this.Solution;
            if (solutionState.TryGetStateChecksums(out var stateChecksums))
                await stateChecksums.FindAsync(solutionState, hintProject, remainingChecksumsToFind, result, cancellationToken).ConfigureAwait(false);

            foreach (var projectId in solutionState.ProjectIds)
            {
                if (remainingChecksumsToFind.Count == 0)
                    break;

                if (solutionState.TryGetStateChecksums(projectId, out var checksums))
                    await checksums.FindAsync(solutionState, hintProject, remainingChecksumsToFind, result, cancellationToken).ConfigureAwait(false);
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
                using var resultPool = Creator.CreateResultMap();

                await scope.FindAssetsAsync(hintProject: null, checksumPool.Object, resultPool.Object, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfTrue(resultPool.Object.Count != 1);

                var (resultingChecksum, value) = resultPool.Object.First();
                Contract.ThrowIfFalse(checksum == resultingChecksum);

                return value;
            }
        }
    }
}
