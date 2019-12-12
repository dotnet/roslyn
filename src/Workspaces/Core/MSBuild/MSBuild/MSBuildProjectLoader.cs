// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild.Build;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// An API for loading msbuild project files.
    /// </summary>
    public partial class MSBuildProjectLoader
    {
        // the workspace that the projects and solutions are intended to be loaded into.
        private readonly Workspace _workspace;

        private readonly DiagnosticReporter _diagnosticReporter;
        private readonly PathResolver _pathResolver;
        private readonly ProjectFileLoaderRegistry _projectFileLoaderRegistry;

        // used to protect access to the following mutable state
        private readonly NonReentrantLock _dataGuard = new NonReentrantLock();
        private ImmutableDictionary<string, string> _properties;

        internal MSBuildProjectLoader(
            Workspace workspace,
            DiagnosticReporter diagnosticReporter,
            ProjectFileLoaderRegistry projectFileLoaderRegistry,
            ImmutableDictionary<string, string> properties)
        {
            _workspace = workspace;
            _diagnosticReporter = diagnosticReporter ?? new DiagnosticReporter(workspace);
            _pathResolver = new PathResolver(_diagnosticReporter);
            _projectFileLoaderRegistry = projectFileLoaderRegistry ?? new ProjectFileLoaderRegistry(workspace, _diagnosticReporter);

            _properties = ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase);

            if (properties != null)
            {
                _properties = _properties.AddRange(properties);
            }
        }

        /// <summary>
        /// Create a new instance of an <see cref="MSBuildProjectLoader"/>.
        /// </summary>
        /// <param name="workspace">The workspace whose services this <see cref="MSBuildProjectLoader"/> should use.</param>
        /// <param name="properties">An optional dictionary of additional MSBuild properties and values to use when loading projects.
        /// These are the same properties that are passed to msbuild via the /property:&lt;n&gt;=&lt;v&gt; command line argument.</param>
        public MSBuildProjectLoader(Workspace workspace, ImmutableDictionary<string, string> properties = null)
            : this(workspace, diagnosticReporter: null, projectFileLoaderRegistry: null, properties)
        {
        }

        /// <summary>
        /// The MSBuild properties used when interpreting project files.
        /// These are the same properties that are passed to msbuild via the /property:&lt;n&gt;=&lt;v&gt; command line argument.
        /// </summary>
        public ImmutableDictionary<string, string> Properties => _properties;

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

            _projectFileLoaderRegistry.AssociateFileExtensionWithLanguage(projectFileExtension, language);
        }

        private void SetSolutionProperties(string solutionFilePath)
        {
            const string SolutionDirProperty = "SolutionDir";

            // When MSBuild is building an individual project, it doesn't define $(SolutionDir).
            // However when building an .sln file, or when working inside Visual Studio,
            // $(SolutionDir) is defined to be the directory where the .sln file is located.
            // Some projects out there rely on $(SolutionDir) being set (although the best practice is to
            // use MSBuildProjectDirectory which is always defined).
            if (!string.IsNullOrEmpty(solutionFilePath))
            {
                var solutionDirectory = PathUtilities.GetDirectoryName(solutionFilePath) + PathUtilities.DirectorySeparatorChar;

                if (Directory.Exists(solutionDirectory))
                {
                    _properties = _properties.SetItem(SolutionDirProperty, solutionDirectory);
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
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> to allow cancellation of this operation.</param>
        public async Task<SolutionInfo> LoadSolutionInfoAsync(
            string solutionFilePath,
            IProgress<ProjectLoadProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (solutionFilePath == null)
            {
                throw new ArgumentNullException(nameof(solutionFilePath));
            }

            if (!_pathResolver.TryGetAbsoluteSolutionPath(solutionFilePath, baseDirectory: Directory.GetCurrentDirectory(), DiagnosticReportingMode.Throw, out var absoluteSolutionPath))
            {
                // TryGetAbsoluteSolutionPath should throw before we get here.
                return null;
            }

            using (_dataGuard.DisposableWait(cancellationToken))
            {
                this.SetSolutionProperties(absoluteSolutionPath);
            }

            var solutionFile = MSB.Construction.SolutionFile.Parse(absoluteSolutionPath);

            var reportingMode = GetReportingModeForUnrecognizedProjects();

            var reportingOptions = new DiagnosticReportingOptions(
                onPathFailure: reportingMode,
                onLoaderFailure: reportingMode);

            var projectPaths = ImmutableArray.CreateBuilder<string>();

            // load all the projects
            foreach (var project in solutionFile.ProjectsInOrder)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (project.ProjectType != MSB.Construction.SolutionProjectType.SolutionFolder)
                {
                    projectPaths.Add(project.RelativePath);
                }
            }

            var buildManager = new ProjectBuildManager(_properties);

            var worker = new Worker(
                _workspace,
                _diagnosticReporter,
                _pathResolver,
                _projectFileLoaderRegistry,
                buildManager,
                projectPaths.ToImmutable(),
                baseDirectory: Path.GetDirectoryName(absoluteSolutionPath),
                _properties,
                projectMap: null,
                progress,
                requestedProjectOptions: reportingOptions,
                discoveredProjectOptions: reportingOptions,
                preferMetadataForReferencesOfDiscoveredProjects: false);

            var projects = await worker.LoadAsync(cancellationToken).ConfigureAwait(false);

            // construct workspace from loaded project infos
            return SolutionInfo.Create(
                SolutionId.CreateNewId(debugName: absoluteSolutionPath),
                version: default,
                absoluteSolutionPath,
                projects);
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
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> to allow cancellation of this operation.</param>
        public async Task<ImmutableArray<ProjectInfo>> LoadProjectInfoAsync(
            string projectFilePath,
            ProjectMap projectMap = null,
            IProgress<ProjectLoadProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (projectFilePath == null)
            {
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            var requestedProjectOptions = DiagnosticReportingOptions.ThrowForAll;

            var reportingMode = GetReportingModeForUnrecognizedProjects();

            var discoveredProjectOptions = new DiagnosticReportingOptions(
                onPathFailure: reportingMode,
                onLoaderFailure: reportingMode);

            var buildManager = new ProjectBuildManager(_properties);

            var worker = new Worker(
                _workspace,
                _diagnosticReporter,
                _pathResolver,
                _projectFileLoaderRegistry,
                buildManager,
                requestedProjectPaths: ImmutableArray.Create(projectFilePath),
                baseDirectory: Directory.GetCurrentDirectory(),
                globalProperties: _properties,
                projectMap,
                progress,
                requestedProjectOptions,
                discoveredProjectOptions,
                this.LoadMetadataForReferencedProjects);

            return await worker.LoadAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
