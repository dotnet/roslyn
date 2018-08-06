// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using EnvDTE;
using NuGet.VisualStudio;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    /// <summary>Wrapper type to ensure we delay load the nuget libraries.</summary> 
    /// <remarks>All methods may throw exceptions due to <see cref="IVsPackageSourceProvider"/>
    /// throwing in all sorts of bad nuget states (for example a bad nuget.config file)</remarks>
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
        void InstallLatestPackage(string source, Project project, string packageId, bool includePrerelease, bool ignoreDependencies);

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
