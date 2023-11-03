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
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Export(typeof(LanguageServerProjectSystem)), Shared]
internal sealed class LanguageServerProjectSystem
{
    /// <summary>
    /// A single gate for code that is adding work to <see cref="_projectsToLoadAndReload" /> and modifying <see cref="_msbuildLoaded" />.
    /// This is just we don't have code simultaneously trying to load and unload solutions at once.
    /// </summary>
    private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);
    private bool _msbuildLoaded = false;

    /// <summary>
    /// The suffix to use for the binary log name; incremented each time we have a new build. Should be incremented with <see cref="Interlocked.Increment(ref int)"/>.
    /// </summary>
    private int _binaryLogNumericSuffix;

    /// <summary>
    /// A GUID put into all binary log file names, so that way one session doesn't accidentally overwrite the logs from a prior session.
    /// </summary>
    private readonly Guid _binaryLogGuidSuffix = Guid.NewGuid();

    private readonly AsyncBatchingWorkQueue<ProjectToLoad> _projectsToLoadAndReload;

    private readonly LanguageServerWorkspaceFactory _workspaceFactory;
    private readonly IFileChangeWatcher _fileChangeWatcher;
    private readonly IGlobalOptionService _globalOptionService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly ProjectLoadTelemetryReporter _projectLoadTelemetryReporter;

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
        IGlobalOptionService globalOptionService,
        ILoggerFactory loggerFactory,
        IAsynchronousOperationListenerProvider listenerProvider,
        ProjectLoadTelemetryReporter projectLoadTelemetry)
    {
        _workspaceFactory = workspaceFactory;
        _fileChangeWatcher = fileChangeWatcher;
        _globalOptionService = globalOptionService;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger(nameof(LanguageServerProjectSystem));
        _projectLoadTelemetryReporter = projectLoadTelemetry;

        _projectsToLoadAndReload = new AsyncBatchingWorkQueue<ProjectToLoad>(
            TimeSpan.FromMilliseconds(100),
            LoadOrReloadProjectsAsync,
            ProjectToLoad.Comparer,
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

                _projectsToLoadAndReload.AddWork(new ProjectToLoad(project.AbsolutePath, project.ProjectGuid));
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

        using (await _gate.DisposableWaitAsync())
        {
            _projectsToLoadAndReload.AddWork(projectFilePaths.Select(p => new ProjectToLoad(p, ProjectGuid: null)));

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
                    _logger.LogInformation($"Loaded MSBuild in-process from {msbuildInstance.MSBuildPath}");
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

    private async ValueTask LoadOrReloadProjectsAsync(ImmutableSegmentedList<ProjectToLoad> projectPathsToLoadOrReload, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // TODO: support configuration switching

        var binaryLogPath = GetMSBuildBinaryLogPath();
        var runBuildInProcess = _globalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.LoadInProcess);

        if (runBuildInProcess)
            _logger.LogInformation("In-process project loading is enabled.");

        await using var buildHostProcessManager = !runBuildInProcess ? new BuildHostProcessManager(_loggerFactory, binaryLogPath) : null;
        var inProcessBuildHost = runBuildInProcess ? new BuildHost(_loggerFactory, binaryLogPath) : null;

        var displayedToast = 0;

        try
        {
            var tasks = new List<Task>();

            foreach (var projectToLoad in projectPathsToLoadOrReload)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var errorKind = await LoadOrReloadProjectAsync(projectToLoad, buildHostProcessManager, inProcessBuildHost, cancellationToken);
                    if (errorKind is LSP.MessageType.Error)
                    {
                        // We should display a toast when the value of displayedToast is 0.  This will also update the value to 1 meaning we won't send any more toasts.
                        var shouldShowToast = Interlocked.CompareExchange(ref displayedToast, value: 1, comparand: 0) == 0;
                        if (shouldShowToast)
                        {
                            var message = string.Format(LanguageServerResources.There_were_problems_loading_project_0_See_log_for_details, Path.GetFileName(projectToLoad.Path));
                            await ShowToastNotification.ShowToastNotificationAsync(errorKind.Value, message, cancellationToken, ShowToastNotification.ShowCSharpLogsCommand);
                        }
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
        finally
        {
            _logger.LogInformation($"Completed (re)load of all projects in {stopwatch.Elapsed}");

            if (inProcessBuildHost != null)
                await inProcessBuildHost.ShutdownAsync();
        }
    }

    private string? GetMSBuildBinaryLogPath()
    {
        if (_globalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.BinaryLogPath) is not string binaryLogDirectory)
            return null;

        var numericSuffix = Interlocked.Increment(ref _binaryLogNumericSuffix);
        var binaryLogPath = Path.Combine(binaryLogDirectory, $"LanguageServerDesignTimeBuild-{_binaryLogGuidSuffix}-{numericSuffix}.binlog");

        _logger.LogInformation($"Logging design-time builds to {binaryLogPath}");

        return binaryLogPath;
    }

    private async Task<LSP.MessageType?> LoadOrReloadProjectAsync(ProjectToLoad projectToLoad, BuildHostProcessManager? buildHostProcessManager, BuildHost? inProcessBuildHost, CancellationToken cancellationToken)
    {
        try
        {
            var projectPath = projectToLoad.Path;

            // If we have a process manager, then get an OOP process; otherwise we're still using in-proc builds so just fetch one in-process
            var buildHost = inProcessBuildHost ?? await buildHostProcessManager!.GetBuildHostAsync(projectPath, cancellationToken);

            if (await buildHost.IsProjectFileSupportedAsync(projectPath, cancellationToken))
            {
                var loadedFile = await buildHost.LoadProjectFileAsync(projectPath, cancellationToken);
                var loadedProjectInfos = await loadedFile.GetProjectFileInfosAsync(cancellationToken);

                // The out-of-proc build host supports more languages than we may actually have Workspace binaries for, so ensure we can actually process that
                // language.
                var projectLanguage = loadedProjectInfos.FirstOrDefault()?.Language;
                if (projectLanguage != null && _workspaceFactory.Workspace.Services.GetLanguageService<ICommandLineParserService>(projectLanguage) == null)
                {
                    return null;
                }

                var existingProjects = _loadedProjects.GetOrAdd(projectPath, static _ => new List<LoadedProject>());

                Dictionary<ProjectFileInfo, (ImmutableArray<CommandLineReference> MetadataReferences, OutputKind OutputKind)> projectFileInfos = new();
                foreach (var loadedProjectInfo in loadedProjectInfos)
                {
                    // If we already have the project, just update it
                    var existingProject = existingProjects.Find(p => p.GetTargetFramework() == loadedProjectInfo.TargetFramework);

                    if (existingProject != null)
                    {
                        projectFileInfos[loadedProjectInfo] = await existingProject.UpdateWithNewProjectInfoAsync(loadedProjectInfo);
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
                        loadedProject.NeedsReload += (_, _) => _projectsToLoadAndReload.AddWork(projectToLoad);
                        existingProjects.Add(loadedProject);

                        projectFileInfos[loadedProjectInfo] = await loadedProject.UpdateWithNewProjectInfoAsync(loadedProjectInfo);
                    }
                }

                await _projectLoadTelemetryReporter.ReportProjectLoadTelemetryAsync(projectFileInfos, projectToLoad, cancellationToken);

                var diagnosticLogItems = await loadedFile.GetDiagnosticLogItemsAsync(cancellationToken);
                if (diagnosticLogItems.Any())
                {
                    foreach (var logItem in diagnosticLogItems)
                    {
                        var projectName = Path.GetFileName(projectPath);
                        _logger.Log(logItem.Kind is WorkspaceDiagnosticKind.Failure ? LogLevel.Error : LogLevel.Warning, $"{logItem.Kind} while loading {logItem.ProjectFilePath}: {logItem.Message}");
                    }

                    return diagnosticLogItems.Any(logItem => logItem.Kind is WorkspaceDiagnosticKind.Failure) ? LSP.MessageType.Error : LSP.MessageType.Warning;
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
            _logger.LogError(e, $"Exception thrown while loading {projectToLoad.Path}");
            return LSP.MessageType.Error;
        }
    }
}
