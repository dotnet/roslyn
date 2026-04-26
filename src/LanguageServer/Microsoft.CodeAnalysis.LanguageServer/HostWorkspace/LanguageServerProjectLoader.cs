// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal abstract class LanguageServerProjectLoader
{
    private readonly AsyncBatchingWorkQueue<ProjectToLoad> _projectsToReload;

    protected readonly LanguageServerWorkspaceFactory _workspaceFactory;
    private readonly IFileChangeWatcher _fileChangeWatcher;
    protected readonly IGlobalOptionService GlobalOptionService;
    protected readonly ILoggerFactory LoggerFactory;
    protected readonly IAsynchronousOperationListener Listener;
    private readonly ILogger _logger;
    private readonly ProjectLoadTelemetryReporter _projectLoadTelemetryReporter;
    private readonly IBinLogPathProvider _binLogPathProvider;
    private readonly DotnetCliHelper _dotnetCliHelper;
    protected readonly ImmutableDictionary<string, string> AdditionalProperties;

    /// <summary>
    /// Guards access to <see cref="_loadedProjects"/>.
    /// To keep the LSP queue responsive, <see cref="_gate"/> must not be held while performing design-time builds.
    /// </summary>
    private readonly SemaphoreSlim _gate = new(initialCount: 1);

    /// <summary>
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
    protected abstract record ProjectLoadState
    {
        private ProjectLoadState() { }

        /// <summary>
        /// Represents a project which has not yet had a design-time build performed for it,
        /// and which has an associated "primordial project" in the workspace.
        /// </summary>
        /// <param name="PrimordialProjectFactory">
        /// The project factory for the workspace that the primordial project lives within. This
        /// factory was not used to create the project, but still needs to be used during removal to avoid locking issues.
        /// </param>
        /// <param name="PrimordialProjectId">
        /// ID of the project which LSP uses to fulfill requests until the first design-time build is complete.
        /// The project with this ID is removed from the workspace when unloading or when transitioning to <see cref="LoadedTargets"/> state.
        /// </param>
        public sealed record Primordial(ProjectSystemProjectFactory PrimordialProjectFactory, ProjectId PrimordialProjectId) : ProjectLoadState;

        /// <summary>
        /// Represents a project for which we have loaded zero or more targets.
        /// Generally a project which has zero loaded targets has not had a design-time build completed for it yet.
        /// Incrementally updated upon subsequent design-time builds.
        /// The <see cref="LoadedProjectTargets"/> are disposed when unloading.
        /// </summary>
        /// <param name="LoadedProjectTargets">List of target frameworks which have been loaded for this project so far.</param>
        public sealed record LoadedTargets(ImmutableArray<LoadedProject> LoadedProjectTargets) : ProjectLoadState;
    }

    /// <summary>
    /// Indicates whether loads should report UI progress to the client for this loader.
    /// </summary>
    protected virtual bool EnableProgressReporting => true;

    protected LanguageServerProjectLoader(
        LanguageServerWorkspaceFactory workspaceFactory,
        IFileChangeWatcher fileChangeWatcher,
        IGlobalOptionService globalOptionService,
        ILoggerFactory loggerFactory,
        IAsynchronousOperationListenerProvider listenerProvider,
        ProjectLoadTelemetryReporter projectLoadTelemetry,
        ServerConfigurationFactory serverConfigurationFactory,
        IBinLogPathProvider binLogPathProvider,
        DotnetCliHelper dotnetCliHelper)
    {
        _workspaceFactory = workspaceFactory;
        _fileChangeWatcher = fileChangeWatcher;
        GlobalOptionService = globalOptionService;
        LoggerFactory = loggerFactory;
        Listener = listenerProvider.GetListener(FeatureAttribute.Workspace);
        _logger = loggerFactory.CreateLogger(nameof(LanguageServerProjectLoader));
        _projectLoadTelemetryReporter = projectLoadTelemetry;
        _binLogPathProvider = binLogPathProvider;
        _dotnetCliHelper = dotnetCliHelper;

        AdditionalProperties = BuildAdditionalProperties(serverConfigurationFactory.ServerConfiguration);

        _projectsToReload = new AsyncBatchingWorkQueue<ProjectToLoad>(
            TimeSpan.FromMilliseconds(100),
            ReloadProjectsAsync,
            ProjectToLoad.Comparer,
            Listener,
            CancellationToken.None); // TODO: do we need to introduce a shutdown cancellation token for this?
    }

    private static ImmutableDictionary<string, string> BuildAdditionalProperties(ServerConfiguration? serverConfiguration)
    {
        var properties = ImmutableDictionary<string, string>.Empty;

        if (serverConfiguration is null)
        {
            return properties;
        }

        if (serverConfiguration.RazorDesignTimePath is { } razorDesignTimePath)
        {
            properties = properties.Add("RazorDesignTimeTargets", razorDesignTimePath);
        }

        if (serverConfiguration.CSharpDesignTimePath is { } csharpDesignTimePath)
        {
            properties = properties.Add("CSharpDesignTimeTargetsPath", csharpDesignTimePath);
        }

        return properties;
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

    private async ValueTask ReloadProjectsAsync(ImmutableSegmentedList<ProjectToLoad> projectsToLoadOrReload, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // TODO: support configuration switching

        await using var buildHostProcessManager = new BuildHostProcessManager(
            knownCommandLineParserLanguages: _workspaceFactory.HostWorkspace.Services.SolutionServices.GetSupportedLanguages<ICommandLineParserService>(),
            globalMSBuildProperties: AdditionalProperties,
            binaryLogPathProvider: _binLogPathProvider,
            loggerFactory: LoggerFactory);

        var toastErrorReporter = new ToastErrorReporter();

        try
        {
            var projectsThatNeedRestore = await ProducerConsumer<string>.RunParallelAsync(
                source: projectsToLoadOrReload,
                produceItems: static async (projectToLoad, produceItem, args, cancellationToken) =>
                {
                    var (@this, toastErrorReporter, buildHostProcessManager) = args;
                    var projectRestorePath = await @this.ReloadProjectAsync(
                        projectToLoad, toastErrorReporter, buildHostProcessManager, cancellationToken);

                    if (projectRestorePath is not null)
                        produceItem(projectRestorePath);
                },
                args: (@this: this, toastErrorReporter, buildHostProcessManager),
                cancellationToken).ConfigureAwait(false);

            if (GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableAutomaticRestore) && projectsThatNeedRestore.Any())
            {
                // This request blocks to ensure we aren't trying to run a design time build at the same time as a restore.
                await ProjectDependencyHelper.RestoreProjectsAsync(projectsThatNeedRestore, EnableProgressReporting, _dotnetCliHelper, _logger, cancellationToken);
            }
        }
        finally
        {
            _logger.LogInformation(string.Format(LanguageServerResources.Completed_reload_of_all_projects_in_0, stopwatch.Elapsed));
        }
    }

    internal sealed record RemoteProjectLoadResult
    {
        public required ImmutableArray<ProjectFileInfo> ProjectFileInfos { get; init; }
        public required ImmutableArray<DiagnosticLogItem> DiagnosticLogItems { get; init; }
        public required string? ProjectRestorePath { get; init; }
        public required ProjectSystemProjectFactory ProjectFactory { get; init; }
        public required bool IsFileBasedProgram { get; init; }
        public required bool IsMiscellaneousFile { get; init; }
        public required bool HasAllInformation { get; init; }
        public required BuildHostProcessKind PreferredBuildHostKind { get; init; }
        public required BuildHostProcessKind ActualBuildHostKind { get; init; }
    }

    /// <summary>Loads a project in the MSBuild host.</summary>
    /// <remarks>Caller needs to catch exceptions to avoid bringing down the project loader queue.</remarks>
    protected abstract Task<RemoteProjectLoadResult?> TryLoadProjectInMSBuildHostAsync(
        BuildHostProcessManager buildHostProcessManager, string projectPath, CancellationToken cancellationToken);

    /// <returns>The project file path that needs a NuGet restore, if any.</returns>
    private async Task<string?> ReloadProjectAsync(ProjectToLoad projectToLoad, ToastErrorReporter toastErrorReporter, BuildHostProcessManager buildHostProcessManager, CancellationToken cancellationToken)
    {
        BuildHostProcessKind? preferredBuildHostKindThatWeDidNotGet = null;
        var projectPath = projectToLoad.Path;

        // Before doing any work, check if the project has already been unloaded.
        using (await _gate.DisposableWaitAsync(cancellationToken))
        {
            if (!_loadedProjects.ContainsKey(projectPath))
            {
                return null;
            }
        }

        try
        {
            var remoteProjectLoadResult = await TryLoadProjectInMSBuildHostAsync(buildHostProcessManager, projectPath, cancellationToken);
            if (remoteProjectLoadResult is null)
            {
                // Example cases where this might occur:
                // - Loading VB projects
                // - Reloading file-based app projects, where edits were performed to e.g. delete all `#:` directives,
                //   making the file no longer a file-based app entry point.
                _logger.LogDebug("Reload of '{projectPath}' was canceled.", projectPath);
                return null;
            }

            var projectFactory = remoteProjectLoadResult.ProjectFactory;
            var isMiscellaneousFile = remoteProjectLoadResult.IsMiscellaneousFile;
            var preferredBuildHostKind = remoteProjectLoadResult.PreferredBuildHostKind;
            if (preferredBuildHostKind != remoteProjectLoadResult.ActualBuildHostKind)
                preferredBuildHostKindThatWeDidNotGet = preferredBuildHostKind;

            var diagnosticLogItems = remoteProjectLoadResult.DiagnosticLogItems;
            if (diagnosticLogItems.Any(item => item.Kind is DiagnosticLogItemKind.Error))
            {
                await LogDiagnosticsAsync(diagnosticLogItems);
                // We have total failures in evaluation, no point in continuing.
                return null;
            }

            var loadedProjectInfos = remoteProjectLoadResult.ProjectFileInfos;

            // The out-of-proc build host supports more languages than we may actually have Workspace binaries for, so ensure we can actually process that
            // language in-process.
            var projectLanguage = loadedProjectInfos.FirstOrDefault()?.Language;
            if (projectLanguage != null && projectFactory.Workspace.Services.GetLanguageService<ICommandLineParserService>(projectLanguage) == null)
            {
                return null;
            }

            Dictionary<ProjectFileInfo, ProjectLoadTelemetryReporter.TelemetryInfo> telemetryInfos = [];
            string? projectRestorePath = null;

            using (await _gate.DisposableWaitAsync(cancellationToken))
            {
                if (!_loadedProjects.TryGetValue(projectPath, out var currentLoadState))
                {
                    // Project was unloaded. Do not proceed with reloading it.
                    return null;
                }

                var previousProjectTargets = currentLoadState is ProjectLoadState.LoadedTargets loaded ? loaded.LoadedProjectTargets : [];
                var newProjectTargetsBuilder = ArrayBuilder<LoadedProject>.GetInstance(loadedProjectInfos.Length);
                foreach (var loadedProjectInfo in loadedProjectInfos)
                {
                    var (target, targetAlreadyExists) = await GetOrCreateProjectTargetAsync(previousProjectTargets, projectFactory, loadedProjectInfo);
                    newProjectTargetsBuilder.Add(target);

                    var (outputKind, metadataReferences, targetNeedsRestore) = await target.UpdateWithNewProjectInfoAsync(loadedProjectInfo, isMiscellaneousFile, remoteProjectLoadResult.HasAllInformation, _logger);
                    if (targetNeedsRestore)
                    {
                        projectRestorePath = remoteProjectLoadResult.ProjectRestorePath;
                    }

                    if (!targetAlreadyExists)
                    {
                        telemetryInfos[loadedProjectInfo] = new ProjectLoadTelemetryReporter.TelemetryInfo
                        {
                            OutputKind = outputKind,
                            MetadataReferences = metadataReferences,
                            IsSdkStyle = preferredBuildHostKind == BuildHostProcessKind.NetCore,
                            HasSolutionFile = _workspaceFactory.HostProjectFactory.SolutionPath is not null,
                            IsMiscellaneousFile = isMiscellaneousFile,
                            IsFileBasedProgram = remoteProjectLoadResult.IsFileBasedProgram,
                        };
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

                if (currentLoadState is ProjectLoadState.Primordial primordial)
                {
                    // Remove the primordial project from the workspace now that the design-time build has produced real targets.
                    await primordial.PrimordialProjectFactory.ApplyChangeToWorkspaceAsync(
                        workspace => workspace.OnProjectRemoved(primordial.PrimordialProjectId),
                        cancellationToken);
                }

                // At this point we expect that all the loaded projects are now in the project factory returned, and any previous ones have been removed.
                // this is a Debug.Assert() because if this expectation fails, the user's probably still in a state where things will work just fine;
                // throwing here would mean we don't remember the LoadedProjects we created, and the next update will create more and things will get really broken.
                Debug.Assert(newProjectTargets.All(target => target.ProjectFactory == projectFactory));
                _loadedProjects[projectPath] = new ProjectLoadState.LoadedTargets(newProjectTargets);
            }

            if (diagnosticLogItems.Any())
            {
                await LogDiagnosticsAsync(diagnosticLogItems);
            }
            else
            {
                _logger.LogInformation(string.Format(LanguageServerResources.Successfully_completed_load_of_0, projectPath));
            }

            return projectRestorePath;
        }
        catch (Exception e)
        {
            // Since our LogDiagnosticsAsync helper takes DiagnosticLogItems, let's just make one for this
            var message = string.Format(LanguageServerResources.Exception_thrown_0, e);
            var diagnosticLogItem = new DiagnosticLogItem(DiagnosticLogItemKind.Error, message, projectPath);
            await LogDiagnosticsAsync([diagnosticLogItem]);

            return null;
        }

        async Task<(LoadedProject, bool alreadyExists)> GetOrCreateProjectTargetAsync(ImmutableArray<LoadedProject> previousProjectTargets, ProjectSystemProjectFactory projectFactory, ProjectFileInfo loadedProjectInfo)
        {
            var existingProject = previousProjectTargets.FirstOrDefault(p => p.GetTargetFramework() == loadedProjectInfo.TargetFramework && p.ProjectFactory == projectFactory);
            if (existingProject != null)
            {
                return (existingProject, alreadyExists: true);
            }

            var targetFramework = loadedProjectInfo.TargetFramework;
            var projectSystemName = targetFramework is null ? projectPath : $"{projectPath} (${targetFramework})";

            var projectCreationInfo = new ProjectSystemProjectCreationInfo
            {
                AssemblyName = projectSystemName,
                // Note: the project file might be for a virtual file that doesn't exist on disk.
                // In this case, we don't want to pass its path through here, as this will result in trying to take file system timestamps for it, watch it for changes, etc.
                FilePath = PathUtilities.IsAbsolute(projectPath) && File.Exists(projectPath) ? projectPath : null,
                CompilationOutputAssemblyFilePath = loadedProjectInfo.IntermediateOutputFilePath,
            };

            var projectSystemProject = await projectFactory.CreateAndAddToWorkspaceAsync(
                projectSystemName,
                loadedProjectInfo.Language,
                projectCreationInfo,
                _workspaceFactory.ProjectSystemHostInfo,
                cancellationToken).ConfigureAwait(false);

            var loadedProject = new LoadedProject(projectSystemProject, projectFactory, _fileChangeWatcher, _workspaceFactory.TargetFrameworkManager);
            loadedProject.NeedsReload += (_, _) =>
                _projectsToReload.AddWork(projectToLoad with { ReportTelemetry = false });
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

    protected async ValueTask<Project?> GetOrLoadProjectAsync(string projectPath, ProjectSystemProjectFactory primordialProjectFactory, Func<ProjectSystemProjectFactory, ProjectInfo> createPrimordialProjectInfo, bool doDesignTimeBuild)
    {
        using (await _gate.DisposableWaitAsync(CancellationToken.None))
        {
            if (_loadedProjects.TryGetValue(projectPath, out var existingState))
            {
                // Note: this generally only happens if we fall through to the "add to misc workspace" path,
                // and we lose a race to begin loading the miscellaneous file project.
                return LookupExistingProject(existingState);
            }

            var primordialProjectInfo = createPrimordialProjectInfo(primordialProjectFactory);
            primordialProjectFactory.ApplyChangeToWorkspace(workspace => workspace.OnProjectAdded(primordialProjectInfo));
            _loadedProjects.Add(projectPath, new ProjectLoadState.Primordial(primordialProjectFactory, primordialProjectInfo.Id));
            if (doDesignTimeBuild)
                _projectsToReload.AddWork(new ProjectToLoad(projectPath, ProjectGuid: null, ReportTelemetry: true));

            return primordialProjectFactory.Workspace.CurrentSolution.GetRequiredProject(primordialProjectInfo.Id);
        }

        Project? LookupExistingProject(ProjectLoadState loadState)
        {
            if (loadState is ProjectLoadState.Primordial primordial)
            {
                return primordial.PrimordialProjectFactory.Workspace.CurrentSolution.GetRequiredProject(primordial.PrimordialProjectId);
            }
            else if (loadState is ProjectLoadState.LoadedTargets loadedTargets)
            {
                var target = loadedTargets.LoadedProjectTargets.FirstOrDefault();
                if (target is null)
                {
                    _logger.LogWarning("Could not get a project for '{projectPath}' because it loaded with no targets", projectPath);
                    return null;
                }

                return target.ProjectFactory.Workspace.CurrentSolution.GetRequiredProject(target.ProjectId);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(loadState);
            }
        }
    }

    /// <summary>
    /// Begins loading a project. If the project has already begun loading, returns without doing any additional work.
    /// </summary>
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

    /// <summary>Unloads all projects associated with this project loader.</summary>
    internal async ValueTask UnloadAllProjectsAsync()
    {
        using (await _gate.DisposableWaitAsync(CancellationToken.None))
        {
            foreach (var key in _loadedProjects.Keys)
            {
                // Note that .NET supports removing dictionary entries while enumerating
                var removed = await TryUnloadProject_NoLockAsync(key);
                Contract.ThrowIfFalse(removed); // We obtained lock before enumerating, how was this already removed?
            }
        }
    }

    internal async ValueTask<bool> TryUnloadProjectAsync(string projectPath, ProjectSystemProjectFactory? fromProjectFactory = null)
    {
        using (await _gate.DisposableWaitAsync(CancellationToken.None))
        {
            return await TryUnloadProject_NoLockAsync(projectPath, fromProjectFactory);
        }
    }

    private async ValueTask<bool> TryUnloadProject_NoLockAsync(string projectPath, ProjectSystemProjectFactory? fromProjectFactory = null)
    {
        // Caller can specify to only unload a project if it uses a specific project factory.
        if (fromProjectFactory != null && !UsesProjectFactory(fromProjectFactory))
        {
            return false;
        }

        if (!_loadedProjects.Remove(projectPath, out var loadState))
        {
            // It is common to be called with a path to a project which is already not loaded.
            // In this case, we should do nothing.
            return false;
        }

        if (loadState is ProjectLoadState.Primordial(var projectFactory, var projectId))
        {
            await projectFactory.ApplyChangeToWorkspaceAsync(workspace => workspace.OnProjectRemoved(projectId));
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

        return true;

        bool UsesProjectFactory(ProjectSystemProjectFactory fromProjectFactory)
        {
            if (_loadedProjects.TryGetValue(projectPath, out var loadState1))
            {
                if (loadState1 is ProjectLoadState.Primordial(var projectFactory1, _))
                {
                    if (projectFactory1 == fromProjectFactory)
                        return true;
                }
                else if (loadState1 is ProjectLoadState.LoadedTargets(var existingProjects))
                {
                    // Assumption: All 'existingProject' items will use the same project factory.
                    foreach (var existingProject in existingProjects)
                    {
                        if (existingProject.ProjectFactory == fromProjectFactory)
                            return true;
                    }
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(loadState1);
                }
            }

            return false;
        }
    }
}
