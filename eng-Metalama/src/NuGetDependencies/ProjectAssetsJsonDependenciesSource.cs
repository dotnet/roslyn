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

internal class ProjectAssetsJsonDependenciesSource : NuGetDependenciesSourceBase
{
    // This record represents data from project.assets.json files.
    private record ProjectAssetsPackage(string Type, Dictionary<string, string>? Dependencies);

    // This record represents data from project.assets.json files.
    private record ProjectAssets(Dictionary<string, Dictionary<string, ProjectAssetsPackage>> Targets);

    public override bool GetDependencies(BuildContext context, out IEnumerable<string> dependencies)
    {
        List<string> dependenciesList = new();

        var success = true;

        foreach (var projectAssetsPath in Directory.EnumerateFiles(context.RepoDirectory, "project.assets.json",
                     SearchOption.AllDirectories))
        {
            context.Console.WriteMessage($"Processing {projectAssetsPath}.");

            var projectAssetsJson = File.ReadAllText(projectAssetsPath);
            var projectAssets =
                JsonSerializer.Deserialize<ProjectAssets>(projectAssetsJson, NuGetJsonSerializerOptions.Instance)!;

            foreach (var assetsPackage in projectAssets.Targets.SelectMany(t => t.Value)
                         .Where(x => x.Value.Type == "package"))
            {
                // Eg. "Microsoft.ServiceHub.Framework/4.3.48"
                var assetsPackageKeyParts = assetsPackage.Key.Split('/');

                if (assetsPackageKeyParts.Length != 2)
                {
                    context.Console.WriteError(
                        $"Package '{assetsPackage.Key}' from '{projectAssetsPath}' has invalid key.");
                    success = false;
                    continue;
                }

                void AddIfExists(string name, string version, string origin)
                {
                    var packagePath = GetPackagePath(name, version);

                    if (File.Exists(packagePath))
                    {
                        dependenciesList.Add(packagePath);
                    }
                    else
                    {
                        context.Console.WriteImportantMessage(
                            $"'{packagePath}' doesn't exist. Origin: {origin}");
                    }
                }

                AddIfExists(assetsPackageKeyParts[0], assetsPackageKeyParts[1],
                    $"Package in '{projectAssetsPath}'");

                if (assetsPackage.Value.Dependencies != null)
                {
                    foreach (var dependency in assetsPackage.Value.Dependencies)
                    {
                        AddIfExists(dependency.Key, dependency.Value,
                            $"Dependency of '{assetsPackage.Key}' package in '{projectAssetsPath}'");
                    }
                }
            }
        }

        dependencies = dependenciesList.ToImmutableList();

        return success;
    }
}
