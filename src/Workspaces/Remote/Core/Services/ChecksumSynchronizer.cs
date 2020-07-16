// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal class ChecksumSynchronizer
    {
        // make sure there is always only 1 bulk synchronization
        private static readonly SemaphoreSlim s_gate = new SemaphoreSlim(initialCount: 1);

        private readonly AssetProvider _assetProvider;

        public ChecksumSynchronizer(AssetProvider assetProvider)
            => _assetProvider = assetProvider;

        public async Task SynchronizeAssetsAsync(IEnumerable<Checksum> checksums, CancellationToken cancellationToken)
        {
            using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                await SynchronizeAssets_NoLockAsync(checksums, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task SynchronizeSolutionAssetsAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // this will make 4 round trip to data source (VS) to get all assets that belong to the given solution checksum

                // first, get solution checksum object for the given solution checksum
                var solutionChecksumObject = await _assetProvider.GetAssetAsync<SolutionStateChecksums>(solutionChecksum, cancellationToken).ConfigureAwait(false);

                // second, get direct children of the solution
                await SynchronizeAssets_NoLockAsync(solutionChecksumObject.Children, cancellationToken).ConfigureAwait(false);

                // third and last get direct children for all projects and documents in the solution 
                await SynchronizeProjectAssets_NoLockAsync(solutionChecksumObject.Projects, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task SynchronizeProjectAssetsAsync(IEnumerable<Checksum> projectChecksums, CancellationToken cancellationToken)
        {
            using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                await SynchronizeProjectAssets_NoLockAsync(projectChecksums, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SynchronizeProjectAssets_NoLockAsync(IEnumerable<Checksum> projectChecksums, CancellationToken cancellationToken)
        {
            // get children of project checksum objects at once
            await SynchronizeProjectsAsync(projectChecksums, cancellationToken).ConfigureAwait(false);

            // get children of document checksum objects at once
            using var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();
            var checksums = pooledObject.Object;

            foreach (var projectChecksum in projectChecksums)
            {
                var projectChecksumObject = await _assetProvider.GetAssetAsync<ProjectStateChecksums>(projectChecksum, cancellationToken).ConfigureAwait(false);

                await CollectChecksumChildrenAsync(checksums, projectChecksumObject.Documents, cancellationToken).ConfigureAwait(false);
                await CollectChecksumChildrenAsync(checksums, projectChecksumObject.AdditionalDocuments, cancellationToken).ConfigureAwait(false);
                await CollectChecksumChildrenAsync(checksums, projectChecksumObject.AnalyzerConfigDocuments, cancellationToken).ConfigureAwait(false);
            }

            await _assetProvider.SynchronizeAssetsAsync(checksums, cancellationToken).ConfigureAwait(false);
        }

        private async Task SynchronizeProjectsAsync(IEnumerable<Checksum> projectChecksums, CancellationToken cancellationToken)
        {
            // get children of project checksum objects at once
            using var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();
            var checksums = pooledObject.Object;

            await CollectChecksumChildrenAsync(checksums, projectChecksums, cancellationToken).ConfigureAwait(false);
            await _assetProvider.SynchronizeAssetsAsync(checksums, cancellationToken).ConfigureAwait(false);
        }

        private async Task SynchronizeAssets_NoLockAsync(IEnumerable<object> checksumOrCollections, CancellationToken cancellationToken)
        {
            // get children of solution checksum object at once
            using var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();
            var checksums = pooledObject.Object;

            AddIfNeeded(checksums, checksumOrCollections);
            await _assetProvider.SynchronizeAssetsAsync(checksums, cancellationToken).ConfigureAwait(false);
        }

        private async Task CollectChecksumChildrenAsync(HashSet<Checksum> set, IEnumerable<Checksum> checksums, CancellationToken cancellationToken)
        {
            foreach (var checksum in checksums)
            {
                var checksumObject = await _assetProvider.GetAssetAsync<ChecksumWithChildren>(checksum, cancellationToken).ConfigureAwait(false);
                AddIfNeeded(set, checksumObject.Children);
            }
        }

        private void AddIfNeeded(HashSet<Checksum> checksums, IEnumerable<object> checksumOrCollections)
        {
            foreach (var checksumOrCollection in checksumOrCollections)
            {
                switch (checksumOrCollection)
                {
                    case Checksum checksum:
                        AddIfNeeded(checksums, checksum);
                        continue;
                    case ChecksumCollection checksumCollection:
                        AddIfNeeded(checksums, checksumCollection);
                        continue;
                }

                throw ExceptionUtilities.UnexpectedValue(checksumOrCollection);
            }
        }

        private void AddIfNeeded(HashSet<Checksum> checksums, Checksum checksum)
        {
            if (!_assetProvider.EnsureCacheEntryIfExists(checksum))
            {
                checksums.Add(checksum);
            }
        }
    }
}
