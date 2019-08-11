// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.ComponentModelHost;
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
        private readonly object _gate = new object();
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly SVsServiceProvider _serviceProvider;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;

        private readonly Lazy<IVsPackageInstallerServices> _packageInstallerServices;
        private readonly Lazy<IVsPackageInstaller2> _packageInstaller;
        private readonly Lazy<IVsPackageUninstaller> _packageUninstaller;
        private readonly Lazy<IVsPackageSourceProvider> _packageSourceProvider;

        private ImmutableArray<PackageSource> _packageSources;
        private IVsPackage _nugetPackageManager;

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        // We keep track of what types of changes we've seen so we can then determine what to
        // refresh on the UI thread.  If we hear about project changes, we only refresh that
        // project.  If we hear about a solution level change, we'll refresh all projects.
        private bool _solutionChanged;
        private readonly HashSet<ProjectId> _changedProjects = new HashSet<ProjectId>();

        private readonly ConcurrentDictionary<ProjectId, ProjectState> _projectToInstalledPackageAndVersion =
            new ConcurrentDictionary<ProjectId, ProjectState>();

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
            : base(threadingContext, workspace, SymbolSearchOptions.Enabled,
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
        }

        public ImmutableArray<PackageSource> GetPackageSources()
        {
            // Only read from _packageSources once, since OnSourceProviderSourcesChanged could reset it to default at
            // any time while this method is running.
            var packageSources = _packageSources;
            if (packageSources != null)
            {
                return packageSources;
            }

            try
            {
                packageSources = _packageSourceProvider.Value.GetSources(includeUnOfficial: true, includeDisabled: false)
                    .SelectAsArray(r => new PackageSource(r.Key, r.Value));
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is InvalidOperationException)
            {
                // These exceptions can happen when the nuget.config file is broken.
                packageSources = ImmutableArray<PackageSource>.Empty;
            }

            var previousPackageSources = ImmutableInterlocked.InterlockedCompareExchange(ref _packageSources, packageSources, default);
            if (previousPackageSources != null)
            {
                // Another thread already initialized _packageSources
                packageSources = previousPackageSources;
            }

            return packageSources;
        }

        public event EventHandler PackageSourcesChanged;

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
            OnWorkspaceChanged(null, new WorkspaceChangeEventArgs(
                WorkspaceChangeKind.SolutionAdded, null, null));
        }

        private void OnSourceProviderSourcesChanged(object sender, EventArgs e)
        {
            _packageSources = default;
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
                    var description = string.Format(ServicesVSResources.Install_0, packageName);

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
        {
            return installedVersion == null ? packageName : $"{packageName} - {installedVersion}";
        }

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

            var localSolutionChanged = false;
            ProjectId localChangedProject = null;
            switch (e.Kind)
            {
                default:
                    // Nothing to do for any other events.
                    return;

                case WorkspaceChangeKind.ProjectAdded:
                case WorkspaceChangeKind.ProjectChanged:
                case WorkspaceChangeKind.ProjectReloaded:
                case WorkspaceChangeKind.ProjectRemoved:
                    localChangedProject = e.ProjectId;
                    break;

                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionReloaded:
                case WorkspaceChangeKind.SolutionRemoved:
                    localSolutionChanged = true;
                    break;
            }

            lock (_gate)
            {
                // Augment the data that the foreground thread will process.
                _solutionChanged |= localSolutionChanged;
                if (localChangedProject != null)
                {
                    _changedProjects.Add(localChangedProject);
                }

                // Now cancel any inflight work that is processing the data.
                _tokenSource.Cancel();
                _tokenSource = new CancellationTokenSource();

                // And enqueue a new job to process things.  Wait one second before starting.
                // That way if we get a flurry of events we'll end up processing them after
                // they've all come in.
                var cancellationToken = _tokenSource.Token;
                Task.Delay(TimeSpan.FromSeconds(1), cancellationToken)
                    .ContinueWith(
                        async _ =>
                        {
                            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
                            cancellationToken.ThrowIfCancellationRequested();

                            ProcessBatchedChangesOnForeground(cancellationToken);
                        },
                        cancellationToken,
                        TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default).Unwrap();
            }
        }

        private void ProcessBatchedChangesOnForeground(CancellationToken cancellationToken)
        {
            this.AssertIsForeground();

            // If we've been asked to stop, then there's no point proceeding.
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // If we've been disconnected, then there's no point proceeding.
            if (_workspace == null || !IsEnabled)
            {
                return;
            }

            // Get a project to process.
            var solution = _workspace.CurrentSolution;
            var projectId = DequeueNextProject(solution);
            if (projectId == null)
            {
                // No project to process, nothing to do.
                return;
            }

            // Process this single project.
            ProcessProjectChange(solution, projectId);

            // After processing this single project, yield so the foreground thread
            // can do more work.  Then go and loop again so we can process the 
            // rest of the projects.
            Task.Factory.SafeStartNewFromAsync(
                async () =>
                {
                    await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    ProcessBatchedChangesOnForeground(cancellationToken);
                },
                cancellationToken,
                TaskScheduler.Default);
        }

        private ProjectId DequeueNextProject(Solution solution)
        {
            this.AssertIsForeground();

            lock (_gate)
            {
                // If we detected a solution change, then we need to process all projects.
                // This includes all the projects that we already know about, as well as
                // all the projects in the current workspace solution.
                if (_solutionChanged)
                {
                    _changedProjects.AddRange(solution.ProjectIds);
                    _changedProjects.AddRange(_projectToInstalledPackageAndVersion.Keys);
                }

                _solutionChanged = false;

                // Remove and return the first project in the list.
                var projectId = _changedProjects.FirstOrDefault();
                _changedProjects.Remove(projectId);
                return projectId;
            }
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
        {
            pSearchItemResult.InvokeAction();
        }

        public void ReportResults(IVsSearchTask pTask, uint dwResults, IVsSearchItemResult[] pSearchItemResults)
        {
        }

        private class SearchQuery : IVsSearchQuery
        {
            public SearchQuery(string packageName)
            {
                this.SearchString = packageName;
            }

            public string SearchString { get; }

            public uint ParseError => 0;

            public uint GetTokens(uint dwMaxTokens, IVsSearchToken[] rgpSearchTokens)
            {
                return 0;
            }
        }
    }
}
