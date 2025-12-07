// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild;

public partial class MSBuildProjectLoader
{
    private sealed partial class Worker
    {
        private readonly SolutionServices _solutionServices;
        private readonly DiagnosticReporter _diagnosticReporter;
        private readonly PathResolver _pathResolver;
        private readonly ProjectFileExtensionRegistry _projectFileExtensionRegistry;
        private readonly string _baseDirectory;
        private readonly IProjectFileInfoProvider _projectFileInfoProvider;

        /// <summary>
        /// An ordered list of paths to project files that should be loaded. In the case of a solution,
        /// this is the list of project file paths in the solution.
        /// </summary>
        private readonly ImmutableArray<string> _requestedProjectPaths;

        /// <summary>
        /// Map of <see cref="ProjectId"/>s, project paths, and output file paths.
        /// </summary>
        private readonly ProjectMap _projectMap;

        /// <summary>
        /// Progress reporter.
        /// </summary>
        private readonly IProgress<ProjectLoadProgress>? _progress;

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
            SolutionServices services,
            DiagnosticReporter diagnosticReporter,
            PathResolver pathResolver,
            ProjectFileExtensionRegistry projectFileExtensionRegistry,
            IProjectFileInfoProvider projectFileInfoProvider,
            ImmutableArray<string> requestedProjectPaths,
            string baseDirectory,
            ProjectMap? projectMap,
            IProgress<ProjectLoadProgress>? progress,
            DiagnosticReportingOptions requestedProjectOptions,
            DiagnosticReportingOptions discoveredProjectOptions,
            bool preferMetadataForReferencesOfDiscoveredProjects)
        {
            _solutionServices = services;
            _diagnosticReporter = diagnosticReporter;
            _pathResolver = pathResolver;
            _projectFileExtensionRegistry = projectFileExtensionRegistry;
            _projectFileInfoProvider = projectFileInfoProvider;
            _baseDirectory = baseDirectory;
            _requestedProjectPaths = requestedProjectPaths;
            _projectMap = projectMap ?? ProjectMap.Create();
            _progress = progress;
            _requestedProjectOptions = requestedProjectOptions;
            _discoveredProjectOptions = discoveredProjectOptions;
            _preferMetadataForReferencesOfDiscoveredProjects = preferMetadataForReferencesOfDiscoveredProjects;
            _projectIdToFileInfoMap = [];
            _pathToDiscoveredProjectInfosMap = new Dictionary<string, ImmutableArray<ProjectInfo>>(PathUtilities.Comparer);
            _projectIdToProjectReferencesMap = [];
        }

        public async Task<ImmutableArray<ProjectInfo>> LoadAsync(CancellationToken cancellationToken)
        {
            var results = ImmutableArray.CreateBuilder<ProjectInfo>();
            var processedPaths = new HashSet<string>(PathUtilities.Comparer);

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

            return results.ToImmutableAndClear();
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

            var projectFileInfos = await _projectFileInfoProvider.LoadProjectFileInfosAsync(projectPath, reportingOptions, cancellationToken).ConfigureAwait(false);

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

            // If this project resulted in more than a single project, a discriminator (e.g. TFM) should be
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
            var projectName = Path.GetFileNameWithoutExtension(projectPath) ?? string.Empty;
            if (addDiscriminator && !RoslynString.IsNullOrWhiteSpace(projectFileInfo.TargetFramework))
            {
                projectName += "(" + projectFileInfo.TargetFramework + ")";
            }

            var version = projectPath is null
                ? VersionStamp.Default
                : VersionStamp.Create(FileUtilities.GetFileTimeStamp(projectPath));

            if (projectFileInfo.IsEmpty)
            {
                var assemblyName = GetAssemblyNameFromProjectPath(projectPath);

                var parseOptions = GetLanguageService<ISyntaxTreeFactoryService>(language)
                    ?.GetDefaultParseOptions();
                var compilationOptions = GetLanguageService<ICompilationFactoryService>(language)
                    ?.GetDefaultCompilationOptions();

                return Task.FromResult(
                    ProjectInfo.Create(
                        new ProjectInfo.ProjectAttributes(
                            projectId,
                            version,
                            name: projectName,
                            assemblyName: assemblyName,
                            language: language,
                            compilationOutputInfo: new CompilationOutputInfo(projectFileInfo.IntermediateOutputFilePath, projectFileInfo.GeneratedFilesOutputDirectory),
                            checksumAlgorithm: SourceHashAlgorithms.Default,
                            outputFilePath: projectFileInfo.OutputFilePath,
                            outputRefFilePath: projectFileInfo.OutputRefFilePath,
                            filePath: projectPath),
                        compilationOptions: compilationOptions,
                        parseOptions: parseOptions));
            }

            return _progress.DoOperationAndReportProgressAsync(ProjectLoadOperation.Resolve, projectPath, projectFileInfo.TargetFramework, async () =>
            {
                var projectDirectory = Path.GetDirectoryName(projectPath);

                // parse command line arguments
                var commandLineParser = GetLanguageService<ICommandLineParserService>(projectFileInfo.Language);

                if (commandLineParser is null)
                {
                    var message = string.Format(WorkspaceMSBuildResources.Unable_to_find_a_0_for_1, nameof(ICommandLineParserService), projectFileInfo.Language);
                    throw new Exception(message);
                }

                var commandLineArgs = commandLineParser.Parse(
                    arguments: projectFileInfo.CommandLineArgs,
                    baseDirectory: projectDirectory,
                    isInteractive: false,
                    sdkDirectory: RuntimeEnvironment.GetRuntimeDirectory());

                var assemblyName = commandLineArgs.CompilationName;
                if (RoslynString.IsNullOrWhiteSpace(assemblyName))
                {
                    // if there isn't an assembly name, make one from the file path.
                    // Note: This may not be necessary any longer if the command line args
                    // always produce a valid compilation name.
                    assemblyName = GetAssemblyNameFromProjectPath(projectPath);
                }

                // Ensure that doc-comments are parsed
                var parseOptions = commandLineArgs.ParseOptions;
                if (parseOptions.DocumentationMode == DocumentationMode.None)
                {
                    parseOptions = parseOptions.WithDocumentationMode(DocumentationMode.Parse);
                }

                // add all the extra options that are really behavior overrides
                var metadataService = GetWorkspaceService<IMetadataService>();
                var compilationOptions = commandLineArgs.CompilationOptions
                    .WithXmlReferenceResolver(new XmlFileResolver(projectDirectory))
                    .WithSourceReferenceResolver(new SourceFileResolver([], projectDirectory))
                    // TODO: https://github.com/dotnet/roslyn/issues/4967
                    .WithMetadataReferenceResolver(new WorkspaceMetadataFileReferenceResolver(metadataService, new RelativePathResolver([], projectDirectory)))
                    .WithStrongNameProvider(new DesktopStrongNameProvider(commandLineArgs.KeyFileSearchPaths, Path.GetTempPath()))
                    .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default);

                var documents = CreateDocumentInfos(projectFileInfo.Documents, projectId, commandLineArgs.Encoding);
                var additionalDocuments = CreateDocumentInfos(projectFileInfo.AdditionalDocuments, projectId, commandLineArgs.Encoding);
                var analyzerConfigDocuments = CreateDocumentInfos(projectFileInfo.AnalyzerConfigDocuments, projectId, commandLineArgs.Encoding);
                CheckForDuplicateDocuments(documents.Concat(additionalDocuments).Concat(analyzerConfigDocuments), projectPath, projectId);

                var analyzerReferences = ResolveAnalyzerReferences(commandLineArgs);

                var resolvedReferences = await ResolveReferencesAsync(projectId, projectFileInfo, commandLineArgs, cancellationToken).ConfigureAwait(false);

                return ProjectInfo.Create(
                    new ProjectInfo.ProjectAttributes(
                        projectId,
                        version,
                        projectName,
                        assemblyName,
                        language,
                        compilationOutputInfo: new CompilationOutputInfo(projectFileInfo.IntermediateOutputFilePath, projectFileInfo.GeneratedFilesOutputDirectory),
                        checksumAlgorithm: commandLineArgs.ChecksumAlgorithm,
                        filePath: projectPath,
                        outputFilePath: projectFileInfo.OutputFilePath,
                        outputRefFilePath: projectFileInfo.OutputRefFilePath,
                        isSubmission: false),
                    compilationOptions: compilationOptions,
                    parseOptions: parseOptions,
                    documents: documents,
                    projectReferences: resolvedReferences.ProjectReferences,
                    metadataReferences: resolvedReferences.MetadataReferences,
                    analyzerReferences: analyzerReferences,
                    additionalDocuments: additionalDocuments,
                    hostObjectType: null)
                    .WithDefaultNamespace(projectFileInfo.DefaultNamespace)
                    .WithAnalyzerConfigDocuments(analyzerConfigDocuments);
            });
        }

        private static string GetAssemblyNameFromProjectPath(string? projectFilePath)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(projectFilePath);

            // if this is still unreasonable, use a fixed name.
            if (RoslynString.IsNullOrWhiteSpace(assemblyName))
            {
                assemblyName = "assembly";
            }

            return assemblyName;
        }

        private IEnumerable<AnalyzerReference> ResolveAnalyzerReferences(CommandLineArguments commandLineArgs)
        {
            var analyzerService = GetWorkspaceService<IAnalyzerService>();
            if (analyzerService is null)
            {
                var message = string.Format(WorkspaceMSBuildResources.Unable_to_find_0, nameof(IAnalyzerService));
                throw new Exception(message);
            }

            var analyzerLoader = analyzerService.GetLoader();

            foreach (var path in commandLineArgs.AnalyzerReferences.Select(r => r.FilePath))
            {
                string? fullPath;

                if (PathUtilities.IsAbsolute(path))
                {
                    fullPath = FileUtilities.TryNormalizeAbsolutePath(path);

                    if (fullPath != null && File.Exists(fullPath))
                    {
                        analyzerLoader.AddDependencyLocation(fullPath);
                    }
                }
            }

            return commandLineArgs.ResolveAnalyzerReferences(analyzerLoader).Distinct(AnalyzerReferencePathComparer.Instance);
        }

        private ImmutableArray<DocumentInfo> CreateDocumentInfos(IReadOnlyList<DocumentFileInfo> documentFileInfos, ProjectId projectId, Encoding? encoding)
        {
            var results = new FixedSizeArrayBuilder<DocumentInfo>(documentFileInfos.Count);

            foreach (var info in documentFileInfos)
            {
                GetDocumentNameAndFolders(info.LogicalPath, out var name, out var folders);

                var documentInfo = DocumentInfo.Create(
                    DocumentId.CreateNewId(projectId, debugName: info.FilePath),
                    name,
                    folders,
                    SourceCodeKind.Regular,
                    new WorkspaceFileTextLoader(_solutionServices, info.FilePath, encoding),
                    info.FilePath,
                    info.IsGenerated);

                results.Add(documentInfo);
            }

            return results.MoveToImmutable();
        }

        private static readonly char[] s_directorySplitChars = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

        private static void GetDocumentNameAndFolders(string logicalPath, out string name, out ImmutableArray<string> folders)
        {
            var pathNames = logicalPath.Split(s_directorySplitChars, StringSplitOptions.RemoveEmptyEntries);
            if (pathNames.Length > 0)
            {
                folders = pathNames.Length > 1
                    ? [.. pathNames.Take(pathNames.Length - 1)]
                    : [];

                name = pathNames[^1];
            }
            else
            {
                name = logicalPath;
                folders = [];
            }
        }

        private void CheckForDuplicateDocuments(ImmutableArray<DocumentInfo> documents, string? projectFilePath, ProjectId projectId)
        {
            var paths = new HashSet<string>(PathUtilities.Comparer);
            foreach (var doc in documents)
            {
                if (doc.FilePath is null)
                    continue;

                if (paths.Contains(doc.FilePath))
                {
                    var message = string.Format(WorkspacesResources.Duplicate_source_file_0_in_project_1, doc.FilePath, projectFilePath);
                    var diagnostic = new ProjectDiagnostic(WorkspaceDiagnosticKind.Warning, message, projectId);

                    _diagnosticReporter.Report(diagnostic);
                }

                paths.Add(doc.FilePath);
            }
        }

        private TLanguageService? GetLanguageService<TLanguageService>(string languageName)
            where TLanguageService : ILanguageService
            => _solutionServices
                .GetLanguageServices(languageName)
                .GetService<TLanguageService>();

        private TWorkspaceService? GetWorkspaceService<TWorkspaceService>()
            where TWorkspaceService : IWorkspaceService
            => _solutionServices
                .GetService<TWorkspaceService>();
    }
}
