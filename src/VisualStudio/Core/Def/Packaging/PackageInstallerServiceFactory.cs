// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.SymbolSearch;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Threading;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Contracts;
using Roslyn.Utilities;
using SVsServiceProvider = Microsoft.VisualStudio.Shell.SVsServiceProvider;
using VSUtilities = Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Packaging;

/// <summary>
/// Free threaded wrapper around the NuGet.VisualStudio STA package installer interfaces.
/// We want to be able to make queries about packages from any thread.  For example, the
/// add-NuGet-reference feature wants to know what packages a project already has 
/// references to.  NuGet.VisualStudio provides this information, but only in a COM STA 
/// manner.  As we don't want our background work to bounce and block on the UI thread 
/// we have this helper class which queries the information on the UI thread and caches
/// the data so it can be read from the background.
/// </summary>
[ExportWorkspaceService(typeof(IPackageInstallerService)), Shared]
internal partial class PackageInstallerService : AbstractDelayStartedService, IPackageInstallerService, IVsSearchProviderCallback
{
    // Proper name, should not be localized.
    private const string NugetTitle = "NuGet";

    private readonly VSUtilities.IUIThreadOperationExecutor _operationExecutor;
    private readonly IVsService<IBrokeredServiceContainer> _brokeredServiceContainer;
    private readonly SVsServiceProvider _serviceProvider;
    private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
    private readonly IAsynchronousOperationListener _listener;

    private readonly Lazy<IVsPackageInstaller2>? _packageInstaller;
    private readonly Lazy<IVsPackageUninstaller>? _packageUninstaller;
    private readonly Lazy<IVsPackageSourceProvider>? _packageSourceProvider;
    private IVsPackage? _nugetPackageManager;

    /// <summary>
    /// Used to keep track of what types of changes we've seen so we can then determine what to refresh on the UI
    /// thread.  If we hear about project changes, we only refresh that project.  If we hear about a solution level
    /// change, we'll refresh all projects.
    /// </summary>
    /// <remarks>
    /// <c>solutionChanged == true iff changedProject == null</c> and <c>solutionChanged == false iff changedProject
    /// != null</c>. So technically having both values is redundant.  However, i like the clarity of having both.
    /// </remarks>
    private readonly AsyncBatchingWorkQueue<(bool solutionChanged, ProjectId? changedProject)> _workQueue;

    private readonly ConcurrentDictionary<ProjectId, ProjectState> _projectToInstalledPackageAndVersion = [];

    /// <summary>
    /// Lock used to protect reads and writes of <see cref="_packageSourcesTask"/>.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// Task uses to compute the set of package sources on demand when asked the first time.  The value will be
    /// computed and cached in the task.  When this value changes, the task will simply be cleared out.
    /// </summary>
    private Task<ImmutableArray<PackageSource>>? _packageSourcesTask;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PackageInstallerService(
        IThreadingContext threadingContext,
        VSUtilities.IUIThreadOperationExecutor operationExecutor,
        IAsynchronousOperationListenerProvider listenerProvider,
        VisualStudioWorkspaceImpl workspace,
        IGlobalOptionService globalOptions,
        IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> brokeredServiceContainer,
        SVsServiceProvider serviceProvider,
        IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
        [Import(AllowDefault = true)] Lazy<IVsPackageInstaller2>? packageInstaller,
        [Import(AllowDefault = true)] Lazy<IVsPackageUninstaller>? packageUninstaller,
        [Import(AllowDefault = true)] Lazy<IVsPackageSourceProvider>? packageSourceProvider)
        : base(threadingContext,
               globalOptions,
               workspace,
               listenerProvider,
               SymbolSearchGlobalOptionsStorage.Enabled,
               [SymbolSearchOptionsStorage.SearchReferenceAssemblies, SymbolSearchOptionsStorage.SearchNuGetPackages])
    {
        _operationExecutor = operationExecutor;

        _brokeredServiceContainer = brokeredServiceContainer;
        _serviceProvider = serviceProvider;

        _editorAdaptersFactoryService = editorAdaptersFactoryService;
        _packageInstaller = packageInstaller;
        _packageUninstaller = packageUninstaller;
        _packageSourceProvider = packageSourceProvider;
        _listener = listenerProvider.GetListener(FeatureAttribute.PackageInstaller);

        // Setup the work queue to allow us to hear about flurries of changes and then respond to them in batches
        // every second.  Note: we pass in EqualityComparer<...>.Default since we don't care about ordering, and
        // since once we hear about changes to a project (or the whole solution), we don't need to keep track if we
        // hear about the same thing in that batch window interval.
        _workQueue = new AsyncBatchingWorkQueue<(bool solutionChanged, ProjectId? changedProject)>(
            TimeSpan.FromSeconds(1),
            this.ProcessWorkQueueAsync,
            equalityComparer: EqualityComparer<(bool solutionChanged, ProjectId? changedProject)>.Default,
            _listener,
            this.DisposalToken);
    }

    public event EventHandler? PackageSourcesChanged;

    public ImmutableArray<PackageSource> TryGetPackageSources()
    {
        Task<ImmutableArray<PackageSource>> localPackageSourcesTask;
        lock (_gate)
        {
            _packageSourcesTask ??= Task.Run(() => GetPackageSourcesAsync(), this.DisposalToken);

            localPackageSourcesTask = _packageSourcesTask;
        }

        if (localPackageSourcesTask.Status == TaskStatus.RanToCompletion)
        {
            return localPackageSourcesTask.Result;
        }
        else
        {
            // The result was not available yet (or it was canceled/faulted).  Just return an empty result to
            // signify we couldn't get this right now.
            return ImmutableArray<PackageSource>.Empty;
        }
    }

    private async Task<ImmutableArray<PackageSource>> GetPackageSourcesAsync()
    {
        await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);

        try
        {
            if (_packageSourceProvider != null)
                return _packageSourceProvider.Value.GetSources(includeUnOfficial: true, includeDisabled: false).SelectAsArray(r => new PackageSource(r.Key, r.Value));
        }
        catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException)
        {
            // These exceptions can happen when the nuget.config file is broken.
        }
        catch (ArgumentException ae) when (FatalError.ReportAndCatch(ae))
        {
            // This exception can happen when the nuget.config file is broken, e.g. invalid credentials.
            // https://github.com/dotnet/roslyn/issues/40857
        }

        return ImmutableArray<PackageSource>.Empty;
    }

    [MemberNotNullWhen(true, nameof(_packageInstaller))]
    [MemberNotNullWhen(true, nameof(_packageUninstaller))]
    [MemberNotNullWhen(true, nameof(_packageSourceProvider))]
    private bool IsEnabled
    {
        get
        {
            return _packageInstaller != null
                && _packageUninstaller != null
                && _packageSourceProvider != null;
        }
    }

    bool IPackageInstallerService.IsEnabled(ProjectId projectId)
    {
        if (!IsEnabled)
        {
            return false;
        }

        if (_projectToInstalledPackageAndVersion.TryGetValue(projectId, out var state))
        {
            return state.IsEnabled;
        }

        // If we haven't scanned the project yet, assume that we're available for it.
        return true;
    }

    private async ValueTask<IVsPackageSourceProvider> GetPackageSourceProviderAsync()
    {
        Contract.ThrowIfFalse(IsEnabled);

        if (!_packageSourceProvider.IsValueCreated)
        {
            // Switch to background thread for known assembly load
            await TaskScheduler.Default;
        }

        return _packageSourceProvider.Value;
    }

    protected override async Task EnableServiceAsync(CancellationToken cancellationToken)
    {
        if (!IsEnabled)
            return;

        var packageSourceProvider = await GetPackageSourceProviderAsync().ConfigureAwait(false);

        // Start listening to additional events workspace changes.
        Workspace.WorkspaceChanged += OnWorkspaceChanged;
        packageSourceProvider.SourcesChanged += OnSourceProviderSourcesChanged;

        // Kick off an initial set of work that will analyze the entire solution.
        OnSourceProviderSourcesChanged(this, EventArgs.Empty);
        _workQueue.AddWork((solutionChanged: true, changedProject: null));
    }

    private void OnSourceProviderSourcesChanged(object sender, EventArgs e)
    {
        lock (_gate)
        {
            // If the existing _packageSourcesTask is null, that means no one has asked us about package sources
            // yet.  So no need for us to do anything if that's true.  We'll just continue waiting until first
            // asked.  However, if it's not null, that means we have already been asked.  In that case, proactively
            // get the new set of sources so they're ready for the next time we're asked.
            if (_packageSourcesTask != null)
                _packageSourcesTask = Task.Run(() => GetPackageSourcesAsync(), this.DisposalToken);
        }

        PackageSourcesChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<bool> TryInstallPackageAsync(
        Workspace workspace,
        DocumentId documentId,
        string? source,
        string packageName,
        string? version,
        bool includePrerelease,
        IProgress<CodeAnalysisProgress> progressTracker,
        CancellationToken cancellationToken)
    {
        // The 'workspace == _workspace' line is probably not necessary. However, we include 
        // it just to make sure that someone isn't trying to install a package into a workspace
        // other than the VisualStudioWorkspace.
        if (workspace == Workspace && Workspace != null && IsEnabled)
        {
            await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var projectId = documentId.ProjectId;
            var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(SDTE));
            var dteProject = Workspace.TryGetDTEProject(projectId);
            var projectGuid = Workspace.GetProjectGuid(projectId);
            if (dteProject != null && projectGuid != Guid.Empty)
            {
                var undoManager = _editorAdaptersFactoryService.TryGetUndoManager(
                    workspace, documentId, cancellationToken);

                return await TryInstallAndAddUndoActionAsync(
                    source, packageName, version, includePrerelease, projectGuid, dte, dteProject, undoManager,
                    progressTracker, cancellationToken).ConfigureAwait(false);
            }
        }

        return false;
    }

    private async Task<bool> TryInstallPackageAsync(
        string? source,
        string packageName,
        string? version,
        bool includePrerelease,
        Guid projectGuid,
        EnvDTE.DTE dte,
        EnvDTE.Project dteProject,
        IProgress<CodeAnalysisProgress> progressTracker,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(IsEnabled);

        var description = string.Format(ServicesVSResources.Installing_0, packageName);
        progressTracker.Report(CodeAnalysisProgress.Description(description));
        await UpdateStatusBarAsync(dte, description, cancellationToken).ConfigureAwait(false);

        try
        {
            return await this.PerformNuGetProjectServiceWorkAsync(async (nugetService, cancellationToken) =>
            {
                // explicitly switch to BG thread to do the installation as nuget installer APIs are free threaded.
                await TaskScheduler.Default;

                var installedPackagesMap = await GetInstalledPackagesMapAsync(nugetService, projectGuid, cancellationToken).ConfigureAwait(false);
                if (installedPackagesMap.ContainsKey(packageName))
                    return false;

                // Once we start the installation, we can't cancel anymore.
                cancellationToken = default;
                if (version == null)
                {
                    _packageInstaller.Value.InstallLatestPackage(
                        source, dteProject, packageName, includePrerelease, ignoreDependencies: false);
                }
                else
                {
                    _packageInstaller.Value.InstallPackage(
                        source, dteProject, packageName, version, ignoreDependencies: false);
                }

                installedPackagesMap = await GetInstalledPackagesMapAsync(nugetService, projectGuid, cancellationToken).ConfigureAwait(false);
                var installedVersion = installedPackagesMap.TryGetValue(packageName, out var result) ? result : null;

                await UpdateStatusBarAsync(
                    dte, string.Format(ServicesVSResources.Installing_0_completed, GetStatusBarText(packageName, installedVersion)),
                    cancellationToken).ConfigureAwait(false);

                return true;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await UpdateStatusBarAsync(dte, ServicesVSResources.Package_install_canceled, cancellationToken).ConfigureAwait(false);
            return false;
        }
        catch (Exception e) when (FatalError.ReportAndCatch(e))
        {
            await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            dte.StatusBar.Text = string.Format(ServicesVSResources.Package_install_failed_colon_0, e.Message);

            var notificationService = Workspace.Services.GetService<INotificationService>();
            notificationService?.SendNotification(
                string.Format(ServicesVSResources.Installing_0_failed_Additional_information_colon_1, packageName, e.Message),
                severity: NotificationSeverity.Error);

            return false;
        }
    }

    private async Task UpdateStatusBarAsync(EnvDTE.DTE dte, string text, CancellationToken cancellationToken)
    {
        await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        dte.StatusBar.Text = text;
    }

    private static string GetStatusBarText(string packageName, string? installedVersion)
        => installedVersion == null ? packageName : $"{packageName} - {installedVersion}";

    private async Task<bool> TryUninstallPackageAsync(
        string packageName, Guid projectGuid, EnvDTE.DTE dte, EnvDTE.Project dteProject,
        IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(IsEnabled);

        var description = string.Format(ServicesVSResources.Uninstalling_0, packageName);
        progressTracker.Report(CodeAnalysisProgress.Description(description));
        await UpdateStatusBarAsync(dte, description, cancellationToken).ConfigureAwait(false);

        try
        {
            return await this.PerformNuGetProjectServiceWorkAsync(async (nugetService, cancellationToken) =>
            {
                // explicitly switch to BG thread to do the installation as nuget installer APIs are free threaded.
                await TaskScheduler.Default;

                var installedPackagesMap = await GetInstalledPackagesMapAsync(nugetService, projectGuid, cancellationToken).ConfigureAwait(false);
                if (!installedPackagesMap.TryGetValue(packageName, out var installedVersion))
                    return false;

                cancellationToken.ThrowIfCancellationRequested();

                // Once we start the installation, we can't cancel anymore.
                cancellationToken = default;
                _packageUninstaller.Value.UninstallPackage(dteProject, packageName, removeDependencies: true);

                await UpdateStatusBarAsync(
                    dte, string.Format(ServicesVSResources.Uninstalling_0_completed, GetStatusBarText(packageName, installedVersion)), cancellationToken).ConfigureAwait(false);

                return true;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await UpdateStatusBarAsync(dte, ServicesVSResources.Package_uninstall_canceled, cancellationToken).ConfigureAwait(false);
            return false;
        }
        catch (Exception e) when (FatalError.ReportAndCatch(e))
        {
            await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            dte.StatusBar.Text = string.Format(ServicesVSResources.Package_uninstall_failed_colon_0, e.Message);

            var notificationService = Workspace.Services.GetService<INotificationService>();
            notificationService?.SendNotification(
                string.Format(ServicesVSResources.Uninstalling_0_failed_Additional_information_colon_1, packageName, e.Message),
                severity: NotificationSeverity.Error);

            return false;
        }
    }

    private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
    {
        // ThisCanBeCalledOnAnyThread();

        var solutionChanged = false;
        ProjectId? changedProject = null;
        switch (e.Kind)
        {
            default:
                // Nothing to do for any other events.
                return;

            case WorkspaceChangeKind.ProjectAdded:
            case WorkspaceChangeKind.ProjectChanged:
            case WorkspaceChangeKind.ProjectReloaded:
            case WorkspaceChangeKind.ProjectRemoved:
                changedProject = e.ProjectId;
                break;

            case WorkspaceChangeKind.SolutionAdded:
            case WorkspaceChangeKind.SolutionChanged:
            case WorkspaceChangeKind.SolutionCleared:
            case WorkspaceChangeKind.SolutionReloaded:
            case WorkspaceChangeKind.SolutionRemoved:
                solutionChanged = true;
                break;
        }

        Contract.ThrowIfNull(_workQueue, "We should only register for events after having create the WorkQueue");
        _workQueue.AddWork((solutionChanged, changedProject));
    }

    private ValueTask ProcessWorkQueueAsync(
        ImmutableSegmentedList<(bool solutionChanged, ProjectId? changedProject)> workQueue, CancellationToken cancellationToken)
    {
        // ThisCanBeCalledOnAnyThread();

        Contract.ThrowIfNull(_workQueue, "How could we be processing a workqueue change without a workqueue?");

        // If we've been disconnected, then there's no point proceeding.
        if (Workspace == null || !IsEnabled)
            return ValueTaskFactory.CompletedTask;

        return ProcessWorkQueueWorkerAsync(workQueue, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private async ValueTask ProcessWorkQueueWorkerAsync(
        ImmutableSegmentedList<(bool solutionChanged, ProjectId? changedProject)> workQueue, CancellationToken cancellationToken)
    {
        // ThisCanBeCalledOnAnyThread();

        await PerformNuGetProjectServiceWorkAsync<object>(async (nugetService, cancellationToken) =>
        {
            // Figure out the entire set of projects to process.
            using var _ = PooledHashSet<ProjectId>.GetInstance(out var projectsToProcess);

            var solution = Workspace.CurrentSolution;
            AddProjectsToProcess(workQueue, solution, projectsToProcess);

            // And Process them one at a time.
            foreach (var projectId in projectsToProcess)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ProcessProjectChangeAsync(nugetService, solution, projectId, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T?> PerformNuGetProjectServiceWorkAsync<T>(
        Func<INuGetProjectService, CancellationToken, ValueTask<T?>> doWorkAsync, CancellationToken cancellationToken)
    {
        // Make sure we are on the thread pool to avoid UI thread dependencies if external code uses ConfigureAwait(true).
        // GetServiceAsync/GetProxyAsync and the cast below are all explicitly documented as being BG thread safe.
        await TaskScheduler.Default;

        var serviceContainer = await _brokeredServiceContainer.GetValueOrNullAsync(cancellationToken).ConfigureAwait(false);
        var serviceBroker = serviceContainer?.GetFullAccessServiceBroker();
        if (serviceBroker == null)
            return default;

        var nugetService = await serviceBroker.GetProxyAsync<INuGetProjectService>(NuGetServices.NuGetProjectServiceV1, cancellationToken: cancellationToken).ConfigureAwait(false);

        using (nugetService as IDisposable)
        {
            // If we didn't get a nuget service, there's nothing we can do in terms of querying the solution for
            // nuget info.
            if (nugetService == null)
                return default;

            return await doWorkAsync(nugetService, cancellationToken).ConfigureAwait(false);
        }
    }

    private void AddProjectsToProcess(
        ImmutableSegmentedList<(bool solutionChanged, ProjectId? changedProject)> workQueue, Solution solution, HashSet<ProjectId> projectsToProcess)
    {
        // ThisCanBeCalledOnAnyThread();

        // If we detected a solution change, then we need to process all projects.
        // This includes all the projects that we already know about, as well as
        // all the projects in the current workspace solution.
        if (workQueue.Any(t => t.solutionChanged))
        {
            projectsToProcess.AddRange(solution.ProjectIds);
            projectsToProcess.AddRange(_projectToInstalledPackageAndVersion.Keys);
        }
        else
        {
            projectsToProcess.AddRange(workQueue.Select(t => t.changedProject).WhereNotNull());
        }
    }

    private async Task ProcessProjectChangeAsync(
        INuGetProjectService nugetService,
        Solution solution,
        ProjectId projectId,
        CancellationToken cancellationToken)
    {
        // ThisCanBeCalledOnAnyThread();

        var project = solution.GetProject(projectId);

        // We really only need to know the NuGet status for managed language projects.
        // Also, the NuGet APIs may throw on some projects that don't implement the 
        // full set of DTE APIs they expect.  So we filter down to just C# and VB here
        // as we know these languages are safe to build up this index for.
        ProjectState? newState = null;

        if (project?.Language is LanguageNames.CSharp or
            LanguageNames.VisualBasic)
        {
            var projectGuid = Workspace.GetProjectGuid(projectId);
            if (projectGuid != Guid.Empty)
            {
                newState = await GetCurrentProjectStateAsync(
                    nugetService, projectGuid, cancellationToken).ConfigureAwait(false);
            }
        }

        // If we weren't able to get the nuget state for the project (i.e. it's not a c#/vb project, or we got a
        // crash attempting to get nuget information).  Mark this project as something that nuget-add-import is not
        // supported for.
        _projectToInstalledPackageAndVersion[projectId] = newState ?? ProjectState.Disabled;
    }

    private static async Task<ProjectState?> GetCurrentProjectStateAsync(
        INuGetProjectService nugetService,
        Guid projectGuid,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var installedPackagesMap = await GetInstalledPackagesMapAsync(nugetService, projectGuid, cancellationToken).ConfigureAwait(false);

            return new ProjectState(installedPackagesMap);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            return null;
        }
    }

    private static async Task<ImmutableDictionary<string, string>> GetInstalledPackagesMapAsync(INuGetProjectService nugetService, Guid projectGuid, CancellationToken cancellationToken)
    {
        var installedPackagesResult = await nugetService.GetInstalledPackagesAsync(projectGuid, cancellationToken).ConfigureAwait(false);

        using var _ = PooledDictionary<string, string>.GetInstance(out var installedPackages);
        if (installedPackagesResult?.Status == InstalledPackageResultStatus.Successful)
        {
            foreach (var installedPackage in installedPackagesResult.Packages)
                installedPackages[installedPackage.Id] = installedPackage.Version;
        }

        var installedPackagesMap = installedPackages.ToImmutableDictionary();
        return installedPackagesMap;
    }

    public bool IsInstalled(ProjectId projectId, string packageName)
    {
        // ThisCanBeCalledOnAnyThread();
        return _projectToInstalledPackageAndVersion.TryGetValue(projectId, out var installedPackages) &&
            installedPackages.IsInstalled(packageName);
    }

    public ImmutableArray<string> GetInstalledVersions(string packageName)
    {
        // ThisCanBeCalledOnAnyThread();

        using var _ = PooledHashSet<string>.GetInstance(out var installedVersions);
        foreach (var state in _projectToInstalledPackageAndVersion.Values)
        {
            if (state.TryGetInstalledVersion(packageName, out var version))
                installedVersions.Add(version);
        }

        // Order the versions with a weak heuristic so that 'newer' versions come first.
        // Essentially, we try to break the version on dots, and then we use a LogicalComparer
        // to try to more naturally order the things we see between the dots.
        var versionsAndSplits = installedVersions.Select(v => new { Version = v, Split = v.Split('.') }).ToList();

        versionsAndSplits.Sort((v1, v2) =>
        {
            var diff = CompareSplit(v1.Split, v2.Split);
            return diff != 0 ? diff : -v1.Version.CompareTo(v2.Version);
        });

        return versionsAndSplits.Select(v => v.Version).ToImmutableArray();
    }

    private static int CompareSplit(string[] split1, string[] split2)
    {
        // ThisCanBeCalledOnAnyThread();

        for (int i = 0, n = Math.Min(split1.Length, split2.Length); i < n; i++)
        {
            // Prefer things that look larger.  i.e. 7 should come before 6. 
            // Use a logical string comparer so that 10 is understood to be
            // greater than 3.
            var diff = -LogicalStringComparer.Instance.Compare(split1[i], split2[i]);
            if (diff != 0)
            {
                return diff;
            }
        }

        // Choose the one with more parts.
        return split2.Length - split1.Length;
    }

    public ImmutableArray<Project> GetProjectsWithInstalledPackage(Solution solution, string packageName, string version)
    {
        // ThisCanBeCalledOnAnyThread();

        using var _ = ArrayBuilder<Project>.GetInstance(out var result);

        foreach (var (projectId, state) in _projectToInstalledPackageAndVersion)
        {
            if (state.TryGetInstalledVersion(packageName, out var installedVersion) &&
                installedVersion == version)
            {
                var project = solution.GetProject(projectId);
                if (project != null)
                    result.Add(project);
            }
        }

        return result.ToImmutableAndClear();
    }

    public bool CanShowManagePackagesDialog()
        => TryGetOrLoadNuGetPackageManager(out _);

    private bool TryGetOrLoadNuGetPackageManager([NotNullWhen(true)] out IVsPackage? nugetPackageManager)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();

        if (_nugetPackageManager != null)
        {
            nugetPackageManager = _nugetPackageManager;
            return true;
        }

        nugetPackageManager = null;
        var shell = (IVsShell)_serviceProvider.GetService(typeof(SVsShell));
        if (shell == null)
        {
            return false;
        }

        var nugetGuid = new Guid("5fcc8577-4feb-4d04-ad72-d6c629b083cc");
        shell.LoadPackage(ref nugetGuid, out nugetPackageManager);
        _nugetPackageManager = nugetPackageManager;
        return nugetPackageManager != null;
    }

    public void ShowManagePackagesDialog(string packageName)
    {
        if (!TryGetOrLoadNuGetPackageManager(out var nugetPackageManager))
        {
            return;
        }

        // We're able to launch the package manager (with an item in its search box) by
        // using the IVsSearchProvider API that the NuGet package exposes.
        //
        // We get that interface for it and then pass it a SearchQuery that effectively
        // wraps the package name we're looking for.  The NuGet package will then read
        // out that string and populate their search box with it.
        var extensionProvider = (IVsPackageExtensionProvider)nugetPackageManager;
        var extensionGuid = new Guid("042C2B4B-C7F7-49DB-B7A2-402EB8DC7892");
        var emptyGuid = Guid.Empty;
        var searchProvider = (IVsSearchProvider)extensionProvider.CreateExtensionInstance(ref emptyGuid, ref extensionGuid);
        var task = searchProvider.CreateSearch(dwCookie: 1, pSearchQuery: new SearchQuery(packageName), pSearchCallback: this);
        task.Start();
    }

    public void ReportProgress(IVsSearchTask pTask, uint dwProgress, uint dwMaxProgress)
    {
    }

    public void ReportComplete(IVsSearchTask pTask, uint dwResultsFound)
    {
    }

    public void ReportResult(IVsSearchTask pTask, IVsSearchItemResult pSearchItemResult)
        => pSearchItemResult.InvokeAction();

    public void ReportResults(IVsSearchTask pTask, uint dwResults, IVsSearchItemResult[] pSearchItemResults)
    {
    }

    private class SearchQuery : IVsSearchQuery
    {
        public SearchQuery(string packageName)
            => this.SearchString = packageName;

        public string SearchString { get; }

        public uint ParseError => 0;

        public uint GetTokens(uint dwMaxTokens, IVsSearchToken[] rgpSearchTokens)
            => 0;
    }
}
