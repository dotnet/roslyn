// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    internal partial class PackageInstallerService
    {
        private struct ProjectState
        {
            public readonly bool IsEnabled;
            public readonly MultiDictionary<string, string> InstalledPackageToVersion;

            public ProjectState(bool isEnabled, MultiDictionary<string, string> installedPackageToVersion)
            {
                IsEnabled = isEnabled;
                InstalledPackageToVersion = installedPackageToVersion;
            }
        }
    }
}
