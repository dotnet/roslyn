// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;
using Roslyn.Utilities;
using VSShell = Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    /// <summary>
    /// Free threaded wrapper around the NuGet.VisualStudio STA package installer interfaces.
    /// We want to be able to make queries about packages from any thread.  For example, the
    /// add-nuget-reference feature wants to know what packages a project already has 
    /// references to.  NuGet.VisualStudio provides this information, but only in a COM STA 
    /// manner.  As we don't want our background work to bounce and block on the UI thread 
    /// we have this helper class which queries the information on the UI thread and caches
    /// the data so it can be read from the background.
    /// </summary>
    [ExportWorkspaceService(typeof(IPackageInstallerService)), Shared]
    internal partial class PackageInstallerService : ForegroundThreadAffinitizedObject, IPackageInstallerService, IVsSearchProviderCallback
    {
        private readonly object _gate = new object();
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;

        /// <summary>
        /// The workspace we're connected to.  When we're disconnected this will become 'null'.
        /// That's our signal to stop working.
        /// </summary>
        private VisualStudioWorkspaceImpl _workspace;
        private IVsPackageInstallerServices _packageInstallerServices;
        private IVsPackageInstaller _packageInstaller;
        private IVsPackageUninstaller _packageUninstaller;
        private IVsPackageSourceProvider _packageSourceProvider;

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        // We keep track of what types of changes we've seen so we can then determine what to
        // refresh on the UI thread.  If we hear about project changes, we only refresh that
        // project.  If we hear about a solution level change, we'll refresh all projects.
        private bool _solutionChanged;
        private HashSet<ProjectId> _changedProjects = new HashSet<ProjectId>();

        private readonly ConcurrentDictionary<ProjectId, Dictionary<string, string>> _projectToInstalledPackageAndVersion =
            new ConcurrentDictionary<ProjectId, Dictionary<string, string>>();

        [ImportingConstructor]
        public PackageInstallerService(
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
        {
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
        }

        public ImmutableArray<string> PackageSources { get; private set; } = ImmutableArray<string>.Empty;

        internal void Connect(VisualStudioWorkspaceImpl workspace)
        {
            this.AssertIsForeground();

            var componentModel = workspace.GetVsService<SComponentModel, IComponentModel>();
            _packageInstallerServices = componentModel.GetExtensions<IVsPackageInstallerServices>().FirstOrDefault();
            _packageInstaller = componentModel.GetExtensions<IVsPackageInstaller>().FirstOrDefault();
            _packageUninstaller = componentModel.GetExtensions<IVsPackageUninstaller>().FirstOrDefault();
            _packageSourceProvider = componentModel.GetExtensions<IVsPackageSourceProvider>().FirstOrDefault();

            if (!this.IsEnabled)
            {
                return;
            }

            // Start listening to workspace changes.
            _workspace = workspace;
            _workspace.WorkspaceChanged += OnWorkspaceChanged;
            _packageSourceProvider.SourcesChanged += OnSourceProviderSourcesChanged;

            OnSourceProviderSourcesChanged(null, EventArgs.Empty);
        }

        public bool IsEnabled => 
            _packageInstallerServices != null &&
            _packageInstallerServices != null &&
            _packageUninstaller != null &&
            _packageSourceProvider != null;

        internal void Disconnect(VisualStudioWorkspaceImpl workspace)
        {
            this.AssertIsForeground();

            Debug.Assert(workspace == _workspace);

            if (!this.IsEnabled)
            {
                return;
            }

            _packageSourceProvider.SourcesChanged -= OnSourceProviderSourcesChanged;
            _workspace.WorkspaceChanged -= OnWorkspaceChanged;

            _workspace = null;
        }

        private void OnSourceProviderSourcesChanged(object sender, EventArgs e)
        {
            if (!this.IsForeground())
            {
                this.InvokeBelowInputPriority(() => OnSourceProviderSourcesChanged(sender, e));
                return;
            }

            this.AssertIsForeground();

            PackageSources = _packageSourceProvider.GetSources(includeUnOfficial: true, includeDisabled: false)
                .Select(r => r.Key)
                .ToImmutableArrayOrEmpty();
        }

        public bool TryInstallPackage(
            Workspace workspace, DocumentId documentId, string packageName, string versionOpt, CancellationToken cancellationToken)
        {
            this.AssertIsForeground();

            // The 'workspace == _workspace' line is probably not necessary. However, we include 
            // it just to make sure that someone isn't trying to install a package into a workspace
            // other than the VisualStudioWorkspace.
            if (workspace == _workspace && _workspace != null && _packageInstallerServices != null)
            {
                var projectId = documentId.ProjectId;
                var dte = _workspace.GetVsService<SDTE, EnvDTE.DTE>();
                var dteProject = _workspace.TryGetDTEProject(projectId);
                if (dteProject != null)
                {
                    var description = string.Format(ServicesVSResources.Install_0, packageName);

                    var document = workspace.CurrentSolution.GetDocument(documentId);
                    var text = document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken);
                    var textSnapshot = text.FindCorrespondingEditorTextSnapshot();
                    var textBuffer = textSnapshot?.TextBuffer;
                    var undoManager = GetUndoManager(textBuffer);

                    return TryInstallAndAddUndoAction(packageName, versionOpt, dte, dteProject, undoManager);
                }
            }

            return false;
        }

        private bool TryInstallPackage(
            string packageName, string versionOpt, EnvDTE.DTE dte, EnvDTE.Project dteProject)
        {
            try
            {
                if (!_packageInstallerServices.IsPackageInstalled(dteProject, packageName))
                {
                    dte.StatusBar.Text = string.Format(ServicesVSResources.Installing_0, packageName);
                    _packageInstaller.InstallPackage(source: null, project: dteProject, packageId: packageName, version: versionOpt, ignoreDependencies: false);

                    var installedVersion = GetInstalledVersion(packageName, dteProject);
                    dte.StatusBar.Text = string.Format(ServicesVSResources.Installing_0_completed,
                        GetStatusBarText(packageName, installedVersion));

                    return true;
                }

                // fall through.
            }
            catch (Exception e)
            {
                dte.StatusBar.Text = string.Format(ServicesVSResources.Package_install_failed_0, e.Message);
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
                if (_packageInstallerServices.IsPackageInstalled(dteProject, packageName))
                {
                    dte.StatusBar.Text = string.Format(ServicesVSResources.Uninstalling_0, packageName);
                    var installedVersion = GetInstalledVersion(packageName, dteProject);
                    _packageUninstaller.UninstallPackage(dteProject, packageName, removeDependencies: true);

                    dte.StatusBar.Text = string.Format(ServicesVSResources.Uninstalling_0_completed, 
                        GetStatusBarText(packageName, installedVersion));

                    return true;
                }

                // fall through.
            }
            catch (Exception e)
            {
                dte.StatusBar.Text = string.Format(ServicesVSResources.Package_uninstall_failed_0, e.Message);
                // fall through.
            }

            return false;
        }

        private string GetInstalledVersion(string packageName, EnvDTE.Project dteProject)
        {
            this.AssertIsForeground();

            try
            {
                var installedPackages = _packageInstallerServices.GetInstalledPackages(dteProject);
                var metadata = installedPackages.FirstOrDefault(m => m.Id == packageName);
                return metadata?.VersionString;
            }
            catch
            {
                return null;
            }
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
                    .ContinueWith(_ => ProcessBatchedChangesOnForeground(cancellationToken), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, this.ForegroundTaskScheduler);
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
            if (_workspace == null || _packageInstallerServices == null)
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
            Dictionary<string, string> installedPackages;
            _projectToInstalledPackageAndVersion.TryRemove(projectId, out installedPackages);

            if (!solution.ContainsProject(projectId))
            {
                // Project was removed.  Nothing needs to be done.
                return;
            }

            // Project was changed in some way.  Let's go find the set of installed packages for it.
            var dteProject = _workspace.TryGetDTEProject(projectId);
            if (dteProject == null)
            {
                // Don't have a DTE project for this project ID.  not something we can query nuget for.
                return;
            }

            installedPackages = new Dictionary<string, string>();

            // Calling into nuget.  Assume they may fail for any reason.
            try
            {
                var installedPackageMetadata = _packageInstallerServices.GetInstalledPackages(dteProject);
                installedPackages.AddRange(installedPackageMetadata.Select(m => new KeyValuePair<string, string>(m.Id, m.VersionString)));
            }
            catch
            {
                // TODO(cyrusn): Telemetry on this.
            }

            _projectToInstalledPackageAndVersion.AddOrUpdate(projectId, installedPackages, (_1, _2) => installedPackages);
        }

        public bool IsInstalled(Workspace workspace, ProjectId projectId, string packageName)
        {
            ThisCanBeCalledOnAnyThread();

            Dictionary<string, string> installedPackages;
            return _projectToInstalledPackageAndVersion.TryGetValue(projectId, out installedPackages) &&
                installedPackages.ContainsKey(packageName);
        }

        public IEnumerable<string> GetInstalledVersions(string packageName)
        {
            ThisCanBeCalledOnAnyThread();

            var installedVersions = new HashSet<string>();
            foreach (var installedPackages in _projectToInstalledPackageAndVersion.Values)
            {
                string version = null;
                if (installedPackages?.TryGetValue(packageName, out version) == true && version != null)
                {
                    installedVersions.Add(version);
                }
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

            return versionsAndSplits.Select(v => v.Version).ToList();
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
                var installedPackageAndVersion = kvp.Value;
                if (installedPackageAndVersion != null)
                {
                    string installedVersion;
                    if (installedPackageAndVersion.TryGetValue(packageName, out installedVersion) && installedVersion == version)
                    {
                        var project = solution.GetProject(kvp.Key);
                        if (project != null)
                        {
                            result.Add(project);
                        }
                    }
                }
            }

            return result;
        }

        public void ShowManagePackagesDialog(string packageName)
        {
            this.AssertIsForeground();

            var shell = _workspace.GetVsService<SVsShell, IVsShell>();
            if (shell == null)
            {
                return;
            }

            IVsPackage nugetPackage;
            var nugetGuid = new Guid("5fcc8577-4feb-4d04-ad72-d6c629b083cc");
            shell.LoadPackage(ref nugetGuid, out nugetPackage);
            if (nugetPackage == null)
            {
                return;
            }

            // We're able to launch the package manager (with an item in its search box) by
            // using the IVsSearchProvider API that the nuget package exposes.
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
    }
}