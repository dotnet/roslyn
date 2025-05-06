// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DebugConfiguration;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Options;
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
    protected readonly AsyncBatchingWorkQueue<ProjectToLoad> ProjectsToLoadAndReload;

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
    /// The list of loaded projects in the workspace, keyed by project file path. The outer dictionary is a concurrent dictionary since we may be loading
    /// multiple projects at once; the key is a single List we just have a single thread processing any given project file. This is only to be used
    /// in <see cref="LoadOrReloadProjectsAsync" /> and downstream calls; any other updating of this (like unloading projects) should be achieved by adding
    /// things to the <see cref="ProjectsToLoadAndReload" />.
    /// </summary>
    private readonly ConcurrentDictionary<string, ImmutableArray<LoadedProject>> _loadedProjects = [];

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

        ProjectsToLoadAndReload = new AsyncBatchingWorkQueue<ProjectToLoad>(
            TimeSpan.FromMilliseconds(100),
            LoadOrReloadProjectsAsync,
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

    private async ValueTask LoadOrReloadProjectsAsync(ImmutableSegmentedList<ProjectToLoad> projectPathsToLoadOrReload, CancellationToken cancellationToken)
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
                    var projectNeedsRestore = await @this.LoadOrReloadProjectAsync(
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

    protected abstract Task<(RemoteProjectFile? projectFile, BuildHostProcessKind preferred, BuildHostProcessKind actual)> TryLoadProjectAsync(
        BuildHostProcessManager buildHostProcessManager, ProjectToLoad projectToLoad, CancellationToken cancellationToken);

    /// <returns>True if the project needs a NuGet restore, false otherwise.</returns>
    private async Task<bool> LoadOrReloadProjectAsync(ProjectToLoad projectToLoad, ToastErrorReporter toastErrorReporter, BuildHostProcessManager buildHostProcessManager, CancellationToken cancellationToken)
    {
        BuildHostProcessKind? preferredBuildHostKindThatWeDidNotGet = null;
        var projectPath = projectToLoad.Path;
        Contract.ThrowIfFalse(PathUtilities.IsAbsolute(projectPath));

        try
        {
            var (loadedFile, preferredBuildHostKind, actualBuildHostKind) = await TryLoadProjectAsync(buildHostProcessManager, projectToLoad, cancellationToken);
            if (preferredBuildHostKind != actualBuildHostKind)
                preferredBuildHostKindThatWeDidNotGet = preferredBuildHostKind;

            if (loadedFile is null)
            {
                _logger.LogWarning($"Unable to load project '{projectPath}'.");
                return false;
            }

            var diagnosticLogItems = await loadedFile.GetDiagnosticLogItemsAsync(cancellationToken);
            if (diagnosticLogItems.Any(item => item.Kind is DiagnosticLogItemKind.Error))
            {
                await LogDiagnosticsAsync(diagnosticLogItems);
                // We have total failures in evaluation, no point in continuing.
                return false;
            }

            var loadedProjectInfos = await loadedFile.GetProjectFileInfosAsync(cancellationToken);

            // The out-of-proc build host supports more languages than we may actually have Workspace binaries for, so ensure we can actually process that
            // language in-process.
            var projectLanguage = loadedProjectInfos.FirstOrDefault()?.Language;
            if (projectLanguage != null && ProjectFactory.Workspace.Services.GetLanguageService<ICommandLineParserService>(projectLanguage) == null)
            {
                return false;
            }

            if (!_loadedProjects.TryGetValue(projectPath, out var existingProjects))
                existingProjects = [];

            Dictionary<ProjectFileInfo, ProjectLoadTelemetryReporter.TelemetryInfo> telemetryInfos = [];
            var needsRestore = false;

            HashSet<LoadedProject> projectsToRemove = [.. existingProjects];
            foreach (var loadedProjectInfo in loadedProjectInfos)
            {
                var existingProject = existingProjects.FirstOrDefault(p => p.GetTargetFramework() == loadedProjectInfo.TargetFramework);
                bool targetNeedsRestore;
                ProjectLoadTelemetryReporter.TelemetryInfo targetTelemetryInfo;

                if (existingProject != null)
                {
                    projectsToRemove.Remove(existingProject);
                    (targetTelemetryInfo, targetNeedsRestore) = await existingProject.UpdateWithNewProjectInfoAsync(loadedProjectInfo, _logger);
                }
                else
                {
                    var loadedProject = await CreateAndTrackInitialProjectAsync(
                        projectPath,
                        loadedProjectInfo.Language,
                        loadedProjectInfo.TargetFramework,
                        loadedProjectInfo.IntermediateOutputFilePath);
                    loadedProject.NeedsReload += (_, _) => ProjectsToLoadAndReload.AddWork(projectToLoad with { ReportTelemetry = false });

                    (targetTelemetryInfo, targetNeedsRestore) = await loadedProject.UpdateWithNewProjectInfoAsync(loadedProjectInfo, _logger);

                    needsRestore |= targetNeedsRestore;
                    telemetryInfos[loadedProjectInfo] = targetTelemetryInfo with { IsSdkStyle = preferredBuildHostKind == BuildHostProcessKind.NetCore };
                }
            }

            using var newProjects = TemporaryArray<LoadedProject>.Empty;
            foreach (var project in existingProjects)
            {
                if (projectsToRemove.Contains(project))
                {
                    project.Dispose();
                }
                else
                {
                    newProjects.Add(project);
                }
            }

            _loadedProjects.AddOrUpdate(projectPath,
                // We expect the key always continues to be present in the dictionary during this operation.
                addValueFactory: (_, _) => throw new InvalidOperationException(),
                updateValueFactory: static (_, _, newProjects) => newProjects,
                factoryArgument: newProjects.ToImmutableAndClear());

            if (projectToLoad.ReportTelemetry)
            {
                await _projectLoadTelemetryReporter.ReportProjectLoadTelemetryAsync(telemetryInfos, projectToLoad, cancellationToken);
            }

            diagnosticLogItems = await loadedFile.GetDiagnosticLogItemsAsync(cancellationToken);
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

    /// <summary>
    /// Creates a <see cref="LoadedProject"/> which has bare minimum information, but, which documents can be added to and obtained from.
    /// </summary>
    protected async Task<LoadedProject> CreateAndTrackInitialProjectAsync(
        string projectPath,
        string language,
        string? targetFramework = null,
        string? intermediateOutputFilePath = null)
    {
        var projectSystemName = targetFramework is null ? projectPath : $"{projectPath} (${targetFramework})";

        var projectCreationInfo = new ProjectSystemProjectCreationInfo
        {
            AssemblyName = projectSystemName,
            FilePath = projectPath,
            CompilationOutputAssemblyFilePath = intermediateOutputFilePath,
        };

        var projectSystemProject = await ProjectFactory.CreateAndAddToWorkspaceAsync(
            projectSystemName,
            language,
            projectCreationInfo,
            _projectSystemHostInfo);

        var loadedProject = new LoadedProject(projectSystemProject, ProjectFactory.Workspace.Services.SolutionServices, _fileChangeWatcher, _targetFrameworkManager);
        _loadedProjects.AddOrUpdate(projectPath, addValue: [loadedProject], updateValueFactory: (_, arr) => arr.Add(loadedProject));
        return loadedProject;
    }
}
