// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.VSCode.API;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild.Build;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Export(typeof(LanguageServerProjectSystem)), Shared]
internal sealed class LanguageServerProjectSystem
{
    private readonly ILogger _logger;
    private readonly ProjectSystemProjectFactory _projectSystemProjectFactory;
    private readonly ProjectFileLoaderRegistry _projectFileLoaderRegistry;
    private readonly AsyncBatchingWorkQueue<string> _projectsToLoadAndReload;
    private readonly ProjectSystemHostInfo _projectHostInfo;
    private readonly IFileChangeWatcher _fileChangeWatcher;

    /// <summary>
    /// The list of loaded projects in the workspace, keyed by project file path. The outer dictionary is a concurrent dictionary since we may be loading
    /// multiple projects at once; the key is a single List we just have a single thread processing any given project file.
    /// </summary>
    private readonly ConcurrentDictionary<string, List<LoadedProject>> _loadedProjects = new();

    [ImportingConstructor]
    public LanguageServerProjectSystem(HostServicesProvider hostServicesProvider, VSCodeAnalyzerLoader analyzerLoader, IFileChangeWatcher fileChangeWatcher, ILoggerFactory loggerFactory, IAsynchronousOperationListenerProvider listenerProvider)
    {
        _logger = loggerFactory.CreateLogger(nameof(LanguageServerProjectSystem));
        Workspace = new LanguageServerWorkspace(hostServicesProvider.HostServices);
        _projectSystemProjectFactory = new ProjectSystemProjectFactory(Workspace, new FileChangeWatcher(), static _ => { }, _ => { });

        analyzerLoader.InitializeDiagnosticsServices(Workspace);

        // TODO: remove the DiagnosticReporter that's coupled to the Workspace here
        _projectFileLoaderRegistry = new ProjectFileLoaderRegistry(Workspace.Services.SolutionServices, new DiagnosticReporter(Workspace));

        _projectsToLoadAndReload = new AsyncBatchingWorkQueue<string>(
            TimeSpan.FromMilliseconds(100),
            LoadOrReloadProjectsAsync,
            StringComparer.Ordinal,
            listenerProvider.GetListener(FeatureAttribute.Workspace),
            CancellationToken.None); // TODO: do we need to introduce a shutdown cancellation token for this?

        // TODO: fill this out
        _projectHostInfo = new ProjectSystemHostInfo(
            DynamicFileInfoProviders: ImmutableArray<Lazy<IDynamicFileInfoProvider, Host.Mef.FileExtensionsMetadata>>.Empty,
            null!,
            null!);
        _fileChangeWatcher = fileChangeWatcher;
    }

    public Workspace Workspace { get; }

    public async Task InitializeSolutionLevelAnalyzersAsync(ImmutableArray<string> analyzerPaths)
    {
        var references = new List<AnalyzerFileReference>();
        var analyzerLoader = VSCodeAnalyzerLoader.CreateAnalyzerAssemblyLoader();

        foreach (var analyzerPath in analyzerPaths)
        {
            if (File.Exists(analyzerPath))
            {
                _logger.LogWarning($"Solution-level analyzer at {analyzerPath} added to workspace.");
                references.Add(new AnalyzerFileReference(analyzerPath, analyzerLoader));
            }
            else
            {
                _logger.LogWarning($"Solution-level analyzer at {analyzerPath} could not be found.");
            }
        }

        await _projectSystemProjectFactory.ApplyChangeToWorkspaceAsync(w => w.SetCurrentSolution(s => s.WithAnalyzerReferences(references), WorkspaceChangeKind.SolutionChanged)).ConfigureAwait(false);
    }

    public void OpenSolution(string solutionFilePath)
    {
        _logger.LogInformation($"Opening solution {solutionFilePath}");

        var solutionFile = Microsoft.Build.Construction.SolutionFile.Parse(solutionFilePath);

        foreach (var project in solutionFile.ProjectsInOrder)
        {
            if (project.ProjectType == Microsoft.Build.Construction.SolutionProjectType.SolutionFolder)
            {
                continue;
            }

            _projectsToLoadAndReload.AddWork(project.AbsolutePath);
        }
    }

    private async ValueTask LoadOrReloadProjectsAsync(ImmutableSegmentedList<string> projectPathsToLoadOrReload, CancellationToken disposalToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // TODO: support configuration switching
        var projectBuildManager = new ProjectBuildManager(additionalGlobalProperties: ImmutableDictionary<string, string>.Empty);

        projectBuildManager.StartBatchBuild();

        try
        {
            var tasks = new List<Task>();

            foreach (var projectPathToLoadOrReload in projectPathsToLoadOrReload)
            {
                tasks.Add(Task.Run(() => LoadOrReloadProjectAsync(projectPathToLoadOrReload, projectBuildManager, disposalToken), disposalToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        finally
        {
            projectBuildManager.EndBatchBuild();

            _logger.LogInformation($"Completed (re)load of all projects in {stopwatch.Elapsed}");
        }
    }

    private async Task LoadOrReloadProjectAsync(string projectPath, ProjectBuildManager projectBuildManager, CancellationToken disposalToken)
    {
        try
        {
            if (_projectFileLoaderRegistry.TryGetLoaderFromProjectPath(projectPath, out var loader))
            {
                var loadedFile = await loader.LoadProjectFileAsync(projectPath, projectBuildManager, disposalToken).ConfigureAwait(false);
                var loadedProjectInfos = await loadedFile.GetProjectFileInfosAsync(disposalToken).ConfigureAwait(false);

                var existingProjects = _loadedProjects.GetOrAdd(projectPath, static _ => new List<LoadedProject>());

                foreach (var loadedProjectInfo in loadedProjectInfos)
                {
                    // If we already have the project, just update it
                    var existingProject = existingProjects.Find(p => p.GetTargetFramework() == loadedProjectInfo.TargetFramework);

                    if (existingProject != null)
                    {
                        await existingProject.UpdateWithNewProjectInfoAsync(loadedProjectInfo).ConfigureAwait(false);
                    }
                    else
                    {
                        var projectSystemName = $"{projectPath} (${loadedProjectInfo.TargetFramework})";
                        var projectCreationInfo = new ProjectSystemProjectCreationInfo { AssemblyName = projectSystemName, FilePath = projectPath };

                        var projectSystemProject = await _projectSystemProjectFactory.CreateAndAddToWorkspaceAsync(
                            projectSystemName,
                            loadedProjectInfo.Language,
                            projectCreationInfo,
                            _projectHostInfo).ConfigureAwait(false);

                        var loadedProject = new LoadedProject(projectSystemProject, Workspace.Services.SolutionServices, _fileChangeWatcher);
                        loadedProject.NeedsReload += (_, _) => _projectsToLoadAndReload.AddWork(projectPath);
                        existingProjects.Add(loadedProject);

                        await loadedProject.UpdateWithNewProjectInfoAsync(loadedProjectInfo).ConfigureAwait(false);
                    }
                }
            }

            _logger.LogInformation($"Successfully completed load of {projectPath}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Exception thrown while loading {projectPath}");
        }
    }
}
