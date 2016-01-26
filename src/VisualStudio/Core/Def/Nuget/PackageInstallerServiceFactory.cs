using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
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

namespace Microsoft.VisualStudio.LanguageServices.Nuget
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

        private Task updateTask;

        private readonly ConcurrentDictionary<ProjectId, HashSet<string>> projectToInstalledPackages = new ConcurrentDictionary<ProjectId, HashSet<string>>();

        public PackageInstallerService(HostWorkspaceServices workspaceServices)
        {
            this.workspaceServices = workspaceServices;
        }

        public bool TryInstallPackage(Workspace workspace, ProjectId projectId, string packageName)
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
                            _packageInstaller.InstallPackage(source: null, project: dteProject, packageId: packageName, version: (Version)null, ignoreDependencies: false);
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

            switch (e.Kind)
            {
                case WorkspaceChangeKind.ProjectAdded:
                case WorkspaceChangeKind.ProjectChanged:
                case WorkspaceChangeKind.ProjectReloaded:
                case WorkspaceChangeKind.ProjectRemoved:
                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionReloaded:
                case WorkspaceChangeKind.SolutionRemoved:
                    lock (gate)
                    {
                        if (updateTask != null)
                        {
                            // There is already a pending update task.  Nothing to do.
                            return;
                        }

                        // Because we may get a lot of events, we attempt to throttle things so that
                        // we only update our nuget information once a second has passed.
                        updateTask = Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(_ => OnWorkspaceChangedOnForeground(e), this.ForegroundTaskScheduler);
                    }
                    break;
            }
        }

        private void OnWorkspaceChangedOnForeground(WorkspaceChangeEventArgs e)
        {
            this.AssertIsForeground();
            lock (gate)
            {
                // clear out the existing 
                updateTask = null;
            }

            if (_workspace == null || _packageInstallerServices == null)
            {
                return;
            }

            switch (e.Kind)
            {
                case WorkspaceChangeKind.ProjectAdded:
                case WorkspaceChangeKind.ProjectChanged:
                case WorkspaceChangeKind.ProjectReloaded:
                case WorkspaceChangeKind.ProjectRemoved:
                    ProcessProjectChange(_workspace.CurrentSolution, e.ProjectId);
                    break;
                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionReloaded:
                case WorkspaceChangeKind.SolutionRemoved:
                    ProcessSolutionChange();
                    break;
            }
        }

        private void ProcessSolutionChange()
        {
            this.AssertIsForeground();

            // Just clear out everything we have and start over in the case of a solution change.
            projectToInstalledPackages.Clear();

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
            HashSet<string> installedPackages;
            projectToInstalledPackages.TryRemove(projectId, out installedPackages);

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

            installedPackages = new HashSet<string>();

            // Calling into nuget.  Assume they may fail for any reason.
            try
            {
                var installedPackageMetadata = _packageInstallerServices.GetInstalledPackages(dteProject);
                installedPackages.AddRange(installedPackageMetadata.Select(m => m.Id));
            }
            catch
            {
                // TODO(cyrusn): Telemetry on this.
            }

            projectToInstalledPackages.AddOrUpdate(projectId, installedPackages, (_1, _2) => installedPackages);
        }

        public bool IsInstalled(Workspace workspace, ProjectId projectId, string packageName)
        {
            // Can be called on any thread.

            HashSet<string> installedPackages;
            return projectToInstalledPackages.TryGetValue(projectId, out installedPackages) &&
                installedPackages.Contains(packageName);
        }
    }
}
