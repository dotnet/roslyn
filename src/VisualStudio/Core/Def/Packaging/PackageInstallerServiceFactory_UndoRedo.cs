// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Progress;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Packaging;

internal partial class PackageInstallerService
{
    private async Task<bool> TryInstallAndAddUndoActionAsync(
        string? source, string packageName, string? version, bool includePrerelease,
        Guid projectGuid, EnvDTE.DTE dte, EnvDTE.Project dteProject, IOleUndoManager undoManager,
        IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        var installed = await TryInstallPackageAsync(
            source, packageName, version, includePrerelease,
            projectGuid, dte, dteProject, progressTracker, cancellationToken).ConfigureAwait(false);
        if (installed)
        {
            // if the install succeeded, then add an uninstall item to the undo manager.
            await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            undoManager?.Add(new UninstallPackageUndoUnit(
                this, source, packageName,
                version, includePrerelease,
                projectGuid, dte, dteProject, undoManager));
        }

        return installed;
    }

    private async Task<bool> TryUninstallAndAddRedoActionAsync(
        string? source, string packageName, string? version, bool includePrerelease,
        Guid projectGuid, EnvDTE.DTE dte, EnvDTE.Project dteProject, IOleUndoManager undoManager,
        IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        var uninstalled = await TryUninstallPackageAsync(
            packageName, projectGuid, dte, dteProject, progressTracker, cancellationToken).ConfigureAwait(false);
        if (uninstalled)
        {
            // if the install succeeded, then add an uninstall item to the undo manager.
            await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            undoManager?.Add(new InstallPackageUndoUnit(
                this, source, packageName,
                version, includePrerelease,
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
        protected readonly string? source;
        protected readonly string packageName;
        protected readonly IOleUndoManager undoManager;
        protected readonly string? version;
        protected readonly bool includePrerelease;

        protected BaseUndoUnit(
            PackageInstallerService packageInstallerService,
            string? source,
            string packageName,
            string? version,
            bool includePrerelease,
            Guid projectGuid,
            EnvDTE.DTE dte,
            EnvDTE.Project dteProject,
            IOleUndoManager undoManager)
        {
            this.packageInstallerService = packageInstallerService;
            this.source = source;
            this.packageName = packageName;
            this.version = version;
            this.includePrerelease = includePrerelease;
            this.projectGuid = projectGuid;
            this.dte = dte;
            this.dteProject = dteProject;
            this.undoManager = undoManager;
        }

        protected abstract Task DoWorkerAsync(IOleUndoManager pUndoManager);
        public abstract void GetDescription(out string pBstr);

        public void Do(IOleUndoManager pUndoManager)
        {
            var token = this.packageInstallerService._listener.BeginAsyncOperation($"{GetType().Name}.{nameof(Do)}");
            DoAsync(pUndoManager).CompletesAsyncOperation(token);
        }

        private async Task DoAsync(IOleUndoManager pUndoManager)
        {
            try
            {
                await DoWorkerAsync(pUndoManager).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
            }
        }

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
            string? source,
            string packageName,
            string? version,
            bool includePrerelease,
            Guid projectGuid,
            EnvDTE.DTE dte,
            EnvDTE.Project dteProject,
            IOleUndoManager undoManager)
            : base(packageInstallerService, source, packageName,
                   version, includePrerelease,
                   projectGuid, dte, dteProject, undoManager)
        {
        }

        protected override async Task DoWorkerAsync(IOleUndoManager pUndoManager)
        {
            var description = string.Format(ServicesVSResources.Uninstalling_0, packageName);
            using var context = this.packageInstallerService._operationExecutor.BeginExecute(NugetTitle, description, allowCancellation: true, showProgress: false);
            using var scope = context.AddScope(allowCancellation: true, description);

            await packageInstallerService.TryUninstallAndAddRedoActionAsync(
                source, packageName, version, includePrerelease,
                projectGuid, dte, dteProject, undoManager,
                scope.GetCodeAnalysisProgress(),
                context.UserCancellationToken).ConfigureAwait(false);
        }

        public override void GetDescription(out string pBstr)
            => pBstr = string.Format(ServicesVSResources.Uninstall_0, packageName);
    }

    private class InstallPackageUndoUnit : BaseUndoUnit
    {
        public InstallPackageUndoUnit(
            PackageInstallerService packageInstallerService,
            string? source,
            string packageName,
            string? version,
            bool includePrerelease,
            Guid projectGuid,
            EnvDTE.DTE dte,
            EnvDTE.Project dteProject,
            IOleUndoManager undoManager)
            : base(packageInstallerService, source, packageName,
                   version, includePrerelease,
                   projectGuid, dte, dteProject, undoManager)
        {
        }

        public override void GetDescription(out string pBstr)
            => pBstr = string.Format(ServicesVSResources.Install_0, packageName);

        protected override async Task DoWorkerAsync(IOleUndoManager pUndoManager)
        {
            var description = string.Format(ServicesVSResources.Installing_0, packageName);
            using var context = this.packageInstallerService._operationExecutor.BeginExecute(NugetTitle, description, allowCancellation: true, showProgress: false);
            using var scope = context.AddScope(allowCancellation: true, description);

            await packageInstallerService.TryInstallAndAddUndoActionAsync(
                source, packageName, version, includePrerelease,
                projectGuid, dte, dteProject, undoManager,
                scope.GetCodeAnalysisProgress(),
                context.UserCancellationToken).ConfigureAwait(false);
        }
    }
}
