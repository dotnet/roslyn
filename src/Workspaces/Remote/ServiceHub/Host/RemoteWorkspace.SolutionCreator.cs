// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;

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

            public async Task<bool> IsIncrementalUpdateAsync(Checksum newSolutionChecksum, CancellationToken cancellationToken)
            {
                var newSolutionChecksums = await _assetProvider.GetAssetAsync<SolutionStateChecksums>(
                    assetHint: AssetHint.None, newSolutionChecksum, cancellationToken).ConfigureAwait(false);
                var newSolutionInfo = await _assetProvider.GetAssetAsync<SolutionInfo.SolutionAttributes>(
                    assetHint: AssetHint.None, newSolutionChecksums.Attributes, cancellationToken).ConfigureAwait(false);

                // if either solution id or file path changed, then we consider it as new solution
                return _baseSolution.Id == newSolutionInfo.Id && _baseSolution.FilePath == newSolutionInfo.FilePath;
            }

            public async Task<Solution> CreateSolutionAsync(Checksum newSolutionChecksum, CancellationToken cancellationToken)
            {
                try
                {
                    var solution = _baseSolution;

                    // If we previously froze a source generated document and then held onto that, unfreeze it now. We'll re-freeze the new document
                    // if needed again later.
                    solution = solution.WithoutFrozenSourceGeneratedDocuments();

                    var oldSolutionChecksums = await solution.CompilationState.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                    var newSolutionChecksums = await _assetProvider.GetAssetAsync<SolutionStateChecksums>(
                        assetHint: AssetHint.None, newSolutionChecksum, cancellationToken).ConfigureAwait(false);

                    if (oldSolutionChecksums.Attributes != newSolutionChecksums.Attributes)
                    {
                        var newSolutionInfo = await _assetProvider.GetAssetAsync<SolutionInfo.SolutionAttributes>(
                            assetHint: AssetHint.None, newSolutionChecksums.Attributes, cancellationToken).ConfigureAwait(false);

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
                        solution = solution.WithAnalyzerReferences(await _assetProvider.CreateCollectionAsync<AnalyzerReference>(
                            assetHint: AssetHint.None, newSolutionChecksums.AnalyzerReferences, cancellationToken).ConfigureAwait(false));
                    }

                    if (newSolutionChecksums.FrozenSourceGeneratedDocumentIdentity != Checksum.Null && newSolutionChecksums.FrozenSourceGeneratedDocumentText != Checksum.Null)
                    {
                        var identity = await _assetProvider.GetAssetAsync<SourceGeneratedDocumentIdentity>(
                            assetHint: AssetHint.None, newSolutionChecksums.FrozenSourceGeneratedDocumentIdentity, cancellationToken).ConfigureAwait(false);
                        var serializableSourceText = await _assetProvider.GetAssetAsync<SerializableSourceText>(
                            assetHint: AssetHint.None, newSolutionChecksums.FrozenSourceGeneratedDocumentText, cancellationToken).ConfigureAwait(false);

                        var sourceText = await serializableSourceText.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        solution = solution.WithFrozenSourceGeneratedDocument(identity, sourceText).Project.Solution;
                    }

#if DEBUG
                    // make sure created solution has same checksum as given one
                    await ValidateChecksumAsync(newSolutionChecksum, solution, cancellationToken).ConfigureAwait(false);
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
                using var olds = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();
                using var news = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();

                olds.Object.UnionWith(oldSolutionChecksums.Projects.Checksums);
                news.Object.UnionWith(newSolutionChecksums.Projects.Checksums);

                // remove projects that exist in both side
                olds.Object.ExceptWith(newSolutionChecksums.Projects.Checksums);
                news.Object.ExceptWith(oldSolutionChecksums.Projects.Checksums);

                using var _1 = PooledDictionary<ProjectId, ProjectStateChecksums>.GetInstance(out var oldMap);
                using var _2 = PooledDictionary<ProjectId, ProjectStateChecksums>.GetInstance(out var newMap);

                await PopulateOldProjectMapAsync().ConfigureAwait(false);
                await PopulateNewProjectMapAsync(this).ConfigureAwait(false);

                // bulk sync assets
                await SynchronizeAssetsAsync(oldMap, newMap, cancellationToken).ConfigureAwait(false);

                // added project
                foreach (var (projectId, newProjectChecksums) in newMap)
                {
                    if (!oldMap.ContainsKey(projectId))
                    {
                        var projectInfo = await _assetProvider.CreateProjectInfoAsync(projectId, newProjectChecksums.Checksum, cancellationToken).ConfigureAwait(false);
                        if (projectInfo == null)
                        {
                            // this project is not supported in OOP
                            continue;
                        }

                        // we have new project added
                        solution = solution.AddProject(projectInfo);
                    }
                }

                // remove all project references from projects that changed. this ensures exceptions will not occur for
                // cyclic references during an incremental update.
                foreach (var (projectId, newProjectChecksums) in newMap)
                {
                    if (!oldMap.TryGetValue(projectId, out var oldProjectChecksums))
                    {
                        continue;
                    }

                    if (oldProjectChecksums.ProjectReferences.Checksum != newProjectChecksums.ProjectReferences.Checksum)
                    {
                        solution = solution.WithProjectReferences(projectId, SpecializedCollections.EmptyEnumerable<ProjectReference>());
                    }
                }

                // removed project
                foreach (var (projectId, _) in oldMap)
                {
                    if (!newMap.ContainsKey(projectId))
                    {
                        // we have a project removed
                        solution = solution.RemoveProject(projectId);
                    }
                }

                // changed project
                foreach (var (projectId, newProjectChecksums) in newMap)
                {
                    if (!oldMap.TryGetValue(projectId, out var oldProjectChecksums))
                    {
                        continue;
                    }

                    Contract.ThrowIfTrue(oldProjectChecksums.Checksum == newProjectChecksums.Checksum);

                    solution = await UpdateProjectAsync(solution.GetRequiredProject(projectId), oldProjectChecksums, newProjectChecksums, cancellationToken).ConfigureAwait(false);
                }

                return solution;

                async Task PopulateOldProjectMapAsync()
                {
                    foreach (var (projectId, projectState) in solution.SolutionState.ProjectStates)
                    {
                        var projectChecksums = await projectState.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                        if (olds.Object.Contains(projectChecksums.Checksum))
                            oldMap.Add(projectId, projectChecksums);
                    }
                }

                async Task PopulateNewProjectMapAsync(SolutionCreator @this)
                {
                    var projectStateChecksums = await @this._assetProvider.GetAssetsAsync<ProjectStateChecksums>(
                        assetHint: AssetHint.None, news.Object, cancellationToken).ConfigureAwait(false);

                    foreach (var (_, projectStateChecksum) in projectStateChecksums)
                        newMap.Add(projectStateChecksum.ProjectId, projectStateChecksum);
                }
            }

            private async ValueTask SynchronizeAssetsAsync(Dictionary<ProjectId, ProjectStateChecksums> oldMap, Dictionary<ProjectId, ProjectStateChecksums> newMap, CancellationToken cancellationToken)
            {
                // added project
                foreach (var (projectId, projectStateChecksums) in newMap)
                {
                    if (oldMap.ContainsKey(projectId))
                        continue;

                    await _assetProvider.SynchronizeProjectAssetsAsync(projectStateChecksums, cancellationToken).ConfigureAwait(false);
                }
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
                                assetHint: project.Id, newProjectChecksums.CompilationOptions, cancellationToken).ConfigureAwait(false)));
                }

                // changed parse options
                if (oldProjectChecksums.ParseOptions != newProjectChecksums.ParseOptions)
                {
                    project = project.WithParseOptions(await _assetProvider.GetAssetAsync<ParseOptions>(
                        assetHint: project.Id, newProjectChecksums.ParseOptions, cancellationToken).ConfigureAwait(false));
                }

                // changed project references
                if (oldProjectChecksums.ProjectReferences.Checksum != newProjectChecksums.ProjectReferences.Checksum)
                {
                    project = project.WithProjectReferences(await _assetProvider.CreateCollectionAsync<ProjectReference>(
                        assetHint: project.Id, newProjectChecksums.ProjectReferences, cancellationToken).ConfigureAwait(false));
                }

                // changed metadata references
                if (oldProjectChecksums.MetadataReferences.Checksum != newProjectChecksums.MetadataReferences.Checksum)
                {
                    project = project.WithMetadataReferences(await _assetProvider.CreateCollectionAsync<MetadataReference>(
                        assetHint: project.Id, newProjectChecksums.MetadataReferences, cancellationToken).ConfigureAwait(false));
                }

                // changed analyzer references
                if (oldProjectChecksums.AnalyzerReferences.Checksum != newProjectChecksums.AnalyzerReferences.Checksum)
                {
                    project = project.WithAnalyzerReferences(await _assetProvider.CreateCollectionAsync<AnalyzerReference>(
                        assetHint: project.Id, newProjectChecksums.AnalyzerReferences, cancellationToken).ConfigureAwait(false));
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
                    assetHint: project.Id, infoChecksum, cancellationToken).ConfigureAwait(false);

                // there is no API to change these once project is created
                Contract.ThrowIfFalse(project.State.ProjectInfo.Attributes.Id == newProjectAttributes.Id);
                Contract.ThrowIfFalse(project.State.ProjectInfo.Attributes.Language == newProjectAttributes.Language);
                Contract.ThrowIfFalse(project.State.ProjectInfo.Attributes.IsSubmission == newProjectAttributes.IsSubmission);

                var projectId = project.Id;

                if (project.State.ProjectInfo.Attributes.Name != newProjectAttributes.Name)
                {
                    project = project.Solution.WithProjectName(projectId, newProjectAttributes.Name).GetProject(projectId)!;
                }

                if (project.State.ProjectInfo.Attributes.AssemblyName != newProjectAttributes.AssemblyName)
                {
                    project = project.Solution.WithProjectAssemblyName(projectId, newProjectAttributes.AssemblyName).GetProject(projectId)!;
                }

                if (project.State.ProjectInfo.Attributes.FilePath != newProjectAttributes.FilePath)
                {
                    project = project.Solution.WithProjectFilePath(projectId, newProjectAttributes.FilePath).GetProject(projectId)!;
                }

                if (project.State.ProjectInfo.Attributes.OutputFilePath != newProjectAttributes.OutputFilePath)
                {
                    project = project.Solution.WithProjectOutputFilePath(projectId, newProjectAttributes.OutputFilePath).GetProject(projectId)!;
                }

                if (project.State.ProjectInfo.Attributes.OutputRefFilePath != newProjectAttributes.OutputRefFilePath)
                {
                    project = project.Solution.WithProjectOutputRefFilePath(projectId, newProjectAttributes.OutputRefFilePath).GetProject(projectId)!;
                }

                if (project.State.ProjectInfo.Attributes.CompilationOutputInfo != newProjectAttributes.CompilationOutputInfo)
                {
                    project = project.Solution.WithProjectCompilationOutputInfo(project.Id, newProjectAttributes.CompilationOutputInfo).GetProject(project.Id)!;
                }

                if (project.State.ProjectInfo.Attributes.DefaultNamespace != newProjectAttributes.DefaultNamespace)
                {
                    project = project.Solution.WithProjectDefaultNamespace(projectId, newProjectAttributes.DefaultNamespace).GetProject(projectId)!;
                }

                if (project.State.ProjectInfo.Attributes.HasAllInformation != newProjectAttributes.HasAllInformation)
                {
                    project = project.Solution.WithHasAllInformation(projectId, newProjectAttributes.HasAllInformation).GetProject(projectId)!;
                }

                if (project.State.ProjectInfo.Attributes.RunAnalyzers != newProjectAttributes.RunAnalyzers)
                {
                    project = project.Solution.WithRunAnalyzers(projectId, newProjectAttributes.RunAnalyzers).GetProject(projectId)!;
                }

                if (project.State.ProjectInfo.Attributes.ChecksumAlgorithm != newProjectAttributes.ChecksumAlgorithm)
                {
                    project = project.Solution.WithProjectChecksumAlgorithm(projectId, newProjectAttributes.ChecksumAlgorithm).GetProject(projectId)!;
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
                using var olds = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();
                using var news = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();

                olds.Object.UnionWith(oldChecksums.Checksums);
                news.Object.UnionWith(newChecksums.Checksums);

                // remove documents that exist in both side
                olds.Object.ExceptWith(newChecksums.Checksums);
                news.Object.ExceptWith(oldChecksums.Checksums);

                using var _1 = PooledDictionary<DocumentId, DocumentStateChecksums>.GetInstance(out var oldMap);
                using var _2 = PooledDictionary<DocumentId, DocumentStateChecksums>.GetInstance(out var newMap);

                await PopulateOldDocumentMapAsync().ConfigureAwait(false);
                await PopulateNewDocumentMapAsync(this).ConfigureAwait(false);

                // If more than two documents changed during a single update, perform a bulk synchronization on the
                // project to avoid large numbers of small synchronization calls during document updates.
                // 🔗 https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1365014
                if (newMap.Count > 2)
                {
                    await _assetProvider.SynchronizeProjectAssetsAsync(projectChecksums, cancellationToken).ConfigureAwait(false);
                }

                // added document
                ImmutableArray<DocumentInfo>.Builder? lazyDocumentsToAdd = null;
                foreach (var (documentId, newDocumentChecksums) in newMap)
                {
                    if (!oldMap.ContainsKey(documentId))
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

                // changed document
                foreach (var (documentId, newDocumentChecksums) in newMap)
                {
                    if (!oldMap.TryGetValue(documentId, out var oldDocumentChecksums))
                    {
                        continue;
                    }

                    Contract.ThrowIfTrue(oldDocumentChecksums.Checksum == newDocumentChecksums.Checksum);

                    var document = project.GetDocument(documentId) ?? project.GetAdditionalDocument(documentId) ?? project.GetAnalyzerConfigDocument(documentId);
                    Contract.ThrowIfNull(document);

                    project = await UpdateDocumentAsync(document, oldDocumentChecksums, newDocumentChecksums, cancellationToken).ConfigureAwait(false);
                }

                // removed document
                ImmutableArray<DocumentId>.Builder? lazyDocumentsToRemove = null;
                foreach (var (documentId, _) in oldMap)
                {
                    if (!newMap.ContainsKey(documentId))
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

                return project;

                async Task PopulateOldDocumentMapAsync()
                {
                    foreach (var (_, state) in existingTextDocumentStates.States)
                    {
                        var documentChecksums = await state.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                        if (olds.Object.Contains(documentChecksums.Checksum))
                            oldMap.Add(state.Id, documentChecksums);
                    }
                }

                async Task PopulateNewDocumentMapAsync(SolutionCreator @this)
                {
                    var documentStateChecksums = await @this._assetProvider.GetAssetsAsync<DocumentStateChecksums>(
                        assetHint: project.Id, news.Object, cancellationToken).ConfigureAwait(false);

                    foreach (var (_, documentStateChecksum) in documentStateChecksums)
                        newMap.Add(documentStateChecksum.DocumentId, documentStateChecksum);
                }
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
                        assetHint: document.Id, newDocumentChecksums.Text, cancellationToken).ConfigureAwait(false);
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
                    assetHint: document.Id, infoChecksum, cancellationToken).ConfigureAwait(false);

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
            private async Task ValidateChecksumAsync(Checksum checksumFromRequest, Solution incrementalSolutionBuilt, CancellationToken cancellationToken)
            {
                var currentSolutionChecksum = await incrementalSolutionBuilt.CompilationState.GetChecksumAsync(CancellationToken.None).ConfigureAwait(false);
                if (checksumFromRequest == currentSolutionChecksum)
                {
                    return;
                }

                var solutionInfo = await _assetProvider.CreateSolutionInfoAsync(checksumFromRequest, cancellationToken).ConfigureAwait(false);
                var workspace = new AdhocWorkspace(_hostServices);
                workspace.AddSolution(solutionInfo);

                await TestUtils.AssertChecksumsAsync(_assetProvider, checksumFromRequest, workspace.CurrentSolution, incrementalSolutionBuilt).ConfigureAwait(false);
            }
#endif
        }
    }
}
