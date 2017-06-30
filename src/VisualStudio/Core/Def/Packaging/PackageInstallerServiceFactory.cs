﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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

        // We refer to the package services through proxy types so that we can
        // delay loading their DLLs until we actually need them.
        private IPackageServicesProxy _packageServices;

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        // We keep track of what types of changes we've seen so we can then determine what to
        // refresh on the UI thread.  If we hear about project changes, we only refresh that
        // project.  If we hear about a solution level change, we'll refresh all projects.
        private bool _solutionChanged;
        private HashSet<ProjectId> _changedProjects = new HashSet<ProjectId>();

        private readonly ConcurrentDictionary<ProjectId, ProjectState> _projectToInstalledPackageAndVersion =
            new ConcurrentDictionary<ProjectId, ProjectState>();

        [ImportingConstructor]
        public PackageInstallerService(
            VisualStudioWorkspaceImpl workspace,
            SVsServiceProvider serviceProvider,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
            : base(workspace, SymbolSearchOptions.Enabled,
                              SymbolSearchOptions.SuggestForTypesInReferenceAssemblies,
                              SymbolSearchOptions.SuggestForTypesInNuGetPackages)
        {
            _workspace = workspace;
            _serviceProvider = serviceProvider;
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
        }

        public ImmutableArray<PackageSource> PackageSources { get; private set; } = ImmutableArray<PackageSource>.Empty;

        public event EventHandler PackageSourcesChanged;

        private bool IsEnabled => _packageServices != null;

        bool IPackageInstallerService.IsEnabled(ProjectId projectId)
        {
            if (_packageServices == null)
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
            // Our service has been enabled.  Now load the VS package dlls.
            var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));

            var packageInstallerServices = componentModel.GetExtensions<IVsPackageInstallerServices>().FirstOrDefault();
            var packageInstaller = componentModel.GetExtensions<IVsPackageInstaller2>().FirstOrDefault();
            var packageUninstaller = componentModel.GetExtensions<IVsPackageUninstaller>().FirstOrDefault();
            var packageSourceProvider = componentModel.GetExtensions<IVsPackageSourceProvider>().FirstOrDefault();

            if (packageInstallerServices == null ||
                packageInstaller == null ||
                packageUninstaller == null ||
                packageSourceProvider == null)
            {
                return;
            }

            _packageServices = new PackageServicesProxy(
                packageInstallerServices, packageInstaller, packageUninstaller, packageSourceProvider);

            // Start listening to additional events workspace changes.
            _workspace.WorkspaceChanged += OnWorkspaceChanged;
            _packageServices.SourcesChanged += OnSourceProviderSourcesChanged;
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
            if (!this.IsForeground())
            {
                this.InvokeBelowInputPriority(() => OnSourceProviderSourcesChanged(sender, e));
                return;
            }

            this.AssertIsForeground();

            PackageSources = _packageServices.GetSources(includeUnOfficial: true, includeDisabled: false)
                .Select(r => new PackageSource(r.Key, r.Value))
                .ToImmutableArrayOrEmpty();

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
            if (workspace == _workspace && _workspace != null && _packageServices != null)
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
                if (!_packageServices.IsPackageInstalled(dteProject, packageName))
                {
                    dte.StatusBar.Text = string.Format(ServicesVSResources.Installing_0, packageName);

                    if (versionOpt == null)
                    {
                        _packageServices.InstallLatestPackage(
                            source, dteProject, packageName, includePrerelease, ignoreDependencies: false);
                    }
                    else
                    {
                        _packageServices.InstallPackage(
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
                if (_packageServices.IsPackageInstalled(dteProject, packageName))
                {
                    dte.StatusBar.Text = string.Format(ServicesVSResources.Uninstalling_0, packageName);
                    var installedVersion = GetInstalledVersion(packageName, dteProject);
                    _packageServices.UninstallPackage(dteProject, packageName, removeDependencies: true);

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
                var installedPackages = _packageServices.GetInstalledPackages(dteProject);
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

            bool localSolutionChanged = false;
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
                    .ContinueWith(_ => ProcessBatchedChangesOnForeground(cancellationToken), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, ForegroundTaskScheduler);
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
            if (_workspace == null || _packageServices == null)
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
            Task.Factory.SafeStartNew(
                () => ProcessBatchedChangesOnForeground(cancellationToken), cancellationToken, ForegroundTaskScheduler);
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
                var installedPackageMetadata = _packageServices.GetInstalledPackages(dteProject);
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
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
            }

            var state = new ProjectState(isEnabled, installedPackages);
            _projectToInstalledPackageAndVersion.AddOrUpdate(
                projectId, state, (_1, _2) => state);
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

        public void ShowManagePackagesDialog(string packageName)
        {
            this.AssertIsForeground();

            var shell = (IVsShell)_serviceProvider.GetService(typeof(SVsShell));
            if (shell == null)
            {
                return;
            }

            var nugetGuid = new Guid("5fcc8577-4feb-4d04-ad72-d6c629b083cc");
            shell.LoadPackage(ref nugetGuid, out var nugetPackage);
            if (nugetPackage == null)
            {
                return;
            }

            // We're able to launch the package manager (with an item in its search box) by
            // using the IVsSearchProvider API that the NuGet package exposes.
            //
            // We get that interface for it and then pass it a SearchQuery that effectively
            // wraps the package name we're looking for.  The NuGet package will then read
            // out that string and populate their search box with it.
            var extensionProvider = (IVsPackageExtensionProvider)nugetPackage;
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

        private class PackageServicesProxy : IPackageServicesProxy
        {
            private readonly IVsPackageInstaller2 _packageInstaller;
            private readonly IVsPackageInstallerServices _packageInstallerServices;
            private readonly IVsPackageSourceProvider _packageSourceProvider;
            private readonly IVsPackageUninstaller _packageUninstaller;

            public PackageServicesProxy(
                IVsPackageInstallerServices packageInstallerServices,
                IVsPackageInstaller2 packageInstaller,
                IVsPackageUninstaller packageUninstaller,
                IVsPackageSourceProvider packageSourceProvider)
            {
                _packageInstallerServices = packageInstallerServices;
                _packageInstaller = packageInstaller;
                _packageUninstaller = packageUninstaller;
                _packageSourceProvider = packageSourceProvider;
            }

            public event EventHandler SourcesChanged
            {
                add
                {
                    _packageSourceProvider.SourcesChanged += value;
                }

                remove
                {
                    _packageSourceProvider.SourcesChanged -= value;
                }
            }

            public IEnumerable<PackageMetadata> GetInstalledPackages(EnvDTE.Project project)
            {
                return _packageInstallerServices.GetInstalledPackages(project)
                                  .Select(m => new PackageMetadata(m.Id, m.VersionString))
                                  .ToList();
            }

            public bool IsPackageInstalled(EnvDTE.Project project, string id)
                => _packageInstallerServices.IsPackageInstalled(project, id);

            public void InstallPackage(string source, EnvDTE.Project project, string packageId, string version, bool ignoreDependencies)
                => _packageInstaller.InstallPackage(source, project, packageId, version, ignoreDependencies);

            public void InstallLatestPackage(string source, EnvDTE.Project project, string packageId, bool includePrerelease, bool ignoreDependencies)
                => _packageInstaller.InstallLatestPackage(source, project, packageId, includePrerelease, ignoreDependencies);

            public IEnumerable<KeyValuePair<string, string>> GetSources(bool includeUnOfficial, bool includeDisabled)
                => _packageSourceProvider.GetSources(includeUnOfficial, includeDisabled);

            public void UninstallPackage(EnvDTE.Project project, string packageId, bool removeDependencies)
                => _packageUninstaller.UninstallPackage(project, packageId, removeDependencies);
        }
    }
}
