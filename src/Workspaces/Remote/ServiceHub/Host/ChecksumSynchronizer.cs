// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed partial class AssetProvider
{
    private readonly struct ChecksumSynchronizer(AssetProvider assetProvider)
    {
        // make sure there is always only 1 bulk synchronization
        private static readonly SemaphoreSlim s_gate = new(initialCount: 1);

        private readonly AssetProvider _assetProvider = assetProvider;

        public async ValueTask SynchronizeAssetsAsync(
            AssetHint assetHint,
            HashSet<Checksum> checksums,
            Dictionary<Checksum, object>? results,
            CancellationToken cancellationToken)
        {
            using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                await _assetProvider.SynchronizeAssetsAsync(assetHint, checksums, results, cancellationToken).ConfigureAwait(false);
            }
        }

        public async ValueTask SynchronizeSolutionAssetsAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            SolutionStateChecksums solutionChecksumObject;
            using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // this will make 4 round trip to data source (VS) to get all assets that belong to the given solution checksum

                // first, get solution checksum object for the given solution checksum
                var solutionCompilationChecksumObject = await _assetProvider.GetAssetAsync<SolutionCompilationStateChecksums>(
                    assetHint: AssetHint.None, solutionChecksum, cancellationToken).ConfigureAwait(false);
                solutionChecksumObject = await _assetProvider.GetAssetAsync<SolutionStateChecksums>(
                    assetHint: AssetHint.None, solutionCompilationChecksumObject.SolutionState, cancellationToken).ConfigureAwait(false);

                // second, get direct children of the solution
                {
                    using var _ = PooledHashSet<Checksum>.GetInstance(out var checksums);

                    solutionChecksumObject.AddAllTo(checksums);
                    checksums.Remove(solutionChecksumObject.Checksum);
                    await _assetProvider.SynchronizeAssetsAsync(assetHint: AssetHint.None, checksums, results: null, cancellationToken).ConfigureAwait(false);
                }
            }

            // third and last get direct children for all projects and documents in the solution 
            foreach (var (projectChecksum, _) in solutionChecksumObject.Projects)
            {
                // These GetAssetAsync calls should be fast since they were just retrieved above.  There's a small
                // chance the asset-cache GC pass may have cleaned them up, but that should be exceedingly rare.
                var projectStateChecksums = await _assetProvider.GetAssetAsync<ProjectStateChecksums>(
                    assetHint: AssetHint.None, projectChecksum, cancellationToken).ConfigureAwait(false);
                await SynchronizeProjectAssetsAsync(projectStateChecksums, cancellationToken).ConfigureAwait(false);
            }
        }

        public async ValueTask SynchronizeProjectAssetsAsync(ProjectStateChecksums projectChecksum, CancellationToken cancellationToken)
        {
            using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                await SynchronizeProjectAssets_NoLockAsync(projectChecksum, cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask SynchronizeProjectAssets_NoLockAsync(ProjectStateChecksums projectChecksum, CancellationToken cancellationToken)
        {
            // get children of project checksum objects at once
            using var _ = PooledHashSet<Checksum>.GetInstance(out var checksums);

            checksums.Add(projectChecksum.Info);
            checksums.Add(projectChecksum.CompilationOptions);
            checksums.Add(projectChecksum.ParseOptions);
            AddAll(checksums, projectChecksum.ProjectReferences);
            AddAll(checksums, projectChecksum.MetadataReferences);
            AddAll(checksums, projectChecksum.AnalyzerReferences);
            AddAll(checksums, projectChecksum.Documents.Checksums);
            AddAll(checksums, projectChecksum.AdditionalDocuments.Checksums);
            AddAll(checksums, projectChecksum.AnalyzerConfigDocuments.Checksums);

            // First synchronize all the top-level info about this project.
            await _assetProvider.SynchronizeAssetsAsync(
                assetHint: projectChecksum.ProjectId, checksums, results: null, cancellationToken).ConfigureAwait(false);

            checksums.Clear();

            // Then synchronize the info about all the documents within.
            await CollectChecksumChildrenAsync(this, projectChecksum.Documents.Checksums).ConfigureAwait(false);
            await CollectChecksumChildrenAsync(this, projectChecksum.AdditionalDocuments.Checksums).ConfigureAwait(false);
            await CollectChecksumChildrenAsync(this, projectChecksum.AnalyzerConfigDocuments.Checksums).ConfigureAwait(false);

            await _assetProvider.SynchronizeAssetsAsync(
                assetHint: projectChecksum.ProjectId, checksums, results: null, cancellationToken).ConfigureAwait(false);

            async ValueTask CollectChecksumChildrenAsync(ChecksumSynchronizer @this, ChecksumCollection collection)
            {
                foreach (var checksum in collection)
                {
                    // These GetAssetAsync calls should be fast since they were just retrieved above.  There's a small
                    // chance the asset-cache GC pass may have cleaned them up, but that should be exceedingly rare.
                    var checksumObject = await @this._assetProvider.GetAssetAsync<DocumentStateChecksums>(
                        assetHint: projectChecksum.ProjectId, checksum, cancellationToken).ConfigureAwait(false);
                    checksums.Add(checksumObject.Info);
                    checksums.Add(checksumObject.Text);
                }
            }
        }

        private static void AddAll(HashSet<Checksum> checksums, ChecksumCollection checksumCollection)
        {
            foreach (var checksum in checksumCollection)
                checksums.Add(checksum);
        }
    }
}
