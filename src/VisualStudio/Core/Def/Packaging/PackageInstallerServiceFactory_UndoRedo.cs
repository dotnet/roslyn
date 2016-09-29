// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    internal partial class PackageInstallerService
    {
        private bool TryInstallAndAddUndoAction(
            string source, string packageName, string versionOpt, EnvDTE.DTE dte, EnvDTE.Project dteProject, IOleUndoManager undoManager)
        {
            var installed = TryInstallPackage(source, packageName, versionOpt, dte, dteProject);
            if (installed)
            {
                // if the install succeeded, then add an uninstall item to the undo manager.
                undoManager?.Add(new UninstallPackageUndoUnit(this, source, packageName, versionOpt, dte, dteProject, undoManager));
            }
            return installed;
        }

        private bool TryUninstallAndAddRedoAction(string source, string packageName, string versionOpt, EnvDTE.DTE dte, EnvDTE.Project dteProject, IOleUndoManager undoManager)
        {
            var uninstalled = TryUninstallPackage(packageName, dte, dteProject);
            if (uninstalled)
            {
                // if the install succeeded, then add an uninstall item to the undo manager.
                undoManager?.Add(new InstallPackageUndoUnit(this, source, packageName, versionOpt, dte, dteProject, undoManager));
            }
            return uninstalled;
        }

        private IOleUndoManager GetUndoManager(ITextBuffer subjectBuffer)
        {
            var adapter = _editorAdaptersFactoryService.GetBufferAdapter(subjectBuffer);
            if (adapter != null)
            {
                IOleUndoManager manager = null;
                if (ErrorHandler.Succeeded(adapter.GetUndoManager(out manager)))
                {
                    return manager;
                }
            }

            return null;
        }

        private abstract class BaseUndoUnit : IOleUndoUnit
        {
            protected readonly EnvDTE.DTE dte;
            protected readonly EnvDTE.Project dteProject;
            protected readonly PackageInstallerService packageInstallerService;
            protected readonly string source;
            protected readonly string packageName;
            protected readonly IOleUndoManager undoManager;
            protected readonly string versionOpt;

            protected BaseUndoUnit(
                PackageInstallerService packageInstallerService,
                string source,
                string packageName,
                string versionOpt,
                EnvDTE.DTE dte,
                EnvDTE.Project dteProject,
                IOleUndoManager undoManager)
            {
                this.packageInstallerService = packageInstallerService;
                this.packageName = packageName;
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
                string source,
                string packageName,
                string versionOpt,
                EnvDTE.DTE dte,
                EnvDTE.Project dteProject,
                IOleUndoManager undoManager) : base(packageInstallerService, source, packageName, versionOpt, dte, dteProject, undoManager)
            {
            }

            public override void Do(IOleUndoManager pUndoManager)
            {
                packageInstallerService.TryUninstallAndAddRedoAction(source, packageName, versionOpt, dte, dteProject, undoManager);
            }

            public override void GetDescription(out string pBstr)
            {
                pBstr = string.Format(ServicesVSResources.Uninstall_0, packageName);
            }
        }

        private class InstallPackageUndoUnit : BaseUndoUnit
        {
            public InstallPackageUndoUnit(
                PackageInstallerService packageInstallerService,
                string source,
                string packageName,
                string versionOpt,
                EnvDTE.DTE dte,
                EnvDTE.Project dteProject,
                IOleUndoManager undoManager) : base(packageInstallerService, source, packageName, versionOpt, dte, dteProject, undoManager)
            {
            }

            public override void GetDescription(out string pBstr)
            {
                pBstr = string.Format(ServicesVSResources.Install_0, packageName);
            }

            public override void Do(IOleUndoManager pUndoManager)
            {
                packageInstallerService.TryInstallAndAddUndoAction(source, packageName, versionOpt, dte, dteProject, undoManager);
            }
        }
    }
}
