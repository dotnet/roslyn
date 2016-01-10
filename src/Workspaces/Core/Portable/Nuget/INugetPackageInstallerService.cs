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
        void InstallPackage(Project currentProject, string _packageName);
    }
}
