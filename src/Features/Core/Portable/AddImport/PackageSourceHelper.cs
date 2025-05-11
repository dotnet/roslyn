// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Packaging;

namespace Microsoft.CodeAnalysis.AddImport;

internal static class PackageSourceHelper
{
    private const string NugetOrg = "nuget.org";
    public const string NugetOrgSourceName = "::nuget::";

    public static IEnumerable<(string sourceName, string sourceUrl)> GetPackageSources(ImmutableArray<PackageSource> packageSources)
    {
        // Package source names are user configurable, but various operations and background tasks process
        // only the nuget source, so we ignore the user defined name for nuget.org so we can identify it later.

        var foundNugetOrg = false;
        foreach (var packageSource in packageSources)
        {
            // If the user has multiple sources from nuget.org, we only need one of them to be special
            if (!foundNugetOrg && IsNugetOrg(packageSource.Source))
            {
                foundNugetOrg = true;
                yield return (NugetOrgSourceName, packageSource.Source);
            }
            else
            {
                yield return (packageSource.Name, packageSource.Source);
            }
        }
    }

    private static bool IsNugetOrg(string sourceUrl)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        // The default source url for nuget.org is "api.nuget.org" so the first case catches everything
        // but the check is a little more expansive just to avoid a maintenance burden.
        return uri.Host.EndsWith($".{NugetOrg}", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals(NugetOrg, StringComparison.OrdinalIgnoreCase);
    }
}
