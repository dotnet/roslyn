using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using NuGet.VisualStudio;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    // Wrapper types to ensure we delay load the nuget libraries.
    internal interface IPackageInstallerServicesProxy
    {
        IEnumerable<PackageMetadata> GetInstalledPackages(Project project);

        bool IsPackageInstalled(Project project, string id);
    }

    internal class PackageMetadata
    {
        public readonly string Id;
        public readonly string VersionString;

        public PackageMetadata(string id, string versionString)
        {
            Id = id;
            VersionString = versionString;
        }
    }
}