// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild;

/// <summary>
/// An API for loading MSBuild project files.
/// </summary>
public partial class MSBuildProjectLoader
{
    // the services for the projects and solutions are intended to be loaded into.
    private readonly SolutionServices _solutionServices;

    private readonly DiagnosticReporter _diagnosticReporter;
    private readonly Microsoft.Extensions.Logging.ILoggerFactory _loggerFactory;
    private readonly PathResolver _pathResolver;
    private readonly ProjectFileExtensionRegistry _projectFileExtensionRegistry;

    // used to protect access to the following mutable state
    private readonly NonReentrantLock _dataGuard = new();

    internal MSBuildProjectLoader(
        SolutionServices solutionServices,
        DiagnosticReporter diagnosticReporter,
        ImmutableDictionary<string, string>? properties)
    {
        _solutionServices = solutionServices;
        _diagnosticReporter = diagnosticReporter;
        _loggerFactory = new Microsoft.Extensions.Logging.LoggerFactory([new DiagnosticReporterLoggerProvider(_diagnosticReporter)]);
        _pathResolver = new PathResolver(_diagnosticReporter);
        _projectFileExtensionRegistry = new ProjectFileExtensionRegistry(solutionServices, diagnosticReporter);

        Properties = ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase);

        if (properties != null)
        {
            Properties = Properties.AddRange(properties);
        }
    }

    /// <summary>
    /// Create a new instance of an <see cref="MSBuildProjectLoader"/>.
    /// </summary>
    /// <param name="workspace">The workspace whose services this <see cref="MSBuildProjectLoader"/> should use.</param>
    /// <param name="properties">An optional dictionary of additional MSBuild properties and values to use when loading projects.
    /// These are the same properties that are passed to MSBuild via the /property:&lt;n&gt;=&lt;v&gt; command line argument.</param>
    public MSBuildProjectLoader(Workspace workspace, ImmutableDictionary<string, string>? properties = null)
        : this(workspace.Services.SolutionServices, new DiagnosticReporter(workspace), properties)
    {
    }

    internal DiagnosticReporter Reporter
        => _diagnosticReporter;

    internal ProjectFileExtensionRegistry ProjectFileExtensionRegistry
        => _projectFileExtensionRegistry;

    internal Microsoft.Extensions.Logging.ILoggerFactory LoggerFactory
        => _loggerFactory;

    /// <summary>
    /// The MSBuild properties used when interpreting project files.
    /// These are the same properties that are passed to MSBuild via the /property:&lt;n&gt;=&lt;v&gt; command line argument.
    /// </summary>
    public ImmutableDictionary<string, string> Properties { get; private set; }

    /// <summary>
    /// Determines if metadata from existing output assemblies is loaded instead of opening referenced projects.
    /// If the referenced project is already opened, the metadata will not be loaded.
    /// If the metadata assembly cannot be found the referenced project will be opened instead.
    /// </summary>
    public bool LoadMetadataForReferencedProjects { get; set; } = false;

    /// <summary>
    /// Determines if unrecognized projects are skipped when solutions or projects are opened.
    ///
    /// A project is unrecognized if it either has
    ///   a) an invalid file path,
    ///   b) a non-existent project file,
    ///   c) has an unrecognized file extension or
    ///   d) a file extension associated with an unsupported language.
    ///
    /// If unrecognized projects cannot be skipped a corresponding exception is thrown.
    /// </summary>
    public bool SkipUnrecognizedProjects { get; set; } = true;

    /// <summary>
    /// Associates a project file extension with a language name.
    /// </summary>
    /// <param name="projectFileExtension">The project file extension to associate with <paramref name="language"/>.</param>
    /// <param name="language">The language to associate with <paramref name="projectFileExtension"/>. This value
    /// should typically be taken from <see cref="LanguageNames"/>.</param>
    public void AssociateFileExtensionWithLanguage(string projectFileExtension, string language)
    {
        if (projectFileExtension == null)
        {
            throw new ArgumentNullException(nameof(projectFileExtension));
        }

        if (language == null)
        {
            throw new ArgumentNullException(nameof(language));
        }

        _projectFileExtensionRegistry.AssociateFileExtensionWithLanguage(projectFileExtension, language);
    }

    private void SetSolutionProperties(string? solutionFilePath)
    {
        const string SolutionDirProperty = "SolutionDir";

        // When MSBuild is building an individual project, it doesn't define $(SolutionDir).
        // However when building an .sln file, or when working inside Visual Studio,
        // $(SolutionDir) is defined to be the directory where the .sln file is located.
        // Some projects out there rely on $(SolutionDir) being set (although the best practice is to
        // use MSBuildProjectDirectory which is always defined).
        if (!RoslynString.IsNullOrEmpty(solutionFilePath))
        {
            var solutionDirectory = PathUtilities.GetDirectoryName(solutionFilePath) + PathUtilities.DirectorySeparatorChar;

            if (Directory.Exists(solutionDirectory))
            {
                Properties = Properties.SetItem(SolutionDirProperty, solutionDirectory);
            }
        }
    }

    private DiagnosticReportingMode GetReportingModeForUnrecognizedProjects()
        => this.SkipUnrecognizedProjects
            ? DiagnosticReportingMode.Log
            : DiagnosticReportingMode.Throw;

    /// <summary>
    /// Loads the <see cref="SolutionInfo"/> for the specified solution file, including all projects referenced by the solution file and
    /// all the projects referenced by the project files.
    /// </summary>
    /// <param name="solutionFilePath">The path to the solution file to be loaded. This may be an absolute path or a path relative to the
    /// current working directory.</param>
    /// <param name="progress">An optional <see cref="IProgress{T}"/> that will receive updates as the solution is loaded.</param>
    /// <param name="msbuildLogger">An optional <see cref="ILogger"/> that will log MSBuild results.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> to allow cancellation of this operation.</param>
    public async Task<SolutionInfo> LoadSolutionInfoAsync(
        string solutionFilePath,
        IProgress<ProjectLoadProgress>? progress = null,
        ILogger? msbuildLogger = null,
        CancellationToken cancellationToken = default)
    {
        if (solutionFilePath == null)
            throw new ArgumentNullException(nameof(solutionFilePath));

        var reportingMode = GetReportingModeForUnrecognizedProjects();
        var reportingOptions = new DiagnosticReportingOptions(
            onPathFailure: reportingMode,
            onLoaderFailure: reportingMode);

        var (absoluteSolutionPath, projects) = await SolutionFileReader.ReadSolutionFileAsync(solutionFilePath, _pathResolver, reportingMode, cancellationToken).ConfigureAwait(false);

        // TryGetAbsoluteSolutionPath should not return an invalid path
        var solutionDir = Path.GetDirectoryName(absoluteSolutionPath)!;

        var projectPaths =
            from project in projects
            let fullPath = _pathResolver.TryGetAbsoluteProjectPath(project.ProjectPath, solutionDir, reportingMode, out var absoluteProjectPath) ? absoluteProjectPath : null
            where fullPath != null
            select fullPath;

        using (_dataGuard.DisposableWait(cancellationToken))
        {
            SetSolutionProperties(absoluteSolutionPath);
        }

        var projectInfos = await LoadInfoAsync(
            [.. projectPaths],
            projectMap: null,
            progress,
            msbuildLogger,
            requestedProjectOptions: reportingOptions,
            discoveredProjectOptions: reportingOptions,
            preferMetadataForReferencesOfDiscoveredProjects: false,
            cancellationToken).ConfigureAwait(false);

        // construct workspace from loaded project infos
        return SolutionInfo.Create(
            SolutionId.CreateNewId(debugName: absoluteSolutionPath),
            version: default,
            absoluteSolutionPath,
            projectInfos);
    }

    /// <summary>
    /// Loads the <see cref="ProjectInfo"/> from the specified project file and all referenced projects.
    /// The first <see cref="ProjectInfo"/> in the result corresponds to the specified project file.
    /// </summary>
    /// <param name="projectFilePath">The path to the project file to be loaded. This may be an absolute path or a path relative to the
    /// current working directory.</param>
    /// <param name="projectMap">An optional <see cref="ProjectMap"/> that will be used to resolve project references to existing projects.
    /// This is useful when populating a custom <see cref="Workspace"/>.</param>
    /// <param name="progress">An optional <see cref="IProgress{T}"/> that will receive updates as the project is loaded.</param>
    /// <param name="msbuildLogger">An optional <see cref="ILogger"/> that will log msbuild results.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> to allow cancellation of this operation.</param>
    public Task<ImmutableArray<ProjectInfo>> LoadProjectInfoAsync(
        string projectFilePath,
        ProjectMap? projectMap = null,
        IProgress<ProjectLoadProgress>? progress = null,
        ILogger? msbuildLogger = null,
        CancellationToken cancellationToken = default)
    {
        if (projectFilePath == null)
            throw new ArgumentNullException(nameof(projectFilePath));

        var reportingMode = GetReportingModeForUnrecognizedProjects();
        var requestedProjectOptions = DiagnosticReportingOptions.ThrowForAll;
        var discoveredProjectOptions = new DiagnosticReportingOptions(
            onPathFailure: reportingMode,
            onLoaderFailure: reportingMode);

        if (!_pathResolver.TryGetAbsoluteProjectPath(projectFilePath, Directory.GetCurrentDirectory(), reportingMode, out var absoluteProjectPath))
        {
            return Task.FromResult(ImmutableArray<ProjectInfo>.Empty);
        }

        return LoadInfoAsync(
            [absoluteProjectPath],
            projectMap,
            progress,
            msbuildLogger,
            requestedProjectOptions,
            discoveredProjectOptions,
            LoadMetadataForReferencedProjects,
            cancellationToken);
    }

    private async Task<ImmutableArray<ProjectInfo>> LoadInfoAsync(
        ImmutableArray<string> projectPaths,
        ProjectMap? projectMap,
        IProgress<ProjectLoadProgress>? progress,
        ILogger? msbuildLogger,
        DiagnosticReportingOptions requestedProjectOptions,
        DiagnosticReportingOptions discoveredProjectOptions,
        bool preferMetadataForReferencesOfDiscoveredProjects,
        CancellationToken cancellationToken)
    {
        var binLogPathProvider = IsBinaryLogger(msbuildLogger, out var fileName)
            ? new BinLogPathProvider(fileName)
            : null;

        var buildHostProcessManager = new BuildHostProcessManager(Properties, binLogPathProvider, _loggerFactory);
        await using var _ = buildHostProcessManager.ConfigureAwait(false);

        var projectFileProvider = new BuildHostProjectFileInfoProvider(
            buildHostProcessManager,
            _projectFileExtensionRegistry,
            _diagnosticReporter,
            progress);

        var worker = new Worker(
            _solutionServices,
            _diagnosticReporter,
            _pathResolver,
            _projectFileExtensionRegistry,
            projectFileProvider,
            projectPaths,
            projectMap,
            progress,
            requestedProjectOptions,
            discoveredProjectOptions,
            preferMetadataForReferencesOfDiscoveredProjects);

        return await worker.LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    internal Task<ImmutableArray<ProjectInfo>> LoadInfosAsync(
        ImmutableArray<string> projectFilePaths,
        IProjectFileInfoProvider projectFileInfoProvider,
        ProjectMap? projectMap,
        IProgress<ProjectLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        Debug.Assert(projectFilePaths.All(PathUtilities.IsAbsolute));

        var reportingMode = GetReportingModeForUnrecognizedProjects();
        var requestedProjectOptions = DiagnosticReportingOptions.ThrowForAll;
        var discoveredProjectOptions = new DiagnosticReportingOptions(
            onPathFailure: reportingMode,
            onLoaderFailure: reportingMode);

        var worker = new Worker(
            _solutionServices,
            _diagnosticReporter,
            _pathResolver,
            _projectFileExtensionRegistry,
            projectFileInfoProvider,
            projectFilePaths,
            projectMap,
            progress,
            requestedProjectOptions,
            discoveredProjectOptions,
            LoadMetadataForReferencedProjects);

        return worker.LoadAsync(cancellationToken);
    }
    private static bool IsBinaryLogger([NotNullWhen(returnValue: true)] ILogger? logger, out string? fileName)
    {
        // We validate the type name to avoid taking a dependency on the Microsoft.Build package
        // because it brings along additional dependencies and servicing requirements.
        if (logger?.GetType().FullName != "Microsoft.Build.Logging.BinaryLogger")
        {
            fileName = null;
            return false;
        }

        // The logger.Parameters could contain more than just the filename, such as "ProjectImports" or "OmitInitialInfo".
        // Attempt to get the parsed filname directly from the logger if possible.
        var fileNameProperty = logger.GetType().GetProperty("FileName");
        fileName = fileNameProperty?.GetValue(logger) as string ?? logger.Parameters;
        return true;
    }

    internal sealed class BinLogPathProvider : IBinLogPathProvider
    {
        private const string DefaultFileName = "msbuild";
        private const string DefaultExtension = ".binlog";

        private readonly string _directory;
        private readonly string _filename;
        private readonly string _extension;
        private int _suffix = -1;

        public BinLogPathProvider(string? logFilePath)
        {
            logFilePath ??= DefaultFileName + DefaultExtension;

            _directory = Path.GetDirectoryName(logFilePath) ?? ".";
            _filename = Path.GetFileNameWithoutExtension(logFilePath) is { Length: > 0 } fileName
                ? fileName
                : DefaultFileName;
            _extension = Path.GetExtension(logFilePath) is { Length: > 0 } extension
                ? extension
                : DefaultExtension;
        }

        public string? GetNewLogPath()
        {
            var suffix = Interlocked.Increment(ref _suffix);

            var newPath = suffix == 0
                ? Path.Combine(_directory, _filename + _extension)
                : Path.Combine(_directory, $"{_filename}-{suffix}{_extension}");

            return Path.GetFullPath(newPath);
        }
    }
}
