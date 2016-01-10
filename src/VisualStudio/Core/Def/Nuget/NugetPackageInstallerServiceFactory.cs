using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Nuget;

namespace Microsoft.VisualStudio.LanguageServices.Nuget
{
    [ExportWorkspaceServiceFactory(typeof(INugetPackageInstallerService)), Shared]
    internal class NugetPackageInstallerServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new NugetPackageInstallerService(workspaceServices);
        }

        private class NugetPackageInstallerService : ForegroundThreadAffinitizedObject, INugetPackageInstallerService
        {
            private readonly HostWorkspaceServices workspaceServices;

            public NugetPackageInstallerService(HostWorkspaceServices workspaceServices)
            {
                this.workspaceServices = workspaceServices;
            }

            public bool TryInstallPackage(Workspace workspace, ProjectId projectId, string packageName)
            {
                this.AssertIsForeground();
                var vsWorkspace = workspace as VisualStudioWorkspace;
                if (vsWorkspace == null)
                {
                    return false;
                }

                return vsWorkspace.TryInstallPackage(projectId, packageName);
            }
        }
    }
}
