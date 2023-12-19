// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using PostSharp.Engineering.BuildTools.Build;

namespace Build.NuGetDependencies;

internal class DotNetToolDependenciesSource : NuGetDependenciesSourceBase
{
    // This record represents data from dotnet-tools.json files.
    private record DotNetTool(string Version);

    // This record represents data from dotnet-tools.json files.
    private record DotNetTools(Dictionary<string, DotNetTool> Tools);
    
    public override bool GetDependencies(BuildContext context, out IEnumerable<string> dependencies)
    {
        List<string> dependenciesList = new();
        
        var toolsPath = Path.Combine(context.RepoDirectory, "dotnet-tools.json");
        var toolsJson = File.ReadAllText(toolsPath);
        var tools = JsonSerializer.Deserialize<DotNetTools>(toolsJson, NuGetJsonSerializerOptions.Instance)!;
            
        foreach (var tool in tools.Tools)
        {
            var packageName = tool.Key.ToLowerInvariant();
            var packageVersion = tool.Value.Version;
            var toolPackagePath = GetPackagePath(packageName, packageVersion);
            
            dependenciesList.Add(toolPackagePath);
        }

        dependencies = dependenciesList.ToImmutableList();

        return true;
    }
}
