// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    internal partial class PackageInstallerService
    {
        private readonly struct ProjectState
        {
            public static readonly ProjectState Disabled = new(isEnabled: false, ImmutableDictionary<string, string>.Empty);

            public readonly bool IsEnabled;

            private readonly ImmutableDictionary<string, string> InstalledPackageToVersion;

            private ProjectState(bool isEnabled, ImmutableDictionary<string, string> installedPackageToVersion)
            {
                IsEnabled = isEnabled;
                InstalledPackageToVersion = installedPackageToVersion;
            }

            public ProjectState(ImmutableDictionary<string, string> installedPackageToVersion)
                : this(isEnabled: true, installedPackageToVersion)
            {
            }

            public bool IsInstalled(string package)
                => IsEnabled && InstalledPackageToVersion.ContainsKey(package);

            public bool TryGetInstalledVersion(string packageName, [MaybeNullWhenAttribute(false)] out string version)
                => InstalledPackageToVersion.TryGetValue(packageName, out version);
        }
    }
}
