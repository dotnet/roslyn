// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed partial class AssetProvider
{
    private readonly struct ChecksumSynchronizer(AssetProvider assetProvider)
    {
        private readonly AssetProvider _assetProvider = assetProvider;

        public async ValueTask SynchronizeSolutionAssetsAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            using var _1 = PooledDictionary<Checksum, object>.GetInstance(out var checksumToObjects);

            // first, get top level solution state for the given solution checksum
            var compilationStateChecksums = await _assetProvider.GetAssetAsync<SolutionCompilationStateChecksums>(
                assetPath: AssetPath.SolutionOnly, solutionChecksum, cancellationToken).ConfigureAwait(false);

            using var _2 = PooledHashSet<Checksum>.GetInstance(out var checksums);

            // second, get direct children of the solution compilation state.
            compilationStateChecksums.AddAllTo(checksums);
            await _assetProvider.SynchronizeAssetsAsync(assetPath: AssetPath.SolutionOnly, checksums, results: null, cancellationToken).ConfigureAwait(false);

            // third, get direct children of the solution state.
            var stateChecksums = await _assetProvider.GetAssetAsync<SolutionStateChecksums>(
                assetPath: AssetPath.SolutionOnly, compilationStateChecksums.SolutionState, cancellationToken).ConfigureAwait(false);

            // Ask for solutions and top-level projects as the solution checksums will contain the checksums for
            // the project states and we want to get that all in one batch.
            checksums.Clear();
            stateChecksums.AddAllTo(checksums);
            await _assetProvider.SynchronizeAssetsAsync(assetPath: AssetPath.SolutionAndTopLevelProjectsOnly, checksums, checksumToObjects, cancellationToken).ConfigureAwait(false);

            // fourth, get all projects and documents in the solution 
            foreach (var (projectChecksum, _) in stateChecksums.Projects)
            {
                var projectStateChecksums = (ProjectStateChecksums)checksumToObjects[projectChecksum];
                await SynchronizeProjectAssetsAsync(projectStateChecksums, cancellationToken).ConfigureAwait(false);
            }
        }

        public async ValueTask SynchronizeProjectAssetsAsync(ProjectStateChecksums projectChecksum, CancellationToken cancellationToken)
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
                assetPath: AssetPath.ProjectAndDocuments(projectChecksum.ProjectId), checksums, results: null, cancellationToken).ConfigureAwait(false);

            checksums.Clear();

            // Then synchronize the info about all the documents within.
            await CollectChecksumChildrenAsync(this, projectChecksum.Documents).ConfigureAwait(false);
            await CollectChecksumChildrenAsync(this, projectChecksum.AdditionalDocuments).ConfigureAwait(false);
            await CollectChecksumChildrenAsync(this, projectChecksum.AnalyzerConfigDocuments).ConfigureAwait(false);

            await _assetProvider.SynchronizeAssetsAsync(
                assetPath: AssetPath.ProjectAndDocuments(projectChecksum.ProjectId), checksums, results: null, cancellationToken).ConfigureAwait(false);

            async ValueTask CollectChecksumChildrenAsync(ChecksumSynchronizer @this, ChecksumsAndIds<DocumentId> collection)
            {
                // This GetAssetsAsync call should be fast since they were just retrieved above.  There's a small chance
                // the asset-cache GC pass may have cleaned them up, but that should be exceedingly rare.
                var allDocChecksums = await @this._assetProvider.GetAssetsAsync<DocumentStateChecksums>(
                    AssetPath.ProjectAndDocuments(projectChecksum.ProjectId), collection.Checksums, cancellationToken).ConfigureAwait(false);
                foreach (var docChecksums in allDocChecksums)
                {
                    checksums.Add(docChecksums.Info);
                    checksums.Add(docChecksums.Text);
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
