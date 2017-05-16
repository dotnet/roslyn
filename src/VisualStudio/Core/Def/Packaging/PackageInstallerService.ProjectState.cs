// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    internal partial class PackageInstallerService
    {
        private struct ProjectState
        {
            public readonly bool IsEnabled;
            public readonly Dictionary<string, string> InstalledPackageToVersion;

            public ProjectState(bool isEnabled, Dictionary<string, string> installedPackageToVersion)
            {
                IsEnabled = isEnabled;
                InstalledPackageToVersion = installedPackageToVersion;
            }
        }
    }
}
