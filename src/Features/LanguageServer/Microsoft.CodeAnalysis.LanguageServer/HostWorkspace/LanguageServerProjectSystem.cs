// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild.Build;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Export(typeof(LanguageServerProjectSystem)), Shared]
internal sealed class LanguageServerProjectSystem
{
    private readonly ProjectFileLoaderRegistry _projectFileLoaderRegistry;

    /// <summary>
    /// A single gate for code that is adding work to <see cref="_projectsToLoadAndReload" /> and modifying <see cref="_msbuildLoaded" />.
    /// This is just we don't have code simultaneously trying to load and unload solutions at once.
    /// </summary>
    private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);

    private bool _msbuildLoaded = false;

    private readonly AsyncBatchingWorkQueue<string> _projectsToLoadAndReload;

    private readonly LanguageServerWorkspaceFactory _workspaceFactory;
    private readonly IFileChangeWatcher _fileChangeWatcher;
    private readonly ILogger _logger;

    /// <summary>
    /// The list of loaded projects in the workspace, keyed by project file path. The outer dictionary is a concurrent dictionary since we may be loading
    /// multiple projects at once; the key is a single List we just have a single thread processing any given project file. This is only to be used
    /// in <see cref="LoadOrReloadProjectsAsync" /> and downstream calls; any other updating of this (like unloading projects) should be achieved by adding
    /// things to the <see cref="_projectsToLoadAndReload" />.
    /// </summary>
    private readonly ConcurrentDictionary<string, List<LoadedProject>> _loadedProjects = new();

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LanguageServerProjectSystem(
        LanguageServerWorkspaceFactory workspaceFactory,
        IFileChangeWatcher fileChangeWatcher,
        ILoggerFactory loggerFactory,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _workspaceFactory = workspaceFactory;
        _fileChangeWatcher = fileChangeWatcher;
        _logger = loggerFactory.CreateLogger(nameof(LanguageServerProjectSystem));

        // TODO: remove the DiagnosticReporter that's coupled to the Workspace here
        _projectFileLoaderRegistry = new ProjectFileLoaderRegistry(workspaceFactory.Workspace.Services.SolutionServices, new DiagnosticReporter(workspaceFactory.Workspace));

        _projectsToLoadAndReload = new AsyncBatchingWorkQueue<string>(
            TimeSpan.FromMilliseconds(100),
            LoadOrReloadProjectsAsync,
            StringComparer.Ordinal,
            listenerProvider.GetListener(FeatureAttribute.Workspace),
            CancellationToken.None); // TODO: do we need to introduce a shutdown cancellation token for this?
    }

    public async Task OpenSolutionAsync(string solutionFilePath)
    {

        if (await TryEnsureMSBuildLoadedAsync(Path.GetDirectoryName(solutionFilePath)!))
            await OpenSolutionCoreAsync(solutionFilePath);
    }

    [MethodImpl(MethodImplOptions.NoInlining)] // Don't inline; the caller needs to ensure MSBuild is loaded before we can use MSBuild types here
    private async Task OpenSolutionCoreAsync(string solutionFilePath)
    {
        using (await _gate.DisposableWaitAsync())
        {
            _logger.LogInformation($"Loading {solutionFilePath}...");
            var solutionFile = Microsoft.Build.Construction.SolutionFile.Parse(solutionFilePath);
            _workspaceFactory.ProjectSystemProjectFactory.SolutionPath = solutionFilePath;

            foreach (var project in solutionFile.ProjectsInOrder)
            {
                if (project.ProjectType == Microsoft.Build.Construction.SolutionProjectType.SolutionFolder)
                {
                    continue;
                }

                _projectsToLoadAndReload.AddWork(project.AbsolutePath);
            }

            // Wait for the in progress batch to complete and send a project initialized notification to the client.
            await _projectsToLoadAndReload.WaitUntilCurrentBatchCompletesAsync();
            await ProjectInitializationHandler.SendProjectInitializationCompleteNotificationAsync();
        }
    }

    public async Task OpenProjectsAsync(ImmutableArray<string> projectFilePaths)
    {
        if (!projectFilePaths.Any())
            return;

        if (await TryEnsureMSBuildLoadedAsync(Path.GetDirectoryName(projectFilePaths.First())!))
            await OpenProjectsCoreAsync(projectFilePaths);
    }

    [MethodImpl(MethodImplOptions.NoInlining)] // Don't inline; the caller needs to ensure MSBuild is loaded before we can use MSBuild types here
    private async Task OpenProjectsCoreAsync(ImmutableArray<string> projectFilePaths)
    {
        using (await _gate.DisposableWaitAsync())
        {
            _projectsToLoadAndReload.AddWork(projectFilePaths);

            // Wait for the in progress batch to complete and send a project initialized notification to the client.
            await _projectsToLoadAndReload.WaitUntilCurrentBatchCompletesAsync();
            await ProjectInitializationHandler.SendProjectInitializationCompleteNotificationAsync();
        }
    }

    private async Task<bool> TryEnsureMSBuildLoadedAsync(string workingDirectory)
    {
        using (await _gate.DisposableWaitAsync())
        {
            if (_msbuildLoaded)
            {
                return true;
            }
            else
            {
                var msbuildDiscoveryOptions = new VisualStudioInstanceQueryOptions { DiscoveryTypes = DiscoveryType.DotNetSdk, WorkingDirectory = workingDirectory };
                var msbuildInstances = MSBuildLocator.QueryVisualStudioInstances(msbuildDiscoveryOptions);
                var msbuildInstance = msbuildInstances.FirstOrDefault();

                if (msbuildInstance != null)
                {
                    MSBuildLocator.RegisterInstance(msbuildInstance);
                    _logger.LogInformation($"Loaded MSBuild at {msbuildInstance.MSBuildPath}");
                    _msbuildLoaded = true;

                    return true;
                }
                else
                {
                    _logger.LogError($"Unable to find a MSBuild to use to load {workingDirectory}.");
                    await ShowToastNotification.ShowToastNotificationAsync(LSP.MessageType.Error, LanguageServerResources.There_were_problems_loading_your_projects_See_log_for_details, CancellationToken.None, ShowToastNotification.ShowCSharpLogsCommand);

                    return false;
                }
            }
        }
    }

    private async ValueTask LoadOrReloadProjectsAsync(ImmutableSegmentedList<string> projectPathsToLoadOrReload, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // TODO: support configuration switching
        var projectBuildManager = new ProjectBuildManager(additionalGlobalProperties: ImmutableDictionary<string, string>.Empty);

        projectBuildManager.StartBatchBuild();

        var displayedToast = 0;

        try
        {
            var tasks = new List<Task>();

            foreach (var projectPathToLoadOrReload in projectPathsToLoadOrReload)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var errorKind = await LoadOrReloadProjectAsync(projectPathToLoadOrReload, projectBuildManager, cancellationToken);
                    if (errorKind is not null)
                    {
                        // We should display a toast when the value of displayedToast is 0.  This will also update the value to 1 meaning we won't send any more toasts.
                        var shouldShowToast = Interlocked.CompareExchange(ref displayedToast, value: 1, comparand: 0) == 0;
                        if (shouldShowToast)
                        {
                            var message = string.Format(LanguageServerResources.There_were_problems_loading_project_0_See_log_for_details, Path.GetFileName(projectPathToLoadOrReload));
                            await ShowToastNotification.ShowToastNotificationAsync(errorKind.Value, message, cancellationToken, ShowToastNotification.ShowCSharpLogsCommand);
                        }
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
        finally
        {
            projectBuildManager.EndBatchBuild();

            _logger.LogInformation($"Completed (re)load of all projects in {stopwatch.Elapsed}");
        }
    }

    private async Task<LSP.MessageType?> LoadOrReloadProjectAsync(string projectPath, ProjectBuildManager projectBuildManager, CancellationToken cancellationToken)
    {
        try
        {
            if (_projectFileLoaderRegistry.TryGetLoaderFromProjectPath(projectPath, out var loader))
            {
                var loadedFile = await loader.LoadProjectFileAsync(projectPath, projectBuildManager, cancellationToken);
                var loadedProjectInfos = await loadedFile.GetProjectFileInfosAsync(cancellationToken);

                var existingProjects = _loadedProjects.GetOrAdd(projectPath, static _ => new List<LoadedProject>());

                foreach (var loadedProjectInfo in loadedProjectInfos)
                {
                    // If we already have the project, just update it
                    var existingProject = existingProjects.Find(p => p.GetTargetFramework() == loadedProjectInfo.TargetFramework);

                    if (existingProject != null)
                    {
                        await existingProject.UpdateWithNewProjectInfoAsync(loadedProjectInfo);
                    }
                    else
                    {
                        var projectSystemName = $"{projectPath} (${loadedProjectInfo.TargetFramework})";
                        var projectCreationInfo = new ProjectSystemProjectCreationInfo { AssemblyName = projectSystemName, FilePath = projectPath };

                        var projectSystemProject = await _workspaceFactory.ProjectSystemProjectFactory.CreateAndAddToWorkspaceAsync(
                            projectSystemName,
                            loadedProjectInfo.Language,
                            projectCreationInfo,
                            _workspaceFactory.ProjectSystemHostInfo);

                        var loadedProject = new LoadedProject(projectSystemProject, _workspaceFactory.Workspace.Services.SolutionServices, _fileChangeWatcher, _workspaceFactory.TargetFrameworkManager);
                        loadedProject.NeedsReload += (_, _) => _projectsToLoadAndReload.AddWork(projectPath);
                        existingProjects.Add(loadedProject);

                        await loadedProject.UpdateWithNewProjectInfoAsync(loadedProjectInfo);
                    }
                }

                if (loadedFile.Log.Any())
                {
                    foreach (var logItem in loadedFile.Log)
                    {
                        var projectName = Path.GetFileName(projectPath);
                        _logger.Log(logItem.Kind is WorkspaceDiagnosticKind.Failure ? LogLevel.Error : LogLevel.Warning, $"{logItem.Kind} while loading {logItem.ProjectFilePath}: {logItem.Message}");
                    }

                    return loadedFile.Log.Any(logItem => logItem.Kind is WorkspaceDiagnosticKind.Failure) ? LSP.MessageType.Error : LSP.MessageType.Warning;
                }
                else
                {
                    _logger.LogInformation($"Successfully completed load of {projectPath}");
                    return null;
                }
            }

            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Exception thrown while loading {projectPath}");
            return LSP.MessageType.Error;
        }
    }
}
