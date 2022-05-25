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

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteWorkspace
    {
        /// <summary>
        /// Create solution for given checksum from base solution
        /// </summary>
        private readonly struct SolutionCreator
        {
#pragma warning disable IDE0052 // used only in DEBUG builds
            private readonly HostServices _hostServices;
#pragma warning restore

            private readonly AssetProvider _assetProvider;
            private readonly Solution _baseSolution;
            private readonly CancellationToken _cancellationToken;

            public SolutionCreator(HostServices hostServices, AssetProvider assetService, Solution baseSolution, CancellationToken cancellationToken)
            {
                _hostServices = hostServices;
                _assetProvider = assetService;
                _baseSolution = baseSolution;
                _cancellationToken = cancellationToken;
            }

            public async Task<bool> IsIncrementalUpdateAsync(Checksum newSolutionChecksum)
            {
                var newSolutionChecksums = await _assetProvider.GetAssetAsync<SolutionStateChecksums>(newSolutionChecksum, _cancellationToken).ConfigureAwait(false);
                var newSolutionInfo = await _assetProvider.GetAssetAsync<SolutionInfo.SolutionAttributes>(newSolutionChecksums.Attributes, _cancellationToken).ConfigureAwait(false);

                // if either solution id or file path changed, then we consider it as new solution
                return _baseSolution.Id == newSolutionInfo.Id && _baseSolution.FilePath == newSolutionInfo.FilePath;
            }

            public async Task<Solution> CreateSolutionAsync(Checksum newSolutionChecksum)
            {
                try
                {
                    var solution = _baseSolution;

                    // If we previously froze a source generated document and then held onto that, unfreeze it now. We'll re-freeze the new document
                    // if needed again later.
                    solution = solution.WithoutFrozenSourceGeneratedDocuments();

                    var oldSolutionChecksums = await solution.State.GetStateChecksumsAsync(_cancellationToken).ConfigureAwait(false);
                    var newSolutionChecksums = await _assetProvider.GetAssetAsync<SolutionStateChecksums>(newSolutionChecksum, _cancellationToken).ConfigureAwait(false);

                    if (oldSolutionChecksums.Attributes != newSolutionChecksums.Attributes)
                    {
                        var newSolutionInfo = await _assetProvider.GetAssetAsync<SolutionInfo.SolutionAttributes>(newSolutionChecksums.Attributes, _cancellationToken).ConfigureAwait(false);

                        // if either id or file path has changed, then this is not update
                        Contract.ThrowIfFalse(solution.Id == newSolutionInfo.Id && solution.FilePath == newSolutionInfo.FilePath);
                    }

                    if (oldSolutionChecksums.Options != newSolutionChecksums.Options)
                    {
                        var newOptions = await _assetProvider.GetAssetAsync<SerializableOptionSet>(newSolutionChecksums.Options, _cancellationToken).ConfigureAwait(false);
                        solution = solution.WithOptions(newOptions);
                    }

                    if (oldSolutionChecksums.Projects.Checksum != newSolutionChecksums.Projects.Checksum)
                    {
                        solution = await UpdateProjectsAsync(solution, oldSolutionChecksums.Projects, newSolutionChecksums.Projects).ConfigureAwait(false);
                    }

                    if (oldSolutionChecksums.AnalyzerReferences.Checksum != newSolutionChecksums.AnalyzerReferences.Checksum)
                    {
                        solution = solution.WithAnalyzerReferences(await _assetProvider.CreateCollectionAsync<AnalyzerReference>(
                            newSolutionChecksums.AnalyzerReferences, _cancellationToken).ConfigureAwait(false));
                    }

                    if (newSolutionChecksums.FrozenSourceGeneratedDocumentIdentity != Checksum.Null && newSolutionChecksums.FrozenSourceGeneratedDocumentText != Checksum.Null)
                    {
                        var identity = await _assetProvider.GetAssetAsync<SourceGeneratedDocumentIdentity>(newSolutionChecksums.FrozenSourceGeneratedDocumentIdentity, _cancellationToken).ConfigureAwait(false);
                        var serializableSourceText = await _assetProvider.GetAssetAsync<SerializableSourceText>(newSolutionChecksums.FrozenSourceGeneratedDocumentText, _cancellationToken).ConfigureAwait(false);
                        var sourceText = await serializableSourceText.GetTextAsync(_cancellationToken).ConfigureAwait(false);
                        solution = solution.WithFrozenSourceGeneratedDocument(identity, sourceText).Project.Solution;
                    }

#if DEBUG
                    // make sure created solution has same checksum as given one
                    await ValidateChecksumAsync(newSolutionChecksum, solution).ConfigureAwait(false);
#endif

                    return solution;
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private async Task<Solution> UpdateProjectsAsync(Solution solution, ChecksumCollection oldChecksums, ChecksumCollection newChecksums)
            {
                using var olds = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();
                using var news = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();

                olds.Object.UnionWith(oldChecksums);
                news.Object.UnionWith(newChecksums);

                // remove projects that exist in both side
                olds.Object.ExceptWith(newChecksums);
                news.Object.ExceptWith(oldChecksums);

                return await UpdateProjectsAsync(solution, olds.Object, news.Object).ConfigureAwait(false);
            }

            private async Task<Solution> UpdateProjectsAsync(Solution solution, HashSet<Checksum> oldChecksums, HashSet<Checksum> newChecksums)
            {
                var oldMap = await GetProjectMapAsync(solution, oldChecksums).ConfigureAwait(false);
                var newMap = await GetProjectMapAsync(_assetProvider, newChecksums).ConfigureAwait(false);

                // bulk sync assets
                await SynchronizeAssetsAsync(oldMap, newMap).ConfigureAwait(false);

                // added project
                foreach (var (projectId, newProjectChecksums) in newMap)
                {
                    if (!oldMap.ContainsKey(projectId))
                    {
                        var projectInfo = await _assetProvider.CreateProjectInfoAsync(newProjectChecksums.Checksum, _cancellationToken).ConfigureAwait(false);
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

                    solution = await UpdateProjectAsync(solution.GetProject(projectId)!, oldProjectChecksums, newProjectChecksums).ConfigureAwait(false);
                }

                return solution;
            }

            private async Task SynchronizeAssetsAsync(Dictionary<ProjectId, ProjectStateChecksums> oldMap, Dictionary<ProjectId, ProjectStateChecksums> newMap)
            {
                using var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();

                // added project
                foreach (var kv in newMap)
                {
                    if (oldMap.ContainsKey(kv.Key))
                    {
                        continue;
                    }

                    pooledObject.Object.Add(kv.Value.Checksum);
                }

                await _assetProvider.SynchronizeProjectAssetsAsync(pooledObject.Object, _cancellationToken).ConfigureAwait(false);
            }

            private async Task<Solution> UpdateProjectAsync(Project project, ProjectStateChecksums oldProjectChecksums, ProjectStateChecksums newProjectChecksums)
            {
                // changed info
                if (oldProjectChecksums.Info != newProjectChecksums.Info)
                {
                    project = await UpdateProjectInfoAsync(project, newProjectChecksums.Info).ConfigureAwait(false);
                }

                // changed compilation options
                if (oldProjectChecksums.CompilationOptions != newProjectChecksums.CompilationOptions)
                {
                    project = project.WithCompilationOptions(
                        project.State.ProjectInfo.Attributes.FixUpCompilationOptions(
                            await _assetProvider.GetAssetAsync<CompilationOptions>(
                                newProjectChecksums.CompilationOptions, _cancellationToken).ConfigureAwait(false)));
                }

                // changed parse options
                if (oldProjectChecksums.ParseOptions != newProjectChecksums.ParseOptions)
                {
                    project = project.WithParseOptions(await _assetProvider.GetAssetAsync<ParseOptions>(newProjectChecksums.ParseOptions, _cancellationToken).ConfigureAwait(false));
                }

                // changed project references
                if (oldProjectChecksums.ProjectReferences.Checksum != newProjectChecksums.ProjectReferences.Checksum)
                {
                    project = project.WithProjectReferences(await _assetProvider.CreateCollectionAsync<ProjectReference>(
                        newProjectChecksums.ProjectReferences, _cancellationToken).ConfigureAwait(false));
                }

                // changed metadata references
                if (oldProjectChecksums.MetadataReferences.Checksum != newProjectChecksums.MetadataReferences.Checksum)
                {
                    project = project.WithMetadataReferences(await _assetProvider.CreateCollectionAsync<MetadataReference>(
                        newProjectChecksums.MetadataReferences, _cancellationToken).ConfigureAwait(false));
                }

                // changed analyzer references
                if (oldProjectChecksums.AnalyzerReferences.Checksum != newProjectChecksums.AnalyzerReferences.Checksum)
                {
                    project = project.WithAnalyzerReferences(await _assetProvider.CreateCollectionAsync<AnalyzerReference>(
                        newProjectChecksums.AnalyzerReferences, _cancellationToken).ConfigureAwait(false));
                }

                // changed analyzer references
                if (oldProjectChecksums.Documents.Checksum != newProjectChecksums.Documents.Checksum)
                {
                    project = await UpdateDocumentsAsync(
                        project,
                        newProjectChecksums,
                        project.State.DocumentStates.States.Values,
                        oldProjectChecksums.Documents,
                        newProjectChecksums.Documents,
                        (solution, documents) => solution.AddDocuments(documents),
                        (solution, documentId) => solution.RemoveDocument(documentId)).ConfigureAwait(false);
                }

                // changed additional documents
                if (oldProjectChecksums.AdditionalDocuments.Checksum != newProjectChecksums.AdditionalDocuments.Checksum)
                {
                    project = await UpdateDocumentsAsync(
                        project,
                        newProjectChecksums,
                        project.State.AdditionalDocumentStates.States.Values,
                        oldProjectChecksums.AdditionalDocuments,
                        newProjectChecksums.AdditionalDocuments,
                        (solution, documents) => solution.AddAdditionalDocuments(documents),
                        (solution, documentId) => solution.RemoveAdditionalDocument(documentId)).ConfigureAwait(false);
                }

                // changed analyzer config documents
                if (oldProjectChecksums.AnalyzerConfigDocuments.Checksum != newProjectChecksums.AnalyzerConfigDocuments.Checksum)
                {
                    project = await UpdateDocumentsAsync(
                        project,
                        newProjectChecksums,
                        project.State.AnalyzerConfigDocumentStates.States.Values,
                        oldProjectChecksums.AnalyzerConfigDocuments,
                        newProjectChecksums.AnalyzerConfigDocuments,
                        (solution, documents) => solution.AddAnalyzerConfigDocuments(documents),
                        (solution, documentId) => solution.RemoveAnalyzerConfigDocument(documentId)).ConfigureAwait(false);
                }

                return project.Solution;
            }

            private async Task<Project> UpdateProjectInfoAsync(Project project, Checksum infoChecksum)
            {
                var newProjectAttributes = await _assetProvider.GetAssetAsync<ProjectInfo.ProjectAttributes>(infoChecksum, _cancellationToken).ConfigureAwait(false);

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

                return project;
            }

            private async Task<Project> UpdateDocumentsAsync(
                Project project,
                ProjectStateChecksums projectChecksums,
                IEnumerable<TextDocumentState> existingTextDocumentStates,
                ChecksumCollection oldChecksums,
                ChecksumCollection newChecksums,
                Func<Solution, ImmutableArray<DocumentInfo>, Solution> addDocuments,
                Func<Solution, DocumentId, Solution> removeDocument)
            {
                using var olds = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();
                using var news = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();

                olds.Object.UnionWith(oldChecksums);
                news.Object.UnionWith(newChecksums);

                // remove documents that exist in both side
                olds.Object.ExceptWith(newChecksums);
                news.Object.ExceptWith(oldChecksums);

                var oldMap = await GetDocumentMapAsync(existingTextDocumentStates, olds.Object).ConfigureAwait(false);
                var newMap = await GetDocumentMapAsync(_assetProvider, news.Object).ConfigureAwait(false);

                // If more than two documents changed during a single update, perform a bulk synchronization on the
                // project to avoid large numbers of small synchronization calls during document updates.
                // 🔗 https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1365014
                if (newMap.Count > 2)
                {
                    await _assetProvider.SynchronizeProjectAssetsAsync(new[] { projectChecksums.Checksum }, _cancellationToken).ConfigureAwait(false);
                }

                // added document
                ImmutableArray<DocumentInfo>.Builder? lazyDocumentsToAdd = null;
                foreach (var (documentId, newDocumentChecksums) in newMap)
                {
                    if (!oldMap.ContainsKey(documentId))
                    {
                        lazyDocumentsToAdd ??= ImmutableArray.CreateBuilder<DocumentInfo>();

                        // we have new document added
                        var documentInfo = await _assetProvider.CreateDocumentInfoAsync(newDocumentChecksums.Checksum, _cancellationToken).ConfigureAwait(false);
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

                    project = await UpdateDocumentAsync(document, oldDocumentChecksums, newDocumentChecksums).ConfigureAwait(false);
                }

                // removed document
                foreach (var (documentId, _) in oldMap)
                {
                    if (!newMap.ContainsKey(documentId))
                    {
                        // we have a document removed
                        project = removeDocument(project.Solution, documentId).GetProject(project.Id)!;
                    }
                }

                return project;
            }

            private async Task<Project> UpdateDocumentAsync(TextDocument document, DocumentStateChecksums oldDocumentChecksums, DocumentStateChecksums newDocumentChecksums)
            {
                // changed info
                if (oldDocumentChecksums.Info != newDocumentChecksums.Info)
                {
                    document = await UpdateDocumentInfoAsync(document, newDocumentChecksums.Info).ConfigureAwait(false);
                }

                // changed text
                if (oldDocumentChecksums.Text != newDocumentChecksums.Text)
                {
                    var serializableSourceText = await _assetProvider.GetAssetAsync<SerializableSourceText>(newDocumentChecksums.Text, _cancellationToken).ConfigureAwait(false);
                    var sourceText = await serializableSourceText.GetTextAsync(_cancellationToken).ConfigureAwait(false);

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

            private async Task<TextDocument> UpdateDocumentInfoAsync(TextDocument document, Checksum infoChecksum)
            {
                var newDocumentInfo = await _assetProvider.GetAssetAsync<DocumentInfo.DocumentAttributes>(infoChecksum, _cancellationToken).ConfigureAwait(false);

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

            private async Task<Dictionary<DocumentId, DocumentStateChecksums>> GetDocumentMapAsync(AssetProvider assetProvider, HashSet<Checksum> documents)
            {
                var map = new Dictionary<DocumentId, DocumentStateChecksums>();

                var documentChecksums = await assetProvider.GetAssetsAsync<DocumentStateChecksums>(documents, _cancellationToken).ConfigureAwait(false);
                var infos = await assetProvider.GetAssetsAsync<DocumentInfo.DocumentAttributes>(documentChecksums.Select(p => p.Item2.Info), _cancellationToken).ConfigureAwait(false);

                foreach (var kv in documentChecksums)
                {
                    Debug.Assert(assetProvider.EnsureCacheEntryIfExists(kv.Item2.Info), "Expected the prior call to GetAssetsAsync to obtain all items for this loop.");

                    var info = await assetProvider.GetAssetAsync<DocumentInfo.DocumentAttributes>(kv.Item2.Info, _cancellationToken).ConfigureAwait(false);
                    map.Add(info.Id, kv.Item2);
                }

                return map;
            }

            private async Task<Dictionary<DocumentId, DocumentStateChecksums>> GetDocumentMapAsync(IEnumerable<TextDocumentState> states, HashSet<Checksum> documents)
            {
                var map = new Dictionary<DocumentId, DocumentStateChecksums>();

                foreach (var state in states)
                {
                    var documentChecksums = await state.GetStateChecksumsAsync(_cancellationToken).ConfigureAwait(false);
                    if (documents.Contains(documentChecksums.Checksum))
                    {
                        map.Add(state.Id, documentChecksums);
                    }
                }

                return map;
            }

            private async Task<Dictionary<ProjectId, ProjectStateChecksums>> GetProjectMapAsync(AssetProvider assetProvider, HashSet<Checksum> projects)
            {
                var map = new Dictionary<ProjectId, ProjectStateChecksums>();

                var projectChecksums = await assetProvider.GetAssetsAsync<ProjectStateChecksums>(projects, _cancellationToken).ConfigureAwait(false);
                var infos = await assetProvider.GetAssetsAsync<ProjectInfo.ProjectAttributes>(projectChecksums.Select(p => p.Item2.Info), _cancellationToken).ConfigureAwait(false);

                foreach (var kv in projectChecksums)
                {
                    var info = await assetProvider.GetAssetAsync<ProjectInfo.ProjectAttributes>(kv.Item2.Info, _cancellationToken).ConfigureAwait(false);
                    map.Add(info.Id, kv.Item2);
                }

                return map;
            }

            private async Task<Dictionary<ProjectId, ProjectStateChecksums>> GetProjectMapAsync(Solution solution, HashSet<Checksum> projects)
            {
                var map = new Dictionary<ProjectId, ProjectStateChecksums>();

                foreach (var (projectId, projectState) in solution.State.ProjectStates)
                {
                    var projectChecksums = await projectState.GetStateChecksumsAsync(_cancellationToken).ConfigureAwait(false);
                    if (projects.Contains(projectChecksums.Checksum))
                    {
                        map.Add(projectId, projectChecksums);
                    }
                }

                return map;
            }

#if DEBUG
            private async Task ValidateChecksumAsync(Checksum checksumFromRequest, Solution incrementalSolutionBuilt)
            {
                var currentSolutionChecksum = await incrementalSolutionBuilt.State.GetChecksumAsync(CancellationToken.None).ConfigureAwait(false);
                if (checksumFromRequest == currentSolutionChecksum)
                {
                    return;
                }

                var (solutionInfo, options) = await _assetProvider.CreateSolutionInfoAndOptionsAsync(checksumFromRequest, _cancellationToken).ConfigureAwait(false);
                var workspace = new AdhocWorkspace(_hostServices);
                workspace.AddSolution(solutionInfo);
                workspace.SetCurrentSolution(s => s.WithOptions(options), WorkspaceChangeKind.SolutionChanged);

                await TestUtils.AssertChecksumsAsync(_assetProvider, checksumFromRequest, workspace.CurrentSolution, incrementalSolutionBuilt).ConfigureAwait(false);
            }
#endif
        }
    }
}
