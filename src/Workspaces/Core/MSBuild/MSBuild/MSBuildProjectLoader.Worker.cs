// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    public partial class MSBuildProjectLoader
    {
        private struct ReportingOptions
        {
            public ReportMode OnPathFailure { get; set; }
            public ReportMode OnLoaderFailure { get; set; }
        }

        private class Worker
        {
            private readonly MSBuildProjectLoader _owner;

            private readonly string _baseDirectory;

            /// <summary>
            /// An ordered list of paths to project files that should be loaded. In the case of a solution,
            /// this is the list of project file paths in the solution.
            /// </summary>
            private readonly ImmutableArray<string> _requestedProjectPaths;

            /// <summary>
            /// Global MSBuild properties to set when loading projects.
            /// </summary>
            private readonly ImmutableDictionary<string, string> _globalProperties;

            /// <summary>
            /// Map of <see cref="ProjectId"/>s, project paths, and output file paths.
            /// </summary>
            private readonly ProjectMap _projectMap;

            /// <summary>
            /// Provides options for how failures should be reported when loading requested project files.
            /// </summary>
            private readonly ReportingOptions _requestedProjectOptions;

            /// <summary>
            /// When true, metadata is preferred for any project reference unless the referenced project is already loaded
            /// because it was requested.
            /// </summary>
            private readonly bool _preferMetadataForReferencedProjects;

            /// <summary>
            /// Provides options for how failures should be reported when loading any discovered project files.
            /// </summary>
            private readonly ReportingOptions _discoveredProjectOptions;

            /// <summary>
            /// A list of <see cref="ProjectId"/>s and <see cref="ProjectFileInfo"/>s that were discovered as the requested
            /// projects were loaded.
            /// </summary>
            private readonly List<(ProjectId id, ProjectFileInfo fileInfo)> _discoveredProjects;

            private readonly Dictionary<ProjectId, ProjectFileInfo> _projectIdToFileInfoMap;
            private readonly Dictionary<ProjectId, List<ProjectReference>> _projectIdToProjectReferencesMap;

            public Worker(
                MSBuildProjectLoader owner,
                ImmutableArray<string> requestedProjectPaths,
                string baseDirectory,
                ImmutableDictionary<string, string> globalProperties,
                ProjectMap projectMap,
                ReportingOptions requestedProjectOptions,
                ReportingOptions discoveredProjectOptions,
                bool preferMetadataForReferencedProjects)
            {
                Debug.Assert(owner != null);

                _owner = owner;
                _requestedProjectPaths = requestedProjectPaths;
                _baseDirectory = baseDirectory;
                _globalProperties = globalProperties;
                _projectMap = projectMap ?? new ProjectMap();
                _requestedProjectOptions = requestedProjectOptions;
                _discoveredProjectOptions = discoveredProjectOptions;
                _preferMetadataForReferencedProjects = preferMetadataForReferencedProjects;
                _discoveredProjects = new List<(ProjectId id, ProjectFileInfo fileInfo)>();
                _projectIdToFileInfoMap = new Dictionary<ProjectId, ProjectFileInfo>();
                _projectIdToProjectReferencesMap = new Dictionary<ProjectId, List<ProjectReference>>();
            }

            public async Task<ImmutableArray<ProjectInfo>> LoadAsync(CancellationToken cancellationToken)
            {
                var results = ImmutableArray.CreateBuilder<ProjectInfo>();
                var processedPaths = new HashSet<string>(PathUtilities.Comparer);

                var requestedProjectFileInfos = new List<ProjectFileInfo>();

                foreach (var projectPath in _requestedProjectPaths)
                {
                    if (!processedPaths.Add(projectPath))
                    {
                        // TODO: Report warning if there are duplicate project paths.
                        continue;
                    }

                    if (!_owner.TryGetAbsoluteProjectPath(projectPath, _baseDirectory, _requestedProjectOptions.OnPathFailure, out var fullProjectPath))
                    {
                        continue; // Failure should already be reported.
                    }

                    var projectFileInfos = await LoadProjectFileInfosAsync(fullProjectPath, _requestedProjectOptions, cancellationToken).ConfigureAwait(false);

                    requestedProjectFileInfos.AddRange(projectFileInfos);
                }

                var requestedIdsAndFileInfos = new List<(ProjectId id, ProjectFileInfo fileInfo)>();

                foreach (var projectFileInfo in requestedProjectFileInfos)
                {
                    var projectId = _projectMap.GetOrCreateProjectId(projectFileInfo);
                    _projectIdToFileInfoMap.Add(projectId, projectFileInfo);
                    requestedIdsAndFileInfos.Add((projectId, projectFileInfo));
                }

                foreach (var (id, fileInfo) in requestedIdsAndFileInfos)
                {
                    var projectInfo = fileInfo.IsEmpty
                        ? CreateEmptyProjectInfo(fileInfo, id)
                        : await CreateProjectInfoAsync(fileInfo, id, cancellationToken).ConfigureAwait(false);

                    results.Add(projectInfo);
                }

                foreach (var (id, fileInfo) in _discoveredProjects)
                {
                    var projectInfo = fileInfo.IsEmpty
                        ? CreateEmptyProjectInfo(fileInfo, id)
                        : await CreateProjectInfoAsync(fileInfo, id, cancellationToken).ConfigureAwait(false);

                    results.Add(projectInfo);
                }

                return results.ToImmutable();
            }

            private async Task<ImmutableArray<ProjectFileInfo>> LoadProjectFileInfosAsync(string projectPath, ReportingOptions reportingOptions, CancellationToken cancellationToken)
            {
                if (!_owner.TryGetLoaderFromProjectPath(projectPath, reportingOptions.OnLoaderFailure, out var loader))
                {
                    return ImmutableArray<ProjectFileInfo>.Empty; // Failure should already be reported.
                }

                var projectFile = await loader.LoadProjectFileAsync(projectPath, _globalProperties, _owner.BuildManager, cancellationToken).ConfigureAwait(false);

                // If there were any failures during load, we won't be able to build the project. So, bail early with an empty project.
                if (projectFile.Log.HasFailure)
                {
                    _owner.ReportDiagnosticLog(projectFile.Log);

                    return ImmutableArray.Create(
                        ProjectFileInfo.CreateEmpty(loader.Language, projectPath, projectFile.Log));
                }

                var projectFileInfos = await projectFile.GetProjectFileInfosAsync(cancellationToken).ConfigureAwait(false);

                var results = ImmutableArray.CreateBuilder<ProjectFileInfo>();

                foreach (var projectFileInfo in projectFileInfos)
                {
                    // If any diagnostics were logged during build, we'll carry on and try to produce a meaningful project.
                    if (!projectFileInfo.Log.IsEmpty)
                    {
                        _owner.ReportDiagnosticLog(projectFileInfo.Log);
                    }

                    results.Add(projectFileInfo);
                }

                return results.ToImmutable();
            }

            private async Task<ImmutableArray<ProjectInfo>> LoadProjectInfosFromPathAsync(string projectPath, ReportingOptions reportingOptions, CancellationToken cancellationToken)
            {
                var results = ImmutableArray.CreateBuilder<ProjectInfo>();

                var projectFileInfos = await LoadProjectFileInfosAsync(projectPath, reportingOptions, cancellationToken).ConfigureAwait(false);

                var idsAndFileInfos = new List<(ProjectId id, ProjectFileInfo fileInfo)>();

                foreach (var projectFileInfo in projectFileInfos)
                {
                    var projectId = _projectMap.GetOrCreateProjectId(projectFileInfo);
                    idsAndFileInfos.Add((projectId, projectFileInfo));

                    if (!_projectIdToFileInfoMap.ContainsKey(projectId))
                    {
                        _projectIdToFileInfoMap.Add(projectId, projectFileInfo);
                        _discoveredProjects.Add((projectId, projectFileInfo));
                    }
                }

                foreach (var (id, fileInfo) in idsAndFileInfos)
                {
                    var projectInfo = fileInfo.IsEmpty
                        ? CreateEmptyProjectInfo(fileInfo, id)
                        : await CreateProjectInfoAsync(fileInfo, id, cancellationToken).ConfigureAwait(false);

                    results.Add(projectInfo);
                }

                return results.ToImmutable();
            }

            private ProjectInfo CreateEmptyProjectInfo(ProjectFileInfo projectFileInfo, ProjectId projectId)
            {
                var language = projectFileInfo.Language;
                Debug.Assert(_owner.IsSupportedLanguage(language));

                var projectPath = projectFileInfo.FilePath;

                var version = GetProjectVersion(projectPath);
                var projectName = Path.GetFileNameWithoutExtension(projectPath);
                var assemblyName = GetAssemblyNameFromProjectPath(projectPath);

                var parseOptions = _owner.GetLanguageService<ISyntaxTreeFactoryService>(language)
                    .GetDefaultParseOptions();
                var compilationOptions = _owner.GetLanguageService<ICompilationFactoryService>(language)
                    .GetDefaultCompilationOptions();

                return ProjectInfo.Create(
                    projectId,
                    version,
                    projectName,
                    assemblyName: assemblyName,
                    language: language,
                    filePath: projectPath,
                    outputFilePath: string.Empty,
                    compilationOptions: compilationOptions,
                    parseOptions: parseOptions,
                    documents: SpecializedCollections.EmptyEnumerable<DocumentInfo>(),
                    projectReferences: SpecializedCollections.EmptyEnumerable<ProjectReference>(),
                    metadataReferences: SpecializedCollections.EmptyEnumerable<MetadataReference>(),
                    analyzerReferences: SpecializedCollections.EmptyEnumerable<AnalyzerReference>(),
                    additionalDocuments: SpecializedCollections.EmptyEnumerable<DocumentInfo>(),
                    isSubmission: false,
                    hostObjectType: null);
            }

            private async Task<ProjectInfo> CreateProjectInfoAsync(ProjectFileInfo projectFileInfo, ProjectId projectId, CancellationToken cancellationToken)
            {
                var projectDirectory = Path.GetDirectoryName(projectFileInfo.FilePath);
                var projectName = Path.GetFileNameWithoutExtension(projectFileInfo.FilePath);
                var version = GetProjectVersion(projectFileInfo.FilePath);

                // parse command line arguments
                var commandLineParser = _owner.GetLanguageService<ICommandLineParserService>(projectFileInfo.Language);

                var commandLineArgs = commandLineParser.Parse(
                    arguments: projectFileInfo.CommandLineArgs,
                    baseDirectory: projectDirectory,
                    isInteractive: false,
                    sdkDirectory: RuntimeEnvironment.GetRuntimeDirectory());

                var assemblyName = commandLineArgs.CompilationName;
                if (string.IsNullOrWhiteSpace(assemblyName))
                {
                    // if there isn't an assembly name, make one from the file path.
                    // Note: This may not be necessary any longer if the commmand line args
                    // always produce a valid compilation name.
                    assemblyName = GetAssemblyNameFromProjectPath(projectFileInfo.FilePath);
                }

                // Ensure sure that doc-are parsed
                var parseOptions = commandLineArgs.ParseOptions;
                if (parseOptions.DocumentationMode == DocumentationMode.None)
                {
                    parseOptions = parseOptions.WithDocumentationMode(DocumentationMode.Parse);
                }

                // add all the extra options that are really behavior overrides
                var metadataService = _owner.GetWorkspaceService<IMetadataService>();
                var compilationOptions = commandLineArgs.CompilationOptions
                    .WithXmlReferenceResolver(new XmlFileResolver(projectDirectory))
                    .WithSourceReferenceResolver(new SourceFileResolver(ImmutableArray<string>.Empty, projectDirectory))
                    // TODO: https://github.com/dotnet/roslyn/issues/4967
                    .WithMetadataReferenceResolver(new WorkspaceMetadataFileReferenceResolver(metadataService, new RelativePathResolver(ImmutableArray<string>.Empty, projectDirectory)))
                    .WithStrongNameProvider(new DesktopStrongNameProvider(commandLineArgs.KeyFileSearchPaths))
                    .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default);

                var documents = CreateDocumentInfos(projectFileInfo.Documents, projectId, commandLineArgs.Encoding);
                var additionalDocuments = CreateDocumentInfos(projectFileInfo.AdditionalDocuments, projectId, commandLineArgs.Encoding);
                CheckForDuplicateDocuments(documents, additionalDocuments, projectFileInfo.FilePath, projectId);

                var analyzerReferences = ResolveAnalyzerReferences(commandLineArgs);

                var resolvedReferences = await ResolveReferencesAsync(projectId, projectFileInfo, commandLineArgs, cancellationToken).ConfigureAwait(false);

                return ProjectInfo.Create(
                    projectId,
                    version,
                    projectName,
                    assemblyName,
                    projectFileInfo.Language,
                    projectFileInfo.FilePath,
                    projectFileInfo.OutputFilePath,
                    compilationOptions: compilationOptions,
                    parseOptions: parseOptions,
                    documents: documents,
                    projectReferences: resolvedReferences.ProjectReferences,
                    metadataReferences: resolvedReferences.MetadataReferences,
                    analyzerReferences: analyzerReferences,
                    additionalDocuments: additionalDocuments,
                    isSubmission: false,
                    hostObjectType: null);
            }

            private static VersionStamp GetProjectVersion(string projectFilePath)
            {
                if (!string.IsNullOrEmpty(projectFilePath) && File.Exists(projectFilePath))
                {
                    return VersionStamp.Create(File.GetLastWriteTimeUtc(projectFilePath));
                }
                else
                {
                    return VersionStamp.Create();
                }
            }

            private static string GetAssemblyNameFromProjectPath(string projectFilePath)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(projectFilePath);

                // if this is still unreasonable, use a fixed name.
                if (string.IsNullOrWhiteSpace(assemblyName))
                {
                    assemblyName = "assembly";
                }

                return assemblyName;
            }

            private IEnumerable<AnalyzerReference> ResolveAnalyzerReferences(CommandLineArguments commandLineArgs)
            {
                var analyzerService = _owner.GetWorkspaceService<IAnalyzerService>();
                var analyzerLoader = analyzerService.GetLoader();

                foreach (var path in commandLineArgs.AnalyzerReferences.Select(r => r.FilePath))
                {
                    var fullPath = Path.GetFullPath(path);
                    if (File.Exists(fullPath))
                    {
                        analyzerLoader.AddDependencyLocation(fullPath);
                    }
                }

                return commandLineArgs.ResolveAnalyzerReferences(analyzerLoader);
            }

            private static ImmutableArray<DocumentInfo> CreateDocumentInfos(IReadOnlyList<DocumentFileInfo> documentFileInfos, ProjectId projectId, Encoding encoding)
            {
                var results = ImmutableArray.CreateBuilder<DocumentInfo>();

                foreach (var info in documentFileInfos)
                {
                    GetDocumentNameAndFolders(info.LogicalPath, out var name, out var folders);

                    var documentInfo = DocumentInfo.Create(
                        DocumentId.CreateNewId(projectId, debugName: info.FilePath),
                        name,
                        folders,
                        info.SourceCodeKind,
                        new FileTextLoader(info.FilePath, encoding),
                        info.FilePath,
                        info.IsGenerated);

                    results.Add(documentInfo);
                }

                return results.ToImmutable();
            }

            private static readonly char[] s_directorySplitChars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

            private static void GetDocumentNameAndFolders(string logicalPath, out string name, out ImmutableArray<string> folders)
            {
                var pathNames = logicalPath.Split(s_directorySplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (pathNames.Length > 0)
                {
                    if (pathNames.Length > 1)
                    {
                        folders = pathNames.Take(pathNames.Length - 1).ToImmutableArray();
                    }
                    else
                    {
                        folders = ImmutableArray.Create<string>();
                    }

                    name = pathNames[pathNames.Length - 1];
                }
                else
                {
                    name = logicalPath;
                    folders = ImmutableArray.Create<string>();
                }
            }

            private void CheckForDuplicateDocuments(ImmutableArray<DocumentInfo> documents, ImmutableArray<DocumentInfo> additionalDocuments, string projectFilePath, ProjectId projectId)
            {
                var paths = new HashSet<string>();
                foreach (var doc in documents.Concat(additionalDocuments))
                {
                    if (paths.Contains(doc.FilePath))
                    {
                        var message = string.Format(WorkspacesResources.Duplicate_source_file_0_in_project_1, doc.FilePath, projectFilePath);
                        var diagnostic = new ProjectDiagnostic(WorkspaceDiagnosticKind.Warning, message, projectId);

                        _owner.ReportWorkspaceDiagnostic(diagnostic);
                    }

                    paths.Add(doc.FilePath);
                }
            }

            private struct ResolvedReferences
            {
                public ImmutableArray<ProjectReference> ProjectReferences { get; }
                public ImmutableArray<MetadataReference> MetadataReferences { get; }

                public ResolvedReferences(ImmutableArray<ProjectReference> projectReferences, ImmutableArray<MetadataReference> metadataReferences)
                {
                    ProjectReferences = projectReferences;
                    MetadataReferences = metadataReferences;
                }
            }

            private class MetadataReferenceSet
            {
                private readonly ImmutableArray<MetadataReference> _metadataReferences;
                private readonly ImmutableDictionary<string, HashSet<int>> _pathToIndexMap;
                private readonly HashSet<int> _indecesToRemove;

                public MetadataReferenceSet(ImmutableArray<MetadataReference> metadataReferences)
                {
                    _metadataReferences = metadataReferences;
                    _pathToIndexMap = CreatePathToIndexMap(metadataReferences);
                    _indecesToRemove = new HashSet<int>();
                }

                private static ImmutableDictionary<string, HashSet<int>> CreatePathToIndexMap(IEnumerable<MetadataReference> metadataReferences)
                {
                    var pathToIndexMap = ImmutableDictionary.CreateBuilder<string, HashSet<int>>(PathUtilities.Comparer);

                    var index = 0;
                    foreach (var metadataReference in metadataReferences)
                    {
                        var filePath = GetFilePath(metadataReference);
                        if (!pathToIndexMap.TryGetValue(filePath, out var indeces))
                        {
                            indeces = new HashSet<int>();
                            pathToIndexMap.Add(filePath, indeces);
                        }

                        indeces.Add(index);

                        index++;
                    }

                    return pathToIndexMap.ToImmutable();
                }

                public bool Contains(string filePath) => _pathToIndexMap.ContainsKey(filePath);

                public void Remove(string filePath)
                {
                    var indexSet = _pathToIndexMap[filePath];
                    _indecesToRemove.AddRange(indexSet);
                }

                public ImmutableArray<MetadataReference> GetRemaining()
                {
                    var results = ImmutableArray.CreateBuilder<MetadataReference>(initialCapacity: _metadataReferences.Length);

                    var index = 0;
                    foreach (var metadataReference in _metadataReferences)
                    {
                        if (!_indecesToRemove.Contains(index))
                        {
                            results.Add(metadataReference);
                        }

                        index++;
                    }

                    return results.ToImmutable();
                }

                public ProjectInfo Find(IEnumerable<ProjectInfo> projectInfos)
                {
                    foreach (var projectInfo in projectInfos)
                    {
                        if (_pathToIndexMap.ContainsKey(projectInfo.OutputFilePath))
                        {
                            return projectInfo;
                        }
                    }

                    return null;
                }

                public ProjectFileInfo Find(IEnumerable<ProjectFileInfo> projectFileInfos)
                {
                    foreach (var projectInfo in projectFileInfos)
                    {
                        if (_pathToIndexMap.ContainsKey(projectInfo.OutputFilePath))
                        {
                            return projectInfo;
                        }
                    }

                    return null;
                }
            }

            private async Task<ResolvedReferences> ResolveReferencesAsync(ProjectId id, ProjectFileInfo projectFileInfo, CommandLineArguments commandLineArgs, CancellationToken cancellationToken)
            {
                // First, gather all of the metadata references from the command-line arguments.
                var resolvedMetadataReferences = commandLineArgs.ResolveMetadataReferences(
                    new WorkspaceMetadataFileReferenceResolver(
                        metadataService: _owner.GetWorkspaceService<IMetadataService>(),
                        pathResolver: new RelativePathResolver(commandLineArgs.ReferencePaths, commandLineArgs.BaseDirectory))).ToImmutableArray();

                var projectReferences = new List<ProjectReference>(capacity: projectFileInfo.ProjectReferences.Count);
                var metadataReferenceSet = new MetadataReferenceSet(resolvedMetadataReferences);

                var projectDirectory = Path.GetDirectoryName(projectFileInfo.FilePath);

                // Next, iterate through all project references in the file and create project references.
                foreach (var projectFileReference in projectFileInfo.ProjectReferences)
                {
                    if (_owner.TryGetAbsoluteProjectPath(projectFileReference.Path, baseDirectory: projectDirectory, _discoveredProjectOptions.OnPathFailure, out var fullPath))
                    {
                        // If the project is already loaded, add a reference to it and remove its output from the metadata references.
                        if (TryAddProjectReferenceToLoadedOrMappedProject(id, fullPath, metadataReferenceSet, projectFileReference.Aliases, projectReferences))
                        {
                            continue;
                        }

                        _owner.TryGetLoaderFromProjectPath(fullPath, ReportMode.Ignore, out var loader);
                        if (loader == null)
                        {
                            // We don't have a full project loader, but we can try to use project evaluation to get the output file path.
                            // If that works, we can check to see if the output path is in the metadata references. If it is, we're done:
                            // Leave the metadata reference and don't create a project reference.
                            var outputFilePath = await _owner.BuildManager.TryGetOutputFilePathAsync(fullPath, _globalProperties, cancellationToken).ConfigureAwait(false);
                            if (!string.IsNullOrEmpty(outputFilePath) &&
                                metadataReferenceSet.Contains(outputFilePath) &&
                                File.Exists(outputFilePath))
                            {
                                continue;
                            }
                        }

                        // If metadata is preferred or there's no loader for this project, see if the output path exists.
                        // If it does, don't create a project reference.
                        if (_preferMetadataForReferencedProjects)
                        {
                            var ignoredReportingOptions = new ReportingOptions
                            {
                                OnPathFailure = ReportMode.Ignore,
                                OnLoaderFailure = ReportMode.Ignore
                            };

                            var projectRefFileInfos = await LoadProjectFileInfosAsync(fullPath, ignoredReportingOptions, cancellationToken).ConfigureAwait(false);

                            var done = false;
                            foreach (var projectRefFileInfo in projectRefFileInfos)
                            {
                                if (!string.IsNullOrEmpty(projectRefFileInfo.OutputFilePath) && metadataReferenceSet.Contains(projectRefFileInfo.OutputFilePath))
                                {
                                    // We found it!
                                    if (File.Exists(projectRefFileInfo.OutputFilePath))
                                    {
                                        done = true;
                                        break;
                                    }
                                }
                            }

                            if (done)
                            {
                                continue;
                            }

                            // We didn't find the output file path in this project's metadata references, or it doesn't exist on disk.
                            // In that case, carry on and load the project.
                        }

                        // OK, we've got to load the project.
                        var projectRefInfos = await LoadProjectInfosFromPathAsync(fullPath, _discoveredProjectOptions, cancellationToken).ConfigureAwait(false);

                        var projectRefInfo = metadataReferenceSet.Find(projectRefInfos);
                        if (projectRefInfo != null)
                        {
                            // Don't add a reference if the project already has a reference on us. Otherwise, it will cause a circularity.
                            if (projectRefInfo.ProjectReferences.Any(pr => pr.ProjectId == id))
                            {
                                // If the metadata doesn't exist on disk, we'll have to remove it from the metadata references.
                                if (!File.Exists(projectRefInfo.OutputFilePath))
                                {
                                    metadataReferenceSet.Remove(projectRefInfo.OutputFilePath);
                                }
                            }
                            else
                            {
                                projectReferences.Add(CreateProjectReference(id, projectRefInfo.Id, projectFileReference.Aliases));
                                metadataReferenceSet.Remove(projectRefInfo.OutputFilePath);
                            }

                            continue;
                        }
                    }

                    // We weren't able to handle this project reference, so add it without further processing.
                    var unknownProjectId = _projectMap.GetOrCreateProjectId(projectFileReference.Path);
                    projectReferences.Add(CreateProjectReference(id, unknownProjectId, projectFileReference.Aliases));
                }

                return new ResolvedReferences(
                    projectReferences.ToImmutableArray(),
                    metadataReferenceSet.GetRemaining());
            }

            private ProjectReference CreateProjectReference(ProjectId from, ProjectId to, ImmutableArray<string> aliases)
            {
                var result = new ProjectReference(to, aliases);
                if (!_projectIdToProjectReferencesMap.TryGetValue(from, out var references))
                {
                    references = new List<ProjectReference>();
                    _projectIdToProjectReferencesMap.Add(from, references);
                }

                references.Add(result);

                return result;
            }

            private bool ProjectReferenceExists(ProjectId to, ProjectId from)
                => _projectIdToProjectReferencesMap.TryGetValue(from, out var references)
                && references.Contains(pr => pr.ProjectId == to);

            /// <summary>
            /// Try to get the <see cref="ProjectId"/> for an already loaded project whose output path is in the given <paramref name="metadataReferenceSet"/>.
            /// </summary>
            private bool TryAddProjectReferenceToLoadedOrMappedProject(ProjectId id, string projectReferencePath, MetadataReferenceSet metadataReferenceSet, ImmutableArray<string> aliases, List<ProjectReference> projectReferences)
            {
                if (_projectMap.TryGetIdsByProjectPath(projectReferencePath, out var mappedIds))
                {
                    foreach (var mappedId in mappedIds)
                    {
                        // Don't add a reference if the project already has a reference on us. Otherwise, it will cause a circularity.
                        if (ProjectReferenceExists(to: id, from: mappedId))
                        {
                            return false;
                        }

                        if (_projectMap.TryGetOutputFilePathById(mappedId, out var outputFilePath))
                        {
                            if (metadataReferenceSet.Contains(outputFilePath))
                            {
                                metadataReferenceSet.Remove(outputFilePath);
                                projectReferences.Add(CreateProjectReference(id, mappedId, aliases));
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            private static string GetFilePath(MetadataReference metadataReference)
            {
                if (metadataReference is PortableExecutableReference portableExecutableReference)
                {
                    return portableExecutableReference.FilePath;
                }
                else if (metadataReference is UnresolvedMetadataReference unresolvedMetadataReference)
                {
                    return unresolvedMetadataReference.Reference;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
