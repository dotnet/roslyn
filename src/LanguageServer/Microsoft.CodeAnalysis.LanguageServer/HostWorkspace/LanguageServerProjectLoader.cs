// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal abstract partial class LanguageServerProjectLoader : IDisposable
{
    private static readonly string s_razorDesignTimePath = Path.Combine(AppContext.BaseDirectory, "Targets", "Microsoft.NET.Sdk.Razor.DesignTime.targets");

    private readonly AsyncBatchingWorkQueue<ProjectToLoad> _projectsToReload;
    private readonly CancellationTokenSource _shutdownSource = new();
    private bool _isDisposed;

    protected readonly LanguageServerWorkspaceFactory _workspaceFactory;
    private readonly ProjectTargetFrameworkManager _projectTargetFrameworkManager;
    private readonly IFileChangeWatcher _fileChangeWatcher;
    private readonly IClientLanguageServerManager _clientLanguageServerManager;
    private readonly WorkDoneProgressManager _workDoneProgressManager;
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
    private readonly Dictionary<string, ProjectLoadState> _loadedProjects = new(PathUtilities.Comparer);

    /// <summary>
    /// Indicates whether loads should report UI progress to the client for this loader.
    /// </summary>
    protected virtual bool EnableProgressReporting => true;

    /// <summary>
    /// The max MSBuild node count to use for design-time builds.
    /// </summary>
    protected virtual int MaxNodeCount
        // Don't overload the machine, so leave some CPU cores open. This was chosen without much supporting evidence, other than that it's still pretty close to max.
        => Math.Max(Environment.ProcessorCount / 2, 1);

    protected LanguageServerProjectLoader(
        ILspServices lspServices,
        IGlobalOptionService globalOptionService,
        ILoggerFactory loggerFactory,
        IAsynchronousOperationListenerProvider listenerProvider,
        ServerConfigurationFactory serverConfigurationFactory,
        IBinLogPathProvider binLogPathProvider,
        DotnetCliHelper dotnetCliHelper)
    {
        _workspaceFactory = lspServices.GetRequiredService<LanguageServerWorkspaceFactory>();
        _projectTargetFrameworkManager = lspServices.GetRequiredService<ProjectTargetFrameworkManager>();
        _fileChangeWatcher = lspServices.GetRequiredService<IFileChangeWatcher>();
        _clientLanguageServerManager = lspServices.GetRequiredService<IClientLanguageServerManager>();
        _workDoneProgressManager = lspServices.GetRequiredService<WorkDoneProgressManager>();
        GlobalOptionService = globalOptionService;
        LoggerFactory = loggerFactory;
        Listener = listenerProvider.GetListener(FeatureAttribute.Workspace);
        _logger = loggerFactory.CreateLogger(this.GetTypeDisplayName());
        _projectLoadTelemetryReporter = lspServices.GetRequiredService<ProjectLoadTelemetryReporter>();
        _binLogPathProvider = binLogPathProvider;
        _dotnetCliHelper = dotnetCliHelper;

        AdditionalProperties = BuildAdditionalProperties(serverConfigurationFactory.ServerConfiguration);

        _projectsToReload = new AsyncBatchingWorkQueue<ProjectToLoad>(
            TimeSpan.FromMilliseconds(100),
            ReloadProjectsAsync,
            ProjectToLoad.Comparer,
            Listener,
            _shutdownSource.Token);
    }

    private static ImmutableDictionary<string, string> BuildAdditionalProperties(ServerConfiguration? serverConfiguration)
    {
        var properties = ImmutableDictionary<string, string>.Empty;

        if (serverConfiguration is null)
        {
            return properties;
        }

        properties = properties.Add("RazorDesignTimeTargets", s_razorDesignTimePath);

        if (serverConfiguration.CSharpDesignTimePath is { } csharpDesignTimePath)
        {
            properties = properties.Add("CSharpDesignTimeTargetsPath", csharpDesignTimePath);
        }

        return properties;
    }

    private async ValueTask ReloadProjectsAsync(ImmutableSegmentedList<ProjectToLoad> projectsToLoadOrReload, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // TODO: support configuration switching

        try
        {
            ImmutableArray<string> projectsThatNeedRestore;

            // Disposing of this BuildHostProcessManager will shut down any processes; so be explicit about the scope so we don't hold onto it longer than
            // needed.
            await using (var buildHostProcessManager = new BuildHostProcessManager(
                knownCommandLineParserLanguages: _workspaceFactory.HostWorkspace.Services.SolutionServices.GetSupportedLanguages<ICommandLineParserService>(),
                globalMSBuildProperties: AdditionalProperties,
                binaryLogPathProvider: _binLogPathProvider,
                maxNodeCount: MaxNodeCount,
                loggerFactory: LoggerFactory))
            {
                var toastErrorReporter = new ToastErrorReporter(_clientLanguageServerManager);

                projectsThatNeedRestore = await ProducerConsumer<string>.RunParallelAsync(
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
            }

            if (GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableAutomaticRestore) && projectsThatNeedRestore.Any())
            {
                // This request blocks to ensure we aren't trying to run a design time build at the same time as a restore.
                await ProjectDependencyHelper.RestoreProjectsAsync(_workDoneProgressManager, projectsThatNeedRestore, EnableProgressReporting, _dotnetCliHelper, _logger, cancellationToken);
            }
        }
        finally
        {
            _logger.LogInformation(string.Format(LanguageServerResources.Completed_reload_of_all_projects_in_0, stopwatch.Elapsed));
        }
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

        // Before doing any work, check if the project has already been unloaded
        using (await _gate.DisposableWaitAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_loadedProjects.TryGetValue(projectPath, out var loadState))
            {
                return null;
            }

            if (projectToLoad.LoadOperation is not null)
            {
                if (!HasLoadOperation(loadState, projectToLoad.LoadOperation))
                    return null;

                projectToLoad = projectToLoad with { ProjectGuid = projectToLoad.LoadOperation.StartEvaluation() };
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
                await CompleteInitialLoadAsync(projectToLoad, LanguageServerProjectLoadStatus.Unsupported, cancellationToken);
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
                await CompleteInitialLoadAsync(projectToLoad, LanguageServerProjectLoadStatus.Failed, cancellationToken);
                return null;
            }

            var loadedProjectInfos = remoteProjectLoadResult.ProjectFileInfos;

            // The out-of-proc build host supports more languages than we may actually have Workspace binaries for, so ensure we can actually process that
            // language in-process.
            var projectLanguage = loadedProjectInfos.FirstOrDefault()?.Language;
            if (projectLanguage != null && projectFactory.Workspace.Services.GetLanguageService<ICommandLineParserService>(projectLanguage) == null)
            {
                await CompleteInitialLoadAsync(projectToLoad, LanguageServerProjectLoadStatus.Unsupported, cancellationToken);
                return null;
            }

            Dictionary<ProjectFileInfo, ProjectLoadTelemetryReporter.TelemetryInfo> telemetryInfos = [];
            string? projectRestorePath = null;

            using (await _gate.DisposableWaitAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!_loadedProjects.TryGetValue(projectPath, out var currentLoadState) ||
                    (projectToLoad.LoadOperation is not null && !HasLoadOperation(currentLoadState, projectToLoad.LoadOperation)))
                {
                    // Project was unloaded or a new operation for the same path was queued. Do not commit stale results.
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
                            HasFileBasedAppDirectives = remoteProjectLoadResult.HasFileBasedAppDirectives,
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
                projectToLoad.LoadOperation?.Handle.Complete(new LanguageServerProjectLoadResult(
                    LanguageServerProjectLoadStatus.Loaded,
                    newProjectTargets.SelectAsArray(static target => target.ProjectId)));
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            // Since our LogDiagnosticsAsync helper takes DiagnosticLogItems, let's just make one for this
            var message = string.Format(LanguageServerResources.Exception_thrown_0, e);
            var diagnosticLogItem = new DiagnosticLogItem(DiagnosticLogItemKind.Error, message, projectPath);
            await LogDiagnosticsAsync([diagnosticLogItem]);

            await CompleteInitialLoadAsync(projectToLoad, LanguageServerProjectLoadStatus.Failed, CancellationToken.None);

            return null;
        }

        async Task CompleteInitialLoadAsync(ProjectToLoad projectToLoad, LanguageServerProjectLoadStatus status, CancellationToken cancellationToken)
        {
            if (projectToLoad.LoadOperation is null)
                return;

            using (await _gate.DisposableWaitAsync(cancellationToken))
            {
                if (_loadedProjects.TryGetValue(projectPath, out var loadState) && HasLoadOperation(loadState, projectToLoad.LoadOperation))
                {
                    var result = new LanguageServerProjectLoadResult(status, []);
                    _loadedProjects[projectPath] = loadState is ProjectLoadState.Primordial(var projectFactory, var projectId, _)
                        ? new ProjectLoadState.Failed(result, projectFactory, projectId)
                        : new ProjectLoadState.Failed(result);
                    projectToLoad.LoadOperation.Handle.Complete(result);
                }
            }
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

            var loadedProject = new LoadedProject(projectSystemProject, projectFactory, _fileChangeWatcher, _projectTargetFrameworkManager);
            loadedProject.NeedsReload += (_, _) =>
                // LoadOperation must be cleared: it belongs to the request that triggered this load and is already complete,
                // so gating a later reload on it would always fail once the project leaves the Loading/Primordial state.
                _projectsToReload.AddWork(projectToLoad with { LoadOperation = null, ReportTelemetry = false });
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
        projectPath = NormalizeProjectPath(projectPath);

        using (await _gate.DisposableWaitAsync(CancellationToken.None))
        {
            Contract.ThrowIfTrue(_isDisposed, "Project loader is already disposed");

            if (_loadedProjects.TryGetValue(projectPath, out var existingState))
            {
                // Note: this generally only happens if we fall through to the "add to misc workspace" path,
                // and we lose a race to begin loading the miscellaneous file project.
                return LookupExistingProject(existingState);
            }

            var primordialProjectInfo = createPrimordialProjectInfo(primordialProjectFactory);
            primordialProjectFactory.ApplyChangeToWorkspace(workspace => workspace.OnProjectAdded(primordialProjectInfo));
            var loadOperation = doDesignTimeBuild ? new ProjectLoadOperation(projectGuid: null) : null;
            _loadedProjects.Add(projectPath, new ProjectLoadState.Primordial(primordialProjectFactory, primordialProjectInfo.Id, loadOperation));
            if (doDesignTimeBuild)
                _projectsToReload.AddWork(new ProjectToLoad(projectPath, loadOperation, ProjectGuid: null, ReportTelemetry: true));

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
            else if (loadState is ProjectLoadState.Failed { PrimordialProjectFactory: { } projectFactory, PrimordialProjectId: { } projectId })
            {
                return projectFactory.Workspace.CurrentSolution.GetRequiredProject(projectId);
            }
            else if (loadState is ProjectLoadState.Loading or ProjectLoadState.Failed)
            {
                return null;
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
    protected async Task<LanguageServerProjectLoadHandle> BeginLoadingProjectAsync(string projectPath, string? projectGuid)
    {
        projectPath = NormalizeProjectPath(projectPath);

        using (await _gate.DisposableWaitAsync(CancellationToken.None))
        {
            Contract.ThrowIfTrue(_isDisposed, "Project loader is already disposed");

            if (_loadedProjects.TryGetValue(projectPath, out var loadState))
            {
                if (TryGetLoadOperation(loadState) is { } existingOperation)
                {
                    EnrichProjectGuid(existingOperation, projectPath, projectGuid);
                    return existingOperation.Handle;
                }

                if (loadState is ProjectLoadState.Primordial primordial)
                {
                    var primordialLoadOperation = new ProjectLoadOperation(projectGuid);
                    _loadedProjects[projectPath] = primordial with { LoadOperation = primordialLoadOperation };
                    _projectsToReload.AddWork(new ProjectToLoad(Path: projectPath, primordialLoadOperation, ProjectGuid: projectGuid, ReportTelemetry: true));
                    return primordialLoadOperation.Handle;
                }

                // Unsupported is a structural outcome (e.g. no language service for the project) and won't change on retry,
                // so only re-attempt projects that failed for a reason that might no longer apply (e.g. an environmental issue).
                if (loadState is ProjectLoadState.Failed { Result.Status: LanguageServerProjectLoadStatus.Failed } failed)
                {
                    var retryLoadOperation = new ProjectLoadOperation(projectGuid);
                    _loadedProjects[projectPath] = failed is { PrimordialProjectFactory: { } primordialProjectFactory, PrimordialProjectId: { } primordialProjectId }
                        ? new ProjectLoadState.Primordial(primordialProjectFactory, primordialProjectId, retryLoadOperation)
                        : new ProjectLoadState.Loading(retryLoadOperation);
                    _projectsToReload.AddWork(new ProjectToLoad(Path: projectPath, retryLoadOperation, ProjectGuid: projectGuid, ReportTelemetry: true));
                    return retryLoadOperation.Handle;
                }

                return CreateCompletedHandle(loadState);
            }

            var loadOperation = new ProjectLoadOperation(projectGuid);
            _loadedProjects.Add(projectPath, new ProjectLoadState.Loading(loadOperation));
            _projectsToReload.AddWork(new ProjectToLoad(Path: projectPath, loadOperation, ProjectGuid: projectGuid, ReportTelemetry: true));
            return loadOperation.Handle;
        }
    }

    protected Task WaitForProjectsToFinishLoadingAsync() => _projectsToReload.WaitUntilCurrentBatchCompletesAsync();

    protected static Task WaitForProjectLoadsAsync(
        ImmutableArray<LanguageServerProjectLoadHandle> handles, WorkDoneProgressTracker? progressTracker, CancellationToken cancellationToken = default)
        => Task.WhenAll(handles.SelectAsArray(handle => ObserveProjectLoadAsync(handle, progressTracker, cancellationToken)));

    private static async Task ObserveProjectLoadAsync(
        LanguageServerProjectLoadHandle handle, WorkDoneProgressTracker? progressTracker, CancellationToken cancellationToken)
    {
        try
        {
            await handle.Completion.WaitAsync(cancellationToken);
        }
        finally
        {
            progressTracker?.OnItemProcessed();
        }
    }

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

    public virtual void Dispose()
    {
        using (_gate.DisposableWait(CancellationToken.None))
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _shutdownSource.Cancel();
            _projectsToReload.Dispose();

            foreach (var (_, loadState) in _loadedProjects)
            {
                TryGetLoadOperation(loadState)?.Handle.Cancel(_shutdownSource.Token);

                // Disposing a LoadedProject unloads it, releasing its file watches and removing it from the workspace.
                // Primordial projects don't own any file watches; their placeholder projects are torn down along with
                // the workspace, so there's nothing to release for them here.
                if (loadState is ProjectLoadState.LoadedTargets(var loadedProjectTargets))
                {
                    foreach (var loadedProject in loadedProjectTargets)
                        loadedProject.Dispose();
                }
            }

            _loadedProjects.Clear();
            _shutdownSource.Dispose();
        }
    }

    internal async ValueTask<bool> TryUnloadProjectAsync(string projectPath, ProjectSystemProjectFactory? fromProjectFactory = null)
    {
        projectPath = NormalizeProjectPath(projectPath);

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

        if (loadState is ProjectLoadState.Primordial(var projectFactory, var projectId, _))
        {
            TryGetLoadOperation(loadState)?.Handle.Complete(new LanguageServerProjectLoadResult(LanguageServerProjectLoadStatus.Unloaded, []));
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
        else if (loadState is ProjectLoadState.Loading(var loadOperation))
        {
            loadOperation.Handle.Complete(new LanguageServerProjectLoadResult(LanguageServerProjectLoadStatus.Unloaded, []));
        }
        else if (loadState is ProjectLoadState.Failed { PrimordialProjectFactory: { } failedProjectFactory, PrimordialProjectId: { } failedProjectId })
        {
            await failedProjectFactory.ApplyChangeToWorkspaceAsync(workspace => workspace.OnProjectRemoved(failedProjectId));
        }
        else if (loadState is ProjectLoadState.Failed)
        {
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
                if (loadState1 is ProjectLoadState.Primordial(var projectFactory1, _, _))
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
                else if (loadState1 is ProjectLoadState.Failed { PrimordialProjectFactory: { } failedProjectFactory1 })
                {
                    return failedProjectFactory1 == fromProjectFactory;
                }
                else if (loadState1 is ProjectLoadState.Loading or ProjectLoadState.Failed)
                {
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(loadState1);
                }
            }

            return false;
        }
    }

    private static string NormalizeProjectPath(string projectPath)
        => PathUtilities.IsAbsolute(projectPath) ? Path.GetFullPath(projectPath) : projectPath;

    private static ProjectLoadOperation? TryGetLoadOperation(ProjectLoadState loadState)
        => loadState switch
        {
            ProjectLoadState.Primordial { LoadOperation: { } operation } => operation,
            ProjectLoadState.Loading(var operation) => operation,
            _ => null,
        };

    private static bool HasLoadOperation(ProjectLoadState loadState, ProjectLoadOperation operation)
        => ReferenceEquals(TryGetLoadOperation(loadState), operation);

    private LanguageServerProjectLoadHandle CreateCompletedHandle(ProjectLoadState loadState)
    {
        var result = loadState switch
        {
            ProjectLoadState.LoadedTargets(var targets) => new LanguageServerProjectLoadResult(LanguageServerProjectLoadStatus.Loaded, targets.SelectAsArray(static target => target.ProjectId)),
            ProjectLoadState.Failed(var failedResult, _, _) => failedResult,
            ProjectLoadState.Primordial => throw new InvalidOperationException("A primordial project without an active load operation cannot be treated as loaded."),
            _ => throw ExceptionUtilities.UnexpectedValue(loadState),
        };

        var handle = new LanguageServerProjectLoadHandle();
        handle.Complete(result);
        return handle;
    }

    private void EnrichProjectGuid(ProjectLoadOperation operation, string projectPath, string? projectGuid)
    {
        if (projectGuid is null || operation.ProjectGuid == projectGuid)
            return;

        if (!operation.TrySetProjectGuid(projectGuid) && operation.ProjectGuid is not null)
        {
            _logger.LogWarning(
                "Project '{projectPath}' was requested with conflicting solution GUIDs '{existingProjectGuid}' and '{projectGuid}'. The first GUID will be used.",
                projectPath,
                operation.ProjectGuid,
                projectGuid);
        }
    }
}
