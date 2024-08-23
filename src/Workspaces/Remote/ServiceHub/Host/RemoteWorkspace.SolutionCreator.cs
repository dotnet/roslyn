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
using ICSharpCode.Decompiler.Solution;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal partial class RemoteWorkspace
{
    /// <summary>
    /// Create solution for given checksum from base solution
    /// </summary>
    private readonly struct SolutionCreator(RemoteWorkspace workspace, AssetProvider assetService, Solution baseSolution)
    {
#pragma warning disable IDE0052 // used only in DEBUG builds
        private readonly RemoteWorkspace _workspace = workspace;
#pragma warning restore

        private readonly AssetProvider _assetProvider = assetService;
        private readonly Solution _baseSolution = baseSolution;

        public async Task<Solution> CreateSolutionAsync(Checksum newSolutionChecksum, CancellationToken cancellationToken)
        {
            try
            {
                var solution = _baseSolution;

                // If we previously froze a source generated document and then held onto that, unfreeze it now. We'll re-freeze the new document
                // if needed again later.
                solution = solution.WithoutFrozenSourceGeneratedDocuments();

                var newSolutionCompilationChecksums = await _assetProvider.GetAssetAsync<SolutionCompilationStateChecksums>(
                    AssetPathKind.SolutionCompilationStateChecksums, newSolutionChecksum, cancellationToken).ConfigureAwait(false);
                var newSolutionChecksums = await _assetProvider.GetAssetAsync<SolutionStateChecksums>(
                    AssetPathKind.SolutionStateChecksums, newSolutionCompilationChecksums.SolutionState, cancellationToken).ConfigureAwait(false);

                var oldSolutionCompilationChecksums = await solution.CompilationState.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                var oldSolutionChecksums = await solution.CompilationState.SolutionState.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);

                if (oldSolutionChecksums.Attributes != newSolutionChecksums.Attributes)
                {
                    var newSolutionInfo = await _assetProvider.GetAssetAsync<SolutionInfo.SolutionAttributes>(
                        AssetPathKind.SolutionAttributes, newSolutionChecksums.Attributes, cancellationToken).ConfigureAwait(false);

                    // if either id or file path has changed, then this is not update
                    Contract.ThrowIfFalse(solution.Id == newSolutionInfo.Id && solution.FilePath == newSolutionInfo.FilePath);
                }

                if (oldSolutionChecksums.Projects.Checksum != newSolutionChecksums.Projects.Checksum)
                {
                    solution = await UpdateProjectsAsync(
                        solution, oldSolutionChecksums, newSolutionChecksums, cancellationToken).ConfigureAwait(false);
                }

                if (oldSolutionChecksums.AnalyzerReferences.Checksum != newSolutionChecksums.AnalyzerReferences.Checksum)
                {
                    solution = solution.WithAnalyzerReferences(await _assetProvider.GetAssetsArrayAsync<AnalyzerReference>(
                        AssetPathKind.SolutionAnalyzerReferences, newSolutionChecksums.AnalyzerReferences, cancellationToken).ConfigureAwait(false));
                }

                if (oldSolutionChecksums.FallbackAnalyzerOptions != newSolutionChecksums.FallbackAnalyzerOptions)
                {
                    solution = solution.WithFallbackAnalyzerOptions(await _assetProvider.GetAssetAsync<ImmutableDictionary<string, StructuredAnalyzerConfigOptions>>(
                        AssetPathKind.SolutionFallbackAnalyzerOptions, newSolutionChecksums.FallbackAnalyzerOptions, cancellationToken).ConfigureAwait(false));
                }

                if (newSolutionCompilationChecksums.FrozenSourceGeneratedDocumentIdentities.HasValue &&
                    newSolutionCompilationChecksums.FrozenSourceGeneratedDocuments.HasValue &&
                    !newSolutionCompilationChecksums.FrozenSourceGeneratedDocumentGenerationDateTimes.IsDefault)
                {
                    var newSolutionFrozenSourceGeneratedDocumentIdentities = newSolutionCompilationChecksums.FrozenSourceGeneratedDocumentIdentities.Value;
                    var newSolutionFrozenSourceGeneratedDocuments = newSolutionCompilationChecksums.FrozenSourceGeneratedDocuments.Value;
                    var count = newSolutionFrozenSourceGeneratedDocuments.Ids.Length;

                    var frozenDocuments = new FixedSizeArrayBuilder<(SourceGeneratedDocumentIdentity identity, DateTime generationDateTime, SourceText text)>(count);
                    for (var i = 0; i < count; i++)
                    {
                        var frozenDocumentId = newSolutionFrozenSourceGeneratedDocuments.Ids[i];
                        var frozenDocumentTextChecksum = newSolutionFrozenSourceGeneratedDocuments.TextChecksums[i];
                        var frozenDocumentIdentity = newSolutionFrozenSourceGeneratedDocumentIdentities[i];

                        var identity = await _assetProvider.GetAssetAsync<SourceGeneratedDocumentIdentity>(
                            new(AssetPathKind.SolutionFrozenSourceGeneratedDocumentIdentities, frozenDocumentId), frozenDocumentIdentity, cancellationToken).ConfigureAwait(false);

                        var serializableSourceText = await _assetProvider.GetAssetAsync<SerializableSourceText>(
                            new(AssetPathKind.SolutionFrozenSourceGeneratedDocumentText, frozenDocumentId), frozenDocumentTextChecksum, cancellationToken).ConfigureAwait(false);

                        var generationDateTime = newSolutionCompilationChecksums.FrozenSourceGeneratedDocumentGenerationDateTimes[i];
                        var text = await serializableSourceText.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        frozenDocuments.Add((identity, generationDateTime, text));
                    }

                    solution = solution.WithFrozenSourceGeneratedDocuments(frozenDocuments.MoveToImmutable());
                }

                if (oldSolutionCompilationChecksums.SourceGeneratorExecutionVersionMap !=
                    newSolutionCompilationChecksums.SourceGeneratorExecutionVersionMap)
                {
                    var newVersions = await _assetProvider.GetAssetAsync<SourceGeneratorExecutionVersionMap>(
                        AssetPathKind.SolutionSourceGeneratorExecutionVersionMap, newSolutionCompilationChecksums.SourceGeneratorExecutionVersionMap, cancellationToken).ConfigureAwait(false);

#if DEBUG
                    var projectCone = newSolutionChecksums.ProjectCone;
                    if (projectCone != null)
                    {
                        Debug.Assert(projectCone.ProjectIds.Count == newVersions.Map.Count);
                        Debug.Assert(projectCone.ProjectIds.All(id => newVersions.Map.ContainsKey(id)));
                    }
                    else
                    {
                        Debug.Assert(solution.ProjectIds.Count == newVersions.Map.Count);
                        Debug.Assert(solution.ProjectIds.All(id => newVersions.Map.ContainsKey(id)));
                    }
#endif

                    solution = solution.UpdateSpecificSourceGeneratorExecutionVersions(newVersions);
                }

#if DEBUG
                // make sure created solution has same checksum as given one
                await ValidateChecksumAsync(newSolutionChecksum, solution, newSolutionChecksums.ProjectConeId, cancellationToken).ConfigureAwait(false);
#endif

                return solution;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private async Task<Solution> UpdateProjectsAsync(
            Solution solution, SolutionStateChecksums oldSolutionChecksums, SolutionStateChecksums newSolutionChecksums, CancellationToken cancellationToken)
        {
            var solutionState = solution.SolutionState;

            using var _1 = PooledDictionary<ProjectId, Checksum>.GetInstance(out var oldProjectIdToChecksum);
            using var _2 = PooledDictionary<ProjectId, Checksum>.GetInstance(out var newProjectIdToChecksum);

            foreach (var (oldChecksum, projectId) in oldSolutionChecksums.Projects)
                oldProjectIdToChecksum.Add(projectId, oldChecksum);

            foreach (var (newChecksum, projectId) in newSolutionChecksums.Projects)
                newProjectIdToChecksum.Add(projectId, newChecksum);

            // remove projects that are the same on both sides.  We can just iterate over one of the maps as,
            // definitionally, for the project to be on both sides, it will be contained in both.
            foreach (var (oldChecksum, projectId) in oldSolutionChecksums.Projects)
            {
                if (newProjectIdToChecksum.TryGetValue(projectId, out var newChecksum) &&
                    oldChecksum == newChecksum)
                {
                    oldProjectIdToChecksum.Remove(projectId);
                    newProjectIdToChecksum.Remove(projectId);
                }
            }

            // If there are old projects that are now missing on the new side, and this is a projectConeSync, then
            // exclude them from the old side as well.  This way we only consider projects actually added or
            // changed.
            //
            // Importantly, this means in the event of a cone-sync, we never drop projects locally.  That's very
            // desirable as it will likely be useful in future calls to still know about that project info without
            // it being dropped and having to be resynced.
            var isConeSync = newSolutionChecksums.ProjectConeId != null;
            if (isConeSync)
            {
                foreach (var (oldChecksum, oldProjectId) in oldSolutionChecksums.Projects)
                {
                    if (!newProjectIdToChecksum.ContainsKey(oldProjectId))
                        oldProjectIdToChecksum.Remove(oldProjectId);
                }

                // All the old projects must be in the new project set.  Though the reverse doesn't have to hold.
                // The new project set may contain additional projects to add.
                Contract.ThrowIfFalse(oldProjectIdToChecksum.Keys.All(newProjectIdToChecksum.Keys.Contains));
            }

            using var _3 = PooledDictionary<ProjectId, ProjectStateChecksums>.GetInstance(out var oldProjectIdToStateChecksums);
            using var _4 = PooledDictionary<ProjectId, ProjectStateChecksums>.GetInstance(out var newProjectIdToStateChecksums);

            // Now, find the full state checksums for all the old projects
            foreach (var (projectId, oldChecksum) in oldProjectIdToChecksum)
            {
                // this should be cheap since we already computed oldSolutionChecksums (which calls into this).
                var oldProjectStateChecksums = await solutionState
                    .GetRequiredProjectState(projectId)
                    .GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfTrue(oldProjectStateChecksums.ProjectId != projectId);
                Contract.ThrowIfTrue(oldChecksum != oldProjectStateChecksums.Checksum);

                oldProjectIdToStateChecksums.Add(projectId, oldProjectStateChecksums);
            }

            using var _5 = PooledHashSet<Checksum>.GetInstance(out var newChecksumsToSync);
            newChecksumsToSync.AddRange(newProjectIdToChecksum.Values);

            await _assetProvider.GetAssetHelper<ProjectStateChecksums>().GetAssetsAsync(
                assetPath: AssetPathKind.ProjectStateChecksums, newChecksumsToSync,
                static (checksum, newProjectStateChecksum, newProjectIdToStateChecksums) =>
                {
                    Contract.ThrowIfTrue(checksum != newProjectStateChecksum.Checksum);
                    newProjectIdToStateChecksums.Add(newProjectStateChecksum.ProjectId, newProjectStateChecksum);
                },
                arg: newProjectIdToStateChecksums,
                cancellationToken).ConfigureAwait(false);

            // Now that we've collected the old and new project state checksums, we can actually process them to
            // determine what to remove, what to add, and what to change.
            solution = await UpdateProjectsAsync(
                solution, isConeSync, oldProjectIdToStateChecksums, newProjectIdToStateChecksums, cancellationToken).ConfigureAwait(false);

            return solution;
        }

        private async Task<Solution> UpdateProjectsAsync(
            Solution solution,
            bool isConeSync,
            Dictionary<ProjectId, ProjectStateChecksums> oldProjectIdToStateChecksums,
            Dictionary<ProjectId, ProjectStateChecksums> newProjectIdToStateChecksums,
            CancellationToken cancellationToken)
        {
            // Note: it's common to need to collect a large set of project-attributes and compilation options.  So
            // attempt to collect all of those in a single call for each kind instead of a call for each instance
            // needed.
            {
                using var _ = PooledHashSet<Checksum>.GetInstance(out var projectItemChecksums);
                foreach (var (_, newProjectChecksums) in newProjectIdToStateChecksums)
                    projectItemChecksums.Add(newProjectChecksums.Info);

                await _assetProvider.GetAssetsAsync<ProjectInfo.ProjectAttributes>(
                    assetPath: AssetPathKind.ProjectAttributes, projectItemChecksums, cancellationToken).ConfigureAwait(false);

                projectItemChecksums.Clear();
                foreach (var (_, newProjectChecksums) in newProjectIdToStateChecksums)
                    projectItemChecksums.Add(newProjectChecksums.CompilationOptions);

                await _assetProvider.GetAssetsAsync<CompilationOptions>(
                    assetPath: AssetPathKind.ProjectCompilationOptions, projectItemChecksums, cancellationToken).ConfigureAwait(false);
            }

            using var _2 = ArrayBuilder<ProjectStateChecksums>.GetInstance(out var projectStateChecksumsToAdd);

            // added project
            foreach (var (projectId, newProjectChecksums) in newProjectIdToStateChecksums)
            {
                if (!oldProjectIdToStateChecksums.ContainsKey(projectId))
                    projectStateChecksumsToAdd.Add(newProjectChecksums);
            }

            // bulk sync added project assets fully since we'll definitely need that data, and we can fetch more
            // efficiently in bulk and in parallel.
            await _assetProvider.SynchronizeProjectAssetsAsync(projectStateChecksumsToAdd, cancellationToken).ConfigureAwait(false);

            using var _3 = ArrayBuilder<ProjectInfo>.GetInstance(projectStateChecksumsToAdd.Count, out var projectInfos);
            foreach (var (projectId, newProjectChecksums) in newProjectIdToStateChecksums)
            {
                if (!oldProjectIdToStateChecksums.ContainsKey(projectId))
                {
                    // Now make a ProjectInfo corresponding to the new project checksums.  This should be fast due
                    // to the bulk sync we just performed above.
                    var projectInfo = await _assetProvider.CreateProjectInfoAsync(newProjectChecksums, cancellationToken).ConfigureAwait(false);
                    projectInfos.Add(projectInfo);
                }
            }

            // Add solutions in bulk.  Avoiding intermediary forking of it.
            solution = solution.AddProjects(projectInfos);

            // remove all project references from projects that changed. this ensures exceptions will not occur for
            // cyclic references during an incremental update.
            foreach (var (projectId, newProjectChecksums) in newProjectIdToStateChecksums)
            {
                // Only have to do something if this was a changed project, and specifically the project references
                // changed.
                if (oldProjectIdToStateChecksums.TryGetValue(projectId, out var oldProjectChecksums) &&
                    oldProjectChecksums.ProjectReferences.Checksum != newProjectChecksums.ProjectReferences.Checksum)
                {
                    solution = solution.WithProjectReferences(projectId, projectReferences: []);
                }
            }

            using var _4 = ArrayBuilder<ProjectId>.GetInstance(out var projectsToRemove);

            // removed project
            foreach (var (projectId, _) in oldProjectIdToStateChecksums)
            {
                if (!newProjectIdToStateChecksums.ContainsKey(projectId))
                {
                    // Should never be removing projects during cone syncing.
                    Contract.ThrowIfTrue(isConeSync);
                    projectsToRemove.Add(projectId);
                }
            }

            // Remove solutions in bulk.  Avoiding intermediary forking of it.
            solution = solution.RemoveProjects(projectsToRemove);

            // changed project
            foreach (var (projectId, newProjectChecksums) in newProjectIdToStateChecksums)
            {
                if (oldProjectIdToStateChecksums.TryGetValue(projectId, out var oldProjectChecksums))
                {
                    // If this project was in the old map, then the project must have changed.  Otherwise, we would
                    // have removed it earlier on.
                    Contract.ThrowIfTrue(oldProjectChecksums.Checksum == newProjectChecksums.Checksum);

                    // changed info
                    if (oldProjectChecksums.Info != newProjectChecksums.Info)
                    {
                        solution = solution.WithProjectAttributes(await _assetProvider.GetAssetAsync<ProjectInfo.ProjectAttributes>(
                            assetPath: projectId, newProjectChecksums.Info, cancellationToken).ConfigureAwait(false));
                    }

                    solution = await UpdateProjectAsync(
                        solution.GetRequiredProject(projectId), oldProjectChecksums, newProjectChecksums, cancellationToken).ConfigureAwait(false);
                }
            }

            return solution;
        }

        private async Task<Solution> UpdateProjectAsync(Project project, ProjectStateChecksums oldProjectChecksums, ProjectStateChecksums newProjectChecksums, CancellationToken cancellationToken)
        {
            // changed compilation options
            if (oldProjectChecksums.CompilationOptions != newProjectChecksums.CompilationOptions)
            {
                project = project.WithCompilationOptions(
                    project.State.ProjectInfo.Attributes.FixUpCompilationOptions(
                        await _assetProvider.GetAssetAsync<CompilationOptions>(
                            assetPath: project.Id, newProjectChecksums.CompilationOptions, cancellationToken).ConfigureAwait(false)));
            }

            // changed parse options
            if (oldProjectChecksums.ParseOptions != newProjectChecksums.ParseOptions)
            {
                project = project.WithParseOptions(await _assetProvider.GetAssetAsync<ParseOptions>(
                    assetPath: project.Id, newProjectChecksums.ParseOptions, cancellationToken).ConfigureAwait(false));
            }

            // changed project references
            if (oldProjectChecksums.ProjectReferences.Checksum != newProjectChecksums.ProjectReferences.Checksum)
            {
                project = project.WithProjectReferences(await _assetProvider.GetAssetsArrayAsync<ProjectReference>(
                    assetPath: project.Id, newProjectChecksums.ProjectReferences, cancellationToken).ConfigureAwait(false));
            }

            // changed metadata references
            if (oldProjectChecksums.MetadataReferences.Checksum != newProjectChecksums.MetadataReferences.Checksum)
            {
                project = project.WithMetadataReferences(await _assetProvider.GetAssetsArrayAsync<MetadataReference>(
                    assetPath: project.Id, newProjectChecksums.MetadataReferences, cancellationToken).ConfigureAwait(false));
            }

            // changed analyzer references
            if (oldProjectChecksums.AnalyzerReferences.Checksum != newProjectChecksums.AnalyzerReferences.Checksum)
            {
                project = project.WithAnalyzerReferences(await _assetProvider.GetAssetsArrayAsync<AnalyzerReference>(
                    assetPath: project.Id, newProjectChecksums.AnalyzerReferences, cancellationToken).ConfigureAwait(false));
            }

            // changed analyzer references
            if (oldProjectChecksums.Documents.Checksum != newProjectChecksums.Documents.Checksum)
            {
                project = await UpdateDocumentsAsync<DocumentState>(
                    project,
                    oldProjectChecksums.Documents,
                    newProjectChecksums.Documents,
                    static (solution, documents) => solution.AddDocuments(documents),
                    static (solution, documentIds) => solution.RemoveDocuments(documentIds),
                    cancellationToken).ConfigureAwait(false);
            }

            // changed additional documents
            if (oldProjectChecksums.AdditionalDocuments.Checksum != newProjectChecksums.AdditionalDocuments.Checksum)
            {
                project = await UpdateDocumentsAsync<AdditionalDocumentState>(
                    project,
                    oldProjectChecksums.AdditionalDocuments,
                    newProjectChecksums.AdditionalDocuments,
                    static (solution, documents) => solution.AddAdditionalDocuments(documents),
                    static (solution, documentIds) => solution.RemoveAdditionalDocuments(documentIds),
                    cancellationToken).ConfigureAwait(false);
            }

            // changed analyzer config documents
            if (oldProjectChecksums.AnalyzerConfigDocuments.Checksum != newProjectChecksums.AnalyzerConfigDocuments.Checksum)
            {
                project = await UpdateDocumentsAsync<AnalyzerConfigDocumentState>(
                    project,
                    oldProjectChecksums.AnalyzerConfigDocuments,
                    newProjectChecksums.AnalyzerConfigDocuments,
                    static (solution, documents) => solution.AddAnalyzerConfigDocuments(documents),
                    static (solution, documentIds) => solution.RemoveAnalyzerConfigDocuments(documentIds),
                    cancellationToken).ConfigureAwait(false);
            }

            return project.Solution;
        }

        private async Task<Project> UpdateDocumentsAsync<TDocumentState>(
            Project project,
            DocumentChecksumsAndIds oldChecksums,
            DocumentChecksumsAndIds newChecksums,
            Func<Solution, ImmutableArray<DocumentInfo>, Solution> addDocuments,
            Func<Solution, ImmutableArray<DocumentId>, Solution> removeDocuments,
            CancellationToken cancellationToken) where TDocumentState : TextDocumentState
        {
            using var _1 = PooledDictionary<DocumentId, (Checksum attributeChecksum, Checksum textChecksum)>.GetInstance(out var oldDocumentIdToChecksums);
            using var _2 = PooledDictionary<DocumentId, (Checksum attributeChecksum, Checksum textChecksum)>.GetInstance(out var newDocumentIdToChecksums);

            foreach (var (oldAttributeChecksum, oldTextChecksum, documentId) in oldChecksums)
                oldDocumentIdToChecksums.Add(documentId, (oldAttributeChecksum, oldTextChecksum));

            foreach (var (newAttributeChecksum, newTextChecksum, documentId) in newChecksums)
                newDocumentIdToChecksums.Add(documentId, (newAttributeChecksum, newTextChecksum));

            // remove documents that are the same on both sides.  We can just iterate over one of the maps as,
            // definitionally, for the project to be on both sides, it will be contained in both.
            foreach (var (oldAttributeChecksum, oldTextChecksum, documentId) in oldChecksums)
            {
                if (newDocumentIdToChecksums.TryGetValue(documentId, out var newChecksum) &&
                    oldAttributeChecksum == newChecksum.attributeChecksum &&
                    oldTextChecksum == newChecksum.textChecksum)
                {
                    oldDocumentIdToChecksums.Remove(documentId);
                    newDocumentIdToChecksums.Remove(documentId);
                }
            }

            // sync over the *info* about all the added/changed documents.  We'll want the info so we can determine
            // what actually changed.
            using var _5 = PooledHashSet<Checksum>.GetInstance(out var newChecksumsToSync);
            newChecksumsToSync.AddRange(newDocumentIdToChecksums.Values.Select(v => v.attributeChecksum));

            await _assetProvider.GetAssetsAsync<DocumentInfo.DocumentAttributes>(
                assetPath: new(AssetPathKind.DocumentAttributes, project.Id), newChecksumsToSync, cancellationToken).ConfigureAwait(false);

            newChecksumsToSync.Clear();
            newChecksumsToSync.AddRange(newDocumentIdToChecksums.Values.Select(v => v.textChecksum));

            await _assetProvider.GetAssetsAsync<SerializableSourceText>(
                assetPath: new(AssetPathKind.DocumentText, project.Id), newChecksumsToSync, cancellationToken).ConfigureAwait(false);

            return await UpdateDocumentsAsync(project, addDocuments, removeDocuments, oldDocumentIdToChecksums, newDocumentIdToChecksums, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Project> UpdateDocumentsAsync(
            Project project,
            Func<Solution, ImmutableArray<DocumentInfo>, Solution> addDocuments,
            Func<Solution, ImmutableArray<DocumentId>, Solution> removeDocuments,
            Dictionary<DocumentId, (Checksum attributeChecksum, Checksum textChecksum)> oldDocumentIdToStateChecksums,
            Dictionary<DocumentId, (Checksum attributeChecksum, Checksum textChecksum)> newDocumentIdToStateChecksums,
            CancellationToken cancellationToken)
        {
            // added document
            ImmutableArray<DocumentInfo>.Builder? lazyDocumentsToAdd = null;
            foreach (var (documentId, newDocumentChecksums) in newDocumentIdToStateChecksums)
            {
                if (!oldDocumentIdToStateChecksums.ContainsKey(documentId))
                {
                    lazyDocumentsToAdd ??= ImmutableArray.CreateBuilder<DocumentInfo>();

                    // we have new document added
                    var documentInfo = await _assetProvider.CreateDocumentInfoAsync(
                        documentId, newDocumentChecksums.attributeChecksum, newDocumentChecksums.textChecksum, cancellationToken).ConfigureAwait(false);
                    lazyDocumentsToAdd.Add(documentInfo);
                }
            }

            if (lazyDocumentsToAdd != null)
            {
                project = addDocuments(project.Solution, lazyDocumentsToAdd.ToImmutable()).GetProject(project.Id)!;
            }

            // removed document
            ImmutableArray<DocumentId>.Builder? lazyDocumentsToRemove = null;
            foreach (var (documentId, _) in oldDocumentIdToStateChecksums)
            {
                if (!newDocumentIdToStateChecksums.ContainsKey(documentId))
                {
                    // we have a document removed
                    lazyDocumentsToRemove ??= ImmutableArray.CreateBuilder<DocumentId>();
                    lazyDocumentsToRemove.Add(documentId);
                }
            }

            if (lazyDocumentsToRemove is not null)
            {
                project = removeDocuments(project.Solution, lazyDocumentsToRemove.ToImmutable()).GetProject(project.Id)!;
            }

            // changed document
            foreach (var (documentId, newDocumentChecksums) in newDocumentIdToStateChecksums)
            {
                if (!oldDocumentIdToStateChecksums.TryGetValue(documentId, out var oldDocumentChecksums))
                    continue;

                Contract.ThrowIfTrue(
                    oldDocumentChecksums.attributeChecksum == newDocumentChecksums.attributeChecksum &&
                    oldDocumentChecksums.textChecksum == newDocumentChecksums.textChecksum);

                var document = project.GetDocument(documentId) ?? project.GetAdditionalDocument(documentId) ?? project.GetAnalyzerConfigDocument(documentId);
                Contract.ThrowIfNull(document);

                project = await UpdateDocumentAsync(document, oldDocumentChecksums, newDocumentChecksums, cancellationToken).ConfigureAwait(false);
            }

            return project;
        }

        private async Task<Project> UpdateDocumentAsync(
            TextDocument document,
            (Checksum attributeChecksum, Checksum textChecksum) oldDocumentChecksums,
            (Checksum attributeChecksum, Checksum textChecksum) newDocumentChecksums,
            CancellationToken cancellationToken)
        {
            // changed info
            if (oldDocumentChecksums.attributeChecksum != newDocumentChecksums.attributeChecksum)
            {
                document = await UpdateDocumentInfoAsync(document, newDocumentChecksums.attributeChecksum, cancellationToken).ConfigureAwait(false);
            }

            // changed text
            if (oldDocumentChecksums.textChecksum != newDocumentChecksums.textChecksum)
            {
                var serializableSourceText = await _assetProvider.GetAssetAsync<SerializableSourceText>(
                    assetPath: document.Id, newDocumentChecksums.textChecksum, cancellationToken).ConfigureAwait(false);
                var loader = serializableSourceText.ToTextLoader(document.FilePath);
                var mode = PreservationMode.PreserveValue;

                document = document.Kind switch
                {
                    TextDocumentKind.Document => document.Project.Solution.WithDocumentTextLoader(document.Id, loader, mode).GetRequiredDocument(document.Id),
                    TextDocumentKind.AnalyzerConfigDocument => document.Project.Solution.WithAnalyzerConfigDocumentTextLoader(document.Id, loader, mode).GetRequiredAnalyzerConfigDocument(document.Id),
                    TextDocumentKind.AdditionalDocument => document.Project.Solution.WithAdditionalDocumentTextLoader(document.Id, loader, mode).GetRequiredAdditionalDocument(document.Id),
                    _ => throw ExceptionUtilities.UnexpectedValue(document.Kind),
                };
            }

            return document.Project;
        }

        private async Task<TextDocument> UpdateDocumentInfoAsync(TextDocument document, Checksum infoChecksum, CancellationToken cancellationToken)
        {
            var newDocumentInfo = await _assetProvider.GetAssetAsync<DocumentInfo.DocumentAttributes>(
                assetPath: document.Id, infoChecksum, cancellationToken).ConfigureAwait(false);

            // there is no api to change these once document is created
            Contract.ThrowIfFalse(document.State.Attributes.Id == newDocumentInfo.Id);
            Contract.ThrowIfFalse(document.State.Attributes.Name == newDocumentInfo.Name);
            Contract.ThrowIfFalse(document.State.Attributes.FilePath == newDocumentInfo.FilePath);
            Contract.ThrowIfFalse(document.State.Attributes.IsGenerated == newDocumentInfo.IsGenerated);
            Contract.ThrowIfFalse(document.State.Attributes.DesignTimeOnly == newDocumentInfo.DesignTimeOnly);

            if (document.State.Attributes.Folders != newDocumentInfo.Folders)
            {
                // additional document can't change folder once created
                Contract.ThrowIfFalse(document is Document);
                document = document.Project.Solution.WithDocumentFolders(document.Id, newDocumentInfo.Folders).GetDocument(document.Id)!;
            }

            if (document.State.Attributes.SourceCodeKind != newDocumentInfo.SourceCodeKind)
            {
                // additional document can't change sourcecode kind once created
                Contract.ThrowIfFalse(document is Document);
                document = document.Project.Solution.WithDocumentSourceCodeKind(document.Id, newDocumentInfo.SourceCodeKind).GetDocument(document.Id)!;
            }

            return document;
        }

#if DEBUG
        private async Task ValidateChecksumAsync(
            Checksum checksumFromRequest,
            Solution incrementalSolutionBuilt,
            ProjectId? projectConeId,
            CancellationToken cancellationToken)
        {
            // In the case of a cone sync, we only want to compare the checksum of the cone sync'ed over to the
            // current checksum of that same cone. What is outside of those cones is totally allowed to be
            // different.
            //
            // Note: this is acceptable because that's the contract of a cone sync.  Features themselves are not
            // allowed to cone-sync and then do anything that needs host/remote invariants outside of that cone.
            var currentSolutionChecksum = projectConeId == null
                ? await incrementalSolutionBuilt.CompilationState.GetChecksumAsync(cancellationToken).ConfigureAwait(false)
                : await incrementalSolutionBuilt.CompilationState.GetChecksumAsync(projectConeId, cancellationToken).ConfigureAwait(false);

            if (checksumFromRequest == currentSolutionChecksum)
                return;

            var solutionInfo = await _assetProvider.CreateSolutionInfoAsync(checksumFromRequest, cancellationToken).ConfigureAwait(false);
            var workspace = new AdhocWorkspace(_workspace.Services.HostServices);
            workspace.AddSolution(solutionInfo);

            await TestUtils.AssertChecksumsAsync(_assetProvider, checksumFromRequest, workspace.CurrentSolution, incrementalSolutionBuilt, projectConeId).ConfigureAwait(false);
        }
#endif
    }
}
