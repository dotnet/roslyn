// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    internal partial class PackageInstallerService
    {
        private bool TryInstallAndAddUndoAction(
            PackageInfo packageInfo, string versionOpt, EnvDTE.DTE dte, EnvDTE.Project dteProject, IOleUndoManager undoManager)
        {
            var installed = TryInstallPackage(packageInfo, versionOpt, dte, dteProject);
            if (installed)
            {
                // if the install succeeded, then add an uninstall item to the undo manager.
                undoManager?.Add(new UninstallPackageUndoUnit(this, packageInfo, versionOpt, dte, dteProject, undoManager));
            }
            return installed;
        }

        private bool TryUninstallAndAddRedoAction(PackageInfo packageInfo, string versionOpt, EnvDTE.DTE dte, EnvDTE.Project dteProject, IOleUndoManager undoManager)
        {
            var uninstalled = TryUninstallPackage(packageInfo, dte, dteProject);
            if (uninstalled)
            {
                // if the install succeeded, then add an uninstall item to the undo manager.
                undoManager?.Add(new InstallPackageUndoUnit(this, packageInfo, versionOpt, dte, dteProject, undoManager));
            }
            return uninstalled;
        }

        private abstract class BaseUndoUnit : IOleUndoUnit
        {
            protected readonly EnvDTE.DTE dte;
            protected readonly EnvDTE.Project dteProject;
            protected readonly PackageInstallerService packageInstallerService;
            protected readonly PackageInfo packageInfo;
            protected readonly IOleUndoManager undoManager;
            protected readonly string versionOpt;

            protected BaseUndoUnit(
                PackageInstallerService packageInstallerService,
                PackageInfo packageInfo,
                string versionOpt,
                EnvDTE.DTE dte,
                EnvDTE.Project dteProject,
                IOleUndoManager undoManager)
            {
                this.packageInstallerService = packageInstallerService;
                this.packageInfo = packageInfo;
                this.versionOpt = versionOpt;
                this.dte = dte;
                this.dteProject = dteProject;
                this.undoManager = undoManager;
            }

            public abstract void Do(IOleUndoManager pUndoManager);
            public abstract void GetDescription(out string pBstr);

            public void GetUnitType(out Guid pClsid, out int plID)
            {
                throw new NotImplementedException();
            }

            public void OnNextAdd()
            {

            }
        }

        private class UninstallPackageUndoUnit : BaseUndoUnit
        {
            public UninstallPackageUndoUnit(
                PackageInstallerService packageInstallerService,
                PackageInfo packageInfo,
                string versionOpt,
                EnvDTE.DTE dte,
                EnvDTE.Project dteProject,
                IOleUndoManager undoManager) : base(packageInstallerService, packageInfo, versionOpt, dte, dteProject, undoManager)
            {
            }

            public override void Do(IOleUndoManager pUndoManager)
            {
                packageInstallerService.TryUninstallAndAddRedoAction(packageInfo, versionOpt, dte, dteProject, undoManager);
            }

            public override void GetDescription(out string pBstr)
            {
                pBstr = string.Format(ServicesVSResources.Uninstall_0, packageInfo.PackageName);
            }
        }

        private class InstallPackageUndoUnit : BaseUndoUnit
        {
            public InstallPackageUndoUnit(
                PackageInstallerService packageInstallerService,
                PackageInfo packageInfo,
                string versionOpt,
                EnvDTE.DTE dte,
                EnvDTE.Project dteProject,
                IOleUndoManager undoManager) : base(packageInstallerService, packageInfo, versionOpt, dte, dteProject, undoManager)
            {
            }

            public override void GetDescription(out string pBstr)
            {
                pBstr = string.Format(ServicesVSResources.Install_0, packageInfo.PackageName);
            }

            public override void Do(IOleUndoManager pUndoManager)
            {
                packageInstallerService.TryInstallAndAddUndoAction(packageInfo, versionOpt, dte, dteProject, undoManager);
            }
        }
    }
}
