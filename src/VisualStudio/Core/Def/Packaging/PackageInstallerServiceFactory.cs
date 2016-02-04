using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    [ExportWorkspaceServiceFactory(typeof(IPackageInstallerService)), Shared]
    internal class PackageInstallerServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new PackageInstallerService(workspaceServices);
        }
    }

    internal class PackageInstallerService : ForegroundThreadAffinitizedObject, IPackageInstallerService
    {
        private readonly object gate = new object();
        private readonly HostWorkspaceServices workspaceServices;

        private VisualStudioWorkspaceImpl _workspace;
        private IVsPackageInstallerServices _packageInstallerServices;
        private IVsPackageInstaller _packageInstaller;

        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private bool solutionChanged;
        private HashSet<ProjectId> changedProjects = new HashSet<ProjectId>();

        private readonly ConcurrentDictionary<ProjectId, Dictionary<string, string>> projectToInstalledPackageAndVersion =
            new ConcurrentDictionary<ProjectId, Dictionary<string, string>>();

        public PackageInstallerService(HostWorkspaceServices workspaceServices)
        {
            this.workspaceServices = workspaceServices;
        }

        public bool TryInstallPackage(Workspace workspace, ProjectId projectId, string packageName, string versionOpt)
        {
            this.AssertIsForeground();

            if (workspace == _workspace && _workspace != null && _packageInstallerServices != null)
            {
                var dte = _workspace.GetVsService<SDTE, EnvDTE.DTE>();
                var dteProject = _workspace.TryGetDTEProject(projectId);
                if (dteProject != null)
                {
                    try
                    {
                        if (!_packageInstallerServices.IsPackageInstalled(dteProject, packageName))
                        {
                            dte.StatusBar.Text = string.Format(ServicesVSResources.Installing_package_0, packageName);
                            _packageInstaller.InstallPackage(source: null, project: dteProject, packageId: packageName, version: versionOpt, ignoreDependencies: false);
                            dte.StatusBar.Text = string.Format(ServicesVSResources.Installing_package_0_completed, packageName);
                        }

                        return true;
                    }
                    catch
                    {
                        dte.StatusBar.Text = string.Format(ServicesVSResources.Installing_package_0_failed, packageName);
                    }
                }
            }

            return false;
        }

        internal void Connect(VisualStudioWorkspaceImpl workspace)
        {
            this.AssertIsForeground();

            _workspace = workspace;

            var componentModel = workspace.GetVsService<SComponentModel, IComponentModel>();
            _packageInstallerServices = componentModel.GetService<IVsPackageInstallerServices>();
            _packageInstaller = componentModel.GetService<IVsPackageInstaller>();

            _workspace.WorkspaceChanged += OnWorkspaceChanged;
        }

        internal void Disconnect(VisualStudioWorkspaceImpl workspace)
        {
            this.AssertIsForeground();

            Debug.Assert(workspace == _workspace);
            _workspace.WorkspaceChanged -= OnWorkspaceChanged;
            _workspace = null;
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            // Can be called on any thread.
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

            lock (gate)
            {
                // Cancel any existing update task.
                tokenSource.Cancel();
                tokenSource = new CancellationTokenSource();

                this.solutionChanged |= localSolutionChanged;
                if (localChangedProject != null)
                {
                    this.changedProjects.Add(localChangedProject);
                }

                // Because we may get a lot of events, we attempt to throttle things so that
                // we only update our nuget information once a second has passed.
                var cancellationToken = tokenSource.Token;
                Task.Delay(TimeSpan.FromSeconds(1), cancellationToken)
                    .ContinueWith(OnWorkspaceChangedOnForeground, cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, this.ForegroundTaskScheduler);
            }
        }

        private void OnWorkspaceChangedOnForeground(Task task)
        {
            this.AssertIsForeground();
            bool localSolutionChanged;
            HashSet<ProjectId> localChangedProjects;
            lock (gate)
            {
                localSolutionChanged = solutionChanged;
                localChangedProjects = changedProjects;

                solutionChanged = false;
                changedProjects = new HashSet<ProjectId>();
            }

            if (_workspace == null || _packageInstallerServices == null)
            {
                return;
            }

            if (localSolutionChanged)
            {
                ProcessSolutionChange();
            }
            else
            {
                var solution = _workspace.CurrentSolution;
                foreach (var projectId in localChangedProjects)
                {
                    ProcessProjectChange(solution, projectId);
                }
            }
        }

        private void ProcessSolutionChange()
        {
            this.AssertIsForeground();

            // Just clear out everything we have and start over in the case of a solution change.
            projectToInstalledPackageAndVersion.Clear();

            var solution = _workspace.CurrentSolution;
            foreach (var projectId in _workspace.CurrentSolution.ProjectIds)
            {
                ProcessProjectChange(solution, projectId);
            }
        }

        private void ProcessProjectChange(Solution solution, ProjectId projectId)
        {
            this.AssertIsForeground();

            // Remove anything we have associated with this project.
            Dictionary<string, string> installedPackages;
            projectToInstalledPackageAndVersion.TryRemove(projectId, out installedPackages);

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
                installedPackages.AddRange(installedPackageMetadata.Select(m => new KeyValuePair<string,string>(m.Id, m.VersionString)));
            }
            catch
            {
                // TODO(cyrusn): Telemetry on this.
            }

            projectToInstalledPackageAndVersion.AddOrUpdate(projectId, installedPackages, (_1, _2) => installedPackages);
        }

        public bool IsInstalled(Workspace workspace, ProjectId projectId, string packageName)
        {
            // Can be called on any thread.

            Dictionary<string, string> installedPackages;
            return projectToInstalledPackageAndVersion.TryGetValue(projectId, out installedPackages) &&
                installedPackages.ContainsKey(packageName);
        }

        public IEnumerable<string> GetInstalledVersions(string packageName)
        {
            var result = new HashSet<string>();
            foreach (var installedPackages in projectToInstalledPackageAndVersion.Values)
            {
                string version = null;
                if (installedPackages?.TryGetValue(packageName, out version) == true && version != null)
                {
                    result.Add(version);
                }
            }

            return result;
        }
    }
}
