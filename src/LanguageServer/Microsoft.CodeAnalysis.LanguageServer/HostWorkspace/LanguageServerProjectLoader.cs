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

internal abstract partial class LanguageServerProjectLoader : IAsyncDisposable
{
    private static readonly string s_razorDesignTimePath = Path.Combine(AppContext.BaseDirectory, "Targets", "Microsoft.NET.Sdk.Razor.DesignTime.targets");

    private readonly AsyncBatchingWorkQueue<ProjectToLoad> _projectsToReload;
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
    /// Guards access to <see cref="_loadedProjects"/>. Each <see cref="LoadedProject"/> in the map is expected to be thread safe, so the lock is only needed when initially fetching
    /// the <see cref="LoadedProject"/>, or when creating new projects.
    /// </summary>
    private readonly SemaphoreSlim _gate = new(initialCount: 1);

    /// <summary>
    /// Maps the file path of a tracked project to the load state for the project.
    /// Absence of an entry indicates the project is not tracked, e.g. it was never loaded, or it was unloaded.
    /// When a project is unloaded, the <see cref="LoadedProject"/> is disposed and removed from the map. Any further use of that
    /// <see cref="LoadedProject"/> instance is expected to be a no-op, since it's possible we might have had some scheduled asynchronous work
    /// (a design time build, a file change notification) that might have scheduled and could also be in flight.
    /// </summary>
    private readonly Dictionary<string, LoadedProject> _loadedProjects = [];

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

    /// <summary>
    /// Maps the set of project file paths that were determined to need a NuGet restore to the set of paths that restore
    /// should actually be invoked on. The base implementation restores each project individually. Derived loaders may
    /// override this to coalesce the work, e.g. restoring an entire solution at once instead of restoring each contained
    /// project one at a time. This is invoked at restore time (rather than cached) so overrides can consult current,
    /// possibly-changed state such as the on-disk contents of the open solution.
    /// </summary>
    protected virtual ValueTask<ImmutableArray<string>> GetPathsToRestoreAsync(ImmutableArray<string> projectsThatNeedRestore, CancellationToken cancellationToken)
        => new(projectsThatNeedRestore);

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
            Listener);
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

    private sealed class ToastErrorReporter(IClientLanguageServerManager clientLanguageServerManager)
    {
        private int _displayedToast = 0;

        public async Task ReportErrorAsync(LSP.MessageType errorKind, string message, CancellationToken cancellationToken)
        {
            // We should display a toast when the value of displayedToast is 0.  This will also update the value to 1 meaning we won't send any more toasts.
            var shouldShowToast = Interlocked.CompareExchange(ref _displayedToast, value: 1, comparand: 0) == 0;
            if (shouldShowToast)
            {
                await clientLanguageServerManager.ShowToastNotificationAsync(errorKind, message, cancellationToken, ShowToastNotification.ShowCSharpLogsCommand);
            }
        }
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
                        try
                        {
                            var projectRestorePath = await @this.ReloadProjectAsync(
                                projectToLoad, toastErrorReporter, buildHostProcessManager, cancellationToken);

                            if (projectRestorePath is not null)
                                produceItem(projectRestorePath);
                        }
                        finally
                        {
                            projectToLoad.ProgressTracker?.OnItemProcessed();
                        }
                    },
                    args: (@this: this, toastErrorReporter, buildHostProcessManager),
                    cancellationToken).ConfigureAwait(false);
            }

            if (GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableAutomaticRestore) && projectsThatNeedRestore.Any())
            {
                var pathsToRestore = await GetPathsToRestoreAsync(projectsThatNeedRestore, cancellationToken);

                // This request blocks to ensure we aren't trying to run a design time build at the same time as a restore.
                await ProjectDependencyHelper.RestoreProjectsAsync(_workDoneProgressManager, pathsToRestore, EnableProgressReporting, _dotnetCliHelper, _logger, cancellationToken);
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
        public required bool HasFileBasedAppDirectives { get; init; }
        public required bool HasAllInformation { get; init; }
        public required BuildHostProcessKind PreferredBuildHostKind { get; init; }
        public required BuildHostProcessKind ActualBuildHostKind { get; init; }
    }

    /// <summary>Loads a project in the MSBuild host.</summary>
    /// <remarks>Caller needs to catch exceptions to avoid bringing down the project loader queue.</remarks>
    protected abstract Task<RemoteProjectLoadResult?> TryLoadProjectInMSBuildHostAsync(
        BuildHostProcessManager buildHostProcessManager, string projectPath, CancellationToken cancellationToken);

    protected virtual async Task<(ImmutableArray<ProjectFileInfo>, ProjectSystemProjectFactory)?> TryLoadProjectFromCacheAsync(string projectPath, CancellationToken cancellationToken)
        => null;

    /// <returns>The project file path that needs a NuGet restore, if any.</returns>
    private async Task<string?> ReloadProjectAsync(ProjectToLoad projectToLoad, ToastErrorReporter toastErrorReporter, BuildHostProcessManager buildHostProcessManager, CancellationToken cancellationToken)
    {
        BuildHostProcessKind? preferredBuildHostKindThatWeDidNotGet = null;
        var projectPath = projectToLoad.Path;
        LoadedProject? loadedProject;

        // Before doing any work, check if the project has already been unloaded
        using (await _gate.DisposableWaitAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_loadedProjects.TryGetValue(projectPath, out loadedProject))
                return null;
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

            var applied = await loadedProject.TryApplyLoadedProjectInfosAsync(
                loadedProjectInfos,
                isMiscellaneousFile: remoteProjectLoadResult.IsMiscellaneousFile,
                hasAllInformation: remoteProjectLoadResult.HasAllInformation,
                projectFactory,
                _projectTargetFrameworkManager,
                _workspaceFactory,
                _logger,
                cancellationToken);

            // We might have unloaded in the mean time, just skip
            if (!applied)
                return null;

            await loadedProject.ReportTelemetryIfNotPreviouslyReportedAsync(
                _projectLoadTelemetryReporter,
                isSdkStyle: preferredBuildHostKind == BuildHostProcessKind.NetCore,
                solutionPath: projectFactory.Workspace.CurrentSolution.FilePath,
                isMiscellaneousFile: remoteProjectLoadResult.IsMiscellaneousFile,
                isFileBasedProgram: remoteProjectLoadResult.IsFileBasedProgram,
                hasFileBasedAppDirectives: remoteProjectLoadResult.HasFileBasedAppDirectives);

            if (diagnosticLogItems.Any())
            {
                await LogDiagnosticsAsync(diagnosticLogItems);
            }
            else
            {
                _logger.LogInformation(string.Format(LanguageServerResources.Successfully_completed_load_of_0, projectPath));
            }

            return await loadedProject.NeedsRestoreAsync() ? remoteProjectLoadResult.ProjectRestorePath : null;
        }
        catch (Exception e) when (!ExceptionUtilities.IsCurrentOperationBeingCancelled(e, cancellationToken)) // Cancellation is only expected when we're shutting down, in which case there's no reason to do a report.
        {
            // Since our LogDiagnosticsAsync helper takes DiagnosticLogItems, let's just make one for this
            var message = string.Format(LanguageServerResources.Exception_thrown_0, e);
            var diagnosticLogItem = new DiagnosticLogItem(DiagnosticLogItemKind.Error, message, projectPath);
            await LogDiagnosticsAsync([diagnosticLogItem]);

            return null;
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

    protected async ValueTask<ImmutableArray<Project>> GetOrLoadProjectAsync(string projectPath, ProjectSystemProjectFactory primordialProjectFactory, Func<ProjectSystemProjectFactory, ProjectInfo> createPrimordialProjectInfo, bool doDesignTimeBuild)
    {
        using (await _gate.DisposableWaitAsync(CancellationToken.None))
        {
            Contract.ThrowIfTrue(_isDisposed, "Project loader is already disposed");

            if (_loadedProjects.TryGetValue(projectPath, out var existingLoadedProject))
                return await existingLoadedProject.GetExistingProjectsAsync();

            var primordialProjectInfo = createPrimordialProjectInfo(primordialProjectFactory);

            var newLoadedProject = new LoadedProject(projectPath, _fileChangeWatcher);
            _loadedProjects.Add(projectPath, newLoadedProject);
            var newProject = await newLoadedProject.CreatePrimordialProjectAsync(primordialProjectFactory, primordialProjectInfo);

            if (doDesignTimeBuild)
            {
                _projectsToReload.AddWork(new ProjectToLoad(projectPath));
                newLoadedProject.NeedsReload += (sender, args) => _projectsToReload.AddWork(new ProjectToLoad(projectPath));
            }

            return [newProject];
        }
    }

    /// <summary>
    /// Begins loading a project. If the project has already begun loading, returns without doing any additional work.
    /// </summary>
    protected async Task BeginLoadingProjectAsync(string projectPath, string? projectGuid, WorkDoneProgressTracker? progressTracker = null)
    {
        LoadedProject? loadedProject;

        using (await _gate.DisposableWaitAsync(CancellationToken.None))
        {
            Contract.ThrowIfTrue(_isDisposed, "Project loader is already disposed");

            // If we haven't already started this project loading, then let's create a project and start it loading
            if (!_loadedProjects.TryGetValue(projectPath, out loadedProject))
            {
                loadedProject = new LoadedProject(projectPath, _fileChangeWatcher);
                _loadedProjects.Add(projectPath, loadedProject);

                _projectsToReload.AddWork(new ProjectToLoad(Path: projectPath, progressTracker));

                loadedProject.NeedsReload += (sender, args) => _projectsToReload.AddWork(new ProjectToLoad(Path: projectPath, ProgressTracker: null));
            }

            if (projectGuid is not null)
                await loadedProject.SetProjectGuidForTelemetryAsync(Guid.Parse(projectGuid));
        }

        // Try to load the contents from the project cache if we have one; we'll do this outside the lock
        try
        {
            var cachedProjectStateAndFactory = await TryLoadProjectFromCacheAsync(projectPath, CancellationToken.None);

            if (cachedProjectStateAndFactory is not null)
            {
                var (cachedProjectState, projectFactory) = cachedProjectStateAndFactory.Value;
                await loadedProject.TryApplyLoadedProjectInfosAsync(
                    cachedProjectState,
                    isMiscellaneousFile: false,
                    hasAllInformation: true,
                    projectFactory,
                    _projectTargetFrameworkManager,
                    _workspaceFactory,
                    _logger,
                    CancellationToken.None,
                    onlyIfNoTargets: true);
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Exception encountered while trying to load cached state for {ProjectPath}", projectPath);
        }
    }

    protected Task WaitForProjectsToFinishLoadingAsync() => _projectsToReload.WaitUntilCurrentBatchCompletesAsync();

    /// <summary>Unloads all projects associated with this project loader.</summary>
    internal async ValueTask UnloadAllProjectsAsync()
    {
        using (await _gate.DisposableWaitAsync(CancellationToken.None))
        {
            foreach (var loadedProject in _loadedProjects.Values)
                await loadedProject.DisposeAsync();

            _loadedProjects.Clear();
        }
    }

    public virtual async ValueTask DisposeAsync()
    {
        using (await _gate.DisposableWaitAsync(CancellationToken.None))
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _projectsToReload.Dispose();

            foreach (var (_, project) in _loadedProjects)
                await project.DisposeAsync();

            _loadedProjects.Clear();
        }
    }

    internal async ValueTask<bool> TryUnloadProjectAsync(string projectPath, ProjectSystemProjectFactory? fromProjectFactory = null)
    {
        using (await _gate.DisposableWaitAsync(CancellationToken.None))
        {
            if (!_loadedProjects.TryGetValue(projectPath, out var loadedProject))
            {
                // It is common to be called with a path to a project which is already not loaded.
                // In this case, we should do nothing.
                return false;
            }

            // Caller can specify to only unload a project if it uses a specific project factory.
            if (fromProjectFactory != null && !await loadedProject.UsesProjectFactoryAsync(fromProjectFactory))
                return false;

            await loadedProject.DisposeAsync();
            Contract.ThrowIfFalse(_loadedProjects.Remove(projectPath));

            return true;
        }
    }
}
