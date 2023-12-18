// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild.Logging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.MSBuild.BuildHostProcessManager;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Export(typeof(LanguageServerProjectSystem)), Shared]
internal sealed class LanguageServerProjectSystem
{
    /// <summary>
    /// A single gate for code that is adding work to <see cref="_projectsToLoadAndReload" />. This is just we don't have code simultaneously trying to load and unload solutions at once.
    /// </summary>
    private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);

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
    private readonly ProjectFileExtensionRegistry _projectFileExtensionRegistry;

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
        _projectFileExtensionRegistry = new ProjectFileExtensionRegistry(workspaceFactory.Workspace.CurrentSolution.Services, new DiagnosticReporter(workspaceFactory.Workspace));

        _projectsToLoadAndReload = new AsyncBatchingWorkQueue<ProjectToLoad>(
            TimeSpan.FromMilliseconds(100),
            LoadOrReloadProjectsAsync,
            ProjectToLoad.Comparer,
            listenerProvider.GetListener(FeatureAttribute.Workspace),
            CancellationToken.None); // TODO: do we need to introduce a shutdown cancellation token for this?
    }

    public async Task OpenSolutionAsync(string solutionFilePath)
    {
        using (await _gate.DisposableWaitAsync())
        {
            _logger.LogInformation(string.Format(LanguageServerResources.Loading_0, solutionFilePath));
            _workspaceFactory.ProjectSystemProjectFactory.SolutionPath = solutionFilePath;

            // We'll load solutions out-of-proc, since it's possible we might be running on a runtime that doesn't have a matching SDK installed,
            // and we don't want any MSBuild registration to set environment variables in our process that might impact child processes.
            await using var buildHostProcessManager = new BuildHostProcessManager(loggerFactory: _loggerFactory);
            var buildHost = await buildHostProcessManager.GetBuildHostAsync(BuildHostProcessKind.NetCore, CancellationToken.None);

            // If we don't have a .NET Core SDK on this machine at all, try .NET Framework
            if (!await buildHost.HasUsableMSBuildAsync(solutionFilePath, CancellationToken.None))
            {
                var kind = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? BuildHostProcessKind.NetFramework : BuildHostProcessKind.Mono;
                buildHost = await buildHostProcessManager.GetBuildHostAsync(kind, CancellationToken.None);
            }

            foreach (var project in await buildHost.GetProjectsInSolutionAsync(solutionFilePath, CancellationToken.None))
            {
                _projectsToLoadAndReload.AddWork(new ProjectToLoad(project.ProjectPath, project.ProjectGuid));
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

    private async ValueTask LoadOrReloadProjectsAsync(ImmutableSegmentedList<ProjectToLoad> projectPathsToLoadOrReload, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // TODO: support configuration switching

        var binaryLogPath = GetMSBuildBinaryLogPath();

        await using var buildHostProcessManager = new BuildHostProcessManager(binaryLogPath: binaryLogPath, loggerFactory: _loggerFactory);

        var displayedToast = 0;

        try
        {
            var tasks = new List<Task>();

            var projectsThatNeedRestore = new ConcurrentSet<string>();
            foreach (var projectToLoad in projectPathsToLoadOrReload)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var (errorKind, preferredBuildHostKind, projectNeedsRestore) = await LoadOrReloadProjectAsync(projectToLoad, buildHostProcessManager, cancellationToken);
                    if (errorKind is LSP.MessageType.Error)
                    {
                        // We should display a toast when the value of displayedToast is 0.  This will also update the value to 1 meaning we won't send any more toasts.
                        var shouldShowToast = Interlocked.CompareExchange(ref displayedToast, value: 1, comparand: 0) == 0;
                        if (shouldShowToast)
                        {
                            string message;

                            if (preferredBuildHostKind == BuildHostProcessKind.NetFramework)
                                message = LanguageServerResources.Projects_failed_to_load_because_MSBuild_could_not_be_found;
                            else if (preferredBuildHostKind == BuildHostProcessKind.Mono)
                                message = LanguageServerResources.Projects_failed_to_load_because_Mono_could_not_be_found;
                            else
                                message = string.Format(LanguageServerResources.There_were_problems_loading_project_0_See_log_for_details, Path.GetFileName(projectToLoad.Path));

                            await ShowToastNotification.ShowToastNotificationAsync(errorKind.Value, message, cancellationToken, ShowToastNotification.ShowCSharpLogsCommand);
                        }
                    }

                    if (projectNeedsRestore)
                    {
                        projectsThatNeedRestore.Add(projectToLoad.Path);
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);

            if (_globalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableAutomaticRestore) && projectsThatNeedRestore.Any())
            {
                // Tell the client to restore any projects with unresolved dependencies.
                // This should eventually move entirely server side once we have a mechanism for reporting generic project load progress.
                // Tracking: https://github.com/dotnet/vscode-csharp/issues/6675
                //
                // The request blocks to ensure we aren't trying to run a design time build at the same time as a restore.
                await ProjectDependencyHelper.RestoreProjectsAsync(projectsThatNeedRestore.ToImmutableHashSet(), cancellationToken);
            }
        }
        finally
        {
            _logger.LogInformation(string.Format(LanguageServerResources.Completed_reload_of_all_projects_in_0, stopwatch.Elapsed));
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

    private async Task<(LSP.MessageType? FailureType, BuildHostProcessKind? PreferredKind, bool HasUnresolvedDependencies)> LoadOrReloadProjectAsync(ProjectToLoad projectToLoad, BuildHostProcessManager buildHostProcessManager, CancellationToken cancellationToken)
    {
        BuildHostProcessKind? preferredBuildHostKind = null;

        try
        {
            var projectPath = projectToLoad.Path;

            (var buildHost, preferredBuildHostKind) = await buildHostProcessManager!.GetBuildHostAsync(projectPath, cancellationToken);

            if (_projectFileExtensionRegistry.TryGetLanguageNameFromProjectPath(projectPath, DiagnosticReportingMode.Ignore, out var languageName))
            {
                var loadedFile = await buildHost.LoadProjectFileAsync(projectPath, languageName, cancellationToken);
                var diagnosticLogItems = await loadedFile.GetDiagnosticLogItemsAsync(cancellationToken);
                if (diagnosticLogItems.Any(item => item.Kind is WorkspaceDiagnosticKind.Failure))
                {
                    _ = LogDiagnostics(projectPath, diagnosticLogItems);
                    // We have total failures in evaluation, no point in continuing.
                    return (LSP.MessageType.Error, preferredBuildHostKind, false);
                }

                var loadedProjectInfos = await loadedFile.GetProjectFileInfosAsync(cancellationToken);

                // The out-of-proc build host supports more languages than we may actually have Workspace binaries for, so ensure we can actually process that
                // language.
                var projectLanguage = loadedProjectInfos.FirstOrDefault()?.Language;
                if (projectLanguage != null && _workspaceFactory.Workspace.Services.GetLanguageService<ICommandLineParserService>(projectLanguage) == null)
                {
                    return (null, null, false);
                }

                var existingProjects = _loadedProjects.GetOrAdd(projectPath, static _ => new List<LoadedProject>());

                Dictionary<ProjectFileInfo, (ImmutableArray<CommandLineReference> MetadataReferences, OutputKind OutputKind, bool NeedsRestore)> projectFileInfos = new();
                foreach (var loadedProjectInfo in loadedProjectInfos)
                {
                    // If we already have the project, just update it
                    var existingProject = existingProjects.Find(p => p.GetTargetFramework() == loadedProjectInfo.TargetFramework);

                    if (existingProject != null)
                    {
                        projectFileInfos[loadedProjectInfo] = await existingProject.UpdateWithNewProjectInfoAsync(loadedProjectInfo, _logger);
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

                        projectFileInfos[loadedProjectInfo] = await loadedProject.UpdateWithNewProjectInfoAsync(loadedProjectInfo, _logger);
                    }
                }

                await _projectLoadTelemetryReporter.ReportProjectLoadTelemetryAsync(projectFileInfos, projectToLoad, cancellationToken);
                diagnosticLogItems = await loadedFile.GetDiagnosticLogItemsAsync(cancellationToken);
                var errorLevel = LogDiagnostics(projectPath, diagnosticLogItems);
                if (errorLevel == null)
                {
                    _logger.LogInformation(string.Format(LanguageServerResources.Successfully_completed_load_of_0, projectPath));
                }

                return (errorLevel, preferredBuildHostKind, projectFileInfos.Values.Any(info => info.NeedsRestore));
            }

            return (null, null, false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, string.Format(LanguageServerResources.Exception_thrown_while_loading_0, projectToLoad.Path));
            return (LSP.MessageType.Error, preferredBuildHostKind, false);
        }

        LSP.MessageType? LogDiagnostics(string projectPath, ImmutableArray<DiagnosticLogItem> diagnosticLogItems)
        {
            if (diagnosticLogItems.IsEmpty)
            {
                return null;
            }

            foreach (var logItem in diagnosticLogItems)
            {
                var projectName = Path.GetFileName(projectPath);
                _logger.Log(logItem.Kind is WorkspaceDiagnosticKind.Failure ? LogLevel.Error : LogLevel.Warning, $"{logItem.Kind} while loading {logItem.ProjectFilePath}: {logItem.Message}");
            }

            return diagnosticLogItems.Any(logItem => logItem.Kind is WorkspaceDiagnosticKind.Failure) ? LSP.MessageType.Error : LSP.MessageType.Warning;
        }
    }
}
