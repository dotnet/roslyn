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
    internal interface IPackageServicesProxy
    {
        event EventHandler SourcesChanged;

        /// <summary>
        /// This method just forwards along <see cref="IVsPackageSourceProvider.GetSources(bool, bool)"/>
        /// </summary>
        IEnumerable<KeyValuePair<string, string>> GetSources(bool includeUnOfficial, bool includeDisabled);

        IEnumerable<PackageMetadata> GetInstalledPackages(Project project);

        bool IsPackageInstalled(Project project, string id);

        void InstallPackage(string source, Project project, string packageId, string version, bool ignoreDependencies);

        void UninstallPackage(Project project, string packageId, bool removeDependencies);
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
