// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using PostSharp.Engineering.BuildTools.Build;

namespace Build.NuGetDependencies;

// This source confusingly doesn't contain all the packages referenced in a project,
// but it contains packages referenced via PackageDownload MsBuild item.
internal class NuGetCacheDependenciesSource : NuGetDependenciesSourceBase
{
    // This record represents data from project.nuget.cache files.
    private record ProjectNuGetCache(string[] ExpectedPackageFiles);

    public override bool GetDependencies(BuildContext context, out IEnumerable<string> dependencies)
    {
        List<string> dependenciesList = new();

        var success = true;

        foreach (var cachePath in Directory.EnumerateFiles(context.RepoDirectory, "project.nuget.cache",
                     SearchOption.AllDirectories))
        {
            context.Console.WriteMessage($"Processing {cachePath}.");

            var cacheJson = File.ReadAllText(cachePath);
            var cache = JsonSerializer.Deserialize<ProjectNuGetCache>(cacheJson, NuGetJsonSerializerOptions.Instance)!;

            foreach (var packageHashPath in cache.ExpectedPackageFiles)
            {
                const string nupkgSuffix = ".nupkg";
                const string hashSuffix = ".sha512";
                const string hashFileSuffix = nupkgSuffix + hashSuffix;

                if (!packageHashPath.EndsWith(hashFileSuffix))
                {
                    context.Console.WriteError($"Invalid path '{packageHashPath}' in '{cachePath}'.");
                    success = false;

                    continue;
                }

                var packagePath = packageHashPath.Substring(0, packageHashPath.Length - hashSuffix.Length);

                dependenciesList.Add(packagePath);
            }
        }

        dependencies = dependenciesList.ToImmutableList();

        return success;
    }
}
