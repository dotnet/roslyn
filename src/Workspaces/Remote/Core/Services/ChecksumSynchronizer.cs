// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        private readonly AssetService _assetService;

        public ChecksumSynchronizer(AssetService assetService)
        {
            _assetService = assetService;
        }

        public async Task SynchronizeAssetsAsync(IEnumerable<Checksum> checksums, CancellationToken cancellationToken)
        {
            using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            using (var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
            {
                AddIfNeeded(pooledObject.Object, checksums);
                await _assetService.SynchronizeAssetsAsync(pooledObject.Object, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task SynchronizeSolutionAssetsAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // this will make 4 round trip to data source (VS) to get all assets that belong to the given solution checksum

                // first, get solution checksum object for the given solution checksum
                var solutionChecksumObject = await _assetService.GetAssetAsync<SolutionStateChecksums>(solutionChecksum, cancellationToken).ConfigureAwait(false);

                // second, get direct children of the solution
                await SynchronizeSolutionAsync(solutionChecksumObject, cancellationToken).ConfigureAwait(false);

                // third, get direct children for all projects in the solution
                await SynchronizeProjectsAsync(solutionChecksumObject, cancellationToken).ConfigureAwait(false);

                // last, get direct children for all documents in the solution
                await SynchronizeDocumentsAsync(solutionChecksumObject, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task SynchronizeProjectAssetsAsync(Checksum projectChecksum, CancellationToken cancellationToken)
        {
            using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            using (var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
            {
                var checksums = pooledObject.Object;

                var projectChecksumObject = await _assetService.GetAssetAsync<ProjectStateChecksums>(projectChecksum, cancellationToken).ConfigureAwait(false);
                AddIfNeeded(checksums, projectChecksumObject.Children);

                foreach (var checksum in projectChecksumObject.Documents)
                {
                    var documentChecksumObject = await _assetService.GetAssetAsync<DocumentStateChecksums>(checksum, cancellationToken).ConfigureAwait(false);
                    AddIfNeeded(checksums, documentChecksumObject.Children);
                }

                foreach (var checksum in projectChecksumObject.AdditionalDocuments)
                {
                    var documentChecksumObject = await _assetService.GetAssetAsync<DocumentStateChecksums>(checksum, cancellationToken).ConfigureAwait(false);
                    AddIfNeeded(checksums, documentChecksumObject.Children);
                }

                await _assetService.SynchronizeAssetsAsync(checksums, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SynchronizeSolutionAsync(SolutionStateChecksums solutionChecksumObject, CancellationToken cancellationToken)
        {
            // get children of solution checksum object at once
            using (var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
            {
                var solutionChecksums = pooledObject.Object;

                AddIfNeeded(solutionChecksums, solutionChecksumObject.Children);
                await _assetService.SynchronizeAssetsAsync(solutionChecksums, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SynchronizeProjectsAsync(SolutionStateChecksums solutionChecksumObject, CancellationToken cancellationToken)
        {
            // get children of project checksum objects at once
            using (var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
            {
                var projectChecksums = pooledObject.Object;

                foreach (var projectChecksum in solutionChecksumObject.Projects)
                {
                    var projectChecksumObject = await _assetService.GetAssetAsync<ProjectStateChecksums>(projectChecksum, cancellationToken).ConfigureAwait(false);
                    AddIfNeeded(projectChecksums, projectChecksumObject.Children);
                }

                await _assetService.SynchronizeAssetsAsync(projectChecksums, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SynchronizeDocumentsAsync(SolutionStateChecksums solutionChecksumObject, CancellationToken cancellationToken)
        {
            // get children of document checksum objects at once
            using (var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
            {
                var documentChecksums = pooledObject.Object;

                foreach (var projectChecksum in solutionChecksumObject.Projects)
                {
                    var projectChecksumObject = await _assetService.GetAssetAsync<ProjectStateChecksums>(projectChecksum, cancellationToken).ConfigureAwait(false);

                    foreach (var checksum in projectChecksumObject.Documents)
                    {
                        var documentChecksumObject = await _assetService.GetAssetAsync<DocumentStateChecksums>(checksum, cancellationToken).ConfigureAwait(false);
                        AddIfNeeded(documentChecksums, documentChecksumObject.Children);
                    }

                    foreach (var checksum in projectChecksumObject.AdditionalDocuments)
                    {
                        var documentChecksumObject = await _assetService.GetAssetAsync<DocumentStateChecksums>(checksum, cancellationToken).ConfigureAwait(false);
                        AddIfNeeded(documentChecksums, documentChecksumObject.Children);
                    }
                }

                await _assetService.SynchronizeAssetsAsync(documentChecksums, cancellationToken).ConfigureAwait(false);
            }
        }

        private void AddIfNeeded(HashSet<Checksum> checksums, IReadOnlyList<object> checksumOrCollections)
        {
            foreach (var checksumOrCollection in checksumOrCollections)
            {
                var checksum = checksumOrCollection as Checksum;
                if (checksum != null)
                {
                    AddIfNeeded(checksums, checksum);
                    continue;
                }

                var checksumCollection = checksumOrCollection as ChecksumCollection;
                if (checksumCollection != null)
                {
                    AddIfNeeded(checksums, checksumCollection);
                    continue;
                }

                throw ExceptionUtilities.UnexpectedValue(checksumOrCollection);
            }
        }

        private void AddIfNeeded(HashSet<Checksum> checksums, IEnumerable<Checksum> collection)
        {
            foreach (var checksum in collection)
            {
                AddIfNeeded(checksums, checksum);
            }
        }

        private void AddIfNeeded(HashSet<Checksum> checksums, Checksum checksum)
        {
            if (!_assetService.EnsureCacheEntryIfExists(checksum))
            {
                checksums.Add(checksum);
            }
        }
    }
}