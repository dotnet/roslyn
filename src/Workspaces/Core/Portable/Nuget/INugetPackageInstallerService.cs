using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Nuget
{
    interface INugetPackageInstallerService : IWorkspaceService
    {
        bool TryInstallPackage(Workspace workspace, ProjectId currentProject, string packageName);
    }
}
