// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
