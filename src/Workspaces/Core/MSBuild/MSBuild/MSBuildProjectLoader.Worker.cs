﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.MSBuild.Build;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    public partial class MSBuildProjectLoader
    {
        private partial class Worker
        {
            private readonly Workspace _workspace;
            private readonly DiagnosticReporter _diagnosticReporter;
            private readonly PathResolver _pathResolver;
            private readonly ProjectFileLoaderRegistry _projectFileLoaderRegistry;
            private readonly ProjectBuildManager _buildManager;
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
            /// Progress reporter.
            /// </summary>
            private readonly IProgress<ProjectLoadProgress> _progress;

            /// <summary>
            /// Provides options for how failures should be reported when loading requested project files.
            /// </summary>
            private readonly DiagnosticReportingOptions _requestedProjectOptions;

            /// <summary>
            /// Provides options for how failures should be reported when loading any discovered project files.
            /// </summary>
            private readonly DiagnosticReportingOptions _discoveredProjectOptions;

            /// <summary>
            /// When true, metadata is preferred for any project reference unless the referenced project is already loaded
            /// because it was requested.
            /// </summary>
            private readonly bool _preferMetadataForReferencesOfDiscoveredProjects;

            private readonly Dictionary<ProjectId, ProjectFileInfo> _projectIdToFileInfoMap;
            private readonly Dictionary<ProjectId, List<ProjectReference>> _projectIdToProjectReferencesMap;
            private readonly Dictionary<string, ImmutableArray<ProjectInfo>> _pathToDiscoveredProjectInfosMap;

            public Worker(
                Workspace workspace,
                DiagnosticReporter diagnosticReporter,
                PathResolver pathResolver,
                ProjectFileLoaderRegistry projectFileLoaderRegistry,
                ProjectBuildManager buildManager,
                ImmutableArray<string> requestedProjectPaths,
                string baseDirectory,
                ImmutableDictionary<string, string> globalProperties,
                ProjectMap projectMap,
                IProgress<ProjectLoadProgress> progress,
                DiagnosticReportingOptions requestedProjectOptions,
                DiagnosticReportingOptions discoveredProjectOptions,
                bool preferMetadataForReferencesOfDiscoveredProjects)
            {
                _workspace = workspace;
                _diagnosticReporter = diagnosticReporter;
                _pathResolver = pathResolver;
                _projectFileLoaderRegistry = projectFileLoaderRegistry;
                _buildManager = buildManager;
                _baseDirectory = baseDirectory;
                _requestedProjectPaths = requestedProjectPaths;
                _globalProperties = globalProperties;
                _projectMap = projectMap ?? ProjectMap.Create();
                _progress = progress;
                _requestedProjectOptions = requestedProjectOptions;
                _discoveredProjectOptions = discoveredProjectOptions;
                _preferMetadataForReferencesOfDiscoveredProjects = preferMetadataForReferencesOfDiscoveredProjects;
                _projectIdToFileInfoMap = new Dictionary<ProjectId, ProjectFileInfo>();
                _pathToDiscoveredProjectInfosMap = new Dictionary<string, ImmutableArray<ProjectInfo>>(PathUtilities.Comparer);
                _projectIdToProjectReferencesMap = new Dictionary<ProjectId, List<ProjectReference>>();
            }

            private async Task<TResult> DoOperationAndReportProgressAsync<TResult>(ProjectLoadOperation operation, string projectPath, string targetFramework, Func<Task<TResult>> doFunc)
            {
                var watch = _progress != null
                    ? Stopwatch.StartNew()
                    : null;

                TResult result;
                try
                {
                    result = await doFunc().ConfigureAwait(false);
                }
                finally
                {
                    if (_progress != null)
                    {
                        watch.Stop();
                        _progress.Report(new ProjectLoadProgress(projectPath, operation, targetFramework, watch.Elapsed));
                    }
                }

                return result;
            }

            public async Task<ImmutableArray<ProjectInfo>> LoadAsync(CancellationToken cancellationToken)
            {
                var results = ImmutableArray.CreateBuilder<ProjectInfo>();
                var processedPaths = new HashSet<string>(PathUtilities.Comparer);

                _buildManager.StartBatchBuild(_globalProperties);
                try
                {
                    foreach (var projectPath in _requestedProjectPaths)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (!_pathResolver.TryGetAbsoluteProjectPath(projectPath, _baseDirectory, _requestedProjectOptions.OnPathFailure, out var absoluteProjectPath))
                        {
                            continue; // Failure should already be reported.
                        }

                        if (!processedPaths.Add(absoluteProjectPath))
                        {
                            _diagnosticReporter.Report(
                                new WorkspaceDiagnostic(
                                    WorkspaceDiagnosticKind.Warning,
                                    string.Format(WorkspaceMSBuildResources.Duplicate_project_discovered_and_skipped_0, absoluteProjectPath)));

                            continue;
                        }

                        var projectFileInfos = await LoadProjectInfosFromPathAsync(absoluteProjectPath, _requestedProjectOptions, cancellationToken).ConfigureAwait(false);

                        results.AddRange(projectFileInfos);
                    }

                    foreach (var (projectPath, projectInfos) in _pathToDiscoveredProjectInfosMap)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (!processedPaths.Contains(projectPath))
                        {
                            results.AddRange(projectInfos);
                        }
                    }

                    return results.ToImmutable();
                }
                finally
                {
                    _buildManager.EndBatchBuild();
                }
            }

            private async Task<ImmutableArray<ProjectFileInfo>> LoadProjectFileInfosAsync(string projectPath, DiagnosticReportingOptions reportingOptions, CancellationToken cancellationToken)
            {
                if (!_projectFileLoaderRegistry.TryGetLoaderFromProjectPath(projectPath, reportingOptions.OnLoaderFailure, out var loader))
                {
                    return ImmutableArray<ProjectFileInfo>.Empty; // Failure should already be reported.
                }

                var projectFile = await DoOperationAndReportProgressAsync(
                    ProjectLoadOperation.Evaluate,
                    projectPath,
                    targetFramework: null,
                    () => loader.LoadProjectFileAsync(projectPath, _buildManager, cancellationToken)
                ).ConfigureAwait(false);

                // If there were any failures during load, we won't be able to build the project. So, bail early with an empty project.
                if (projectFile.Log.HasFailure)
                {
                    _diagnosticReporter.Report(projectFile.Log);

                    return ImmutableArray.Create(
                        ProjectFileInfo.CreateEmpty(loader.Language, projectPath, projectFile.Log));
                }

                var projectFileInfos = await DoOperationAndReportProgressAsync(
                    ProjectLoadOperation.Build,
                    projectPath,
                    targetFramework: null,
                    () => projectFile.GetProjectFileInfosAsync(cancellationToken)
                ).ConfigureAwait(false);

                var results = ImmutableArray.CreateBuilder<ProjectFileInfo>(projectFileInfos.Length);

                foreach (var projectFileInfo in projectFileInfos)
                {
                    // If any diagnostics were logged during build, we'll carry on and try to produce a meaningful project.
                    if (!projectFileInfo.Log.IsEmpty)
                    {
                        _diagnosticReporter.Report(projectFileInfo.Log);
                    }

                    results.Add(projectFileInfo);
                }

                return results.MoveToImmutable();
            }

            private async Task<ImmutableArray<ProjectInfo>> LoadProjectInfosFromPathAsync(
                string projectPath, DiagnosticReportingOptions reportingOptions, CancellationToken cancellationToken)
            {
                if (_projectMap.TryGetProjectInfosByProjectPath(projectPath, out var results) ||
                    _pathToDiscoveredProjectInfosMap.TryGetValue(projectPath, out results))
                {
                    return results;
                }

                var builder = ImmutableArray.CreateBuilder<ProjectInfo>();

                var projectFileInfos = await LoadProjectFileInfosAsync(projectPath, reportingOptions, cancellationToken).ConfigureAwait(false);

                var idsAndFileInfos = new List<(ProjectId id, ProjectFileInfo fileInfo)>();

                foreach (var projectFileInfo in projectFileInfos)
                {
                    var projectId = _projectMap.GetOrCreateProjectId(projectFileInfo);

                    if (_projectIdToFileInfoMap.ContainsKey(projectId))
                    {
                        // There are multiple projects with the same project path and output path. This can happen
                        // if a multi-TFM project does not have unique output file paths for each TFM. In that case,
                        // we'll create a new ProjectId to ensure that the project is added to the workspace.

                        _diagnosticReporter.Report(
                            DiagnosticReportingMode.Log,
                            string.Format(WorkspaceMSBuildResources.Found_project_with_the_same_file_path_and_output_path_as_another_project_0, projectFileInfo.FilePath));

                        projectId = ProjectId.CreateNewId(debugName: projectFileInfo.FilePath);
                    }

                    idsAndFileInfos.Add((projectId, projectFileInfo));
                    _projectIdToFileInfoMap.Add(projectId, projectFileInfo);
                }

                // If this project resulted in more than a single project, a discrimator (e.g. TFM) should be
                // added to the project name.
                var addDiscriminator = idsAndFileInfos.Count > 1;

                foreach (var (id, fileInfo) in idsAndFileInfos)
                {
                    var projectInfo = await CreateProjectInfoAsync(fileInfo, id, addDiscriminator, cancellationToken).ConfigureAwait(false);

                    builder.Add(projectInfo);
                    _projectMap.AddProjectInfo(projectInfo);
                }

                results = builder.ToImmutable();

                _pathToDiscoveredProjectInfosMap.Add(projectPath, results);

                return results;
            }

            private Task<ProjectInfo> CreateProjectInfoAsync(ProjectFileInfo projectFileInfo, ProjectId projectId, bool addDiscriminator, CancellationToken cancellationToken)
            {
                var language = projectFileInfo.Language;
                var projectPath = projectFileInfo.FilePath;

                var projectName = Path.GetFileNameWithoutExtension(projectPath);
                if (addDiscriminator && !string.IsNullOrWhiteSpace(projectFileInfo.TargetFramework))
                {
                    projectName += "(" + projectFileInfo.TargetFramework + ")";
                }

                var version = VersionStamp.Create(
                    FileUtilities.GetFileTimeStamp(projectPath));

                if (projectFileInfo.IsEmpty)
                {
                    var assemblyName = GetAssemblyNameFromProjectPath(projectPath);

                    var parseOptions = GetLanguageService<ISyntaxTreeFactoryService>(language)
                        .GetDefaultParseOptions();
                    var compilationOptions = GetLanguageService<ICompilationFactoryService>(language)
                        .GetDefaultCompilationOptions();

                    return Task.FromResult(
                        ProjectInfo.Create(
                            projectId,
                            version,
                            projectName,
                            assemblyName: assemblyName,
                            language: language,
                            filePath: projectPath,
                            outputFilePath: string.Empty,
                            outputRefFilePath: string.Empty,
                            compilationOptions: compilationOptions,
                            parseOptions: parseOptions,
                            documents: SpecializedCollections.EmptyEnumerable<DocumentInfo>(),
                            projectReferences: SpecializedCollections.EmptyEnumerable<ProjectReference>(),
                            metadataReferences: SpecializedCollections.EmptyEnumerable<MetadataReference>(),
                            analyzerReferences: SpecializedCollections.EmptyEnumerable<AnalyzerReference>(),
                            additionalDocuments: SpecializedCollections.EmptyEnumerable<DocumentInfo>(),
                            isSubmission: false,
                            hostObjectType: null));
                }

                return DoOperationAndReportProgressAsync(ProjectLoadOperation.Resolve, projectPath, projectFileInfo.TargetFramework, async () =>
                {
                    var projectDirectory = Path.GetDirectoryName(projectPath);

                    // parse command line arguments
                    var commandLineParser = GetLanguageService<ICommandLineParserService>(projectFileInfo.Language);

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
                        assemblyName = GetAssemblyNameFromProjectPath(projectPath);
                    }

                    // Ensure sure that doc-comments are parsed
                    var parseOptions = commandLineArgs.ParseOptions;
                    if (parseOptions.DocumentationMode == DocumentationMode.None)
                    {
                        parseOptions = parseOptions.WithDocumentationMode(DocumentationMode.Parse);
                    }

                    // add all the extra options that are really behavior overrides
                    var metadataService = GetWorkspaceService<IMetadataService>();
                    var compilationOptions = commandLineArgs.CompilationOptions
                        .WithXmlReferenceResolver(new XmlFileResolver(projectDirectory))
                        .WithSourceReferenceResolver(new SourceFileResolver(ImmutableArray<string>.Empty, projectDirectory))
                        // TODO: https://github.com/dotnet/roslyn/issues/4967
                        .WithMetadataReferenceResolver(new WorkspaceMetadataFileReferenceResolver(metadataService, new RelativePathResolver(ImmutableArray<string>.Empty, projectDirectory)))
                        .WithStrongNameProvider(new DesktopStrongNameProvider(commandLineArgs.KeyFileSearchPaths))
                        .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default);

                    var documents = CreateDocumentInfos(projectFileInfo.Documents, projectId, commandLineArgs.Encoding);
                    var additionalDocuments = CreateDocumentInfos(projectFileInfo.AdditionalDocuments, projectId, commandLineArgs.Encoding);
                    CheckForDuplicateDocuments(documents, additionalDocuments, projectPath, projectId);

                    var analyzerReferences = ResolveAnalyzerReferences(commandLineArgs);

                    var resolvedReferences = await ResolveReferencesAsync(projectId, projectFileInfo, commandLineArgs, cancellationToken).ConfigureAwait(false);

                    return ProjectInfo.Create(
                        projectId,
                        version,
                        projectName,
                        assemblyName,
                        language,
                        projectPath,
                        outputFilePath: projectFileInfo.OutputFilePath,
                        outputRefFilePath: projectFileInfo.OutputRefFilePath,
                        compilationOptions: compilationOptions,
                        parseOptions: parseOptions,
                        documents: documents,
                        projectReferences: resolvedReferences.ProjectReferences,
                        metadataReferences: resolvedReferences.MetadataReferences,
                        analyzerReferences: analyzerReferences,
                        additionalDocuments: additionalDocuments,
                        isSubmission: false,
                        hostObjectType: null)
                        .WithDefaultNamespace(projectFileInfo.DefaultNamespace);
                });
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
                var analyzerService = GetWorkspaceService<IAnalyzerService>();
                var analyzerLoader = analyzerService.GetLoader();

                foreach (var path in commandLineArgs.AnalyzerReferences.Select(r => r.FilePath))
                {
                    string fullPath;

                    if (PathUtilities.IsAbsolute(path))
                    {
                        fullPath = FileUtilities.TryNormalizeAbsolutePath(path);

                        if (fullPath != null && File.Exists(fullPath))
                        {
                            analyzerLoader.AddDependencyLocation(fullPath);
                        }
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
                        folders = ImmutableArray<string>.Empty;
                    }

                    name = pathNames[pathNames.Length - 1];
                }
                else
                {
                    name = logicalPath;
                    folders = ImmutableArray<string>.Empty;
                }
            }

            private void CheckForDuplicateDocuments(ImmutableArray<DocumentInfo> documents, ImmutableArray<DocumentInfo> additionalDocuments, string projectFilePath, ProjectId projectId)
            {
                var paths = new HashSet<string>(PathUtilities.Comparer);
                foreach (var doc in documents.Concat(additionalDocuments))
                {
                    if (paths.Contains(doc.FilePath))
                    {
                        var message = string.Format(WorkspacesResources.Duplicate_source_file_0_in_project_1, doc.FilePath, projectFilePath);
                        var diagnostic = new ProjectDiagnostic(WorkspaceDiagnosticKind.Warning, message, projectId);

                        _diagnosticReporter.Report(diagnostic);
                    }

                    paths.Add(doc.FilePath);
                }
            }

            private TLanguageService GetLanguageService<TLanguageService>(string languageName)
                where TLanguageService : ILanguageService
                => _workspace.Services
                    .GetLanguageServices(languageName)
                    .GetService<TLanguageService>();

            private TWorkspaceService GetWorkspaceService<TWorkspaceService>()
                where TWorkspaceService : IWorkspaceService
                => _workspace.Services
                    .GetService<TWorkspaceService>();
        }
    }
}
