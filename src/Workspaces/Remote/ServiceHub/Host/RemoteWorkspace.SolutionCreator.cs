// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteWorkspace
    {
        /// <summary>
        /// Create solution for given checksum from base solution
        /// </summary>
        private readonly struct SolutionCreator(HostServices hostServices, AssetProvider assetService, Solution baseSolution)
        {
#pragma warning disable IDE0052 // used only in DEBUG builds
            private readonly HostServices _hostServices = hostServices;
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
                        assetPath: AssetPath.SolutionOnly, newSolutionChecksum, cancellationToken).ConfigureAwait(false);
                    var newSolutionChecksums = await _assetProvider.GetAssetAsync<SolutionStateChecksums>(
                        assetPath: AssetPath.SolutionOnly, newSolutionCompilationChecksums.SolutionState, cancellationToken).ConfigureAwait(false);

                    var oldSolutionCompilationChecksums = await solution.CompilationState.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                    var oldSolutionChecksums = await solution.CompilationState.SolutionState.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);

                    if (oldSolutionChecksums.Attributes != newSolutionChecksums.Attributes)
                    {
                        var newSolutionInfo = await _assetProvider.GetAssetAsync<SolutionInfo.SolutionAttributes>(
                            assetPath: AssetPath.SolutionOnly, newSolutionChecksums.Attributes, cancellationToken).ConfigureAwait(false);

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
                        solution = solution.WithAnalyzerReferences(await _assetProvider.GetAssetsAsync<AnalyzerReference>(
                            assetPath: AssetPath.SolutionOnly, newSolutionChecksums.AnalyzerReferences, cancellationToken).ConfigureAwait(false));
                    }

                    if (newSolutionCompilationChecksums.FrozenSourceGeneratedDocumentIdentities.HasValue &&
                        newSolutionCompilationChecksums.FrozenSourceGeneratedDocuments.HasValue &&
                        !newSolutionCompilationChecksums.FrozenSourceGeneratedDocumentGenerationDateTimes.IsDefault)
                    {
                        var count = newSolutionCompilationChecksums.FrozenSourceGeneratedDocumentIdentities.Value.Count;
                        var _ = ArrayBuilder<(SourceGeneratedDocumentIdentity identity, DateTime generationDateTime, SourceText text)>.GetInstance(count, out var frozenDocuments);

                        for (var i = 0; i < count; i++)
                        {
                            var identity = await _assetProvider.GetAssetAsync<SourceGeneratedDocumentIdentity>(
                                assetPath: AssetPath.SolutionOnly, newSolutionCompilationChecksums.FrozenSourceGeneratedDocumentIdentities.Value[i], cancellationToken).ConfigureAwait(false);

                            var documentStateChecksums = await _assetProvider.GetAssetAsync<DocumentStateChecksums>(
                                assetPath: AssetPath.SolutionOnly, newSolutionCompilationChecksums.FrozenSourceGeneratedDocuments.Value.Checksums[i], cancellationToken).ConfigureAwait(false);

                            var serializableSourceText = await _assetProvider.GetAssetAsync<SerializableSourceText>(assetPath: newSolutionCompilationChecksums.FrozenSourceGeneratedDocuments.Value.Ids[i], documentStateChecksums.Text, cancellationToken).ConfigureAwait(false);

                            var generationDateTime = newSolutionCompilationChecksums.FrozenSourceGeneratedDocumentGenerationDateTimes[i];
                            var text = await serializableSourceText.GetTextAsync(cancellationToken).ConfigureAwait(false);
                            frozenDocuments.Add((identity, generationDateTime, text));
                        }

                        solution = solution.WithFrozenSourceGeneratedDocuments(frozenDocuments.ToImmutable());
                    }

                    if (oldSolutionCompilationChecksums.SourceGeneratorExecutionVersionMap !=
                        newSolutionCompilationChecksums.SourceGeneratorExecutionVersionMap)
                    {
                        var newVersions = await _assetProvider.GetAssetAsync<SourceGeneratorExecutionVersionMap>(
                            assetPath: AssetPath.SolutionOnly, newSolutionCompilationChecksums.SourceGeneratorExecutionVersionMap, cancellationToken).ConfigureAwait(false);

                        // The execution version map will be for the entire solution on the host side.  However, we may
                        // only be syncing over a partial cone.  In that case, filter down the version map we apply to
                        // the local solution to only be for that cone as well.
                        newVersions = FilterToProjectCone(newVersions, newSolutionChecksums.ProjectCone);
                        solution = solution.WithSourceGeneratorExecutionVersions(newVersions, cancellationToken);
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

                static SourceGeneratorExecutionVersionMap FilterToProjectCone(SourceGeneratorExecutionVersionMap map, ProjectCone? projectCone)
                {
                    if (projectCone is null)
                        return map;

                    var builder = map.Map.ToBuilder();
                    foreach (var (projectId, _) in map.Map)
                    {
                        if (!projectCone.Contains(projectId))
                            builder.Remove(projectId);
                    }

                    return new(builder.ToImmutable());
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

                await _assetProvider.GetAssetsAsync<ProjectStateChecksums, Dictionary<ProjectId, ProjectStateChecksums>>(
                    assetPath: AssetPath.SolutionAndTopLevelProjectsOnly, newChecksumsToSync,
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
                // Note: it's common to see a whole lot of project-infos change.  So attempt to collect that in one go
                // if we can.
                using var _ = PooledHashSet<Checksum>.GetInstance(out var projectInfoChecksums);
                foreach (var (projectId, newProjectChecksums) in newProjectIdToStateChecksums)
                    projectInfoChecksums.Add(newProjectChecksums.Info);

                await _assetProvider.GetAssetsAsync<ProjectInfo.ProjectAttributes, VoidResult>(
                    assetPath: AssetPath.SolutionAndTopLevelProjectsOnly, projectInfoChecksums, callback: null, arg: default, cancellationToken).ConfigureAwait(false);

                // added project
                foreach (var (projectId, newProjectChecksums) in newProjectIdToStateChecksums)
                {
                    if (!oldProjectIdToStateChecksums.ContainsKey(projectId))
                    {
                        // bulk sync added project assets fully since we'll definitely need that data, and we won't want
                        // to make tons of intermediary calls for it.

                        await _assetProvider.SynchronizeProjectAssetsAsync(newProjectChecksums, cancellationToken).ConfigureAwait(false);
                        var projectInfo = await _assetProvider.CreateProjectInfoAsync(projectId, newProjectChecksums.Checksum, cancellationToken).ConfigureAwait(false);
                        solution = solution.AddProject(projectInfo);
                    }
                }

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

                // removed project
                foreach (var (projectId, _) in oldProjectIdToStateChecksums)
                {
                    if (!newProjectIdToStateChecksums.ContainsKey(projectId))
                    {
                        // Should never be removing projects during cone syncing.
                        Contract.ThrowIfTrue(isConeSync);
                        solution = solution.RemoveProject(projectId);
                    }
                }

                // changed project
                foreach (var (projectId, newProjectChecksums) in newProjectIdToStateChecksums)
                {
                    if (oldProjectIdToStateChecksums.TryGetValue(projectId, out var oldProjectChecksums))
                    {
                        // If this project was in the old map, then the project must have changed.  Otherwise, we would
                        // have removed it earlier on.
                        Contract.ThrowIfTrue(oldProjectChecksums.Checksum == newProjectChecksums.Checksum);
                        solution = await UpdateProjectAsync(
                            solution.GetRequiredProject(projectId), oldProjectChecksums, newProjectChecksums, cancellationToken).ConfigureAwait(false);
                    }
                }

                return solution;
            }

            private async Task<Solution> UpdateProjectAsync(Project project, ProjectStateChecksums oldProjectChecksums, ProjectStateChecksums newProjectChecksums, CancellationToken cancellationToken)
            {
                // changed info
                if (oldProjectChecksums.Info != newProjectChecksums.Info)
                {
                    project = await UpdateProjectInfoAsync(project, newProjectChecksums.Info, cancellationToken).ConfigureAwait(false);
                }

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
                    project = project.WithProjectReferences(await _assetProvider.GetAssetsAsync<ProjectReference>(
                        assetPath: project.Id, newProjectChecksums.ProjectReferences, cancellationToken).ConfigureAwait(false));
                }

                // changed metadata references
                if (oldProjectChecksums.MetadataReferences.Checksum != newProjectChecksums.MetadataReferences.Checksum)
                {
                    project = project.WithMetadataReferences(await _assetProvider.GetAssetsAsync<MetadataReference>(
                        assetPath: project.Id, newProjectChecksums.MetadataReferences, cancellationToken).ConfigureAwait(false));
                }

                // changed analyzer references
                if (oldProjectChecksums.AnalyzerReferences.Checksum != newProjectChecksums.AnalyzerReferences.Checksum)
                {
                    project = project.WithAnalyzerReferences(await _assetProvider.GetAssetsAsync<AnalyzerReference>(
                        assetPath: project.Id, newProjectChecksums.AnalyzerReferences, cancellationToken).ConfigureAwait(false));
                }

                // changed analyzer references
                if (oldProjectChecksums.Documents.Checksum != newProjectChecksums.Documents.Checksum)
                {
                    project = await UpdateDocumentsAsync(
                        project,
                        newProjectChecksums,
                        project.State.DocumentStates,
                        oldProjectChecksums.Documents,
                        newProjectChecksums.Documents,
                        static (solution, documents) => solution.AddDocuments(documents),
                        static (solution, documentIds) => solution.RemoveDocuments(documentIds),
                        cancellationToken).ConfigureAwait(false);
                }

                // changed additional documents
                if (oldProjectChecksums.AdditionalDocuments.Checksum != newProjectChecksums.AdditionalDocuments.Checksum)
                {
                    project = await UpdateDocumentsAsync(
                        project,
                        newProjectChecksums,
                        project.State.AdditionalDocumentStates,
                        oldProjectChecksums.AdditionalDocuments,
                        newProjectChecksums.AdditionalDocuments,
                        static (solution, documents) => solution.AddAdditionalDocuments(documents),
                        static (solution, documentIds) => solution.RemoveAdditionalDocuments(documentIds),
                        cancellationToken).ConfigureAwait(false);
                }

                // changed analyzer config documents
                if (oldProjectChecksums.AnalyzerConfigDocuments.Checksum != newProjectChecksums.AnalyzerConfigDocuments.Checksum)
                {
                    project = await UpdateDocumentsAsync(
                        project,
                        newProjectChecksums,
                        project.State.AnalyzerConfigDocumentStates,
                        oldProjectChecksums.AnalyzerConfigDocuments,
                        newProjectChecksums.AnalyzerConfigDocuments,
                        static (solution, documents) => solution.AddAnalyzerConfigDocuments(documents),
                        static (solution, documentIds) => solution.RemoveAnalyzerConfigDocuments(documentIds),
                        cancellationToken).ConfigureAwait(false);
                }

                return project.Solution;
            }

            private async Task<Project> UpdateProjectInfoAsync(Project project, Checksum infoChecksum, CancellationToken cancellationToken)
            {
                var newProjectAttributes = await _assetProvider.GetAssetAsync<ProjectInfo.ProjectAttributes>(
                    assetPath: project.Id, infoChecksum, cancellationToken).ConfigureAwait(false);

                // there is no API to change these once project is created
                Contract.ThrowIfFalse(project.State.ProjectInfo.Attributes.Id == newProjectAttributes.Id);
                Contract.ThrowIfFalse(project.State.ProjectInfo.Attributes.Language == newProjectAttributes.Language);
                Contract.ThrowIfFalse(project.State.ProjectInfo.Attributes.IsSubmission == newProjectAttributes.IsSubmission);

                var projectId = project.Id;

                if (project.State.ProjectInfo.Attributes.Name != newProjectAttributes.Name)
                {
                    project = project.Solution.WithProjectName(projectId, newProjectAttributes.Name).GetRequiredProject(projectId);
                }

                if (project.State.ProjectInfo.Attributes.AssemblyName != newProjectAttributes.AssemblyName)
                {
                    project = project.Solution.WithProjectAssemblyName(projectId, newProjectAttributes.AssemblyName).GetRequiredProject(projectId);
                }

                if (project.State.ProjectInfo.Attributes.FilePath != newProjectAttributes.FilePath)
                {
                    project = project.Solution.WithProjectFilePath(projectId, newProjectAttributes.FilePath).GetRequiredProject(projectId);
                }

                if (project.State.ProjectInfo.Attributes.OutputFilePath != newProjectAttributes.OutputFilePath)
                {
                    project = project.Solution.WithProjectOutputFilePath(projectId, newProjectAttributes.OutputFilePath).GetRequiredProject(projectId);
                }

                if (project.State.ProjectInfo.Attributes.OutputRefFilePath != newProjectAttributes.OutputRefFilePath)
                {
                    project = project.Solution.WithProjectOutputRefFilePath(projectId, newProjectAttributes.OutputRefFilePath).GetRequiredProject(projectId);
                }

                if (project.State.ProjectInfo.Attributes.CompilationOutputInfo != newProjectAttributes.CompilationOutputInfo)
                {
                    project = project.Solution.WithProjectCompilationOutputInfo(project.Id, newProjectAttributes.CompilationOutputInfo).GetRequiredProject(project.Id);
                }

                if (project.State.ProjectInfo.Attributes.DefaultNamespace != newProjectAttributes.DefaultNamespace)
                {
                    project = project.Solution.WithProjectDefaultNamespace(projectId, newProjectAttributes.DefaultNamespace).GetRequiredProject(projectId);
                }

                if (project.State.ProjectInfo.Attributes.HasAllInformation != newProjectAttributes.HasAllInformation)
                {
                    project = project.Solution.WithHasAllInformation(projectId, newProjectAttributes.HasAllInformation).GetRequiredProject(projectId);
                }

                if (project.State.ProjectInfo.Attributes.RunAnalyzers != newProjectAttributes.RunAnalyzers)
                {
                    project = project.Solution.WithRunAnalyzers(projectId, newProjectAttributes.RunAnalyzers).GetRequiredProject(projectId);
                }

                if (project.State.ProjectInfo.Attributes.ChecksumAlgorithm != newProjectAttributes.ChecksumAlgorithm)
                {
                    project = project.Solution.WithProjectChecksumAlgorithm(projectId, newProjectAttributes.ChecksumAlgorithm).GetRequiredProject(projectId);
                }

                return project;
            }

            private async Task<Project> UpdateDocumentsAsync<TDocumentState>(
                Project project,
                ProjectStateChecksums projectChecksums,
                TextDocumentStates<TDocumentState> existingTextDocumentStates,
                ChecksumsAndIds<DocumentId> oldChecksums,
                ChecksumsAndIds<DocumentId> newChecksums,
                Func<Solution, ImmutableArray<DocumentInfo>, Solution> addDocuments,
                Func<Solution, ImmutableArray<DocumentId>, Solution> removeDocuments,
                CancellationToken cancellationToken) where TDocumentState : TextDocumentState
            {
                using var _1 = PooledDictionary<DocumentId, Checksum>.GetInstance(out var oldDocumentIdToChecksum);
                using var _2 = PooledDictionary<DocumentId, Checksum>.GetInstance(out var newDocumentIdToChecksum);

                foreach (var (oldChecksum, documentId) in oldChecksums)
                    oldDocumentIdToChecksum.Add(documentId, oldChecksum);

                foreach (var (newChecksum, documentId) in newChecksums)
                    newDocumentIdToChecksum.Add(documentId, newChecksum);

                // remove documents that are the same on both sides.  We can just iterate over one of the maps as,
                // definitionally, for the project to be on both sides, it will be contained in both.
                foreach (var (oldChecksum, documentId) in oldChecksums)
                {
                    if (newDocumentIdToChecksum.TryGetValue(documentId, out var newChecksum) &&
                        oldChecksum == newChecksum)
                    {
                        oldDocumentIdToChecksum.Remove(documentId);
                        newDocumentIdToChecksum.Remove(documentId);
                    }
                }

                using var _3 = PooledDictionary<DocumentId, DocumentStateChecksums>.GetInstance(out var oldDocumentIdToStateChecksums);
                using var _4 = PooledDictionary<DocumentId, DocumentStateChecksums>.GetInstance(out var newDocumentIdToStateChecksums);

                // Now, find the full state checksums for all the old documents
                foreach (var (documentId, oldChecksum) in oldDocumentIdToChecksum)
                {
                    // this should be cheap since we already computed oldSolutionChecksums (which calls into this).
                    var oldDocumentStateChecksums = await existingTextDocumentStates
                        .GetRequiredState(documentId)
                        .GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                    Contract.ThrowIfTrue(oldDocumentStateChecksums.DocumentId != documentId);
                    Contract.ThrowIfTrue(oldDocumentStateChecksums.Checksum != oldChecksum);

                    oldDocumentIdToStateChecksums.Add(documentId, oldDocumentStateChecksums);
                }

                // sync over the *info* about all the added/changed documents.  We'll want the info so we can determine
                // what actually changed.
                using var _5 = PooledHashSet<Checksum>.GetInstance(out var newChecksumsToSync);
                newChecksumsToSync.AddRange(newDocumentIdToChecksum.Values);

                await _assetProvider.GetAssetsAsync<DocumentStateChecksums, Dictionary<DocumentId, DocumentStateChecksums>>(
                    assetPath: AssetPath.ProjectAndDocuments(project.Id), newChecksumsToSync,
                    static (checksum, documentStateChecksum, newDocumentIdToStateChecksums) =>
                    {
                        Contract.ThrowIfTrue(checksum != documentStateChecksum.Checksum);
                        newDocumentIdToStateChecksums.Add(documentStateChecksum.DocumentId, documentStateChecksum);
                    },
                    arg: newDocumentIdToStateChecksums,
                    cancellationToken).ConfigureAwait(false);

                // If more than two documents changed during a single update, perform a bulk synchronization on the
                // project to avoid large numbers of small synchronization calls during document updates.
                // 🔗 https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1365014
                if (newDocumentIdToStateChecksums.Count > 2)
                {
                    await _assetProvider.SynchronizeProjectAssetsAsync(projectChecksums, cancellationToken).ConfigureAwait(false);
                }

                return await UpdateDocumentsAsync(project, addDocuments, removeDocuments, oldDocumentIdToStateChecksums, newDocumentIdToStateChecksums, cancellationToken).ConfigureAwait(false);
            }

            private async Task<Project> UpdateDocumentsAsync(
                Project project,
                Func<Solution, ImmutableArray<DocumentInfo>, Solution> addDocuments,
                Func<Solution, ImmutableArray<DocumentId>, Solution> removeDocuments,
                Dictionary<DocumentId, DocumentStateChecksums> oldDocumentIdToStateChecksums,
                Dictionary<DocumentId, DocumentStateChecksums> newDocumentIdToStateChecksums,
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
                            documentId, newDocumentChecksums.Checksum, cancellationToken).ConfigureAwait(false);
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
                    {
                        continue;
                    }

                    Contract.ThrowIfTrue(oldDocumentChecksums.Checksum == newDocumentChecksums.Checksum);

                    var document = project.GetDocument(documentId) ?? project.GetAdditionalDocument(documentId) ?? project.GetAnalyzerConfigDocument(documentId);
                    Contract.ThrowIfNull(document);

                    project = await UpdateDocumentAsync(document, oldDocumentChecksums, newDocumentChecksums, cancellationToken).ConfigureAwait(false);
                }

                return project;
            }

            private async Task<Project> UpdateDocumentAsync(TextDocument document, DocumentStateChecksums oldDocumentChecksums, DocumentStateChecksums newDocumentChecksums, CancellationToken cancellationToken)
            {
                // changed info
                if (oldDocumentChecksums.Info != newDocumentChecksums.Info)
                {
                    document = await UpdateDocumentInfoAsync(document, newDocumentChecksums.Info, cancellationToken).ConfigureAwait(false);
                }

                // changed text
                if (oldDocumentChecksums.Text != newDocumentChecksums.Text)
                {
                    var serializableSourceText = await _assetProvider.GetAssetAsync<SerializableSourceText>(
                        assetPath: document.Id, newDocumentChecksums.Text, cancellationToken).ConfigureAwait(false);
                    var sourceText = await serializableSourceText.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    document = document.Kind switch
                    {
                        TextDocumentKind.Document => document.Project.Solution.WithDocumentText(document.Id, sourceText).GetDocument(document.Id)!,
                        TextDocumentKind.AnalyzerConfigDocument => document.Project.Solution.WithAnalyzerConfigDocumentText(document.Id, sourceText).GetAnalyzerConfigDocument(document.Id)!,
                        TextDocumentKind.AdditionalDocument => document.Project.Solution.WithAdditionalDocumentText(document.Id, sourceText).GetAdditionalDocument(document.Id)!,
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
                var workspace = new AdhocWorkspace(_hostServices);
                workspace.AddSolution(solutionInfo);

                await TestUtils.AssertChecksumsAsync(_assetProvider, checksumFromRequest, workspace.CurrentSolution, incrementalSolutionBuilt).ConfigureAwait(false);
            }
#endif
        }
    }
}
