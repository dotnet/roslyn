// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
        ProjectCone? projectCone,
        SolutionCompilationState compilationState) : IDisposable
    {
        private readonly SolutionAssetStorage _storage = storage;

        public readonly Checksum SolutionChecksum = solutionChecksum;
        public readonly ProjectCone? ProjectCone = projectCone;
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
            AssetPath assetPath,
            ReadOnlyMemory<Checksum> checksums,
            Func<Checksum, object, CancellationToken, ValueTask> onAssetFoundAsync,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var obj = Creator.CreateChecksumSet(checksums);
            var checksumsToFind = obj.Object;

            var numberOfChecksumsToSearch = checksumsToFind.Count;
            Contract.ThrowIfTrue(checksumsToFind.Contains(Checksum.Null));

            await FindAssetsAsync(assetPath, checksumsToFind, onAssetFoundAsync, cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfTrue(checksumsToFind.Count > 0);
        }

        private async Task FindAssetsAsync(
            AssetPath assetPath,
            HashSet<Checksum> remainingChecksumsToFind,
            Func<Checksum, object, CancellationToken, ValueTask> onAssetFoundAsync,
            CancellationToken cancellationToken)
        {
            var solutionState = this.CompilationState;

            if (this.ProjectCone is null)
            {
                // If we're not in a project cone, start the search at the top most state-checksum corresponding to the
                // entire solution.
                Contract.ThrowIfFalse(solutionState.TryGetStateChecksums(out var stateChecksums));
                await stateChecksums.FindAsync(solutionState, this.ProjectCone, assetPath, remainingChecksumsToFind, onAssetFoundAsync, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Otherwise, grab the top-most state checksum for this cone and search within that.
                Contract.ThrowIfFalse(solutionState.TryGetStateChecksums(this.ProjectCone.RootProjectId, out var stateChecksums));
                await stateChecksums.FindAsync(solutionState, this.ProjectCone, assetPath, remainingChecksumsToFind, onAssetFoundAsync, cancellationToken).ConfigureAwait(false);
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

                object? asset = null;
                await scope.FindAssetsAsync(AssetPath.FullLookupForTesting, checksumPool.Object, (foundChecksum, foundAsset, _) =>
                {
                    Contract.ThrowIfTrue(asset != null); // We should only find one asset
                    Contract.ThrowIfTrue(checksum != foundChecksum);
                    asset = foundAsset;
                    return ValueTaskFactory.CompletedTask;
                }, cancellationToken).ConfigureAwait(false);

                Contract.ThrowIfNull(asset);

                return asset;
            }
        }
    }
}
