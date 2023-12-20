// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using PostSharp.Engineering.BuildTools.Build;

namespace Build.NuGetDependencies;

internal class SdkDependenciesSource : NuGetDependenciesSourceBase
{
    public override bool GetDependencies(BuildContext context, out IEnumerable<string> dependencies)
    {
        // List Arcade package. (The other MSBuild SDKs are either from NuGet, or aren't restored.)
        var globalJsonPath = Path.Combine(context.RepoDirectory, "global.json");
        var globalJsonJson = File.ReadAllText(globalJsonPath);
        var globalJson = JsonSerializer.Deserialize<JsonDocument>(globalJsonJson, NuGetJsonSerializerOptions.Instance)!;
        const string ArcadePackageName = "Microsoft.DotNet.Arcade.Sdk";
        var arcadePackageVersion = globalJson.RootElement.EnumerateObject().Single(e => e.Name == "msbuild-sdks")
            .Value.EnumerateObject().Single(e => e.Name == ArcadePackageName).Value.GetString()!;
        var arcadePackagePath = GetPackagePath(ArcadePackageName, arcadePackageVersion);
        dependencies = new[] { arcadePackagePath }.ToImmutableList();

        return true;
    }
}
