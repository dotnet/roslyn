using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    // Wrapper types to ensure we delay load the nuget libraries.
    internal interface IPackageUninstallerProxy
    {
        void UninstallPackage(Project project, string packageId, bool removeDependencies);
    }
}
