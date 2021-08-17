// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Implementation;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.OLE.Interop;
using VSUtilities = Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    internal partial class PackageInstallerService
    {
        private bool TryInstallAndAddUndoAction(
            string source, string packageName, string versionOpt, bool includePrerelease,
            Guid projectGuid, EnvDTE.DTE dte, EnvDTE.Project dteProject, IOleUndoManager undoManager,
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            var installed = TryInstallPackage(
                source, packageName, versionOpt, includePrerelease, projectGuid, dte, dteProject, progressTracker, cancellationToken);
            if (installed)
            {
                // if the install succeeded, then add an uninstall item to the undo manager.
                undoManager?.Add(new UninstallPackageUndoUnit(
                    this, source, packageName,
                    versionOpt, includePrerelease,
                    projectGuid, dte, dteProject, undoManager));
            }

            return installed;
        }

        private bool TryUninstallAndAddRedoAction(
            string source, string packageName, string versionOpt, bool includePrerelease,
            Guid projectGuid, EnvDTE.DTE dte, EnvDTE.Project dteProject, IOleUndoManager undoManager,
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            var uninstalled = TryUninstallPackage(packageName, projectGuid, dte, dteProject, progressTracker, cancellationToken);
            if (uninstalled)
            {
                // if the install succeeded, then add an uninstall item to the undo manager.
                undoManager?.Add(new InstallPackageUndoUnit(
                    this, source, packageName,
                    versionOpt, includePrerelease,
                    projectGuid, dte, dteProject, undoManager));
            }

            return uninstalled;
        }

        private abstract class BaseUndoUnit : IOleUndoUnit
        {
            protected readonly Guid projectGuid;
            protected readonly EnvDTE.DTE dte;
            protected readonly EnvDTE.Project dteProject;
            protected readonly PackageInstallerService packageInstallerService;
            protected readonly string source;
            protected readonly string packageName;
            protected readonly IOleUndoManager undoManager;
            protected readonly string versionOpt;
            protected readonly bool includePrerelease;

            protected BaseUndoUnit(
                PackageInstallerService packageInstallerService,
                string source,
                string packageName,
                string versionOpt,
                bool includePrerelease,
                Guid projectGuid,
                EnvDTE.DTE dte,
                EnvDTE.Project dteProject,
                IOleUndoManager undoManager)
            {
                this.packageInstallerService = packageInstallerService;
                this.source = source;
                this.packageName = packageName;
                this.versionOpt = versionOpt;
                this.includePrerelease = includePrerelease;
                this.projectGuid = projectGuid;
                this.dte = dte;
                this.dteProject = dteProject;
                this.undoManager = undoManager;
            }

            public abstract void Do(IOleUndoManager pUndoManager);
            public abstract void GetDescription(out string pBstr);

            public void GetUnitType(out Guid pClsid, out int plID)
                => throw new NotImplementedException();

            public void OnNextAdd()
            {

            }
        }

        private class UninstallPackageUndoUnit : BaseUndoUnit
        {
            public UninstallPackageUndoUnit(
                PackageInstallerService packageInstallerService,
                string source,
                string packageName,
                string versionOpt,
                bool includePrerelease,
                Guid projectGuid,
                EnvDTE.DTE dte,
                EnvDTE.Project dteProject,
                IOleUndoManager undoManager)
                : base(packageInstallerService, source, packageName,
                       versionOpt, includePrerelease,
                       projectGuid, dte, dteProject, undoManager)
            {
            }

            public override void Do(IOleUndoManager pUndoManager)
            {
                var description = string.Format(ServicesVSResources.Uninstalling_0, packageName);
                using var context = this.packageInstallerService._operationExecutor.BeginExecute(NugetTitle, description, allowCancellation: true, showProgress: false);
                using var scope = context.AddScope(allowCancellation: true, description);

                packageInstallerService.TryUninstallAndAddRedoAction(
                    source, packageName, versionOpt, includePrerelease,
                    projectGuid, dte, dteProject, undoManager,
                    new UIThreadOperationContextProgressTracker(scope),
                    context.UserCancellationToken);
            }

            public override void GetDescription(out string pBstr)
                => pBstr = string.Format(ServicesVSResources.Uninstall_0, packageName);
        }

        private class InstallPackageUndoUnit : BaseUndoUnit
        {
            public InstallPackageUndoUnit(
                PackageInstallerService packageInstallerService,
                string source,
                string packageName,
                string versionOpt,
                bool includePrerelease,
                Guid projectGuid,
                EnvDTE.DTE dte,
                EnvDTE.Project dteProject,
                IOleUndoManager undoManager)
                : base(packageInstallerService, source, packageName,
                       versionOpt, includePrerelease,
                       projectGuid, dte, dteProject, undoManager)
            {
            }

            public override void GetDescription(out string pBstr)
                => pBstr = string.Format(ServicesVSResources.Install_0, packageName);

            public override void Do(IOleUndoManager pUndoManager)
            {
                var description = string.Format(ServicesVSResources.Installing_0, packageName);
                using var context = this.packageInstallerService._operationExecutor.BeginExecute(NugetTitle, description, allowCancellation: true, showProgress: false);
                using var scope = context.AddScope(allowCancellation: true, description);

                packageInstallerService.TryInstallAndAddUndoAction(
                    source, packageName, versionOpt, includePrerelease, projectGuid, dte, dteProject, undoManager,
                    new UIThreadOperationContextProgressTracker(scope),
                    context.UserCancellationToken);
            }
        }
    }
}
