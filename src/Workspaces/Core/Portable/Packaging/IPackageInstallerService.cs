using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Packaging
{
    interface IPackageInstallerService : IWorkspaceService
    {
        bool IsInstalled(Workspace workspace, ProjectId projectId, string packageName);

        bool TryInstallPackage(Workspace workspace, ProjectId projectId, string packageName, string versionOpt);
        
        IEnumerable<string> GetInstalledVersions(string packageName);
    }
}
