// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote.DebugUtil;
using Microsoft.CodeAnalysis.Remote.Shared;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Create solution for given checksum from base solution
    /// </summary>
    internal class SolutionCreator
    {
        private readonly AssetService _assetService;
        private readonly Solution _baseSolution;
        private readonly CancellationToken _cancellationToken;

        public SolutionCreator(AssetService assetService, Solution baseSolution, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(baseSolution);

            _assetService = assetService;
            _baseSolution = baseSolution;
            _cancellationToken = cancellationToken;
        }

        public async Task<bool> IsIncrementalUpdateAsync(Checksum newSolutionChecksum)
        {
            var newSolutionChecksums = await _assetService.GetAssetAsync<SolutionStateChecksums>(newSolutionChecksum, _cancellationToken).ConfigureAwait(false);
            var newSolutionInfo = await _assetService.GetAssetAsync<SolutionInfo.SolutionAttributes>(newSolutionChecksums.Info, _cancellationToken).ConfigureAwait(false);

            // if either solution id or file path changed, then we consider it as new solution
            return _baseSolution.Id == newSolutionInfo.Id && _baseSolution.FilePath == newSolutionInfo.FilePath;
        }

        public async Task<SolutionInfo> CreateSolutionInfoAsync(Checksum solutionChecksum)
        {
            var solutionChecksumObject = await _assetService.GetAssetAsync<SolutionStateChecksums>(solutionChecksum, _cancellationToken).ConfigureAwait(false);
            var solutionInfo = await _assetService.GetAssetAsync<SolutionInfo.SolutionAttributes>(solutionChecksumObject.Info, _cancellationToken).ConfigureAwait(false);

            var projects = new List<ProjectInfo>();
            foreach (var projectChecksum in solutionChecksumObject.Projects)
            {
                var projectInfo = await CreateProjectInfoAsync(projectChecksum).ConfigureAwait(false);
                if (projectInfo != null)
                {
                    projects.Add(projectInfo);
                }
            }

            return SolutionInfo.Create(solutionInfo.Id, solutionInfo.Version, solutionInfo.FilePath, projects);
        }

        public async Task<Solution> CreateSolutionAsync(Checksum newSolutionChecksum)
        {
            var solution = _baseSolution;

            var oldSolutionChecksums = await solution.State.GetStateChecksumsAsync(_cancellationToken).ConfigureAwait(false);
            var newSolutionChecksums = await _assetService.GetAssetAsync<SolutionStateChecksums>(newSolutionChecksum, _cancellationToken).ConfigureAwait(false);

            if (oldSolutionChecksums.Info != newSolutionChecksums.Info)
            {
                var newSolutionInfo = await _assetService.GetAssetAsync<SolutionInfo.SolutionAttributes>(newSolutionChecksums.Info, _cancellationToken).ConfigureAwait(false);

                // if either id or file path has changed, then this is not update
                Contract.ThrowIfFalse(solution.Id == newSolutionInfo.Id && solution.FilePath == newSolutionInfo.FilePath);
            }

            if (oldSolutionChecksums.Projects.Checksum != newSolutionChecksums.Projects.Checksum)
            {
                solution = await UpdateProjectsAsync(solution, oldSolutionChecksums.Projects, newSolutionChecksums.Projects).ConfigureAwait(false);
            }

            // make sure created solution has same checksum as given one
            await ValidateChecksumAsync(newSolutionChecksum, solution).ConfigureAwait(false);

            return solution;
        }

        private async Task<Solution> UpdateProjectsAsync(Solution solution, ChecksumCollection oldChecksums, ChecksumCollection newChecksums)
        {
            using (var olds = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
            using (var news = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
            {
                olds.Object.UnionWith(oldChecksums);
                news.Object.UnionWith(newChecksums);

                // remove projects that exist in both side
                olds.Object.ExceptWith(newChecksums);
                news.Object.ExceptWith(oldChecksums);

                return await UpdateProjectsAsync(solution, olds.Object, news.Object).ConfigureAwait(false);
            }
        }

        private async Task<Solution> UpdateProjectsAsync(Solution solution, HashSet<Checksum> oldChecksums, HashSet<Checksum> newChecksums)
        {
            var oldMap = await GetProjectMapAsync(solution, oldChecksums).ConfigureAwait(false);
            var newMap = await GetProjectMapAsync(_assetService, newChecksums).ConfigureAwait(false);

            // bulk sync assets
            await SynchronizeAssetsAsync(solution, oldMap, newMap).ConfigureAwait(false);

            // added project
            foreach (var kv in newMap)
            {
                if (!oldMap.ContainsKey(kv.Key))
                {
                    var projectInfo = await CreateProjectInfoAsync(kv.Value.Checksum).ConfigureAwait(false);
                    if (projectInfo == null)
                    {
                        // this project is not supported in OOP
                        continue;
                    }

                    // we have new project added
                    solution = solution.AddProject(projectInfo);
                }
            }

            // changed project
            foreach (var kv in newMap)
            {
                if (!oldMap.TryGetValue(kv.Key, out var oldProjectChecksums))
                {
                    continue;
                }

                var newProjectChecksums = kv.Value;
                Contract.ThrowIfTrue(oldProjectChecksums.Checksum == newProjectChecksums.Checksum);

                solution = await UpdateProjectAsync(solution.GetProject(kv.Key), oldProjectChecksums, newProjectChecksums).ConfigureAwait(false);
            }

            // removed project
            foreach (var kv in oldMap)
            {
                if (!newMap.ContainsKey(kv.Key))
                {
                    // we have a project removed
                    solution = solution.RemoveProject(kv.Key);
                }
            }

            return solution;
        }

        private async Task SynchronizeAssetsAsync(Solution solution, Dictionary<ProjectId, ProjectStateChecksums> oldMap, Dictionary<ProjectId, ProjectStateChecksums> newMap)
        {
            using (var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
            {
                // added project
                foreach (var kv in newMap)
                {
                    if (oldMap.ContainsKey(kv.Key))
                    {
                        continue;
                    }

                    pooledObject.Object.Add(kv.Value.Checksum);
                }

                await _assetService.SynchronizeProjectAssetsAsync(pooledObject.Object, _cancellationToken).ConfigureAwait(false);
            }
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
                    FixUpCompilationOptions(
                        project.State.ProjectInfo.Attributes,
                        await _assetService.GetAssetAsync<CompilationOptions>(
                            newProjectChecksums.CompilationOptions, _cancellationToken).ConfigureAwait(false)));
            }

            // changed parse options
            if (oldProjectChecksums.ParseOptions != newProjectChecksums.ParseOptions)
            {
                project = project.WithParseOptions(await _assetService.GetAssetAsync<ParseOptions>(newProjectChecksums.ParseOptions, _cancellationToken).ConfigureAwait(false));
            }

            // changed project references
            if (oldProjectChecksums.ProjectReferences.Checksum != newProjectChecksums.ProjectReferences.Checksum)
            {
                project = project.WithProjectReferences(await CreateCollectionAsync<ProjectReference>(newProjectChecksums.ProjectReferences).ConfigureAwait(false));
            }

            // changed metadata references
            if (oldProjectChecksums.MetadataReferences.Checksum != newProjectChecksums.MetadataReferences.Checksum)
            {
                project = project.WithMetadataReferences(await CreateCollectionAsync<MetadataReference>(newProjectChecksums.MetadataReferences).ConfigureAwait(false));
            }

            // changed analyzer references
            if (oldProjectChecksums.AnalyzerReferences.Checksum != newProjectChecksums.AnalyzerReferences.Checksum)
            {
                project = project.WithAnalyzerReferences(await CreateCollectionAsync<AnalyzerReference>(newProjectChecksums.AnalyzerReferences).ConfigureAwait(false));
            }

            // changed analyzer references
            if (oldProjectChecksums.Documents.Checksum != newProjectChecksums.Documents.Checksum)
            {
                project = await UpdateDocumentsAsync(project, oldProjectChecksums.Documents, newProjectChecksums.Documents, additionalText: false).ConfigureAwait(false);
            }

            // changed analyzer references
            if (oldProjectChecksums.AdditionalDocuments.Checksum != newProjectChecksums.AdditionalDocuments.Checksum)
            {
                project = await UpdateDocumentsAsync(project, oldProjectChecksums.AdditionalDocuments, newProjectChecksums.AdditionalDocuments, additionalText: true).ConfigureAwait(false);
            }

            return project.Solution;
        }

        private async Task<Project> UpdateProjectInfoAsync(Project project, Checksum infoChecksum)
        {
            var newProjectInfo = await _assetService.GetAssetAsync<ProjectInfo.ProjectAttributes>(infoChecksum, _cancellationToken).ConfigureAwait(false);

            // there is no API to change these once project is created
            Contract.ThrowIfFalse(project.State.ProjectInfo.Attributes.Id == newProjectInfo.Id);
            Contract.ThrowIfFalse(project.State.ProjectInfo.Attributes.Language == newProjectInfo.Language);
            Contract.ThrowIfFalse(project.State.ProjectInfo.Attributes.IsSubmission == newProjectInfo.IsSubmission);

            if (project.State.ProjectInfo.Attributes.Name != newProjectInfo.Name)
            {
                project = project.Solution.WithProjectName(project.Id, newProjectInfo.Name).GetProject(project.Id);
            }

            if (project.State.ProjectInfo.Attributes.AssemblyName != newProjectInfo.AssemblyName)
            {
                project = project.Solution.WithProjectAssemblyName(project.Id, newProjectInfo.AssemblyName).GetProject(project.Id);
            }

            if (project.State.ProjectInfo.Attributes.FilePath != newProjectInfo.FilePath)
            {
                project = project.Solution.WithProjectFilePath(project.Id, newProjectInfo.FilePath).GetProject(project.Id);
            }

            if (project.State.ProjectInfo.Attributes.OutputFilePath != newProjectInfo.OutputFilePath)
            {
                project = project.Solution.WithProjectOutputFilePath(project.Id, newProjectInfo.OutputFilePath).GetProject(project.Id);
            }

            if (project.State.ProjectInfo.Attributes.OutputRefFilePath != newProjectInfo.OutputRefFilePath)
            {
                project = project.Solution.WithProjectOutputRefFilePath(project.Id, newProjectInfo.OutputRefFilePath).GetProject(project.Id);
            }

            if (project.State.ProjectInfo.Attributes.DefaultNamespace != newProjectInfo.DefaultNamespace)
            {
                project = project.Solution.WithProjectDefaultNamespace(project.Id, newProjectInfo.DefaultNamespace).GetProject(project.Id);
            }

            if (project.State.ProjectInfo.Attributes.HasAllInformation != newProjectInfo.HasAllInformation)
            {
                project = project.Solution.WithHasAllInformation(project.Id, newProjectInfo.HasAllInformation).GetProject(project.Id);
            }

            return project;
        }

        private async Task<Project> UpdateDocumentsAsync(Project project, ChecksumCollection oldChecksums, ChecksumCollection newChecksums, bool additionalText)
        {
            using (var olds = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
            using (var news = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
            {
                olds.Object.UnionWith(oldChecksums);
                news.Object.UnionWith(newChecksums);

                // remove projects that exist in both side
                olds.Object.ExceptWith(newChecksums);
                news.Object.ExceptWith(oldChecksums);

                return await UpdateDocumentsAsync(project, olds.Object, news.Object, additionalText).ConfigureAwait(false);
            }
        }

        private async Task<Project> UpdateDocumentsAsync(Project project, HashSet<Checksum> oldChecksums, HashSet<Checksum> newChecksums, bool additionalText)
        {
            var oldMap = await GetDocumentMapAsync(project, oldChecksums, additionalText).ConfigureAwait(false);
            var newMap = await GetDocumentMapAsync(_assetService, newChecksums).ConfigureAwait(false);

            // added document
            foreach (var kv in newMap)
            {
                if (!oldMap.ContainsKey(kv.Key))
                {
                    // we have new document added
                    project = AddDocument(project, await CreateDocumentInfoAsync(kv.Value.Checksum).ConfigureAwait(false), additionalText);
                }
            }

            // changed document
            foreach (var kv in newMap)
            {
                if (!oldMap.TryGetValue(kv.Key, out var oldDocumentChecksums))
                {
                    continue;
                }

                var newDocumentChecksums = kv.Value;
                Contract.ThrowIfTrue(oldDocumentChecksums.Checksum == newDocumentChecksums.Checksum);

                var document = additionalText ? project.GetAdditionalDocument(kv.Key) : project.GetDocument(kv.Key);
                project = await UpdateDocumentAsync(document, oldDocumentChecksums, newDocumentChecksums, additionalText).ConfigureAwait(false);
            }

            // removed document
            foreach (var kv in oldMap)
            {
                if (!newMap.ContainsKey(kv.Key))
                {
                    // we have a document removed
                    if (additionalText)
                    {
                        project = project.RemoveAdditionalDocument(kv.Key);
                    }
                    else
                    {
                        project = project.RemoveDocument(kv.Key);
                    }
                }
            }

            return project;
        }

        private async Task<Project> UpdateDocumentAsync(TextDocument document, DocumentStateChecksums oldDocumentChecksums, DocumentStateChecksums newDocumentChecksums, bool additionalText)
        {
            // changed info
            if (oldDocumentChecksums.Info != newDocumentChecksums.Info)
            {
                document = await UpdateDocumentInfoAsync(document, newDocumentChecksums.Info, additionalText).ConfigureAwait(false);
            }

            // changed text
            if (oldDocumentChecksums.Text != newDocumentChecksums.Text)
            {
                var sourceText = await _assetService.GetAssetAsync<SourceText>(newDocumentChecksums.Text, _cancellationToken).ConfigureAwait(false);

                if (additionalText)
                {
                    document = document.Project.Solution.WithAdditionalDocumentText(document.Id, sourceText).GetAdditionalDocument(document.Id);
                }
                else
                {
                    document = document.Project.Solution.WithDocumentText(document.Id, sourceText).GetDocument(document.Id);
                }
            }

            return document.Project;
        }

        private async Task<TextDocument> UpdateDocumentInfoAsync(TextDocument document, Checksum infoChecksum, bool additionalText)
        {
            var newDocumentInfo = await _assetService.GetAssetAsync<DocumentInfo.DocumentAttributes>(infoChecksum, _cancellationToken).ConfigureAwait(false);

            // there is no api to change these once document is created
            Contract.ThrowIfFalse(document.State.Attributes.Id == newDocumentInfo.Id);
            Contract.ThrowIfFalse(document.State.Attributes.Name == newDocumentInfo.Name);
            Contract.ThrowIfFalse(document.State.Attributes.FilePath == newDocumentInfo.FilePath);
            Contract.ThrowIfFalse(document.State.Attributes.IsGenerated == newDocumentInfo.IsGenerated);

            if (document.State.Attributes.Folders != newDocumentInfo.Folders)
            {
                // additional document can't change folder once created
                Contract.ThrowIfTrue(additionalText);
                document = document.Project.Solution.WithDocumentFolders(document.Id, newDocumentInfo.Folders).GetDocument(document.Id);
            }

            if (document.State.Attributes.SourceCodeKind != newDocumentInfo.SourceCodeKind)
            {
                // additional document can't change sourcecode kind once created
                Contract.ThrowIfTrue(additionalText);
                document = document.Project.Solution.WithDocumentSourceCodeKind(document.Id, newDocumentInfo.SourceCodeKind).GetDocument(document.Id);
            }

            return document;
        }

        private async Task<Dictionary<DocumentId, DocumentStateChecksums>> GetDocumentMapAsync(AssetService assetService, HashSet<Checksum> documents)
        {
            var map = new Dictionary<DocumentId, DocumentStateChecksums>();

            var documentChecksums = await assetService.GetAssetsAsync<DocumentStateChecksums>(documents, _cancellationToken).ConfigureAwait(false);
            var infos = await assetService.GetAssetsAsync<DocumentInfo.DocumentAttributes>(documentChecksums.Select(p => p.Item2.Info), _cancellationToken).ConfigureAwait(false);

            foreach (var kv in documentChecksums)
            {
                var info = await assetService.GetAssetAsync<DocumentInfo.DocumentAttributes>(kv.Item2.Info, _cancellationToken).ConfigureAwait(false);
                map.Add(info.Id, kv.Item2);
            }

            return map;
        }

        private Task<Dictionary<DocumentId, DocumentStateChecksums>> GetDocumentMapAsync(Project project, HashSet<Checksum> documents, bool additionalText)
        {
            if (additionalText)
            {
                return GetDocumentMapAsync(project, project.State.AdditionalDocumentStates, documents);
            }

            return GetDocumentMapAsync(project, project.State.DocumentStates, documents);
        }

        private async Task<Dictionary<DocumentId, DocumentStateChecksums>> GetDocumentMapAsync<T>(Project project, IImmutableDictionary<DocumentId, T> states, HashSet<Checksum> documents)
            where T : TextDocumentState
        {
            var map = new Dictionary<DocumentId, DocumentStateChecksums>();

            foreach (var kv in states)
            {
                var documentChecksums = await kv.Value.GetStateChecksumsAsync(_cancellationToken).ConfigureAwait(false);
                if (documents.Contains(documentChecksums.Checksum))
                {
                    map.Add(kv.Key, documentChecksums);
                }
            }

            return map;
        }

        private async Task<Dictionary<ProjectId, ProjectStateChecksums>> GetProjectMapAsync(AssetService assetService, HashSet<Checksum> projects)
        {
            var map = new Dictionary<ProjectId, ProjectStateChecksums>();

            var projectChecksums = await assetService.GetAssetsAsync<ProjectStateChecksums>(projects, _cancellationToken).ConfigureAwait(false);
            var infos = await assetService.GetAssetsAsync<ProjectInfo.ProjectAttributes>(projectChecksums.Select(p => p.Item2.Info), _cancellationToken).ConfigureAwait(false);

            foreach (var kv in projectChecksums)
            {
                var info = await assetService.GetAssetAsync<ProjectInfo.ProjectAttributes>(kv.Item2.Info, _cancellationToken).ConfigureAwait(false);
                map.Add(info.Id, kv.Item2);
            }

            return map;
        }

        private async Task<Dictionary<ProjectId, ProjectStateChecksums>> GetProjectMapAsync(Solution solution, HashSet<Checksum> projects)
        {
            var map = new Dictionary<ProjectId, ProjectStateChecksums>();

            foreach (var kv in solution.State.ProjectStates)
            {
                var projectChecksums = await kv.Value.GetStateChecksumsAsync(_cancellationToken).ConfigureAwait(false);
                if (projects.Contains(projectChecksums.Checksum))
                {
                    map.Add(kv.Key, projectChecksums);
                }
            }

            return map;
        }

        private async Task<ProjectInfo> CreateProjectInfoAsync(Checksum projectChecksum)
        {
            var projectSnapshot = await _assetService.GetAssetAsync<ProjectStateChecksums>(projectChecksum, _cancellationToken).ConfigureAwait(false);

            var projectInfo = await _assetService.GetAssetAsync<ProjectInfo.ProjectAttributes>(projectSnapshot.Info, _cancellationToken).ConfigureAwait(false);
            if (!RemoteSupportedLanguages.IsSupported(projectInfo.Language))
            {
                // only add project our workspace supports. 
                // workspace doesn't allow creating project with unknown languages
                return null;
            }

            Contract.ThrowIfFalse(_baseSolution.Workspace.Services.IsSupported(projectInfo.Language));

            var compilationOptions = FixUpCompilationOptions(
                projectInfo,
                await _assetService.GetAssetAsync<CompilationOptions>(
                    projectSnapshot.CompilationOptions, _cancellationToken).ConfigureAwait(false));

            var parseOptions = await _assetService.GetAssetAsync<ParseOptions>(projectSnapshot.ParseOptions, _cancellationToken).ConfigureAwait(false);

            var p2p = await CreateCollectionAsync<ProjectReference>(projectSnapshot.ProjectReferences).ConfigureAwait(false);
            var metadata = await CreateCollectionAsync<MetadataReference>(projectSnapshot.MetadataReferences).ConfigureAwait(false);
            var analyzers = await CreateCollectionAsync<AnalyzerReference>(projectSnapshot.AnalyzerReferences).ConfigureAwait(false);

            var documents = new List<DocumentInfo>();
            foreach (var documentChecksum in projectSnapshot.Documents)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var documentInfo = await CreateDocumentInfoAsync(documentChecksum).ConfigureAwait(false);
                documents.Add(documentInfo);
            }

            var additionals = new List<DocumentInfo>();
            foreach (var documentChecksum in projectSnapshot.AdditionalDocuments)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var documentInfo = await CreateDocumentInfoAsync(documentChecksum).ConfigureAwait(false);
                additionals.Add(documentInfo);
            }

            return ProjectInfo.Create(
                projectInfo.Id, projectInfo.Version, projectInfo.Name, projectInfo.AssemblyName,
                projectInfo.Language, projectInfo.FilePath, projectInfo.OutputFilePath,
                compilationOptions, parseOptions,
                documents, p2p, metadata, analyzers, additionals, projectInfo.IsSubmission)
                .WithHasAllInformation(projectInfo.HasAllInformation)
                .WithDefaultNamespace(projectInfo.DefaultNamespace);
        }

        private async Task<List<T>> CreateCollectionAsync<T>(ChecksumCollection collections)
        {
            var assets = new List<T>();

            foreach (var checksum in collections)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var asset = await _assetService.GetAssetAsync<T>(checksum, _cancellationToken).ConfigureAwait(false);
                assets.Add(asset);
            }

            return assets;
        }

        private async Task<DocumentInfo> CreateDocumentInfoAsync(Checksum documentChecksum)
        {
            var documentSnapshot = await _assetService.GetAssetAsync<DocumentStateChecksums>(documentChecksum, _cancellationToken).ConfigureAwait(false);
            var documentInfo = await _assetService.GetAssetAsync<DocumentInfo.DocumentAttributes>(documentSnapshot.Info, _cancellationToken).ConfigureAwait(false);

            var textLoader = TextLoader.From(
                TextAndVersion.Create(
                    await _assetService.GetAssetAsync<SourceText>(documentSnapshot.Text, _cancellationToken).ConfigureAwait(false),
                    VersionStamp.Create(),
                    documentInfo.FilePath));

            // TODO: do we need version?
            return DocumentInfo.Create(
                documentInfo.Id,
                documentInfo.Name,
                documentInfo.Folders,
                documentInfo.SourceCodeKind,
                textLoader,
                documentInfo.FilePath,
                documentInfo.IsGenerated);
        }

        private Project AddDocument(Project project, DocumentInfo documentInfo, bool additionalText)
        {
            if (additionalText)
            {
                return project.Solution.AddAdditionalDocument(documentInfo).GetProject(project.Id);
            }

            return project.Solution.AddDocument(documentInfo).GetProject(project.Id);
        }

        private CompilationOptions FixUpCompilationOptions(ProjectInfo.ProjectAttributes info, CompilationOptions compilationOptions)
        {
            return compilationOptions.WithXmlReferenceResolver(GetXmlResolver(info.FilePath))
                                     .WithStrongNameProvider(new DesktopStrongNameProvider(GetStrongNameKeyPaths(info)));
        }

        private static XmlFileResolver GetXmlResolver(string filePath)
        {
            // Given filePath can be any arbitary string project is created with.
            // for primary solution in host such as VSWorkspace, ETA or MSBuildWorkspace
            // filePath will point to actual file on disk, but in memory solultion, or
            // one from AdhocWorkspace and etc, FilePath can be a random string.
            // Make sure we return only if given filePath is in right form.
            if (!PathUtilities.IsAbsolute(filePath))
            {
                // xmlFileResolver can only deal with absolute path
                // return Default
                return XmlFileResolver.Default;
            }

            return new XmlFileResolver(PathUtilities.GetDirectoryName(filePath));
        }

        private ImmutableArray<string> GetStrongNameKeyPaths(ProjectInfo.ProjectAttributes info)
        {
            // Given FilePath/OutputFilePath can be any arbitary strings project is created with.
            // for primary solution in host such as VSWorkspace, ETA or MSBuildWorkspace
            // filePath will point to actual file on disk, but in memory solultion, or
            // one from AdhocWorkspace and etc, FilePath/OutputFilePath can be a random string.
            // Make sure we return only if given filePath is in right form.
            if (info.FilePath == null && info.OutputFilePath == null)
            {
                // return empty since that is what IDE does for this case
                // see AbstractProject.GetStrongNameKeyPaths
                return ImmutableArray<string>.Empty;
            }

            var builder = ArrayBuilder<string>.GetInstance();
            if (info.FilePath != null && PathUtilities.IsAbsolute(info.FilePath))
            {
                // desktop strong name provider only knows how to deal with absolute path
                builder.Add(PathUtilities.GetDirectoryName(info.FilePath));
            }

            if (info.OutputFilePath != null && PathUtilities.IsAbsolute(info.OutputFilePath))
            {
                // desktop strong name provider only knows how to deal with absolute path
                builder.Add(PathUtilities.GetDirectoryName(info.OutputFilePath));
            }

            return builder.ToImmutableAndFree();
        }

        private async Task ValidateChecksumAsync(Checksum givenSolutionChecksum, Solution solution)
        {
#if DEBUG
            var currentSolutionChecksum = await solution.State.GetChecksumAsync(_cancellationToken).ConfigureAwait(false);

            if (givenSolutionChecksum == currentSolutionChecksum)
            {
                return;
            }

            var map = await solution.GetAssetMapAsync(_cancellationToken).ConfigureAwait(false);
            await RemoveDuplicateChecksumsAsync(givenSolutionChecksum, map).ConfigureAwait(false);

            foreach (var kv in map.Where(kv => kv.Value is ChecksumWithChildren).ToList())
            {
                map.Remove(kv.Key);
            }

            var sb = new StringBuilder();
            foreach (var kv in map)
            {
                sb.AppendLine($"{kv.Key.ToString()}, {kv.Value.ToString()}");
            }

            Logger.Log(FunctionId.SolutionCreator_AssetDifferences, sb.ToString());

            Debug.Fail("Differences detected in solution checksum: " + sb.ToString());
#else

            // have this to avoid error on async
            await Task.CompletedTask.ConfigureAwait(false);
#endif
        }

        private async Task RemoveDuplicateChecksumsAsync(Checksum givenSolutionChecksum, Dictionary<Checksum, object> map)
        {
            var solutionChecksums = await _assetService.GetAssetAsync<SolutionStateChecksums>(givenSolutionChecksum, _cancellationToken).ConfigureAwait(false);
            map.RemoveChecksums(solutionChecksums);

            foreach (var projectChecksum in solutionChecksums.Projects)
            {
                var projectChecksums = await _assetService.GetAssetAsync<ProjectStateChecksums>(projectChecksum, _cancellationToken).ConfigureAwait(false);
                map.RemoveChecksums(projectChecksums);

                foreach (var documentChecksum in projectChecksums.Documents)
                {
                    var documentChecksums = await _assetService.GetAssetAsync<DocumentStateChecksums>(documentChecksum, _cancellationToken).ConfigureAwait(false);
                    map.RemoveChecksums(documentChecksums);
                }

                foreach (var documentChecksum in projectChecksums.AdditionalDocuments)
                {
                    var documentChecksums = await _assetService.GetAssetAsync<DocumentStateChecksums>(documentChecksum, _cancellationToken).ConfigureAwait(false);
                    map.RemoveChecksums(documentChecksums);
                }
            }
        }
    }
}
