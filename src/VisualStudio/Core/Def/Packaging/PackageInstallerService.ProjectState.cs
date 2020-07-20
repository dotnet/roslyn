// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    internal partial class PackageInstallerService
    {
        private struct ProjectState
        {
            public static readonly ProjectState Disabled = new ProjectState(isEnabled: false, new MultiDictionary<string, string>());

            public readonly bool IsEnabled;

            private readonly MultiDictionary<string, string> InstalledPackageToVersion;

            private ProjectState(bool isEnabled, MultiDictionary<string, string> installedPackageToVersion)
            {
                IsEnabled = isEnabled;
                InstalledPackageToVersion = installedPackageToVersion;
            }

            public ProjectState(MultiDictionary<string, string> installedPackageToVersion)
                : this(isEnabled: true, installedPackageToVersion)
            {
            }

            public bool IsInstalled(string package)
                => IsEnabled && InstalledPackageToVersion.ContainsKey(package);

            public MultiDictionary<string, string>.ValueSet GetInstalledVersions(string packageName)
                => InstalledPackageToVersion[packageName];
        }
    }
}
