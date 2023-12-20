// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using PostSharp.Engineering.BuildTools.Build;

namespace Build.NuGetDependencies;

internal abstract class NuGetDependenciesSourceBase
{
    protected static string GetPackagePath(string name, string version)
    {
        var lowerName = name.ToLowerInvariant();
        return Environment.ExpandEnvironmentVariables(Path.Combine("%UserProfile%", ".nuget",
            "packages", lowerName, version, $"{lowerName}.{version}.nupkg"));
    }
    
    public abstract bool GetDependencies(BuildContext context, out IEnumerable<string> dependencies);
}
