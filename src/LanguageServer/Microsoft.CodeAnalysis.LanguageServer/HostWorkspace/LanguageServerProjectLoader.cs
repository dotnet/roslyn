// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DebugConfiguration;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.MSBuild.BuildHostProcessManager;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal abstract class LanguageServerProjectLoader
{
    private readonly AsyncBatchingWorkQueue<ProjectToLoad> _projectsToReload;

    protected readonly ProjectSystemProjectFactory ProjectFactory;
    private readonly ProjectTargetFrameworkManager _targetFrameworkManager;
    private readonly ProjectSystemHostInfo _projectSystemHostInfo;
    private readonly IFileChangeWatcher _fileChangeWatcher;
    protected readonly IGlobalOptionService GlobalOptionService;
    protected readonly ILoggerFactory LoggerFactory;
    private readonly ILogger _logger;
    private readonly ProjectLoadTelemetryReporter _projectLoadTelemetryReporter;
    private readonly BinlogNamer _binlogNamer;
    protected readonly ImmutableDictionary<string, string> AdditionalProperties;

    /// <summary>
    /// Guards access to <see cref="_loadedProjects"/>.
    /// To keep the LSP queue responsive, <see cref="_gate"/> must not be held while performing design-time builds.
    /// </summary>
    private readonly SemaphoreSlim _gate = new(initialCount: 1);

    /// <summary>
    /// Guarded by <see cref="_gate"/>.
    /// Maps the file path of a tracked project to the load state for the project.
    /// Absence of an entry indicates the project is not tracked, e.g. it was never loaded, or it was unloaded.
    /// <see cref="_gate"/> must be held when modifying the dictionary or objects contained in it.
    /// </summary>
    private readonly Dictionary<string, ProjectLoadState> _loadedProjects = [];

    /// <summary>
    /// State transitions:
    /// <see cref="Primordial"/> -> <see cref="LoadedTargets"/>
    /// Any state -> unloaded (which is denoted by removing the <see cref="_loadedProjects"/> entry for the project)
    /// </summary>
    private abstract record ProjectLoadState
    {
        private ProjectLoadState() { }

        /// <summary>
        /// Represents a project which has not yet had a design-time build performed for it,
        /// and which has an associated "primordial project" in the workspace.
        /// </summary>
        /// <param name="PrimordialProjectId">
        /// ID of the project which LSP uses to fulfill requests until the first design-time build is complete.
        /// The project with this ID is removed from the workspace when unloading or when transitioning to <see cref="LoadedTargets"/> state.
        /// </param>
        public sealed record Primordial(ProjectId PrimordialProjectId) : ProjectLoadState;

        /// <summary>
        /// Represents a project for which we have loaded zero or more targets.
        /// Generally a project which has zero loaded targets has not had a design-time build completed for it yet.
        /// Incrementally updated upon subsequent design-time builds.
        /// The <see cref="LoadedProjectTargets"/> are disposed when unloading.
        /// </summary>
        /// <param name="LoadedProjectTargets">List of target frameworks which have been loaded for this project so far.</param>
        public sealed record LoadedTargets(ImmutableArray<LoadedProject> LoadedProjectTargets) : ProjectLoadState;
    }

    protected LanguageServerProjectLoader(
        ProjectSystemProjectFactory projectFactory,
        ProjectTargetFrameworkManager targetFrameworkManager,
        ProjectSystemHostInfo projectSystemHostInfo,
        IFileChangeWatcher fileChangeWatcher,
        IGlobalOptionService globalOptionService,
        ILoggerFactory loggerFactory,
        IAsynchronousOperationListenerProvider listenerProvider,
        ProjectLoadTelemetryReporter projectLoadTelemetry,
        ServerConfigurationFactory serverConfigurationFactory,
        BinlogNamer binlogNamer)
    {
        ProjectFactory = projectFactory;
        _targetFrameworkManager = targetFrameworkManager;
        _projectSystemHostInfo = projectSystemHostInfo;
        _fileChangeWatcher = fileChangeWatcher;
        GlobalOptionService = globalOptionService;
        LoggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger(nameof(LanguageServerProjectLoader));
        _projectLoadTelemetryReporter = projectLoadTelemetry;
        _binlogNamer = binlogNamer;
        var workspace = projectFactory.Workspace;
        var razorDesignTimePath = serverConfigurationFactory.ServerConfiguration?.RazorDesignTimePath;

        AdditionalProperties = razorDesignTimePath is null
            ? ImmutableDictionary<string, string>.Empty
            : ImmutableDictionary<string, string>.Empty.Add("RazorDesignTimeTargets", razorDesignTimePath);

        _projectsToReload = new AsyncBatchingWorkQueue<ProjectToLoad>(
            TimeSpan.FromMilliseconds(100),
            ReloadProjectsAsync,
            ProjectToLoad.Comparer,
            listenerProvider.GetListener(FeatureAttribute.Workspace),
            CancellationToken.None); // TODO: do we need to introduce a shutdown cancellation token for this?
    }

    private sealed class ToastErrorReporter
    {
        private int _displayedToast = 0;

        public async Task ReportErrorAsync(LSP.MessageType errorKind, string message, CancellationToken cancellationToken)
        {
            // We should display a toast when the value of displayedToast is 0.  This will also update the value to 1 meaning we won't send any more toasts.
            var shouldShowToast = Interlocked.CompareExchange(ref _displayedToast, value: 1, comparand: 0) == 0;
            if (shouldShowToast)
            {
                await ShowToastNotification.ShowToastNotificationAsync(errorKind, message, cancellationToken, ShowToastNotification.ShowCSharpLogsCommand);
            }
        }
    }

    private async ValueTask ReloadProjectsAsync(ImmutableSegmentedList<ProjectToLoad> projectPathsToLoadOrReload, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // TODO: support configuration switching

        var binaryLogPath = _binlogNamer.GetMSBuildBinaryLogPath();

        await using var buildHostProcessManager = new BuildHostProcessManager(globalMSBuildProperties: AdditionalProperties, binaryLogPath: binaryLogPath, loggerFactory: LoggerFactory);
        var toastErrorReporter = new ToastErrorReporter();

        try
        {
            var projectsThatNeedRestore = await ProducerConsumer<string>.RunParallelAsync(
                source: projectPathsToLoadOrReload,
                produceItems: static async (projectToLoad, callback, args, cancellationToken) =>
                {
                    var (@this, toastErrorReporter, buildHostProcessManager) = args;
                    var projectNeedsRestore = await @this.ReloadProjectAsync(
                        projectToLoad, toastErrorReporter, buildHostProcessManager, cancellationToken);

                    if (projectNeedsRestore)
                        callback(projectToLoad.Path);
                },
                args: (@this: this, toastErrorReporter, buildHostProcessManager),
                cancellationToken).ConfigureAwait(false);

            if (GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableAutomaticRestore) && projectsThatNeedRestore.Any())
            {
                // Tell the client to restore any projects with unresolved dependencies.
                // This should eventually move entirely server side once we have a mechanism for reporting generic project load progress.
                // Tracking: https://github.com/dotnet/vscode-csharp/issues/6675
                //
                // The request blocks to ensure we aren't trying to run a design time build at the same time as a restore.
                await ProjectDependencyHelper.RestoreProjectsAsync(projectsThatNeedRestore, cancellationToken);
            }
        }
        finally
        {
            _logger.LogInformation(string.Format(LanguageServerResources.Completed_reload_of_all_projects_in_0, stopwatch.Elapsed));
        }
    }

    /// <summary>Loads a project in the MSBuild host.</summary>
    /// <remarks>Caller needs to catch exceptions to avoid bringing down the project loader queue.</remarks>
    protected abstract Task<(RemoteProjectFile projectFile, bool hasAllInformation, BuildHostProcessKind preferred, BuildHostProcessKind actual)?> TryLoadProjectInMSBuildHostAsync(
        BuildHostProcessManager buildHostProcessManager, string projectPath, CancellationToken cancellationToken);

    /// <returns>True if the project needs a NuGet restore, false otherwise.</returns>
    private async Task<bool> ReloadProjectAsync(ProjectToLoad projectToLoad, ToastErrorReporter toastErrorReporter, BuildHostProcessManager buildHostProcessManager, CancellationToken cancellationToken)
    {
        BuildHostProcessKind? preferredBuildHostKindThatWeDidNotGet = null;
        var projectPath = projectToLoad.Path;
        Contract.ThrowIfFalse(PathUtilities.IsAbsolute(projectPath));

        // Before doing any work, check if the project has already been unloaded.
        using (await _gate.DisposableWaitAsync(cancellationToken))
        {
            if (!_loadedProjects.ContainsKey(projectPath))
            {
                return false;
            }
        }

        try
        {
            if (await TryLoadProjectInMSBuildHostAsync(buildHostProcessManager, projectPath, cancellationToken)
                is not var (remoteProjectFile, hasAllInformation, preferredBuildHostKind, actualBuildHostKind))
            {
                _logger.LogWarning($"Unable to load project '{projectPath}'.");
                return false;
            }

            if (preferredBuildHostKind != actualBuildHostKind)
                preferredBuildHostKindThatWeDidNotGet = preferredBuildHostKind;

            var diagnosticLogItems = await remoteProjectFile.GetDiagnosticLogItemsAsync(cancellationToken);
            if (diagnosticLogItems.Any(item => item.Kind is DiagnosticLogItemKind.Error))
            {
                await LogDiagnosticsAsync(diagnosticLogItems);
                // We have total failures in evaluation, no point in continuing.
                return false;
            }

            var loadedProjectInfos = await remoteProjectFile.GetProjectFileInfosAsync(cancellationToken);

            // The out-of-proc build host supports more languages than we may actually have Workspace binaries for, so ensure we can actually process that
            // language in-process.
            var projectLanguage = loadedProjectInfos.FirstOrDefault()?.Language;
            if (projectLanguage != null && ProjectFactory.Workspace.Services.GetLanguageService<ICommandLineParserService>(projectLanguage) == null)
            {
                _logger.LogWarning($"Could not run a design-time build for '{projectPath}' because it uses unsupported language '{projectLanguage}'.");
                return false;
            }

            Dictionary<ProjectFileInfo, ProjectLoadTelemetryReporter.TelemetryInfo> telemetryInfos = [];
            var needsRestore = false;

            using (await _gate.DisposableWaitAsync(cancellationToken))
            {
                if (!_loadedProjects.TryGetValue(projectPath, out var currentLoadState))
                {
                    // Project was unloaded. Do not proceed with reloading it.
                    return false;
                }

                var previousProjectTargets = currentLoadState is ProjectLoadState.LoadedTargets loaded ? loaded.LoadedProjectTargets : [];
                var newProjectTargetsBuilder = ArrayBuilder<LoadedProject>.GetInstance(loadedProjectInfos.Length);
                foreach (var loadedProjectInfo in loadedProjectInfos)
                {
                    var (target, targetAlreadyExists) = await GetOrCreateProjectTargetAsync(previousProjectTargets, loadedProjectInfo);
                    newProjectTargetsBuilder.Add(target);

                    if (targetAlreadyExists)
                    {
                        _ = await target.UpdateWithNewProjectInfoAsync(loadedProjectInfo, hasAllInformation, _logger);
                    }
                    else
                    {
                        var (targetTelemetryInfo, targetNeedsRestore) = await target.UpdateWithNewProjectInfoAsync(loadedProjectInfo, hasAllInformation, _logger);
                        needsRestore |= targetNeedsRestore;
                        telemetryInfos[loadedProjectInfo] = targetTelemetryInfo with { IsSdkStyle = preferredBuildHostKind == BuildHostProcessKind.NetCore };
                    }
                }

                var newProjectTargets = newProjectTargetsBuilder.ToImmutableAndFree();
                foreach (var target in previousProjectTargets)
                {
                    // Unload targets which were present in a past design-time build, but absent in the current one.
                    if (!newProjectTargets.Contains(target))
                    {
                        target.Dispose();
                    }
                }

                if (projectToLoad.ReportTelemetry)
                {
                    await _projectLoadTelemetryReporter.ReportProjectLoadTelemetryAsync(telemetryInfos, projectToLoad, cancellationToken);
                }

                if (currentLoadState is ProjectLoadState.Primordial(var projectId))
                {
                    // Remove the primordial project now that the design-time build pass is finished.
                    await ProjectFactory.ApplyChangeToWorkspaceAsync(workspace => workspace.OnProjectRemoved(projectId), cancellationToken);
                }

                _loadedProjects[projectPath] = new ProjectLoadState.LoadedTargets(newProjectTargets);
            }

            diagnosticLogItems = await remoteProjectFile.GetDiagnosticLogItemsAsync(cancellationToken);
            if (diagnosticLogItems.Any())
            {
                await LogDiagnosticsAsync(diagnosticLogItems);
            }
            else
            {
                _logger.LogInformation(string.Format(LanguageServerResources.Successfully_completed_load_of_0, projectPath));
            }

            return needsRestore;
        }
        catch (Exception e)
        {
            // Since our LogDiagnosticsAsync helper takes DiagnosticLogItems, let's just make one for this
            var message = string.Format(LanguageServerResources.Exception_thrown_0, e);
            var diagnosticLogItem = new DiagnosticLogItem(DiagnosticLogItemKind.Error, message, projectPath);
            await LogDiagnosticsAsync([diagnosticLogItem]);

            return false;
        }

        async Task<(LoadedProject, bool alreadyExists)> GetOrCreateProjectTargetAsync(ImmutableArray<LoadedProject> previousProjectTargets, ProjectFileInfo loadedProjectInfo)
        {
            var existingProject = previousProjectTargets.FirstOrDefault(p => p.GetTargetFramework() == loadedProjectInfo.TargetFramework);
            if (existingProject != null)
            {
                return (existingProject, alreadyExists: true);
            }

            var targetFramework = loadedProjectInfo.TargetFramework;
            var projectSystemName = targetFramework is null ? projectPath : $"{projectPath} (${targetFramework})";

            var projectCreationInfo = new ProjectSystemProjectCreationInfo
            {
                AssemblyName = projectSystemName,
                FilePath = projectPath,
                CompilationOutputAssemblyFilePath = loadedProjectInfo.IntermediateOutputFilePath,
            };

            var projectSystemProject = await ProjectFactory.CreateAndAddToWorkspaceAsync(
                projectSystemName,
                loadedProjectInfo.Language,
                projectCreationInfo,
                _projectSystemHostInfo);

            var loadedProject = new LoadedProject(projectSystemProject, ProjectFactory.Workspace.Services.SolutionServices, _fileChangeWatcher, _targetFrameworkManager);
            loadedProject.NeedsReload += (_, _) => _projectsToReload.AddWork(projectToLoad with { ReportTelemetry = false });
            return (loadedProject, alreadyExists: false);
        }

        async Task LogDiagnosticsAsync(ImmutableArray<DiagnosticLogItem> diagnosticLogItems)
        {
            foreach (var logItem in diagnosticLogItems)
            {
                var projectName = Path.GetFileName(projectPath);
                _logger.Log(logItem.Kind is DiagnosticLogItemKind.Error ? LogLevel.Error : LogLevel.Warning, $"{logItem.Kind} while loading {logItem.ProjectFilePath}: {logItem.Message}");
            }

            var worstLspMessageKind = diagnosticLogItems.Any(logItem => logItem.Kind is DiagnosticLogItemKind.Error) ? LSP.MessageType.Error : LSP.MessageType.Warning;

            string message;

            if (preferredBuildHostKindThatWeDidNotGet == BuildHostProcessKind.NetFramework)
                message = LanguageServerResources.Projects_failed_to_load_because_MSBuild_could_not_be_found;
            else if (preferredBuildHostKindThatWeDidNotGet == BuildHostProcessKind.Mono)
                message = LanguageServerResources.Projects_failed_to_load_because_Mono_could_not_be_found;
            else
                message = string.Format(LanguageServerResources.There_were_problems_loading_project_0_See_log_for_details, Path.GetFileName(projectPath));

            await toastErrorReporter.ReportErrorAsync(worstLspMessageKind, message, cancellationToken);
        }
    }

    protected async ValueTask TrackPrimordialOnlyProjectAsync(string projectPath, ProjectId primordialProjectId)
    {
        using (await _gate.DisposableWaitAsync(CancellationToken.None))
        {
            // If project is already tracked, no need to do any further work.
            if (_loadedProjects.ContainsKey(projectPath))
            {
                return;
            }

            _loadedProjects.Add(projectPath, new ProjectLoadState.Primordial(primordialProjectId));
        }
    }

    protected async ValueTask BeginLoadingProjectWithPrimordialAsync(string projectPath, ProjectId primordialProjectId)
    {
        using (await _gate.DisposableWaitAsync(CancellationToken.None))
        {
            // If project has already begun loading, no need to do any further work.
            if (_loadedProjects.ContainsKey(projectPath))
            {
                return;
            }

            _loadedProjects.Add(projectPath, new ProjectLoadState.Primordial(primordialProjectId));
            _projectsToReload.AddWork(new ProjectToLoad(projectPath, ProjectGuid: null, ReportTelemetry: true));
        }
    }

    protected async Task BeginLoadingProjectAsync(string projectPath, string? projectGuid)
    {
        using (await _gate.DisposableWaitAsync(CancellationToken.None))
        {
            // If project has already begun loading, no need to do any further work.
            if (_loadedProjects.ContainsKey(projectPath))
            {
                return;
            }

            _loadedProjects.Add(projectPath, new ProjectLoadState.LoadedTargets(LoadedProjectTargets: []));
            _projectsToReload.AddWork(new ProjectToLoad(Path: projectPath, ProjectGuid: projectGuid, ReportTelemetry: true));
        }
    }

    protected Task WaitForProjectsToFinishLoadingAsync() => _projectsToReload.WaitUntilCurrentBatchCompletesAsync();

    protected async ValueTask UnloadProjectAsync(string projectPath)
    {
        using (await _gate.DisposableWaitAsync(CancellationToken.None))
        {
            if (!_loadedProjects.Remove(projectPath, out var loadState))
            {
                // It is common to be called with a path to a project which is already not loaded.
                // In this case, we should do nothing.
                return;
            }

            if (loadState is ProjectLoadState.Primordial(var projectId))
            {
                await ProjectFactory.ApplyChangeToWorkspaceAsync(workspace => workspace.OnProjectRemoved(projectId));
            }
            else if (loadState is ProjectLoadState.LoadedTargets(var existingProjects))
            {
                foreach (var existingProject in existingProjects)
                {
                    // Disposing a LoadedProject unloads it and removes it from the workspace.
                    existingProject.Dispose();
                }
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(loadState);
            }
        }
    }
}
