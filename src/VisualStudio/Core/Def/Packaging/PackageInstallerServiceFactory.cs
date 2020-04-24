// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.ForegroundNotification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.SymbolSearch;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;
using Roslyn.Utilities;
using SVsServiceProvider = Microsoft.VisualStudio.Shell.SVsServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

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
        /// <summary>
        /// How much time we will give work to run on the UI thread.
        /// </summary>
        private const int DefaultTimeSliceInMS = ForegroundNotificationService.DefaultTimeSliceInMS;

        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly SVsServiceProvider _serviceProvider;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;

        private readonly Lazy<IVsPackageInstallerServices> _packageInstallerServices;
        private readonly Lazy<IVsPackageInstaller2> _packageInstaller;
        private readonly Lazy<IVsPackageUninstaller> _packageUninstaller;
        private readonly Lazy<IVsPackageSourceProvider> _packageSourceProvider;

        private AsyncLazy<ImmutableArray<PackageSource>> _packageSources;
        private IVsPackage _nugetPackageManager;

        // We keep track of what types of changes we've seen so we can then determine what to
        // refresh on the UI thread.  If we hear about project changes, we only refresh that
        // project.  If we hear about a solution level change, we'll refresh all projects.
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly AsyncBatchingWorkQueue<(bool changedSolution, ProjectId changedProject)> _workQueue;

        private readonly ConcurrentDictionary<ProjectId, ProjectState> _projectToInstalledPackageAndVersion =
            new ConcurrentDictionary<ProjectId, ProjectState>();

        public event EventHandler PackageSourcesChanged;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PackageInstallerService(
            IThreadingContext threadingContext,
            VisualStudioWorkspaceImpl workspace,
            SVsServiceProvider serviceProvider,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            [Import(AllowDefault = true)] Lazy<IVsPackageInstallerServices> packageInstallerServices,
            [Import(AllowDefault = true)] Lazy<IVsPackageInstaller2> packageInstaller,
            [Import(AllowDefault = true)] Lazy<IVsPackageUninstaller> packageUninstaller,
            [Import(AllowDefault = true)] Lazy<IVsPackageSourceProvider> packageSourceProvider)
            : base(threadingContext,
                   workspace,
                   SymbolSearchOptions.Enabled,
                   SymbolSearchOptions.SuggestForTypesInReferenceAssemblies,
                   SymbolSearchOptions.SuggestForTypesInNuGetPackages)
        {
            _workspace = workspace;
            _serviceProvider = serviceProvider;
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            _packageInstallerServices = packageInstallerServices;
            _packageInstaller = packageInstaller;
            _packageUninstaller = packageUninstaller;
            _packageSourceProvider = packageSourceProvider;
            ResetPackageSources();

            // Create a work queue to allow us to batch up flurries of notifications to be processed at some time in the
            // future.  To prevent saturating the UI thread, we process no more than once every 250ms.
            //
            // Our work notifications are a simple pair that says if we should process the entire solution or, if not,
            // which project we need to process.  Because it's just simple data, and the order doesn't matter, we can
            // pass in an appropriate IEqualityComparer to dedupe notifications as they come in.
            _workQueue = new AsyncBatchingWorkQueue<(bool changedSolution, ProjectId changedProject)>(
                TimeSpan.FromMilliseconds(250),
                ProcessBatchedChangesAsync,
                EqualityComparer<(bool, ProjectId)>.Default,
                asyncListener: null,
                _cancellationTokenSource.Token);
        }

        void IDisposable.Dispose()
            => _cancellationTokenSource.Cancel();

        private void ResetPackageSources()
            => Interlocked.Exchange(ref _packageSources, new AsyncLazy<ImmutableArray<PackageSource>>(
                c => ComputePackageSourcesAsync(c), cacheResult: true));

        private async Task<ImmutableArray<PackageSource>> ComputePackageSourcesAsync(CancellationToken cancellationToken)
        {
            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);

            try
            {
                return _packageSourceProvider.Value.GetSources(includeUnOfficial: true, includeDisabled: false)
                    .SelectAsArray(r => new PackageSource(r.Key, r.Value));
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is InvalidOperationException)
            {
                // These exceptions can happen when the nuget.config file is broken.
                return ImmutableArray<PackageSource>.Empty;
            }
            catch (ArgumentException ae) when (FatalError.ReportWithoutCrash(ae))
            {
                // This exception can happen when the nuget.config file is broken, e.g. invalid credentials.
                // https://github.com/dotnet/roslyn/issues/40857
                return ImmutableArray<PackageSource>.Empty;
            }
        }

        public Task<ImmutableArray<PackageSource>> GetPackageSourcesAsync(CancellationToken cancellationToken)
            => _packageSources.GetValueAsync(cancellationToken);

        private bool IsEnabled
        {
            get
            {
                return _packageInstallerServices != null
                    && _packageInstaller != null
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

        protected override void EnableService()
        {
            if (!IsEnabled)
            {
                return;
            }

            // Start listening to additional events workspace changes.
            _workspace.WorkspaceChanged += OnWorkspaceChanged;
            _packageSourceProvider.Value.SourcesChanged += OnSourceProviderSourcesChanged;
        }

        protected override void StartWorking()
        {
            this.AssertIsForeground();

            if (!this.IsEnabled)
            {
                return;
            }

            OnSourceProviderSourcesChanged(this, EventArgs.Empty);
            OnWorkspaceChanged(changedSolution: true, changedProjectId: null);
        }

        private void OnSourceProviderSourcesChanged(object sender, EventArgs e)
        {
            ResetPackageSources();
            PackageSourcesChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool TryInstallPackage(
            Workspace workspace,
            DocumentId documentId,
            string source,
            string packageName,
            string versionOpt,
            bool includePrerelease,
            CancellationToken cancellationToken)
        {
            this.AssertIsForeground();

            // The 'workspace == _workspace' line is probably not necessary. However, we include 
            // it just to make sure that someone isn't trying to install a package into a workspace
            // other than the VisualStudioWorkspace.
            if (workspace == _workspace && _workspace != null && IsEnabled)
            {
                var projectId = documentId.ProjectId;
                var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(SDTE));
                var dteProject = _workspace.TryGetDTEProject(projectId);
                if (dteProject != null)
                {
                    var undoManager = _editorAdaptersFactoryService.TryGetUndoManager(
                        workspace, documentId, cancellationToken);

                    return TryInstallAndAddUndoAction(
                        source, packageName, versionOpt, includePrerelease, dte, dteProject, undoManager);
                }
            }

            return false;
        }

        private bool TryInstallPackage(
            string source,
            string packageName,
            string versionOpt,
            bool includePrerelease,
            EnvDTE.DTE dte,
            EnvDTE.Project dteProject)
        {
            try
            {
                if (!_packageInstallerServices.Value.IsPackageInstalled(dteProject, packageName))
                {
                    dte.StatusBar.Text = string.Format(ServicesVSResources.Installing_0, packageName);

                    if (versionOpt == null)
                    {
                        _packageInstaller.Value.InstallLatestPackage(
                            source, dteProject, packageName, includePrerelease, ignoreDependencies: false);
                    }
                    else
                    {
                        _packageInstaller.Value.InstallPackage(
                            source, dteProject, packageName, versionOpt, ignoreDependencies: false);
                    }

                    var installedVersion = GetInstalledVersion(packageName, dteProject);
                    dte.StatusBar.Text = string.Format(ServicesVSResources.Installing_0_completed,
                        GetStatusBarText(packageName, installedVersion));

                    return true;
                }

                // fall through.
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
                dte.StatusBar.Text = string.Format(ServicesVSResources.Package_install_failed_colon_0, e.Message);

                var notificationService = _workspace.Services.GetService<INotificationService>();
                notificationService?.SendNotification(
                    string.Format(ServicesVSResources.Installing_0_failed_Additional_information_colon_1, packageName, e.Message),
                    severity: NotificationSeverity.Error);

                // fall through.
            }

            return false;
        }

        private static string GetStatusBarText(string packageName, string installedVersion)
            => installedVersion == null ? packageName : $"{packageName} - {installedVersion}";

        private bool TryUninstallPackage(
            string packageName, EnvDTE.DTE dte, EnvDTE.Project dteProject)
        {
            this.AssertIsForeground();

            try
            {
                if (_packageInstallerServices.Value.IsPackageInstalled(dteProject, packageName))
                {
                    dte.StatusBar.Text = string.Format(ServicesVSResources.Uninstalling_0, packageName);
                    var installedVersion = GetInstalledVersion(packageName, dteProject);
                    _packageUninstaller.Value.UninstallPackage(dteProject, packageName, removeDependencies: true);

                    dte.StatusBar.Text = string.Format(ServicesVSResources.Uninstalling_0_completed,
                        GetStatusBarText(packageName, installedVersion));

                    return true;
                }

                // fall through.
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
                dte.StatusBar.Text = string.Format(ServicesVSResources.Package_uninstall_failed_colon_0, e.Message);

                var notificationService = _workspace.Services.GetService<INotificationService>();
                notificationService?.SendNotification(
                    string.Format(ServicesVSResources.Uninstalling_0_failed_Additional_information_colon_1, packageName, e.Message),
                    severity: NotificationSeverity.Error);

                // fall through.
            }

            return false;
        }

        private string GetInstalledVersion(string packageName, EnvDTE.Project dteProject)
        {
            this.AssertIsForeground();

            try
            {
                var installedPackages = _packageInstallerServices.Value.GetInstalledPackages(dteProject);
                var metadata = installedPackages.FirstOrDefault(m => m.Id == packageName);
                return metadata?.VersionString;
            }
            catch (ArgumentException e) when (IsKnownNugetIssue(e))
            {
                // Nuget may throw an ArgumentException when there is something about the project 
                // they do not like/support.
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
            }

            return null;
        }

        private bool IsKnownNugetIssue(ArgumentException exception)
        {
            // See https://github.com/NuGet/Home/issues/4706
            // Nuget throws on legal projects.  We do not want to report this exception
            // as it is known (and NFWs are expensive), but we do want to report if we 
            // run into anything else.
            return exception.Message.Contains("is not a valid version string");
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            ThisCanBeCalledOnAnyThread();

            var solutionChanged = false;
            ProjectId changedProjectId = null;
            switch (e.Kind)
            {
                default:
                    // Nothing to do for any other events.
                    return;

                case WorkspaceChangeKind.ProjectAdded:
                case WorkspaceChangeKind.ProjectChanged:
                case WorkspaceChangeKind.ProjectReloaded:
                case WorkspaceChangeKind.ProjectRemoved:
                    changedProjectId = e.ProjectId;
                    break;

                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionReloaded:
                case WorkspaceChangeKind.SolutionRemoved:
                    solutionChanged = true;
                    break;
            }

            this.OnWorkspaceChanged(solutionChanged, changedProjectId);
        }

        private void OnWorkspaceChanged(bool changedSolution, ProjectId changedProjectId)
            => _workQueue.AddWork((changedSolution, changedProjectId));

        private async Task ProcessBatchedChangesAsync(
            ImmutableArray<(bool changedSolution, ProjectId changedProjectId)> work,
            CancellationToken cancellationToken)
        {
            await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Keep track of how much time we're spending on the UI thread.  We want to relinquish it if we're taking
            // too long to process all the notifications.
            var startTimeOnUIThread = Environment.TickCount;

            this.AssertIsForeground();

            // If we've been asked to shutdown, then there's no point proceeding.
            if (cancellationToken.IsCancellationRequested)
                return;

            // If we've been disconnected, then there's no point proceeding.
            if (_workspace == null || !IsEnabled)
                return;

            using var _ = PooledHashSet<ProjectId>.GetInstance(out var projectsToProcess);

            var solution = _workspace.CurrentSolution;
            AddProjectsToProcess(solution, projectsToProcess, work);

            // Now, keep processing projects as long as we haven't exceeded too much time on the UI thread. We'll always
            // proces at least one item, so we're sure to make forward progress.
            using var enumerator = projectsToProcess.GetEnumerator();
            while (enumerator.MoveNext())
            {
                ProcessProjectChange(solution, projectId: enumerator.Current);

                // If too much time has passed, or if there's existing user input on the UI thread then stop what
                // we're doing and enqueue all these projects for the next round.
                var elapsedTime = Environment.TickCount - startTimeOnUIThread;
                if (elapsedTime > DefaultTimeSliceInMS || IsInputPending())
                    break;
            }

            // Any remaining items will be enqueued for the next time we process work.
            while (enumerator.MoveNext())
                OnWorkspaceChanged(changedSolution: false, changedProjectId: enumerator.Current);
        }

        private void AddProjectsToProcess(
            Solution solution,
            HashSet<ProjectId> projectsToProcess,
            ImmutableArray<(bool changedSolution, ProjectId changedProjectId)> work)
        {
            this.AssertIsForeground();

            if (work.Any(t => t.changedSolution))
            {
                // If we detected a solution change, then we need to process all projects.
                // This includes all the projects that we already know about, as well as
                // all the projects in the current workspace solution.
                projectsToProcess.AddRange(solution.ProjectIds);
                projectsToProcess.AddRange(_projectToInstalledPackageAndVersion.Keys);
                return;
            }

            // Otherwise, just collect the set of projects we've been notified about.
            foreach (var (_, projectId) in work)
                projectsToProcess.Add(projectId);
        }

        private void ProcessProjectChange(Solution solution, ProjectId projectId)
        {
            this.AssertIsForeground();

            // Remove anything we have associated with this project.
            _projectToInstalledPackageAndVersion.TryRemove(projectId, out var projectState);

            var project = solution.GetProject(projectId);
            if (project == null)
            {
                // Project was removed.  Nothing needs to be done.
                return;
            }

            // We really only need to know the NuGet status for managed language projects.
            // Also, the NuGet APIs may throw on some projects that don't implement the 
            // full set of DTE APIs they expect.  So we filter down to just C# and VB here
            // as we know these languages are safe to build up this index for.
            if (project.Language != LanguageNames.CSharp &&
                project.Language != LanguageNames.VisualBasic)
            {
                return;
            }

            // Project was changed in some way.  Let's go find the set of installed packages for it.
            var dteProject = _workspace.TryGetDTEProject(projectId);
            if (dteProject == null)
            {
                // Don't have a DTE project for this project ID.  not something we can query NuGet for.
                return;
            }

            var installedPackages = new MultiDictionary<string, string>();
            var isEnabled = false;

            // Calling into NuGet.  Assume they may fail for any reason.
            try
            {
                var installedPackageMetadata = _packageInstallerServices.Value.GetInstalledPackages(dteProject);
                foreach (var metadata in installedPackageMetadata)
                {
                    if (metadata.VersionString != null)
                    {
                        installedPackages.Add(metadata.Id, metadata.VersionString);
                    }
                }

                isEnabled = true;
            }
            catch (ArgumentException e) when (IsKnownNugetIssue(e))
            {
                // Nuget may throw an ArgumentException when there is something about the project 
                // they do not like/support.
            }
            catch (InvalidOperationException e) when (e.StackTrace.Contains("NuGet.PackageManagement.VisualStudio.NetCorePackageReferenceProject.GetPackageSpecsAsync"))
            {
                // NuGet throws an InvalidOperationException if details
                // for the project fail to load. We don't need to report
                // these, and can assume that this will work on a future
                // project change
                // This should be removed with https://github.com/dotnet/roslyn/issues/33187
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
            }

            var state = new ProjectState(isEnabled, installedPackages);
            _projectToInstalledPackageAndVersion[projectId] = state;
        }

        public bool IsInstalled(Workspace workspace, ProjectId projectId, string packageName)
        {
            ThisCanBeCalledOnAnyThread();
            return _projectToInstalledPackageAndVersion.TryGetValue(projectId, out var installedPackages) &&
                installedPackages.InstalledPackageToVersion.ContainsKey(packageName);
        }

        public ImmutableArray<string> GetInstalledVersions(string packageName)
        {
            ThisCanBeCalledOnAnyThread();

            var installedVersions = new HashSet<string>();
            foreach (var state in _projectToInstalledPackageAndVersion.Values)
            {
                installedVersions.AddRange(state.InstalledPackageToVersion[packageName]);
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

        private int CompareSplit(string[] split1, string[] split2)
        {
            ThisCanBeCalledOnAnyThread();

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

        public IEnumerable<Project> GetProjectsWithInstalledPackage(Solution solution, string packageName, string version)
        {
            ThisCanBeCalledOnAnyThread();

            var result = new List<Project>();

            foreach (var kvp in this._projectToInstalledPackageAndVersion)
            {
                var state = kvp.Value;
                var versionSet = state.InstalledPackageToVersion[packageName];
                if (versionSet.Contains(version))
                {
                    var project = solution.GetProject(kvp.Key);
                    if (project != null)
                    {
                        result.Add(project);
                    }
                }
            }

            return result;
        }

        public bool CanShowManagePackagesDialog()
            => TryGetOrLoadNuGetPackageManager(out _);

        private bool TryGetOrLoadNuGetPackageManager(out IVsPackage nugetPackageManager)
        {
            this.AssertIsForeground();

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
}
