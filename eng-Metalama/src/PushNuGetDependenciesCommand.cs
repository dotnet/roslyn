// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Build.Publishers;
using PostSharp.Engineering.BuildTools.Utilities;

namespace Build;

internal class PushNuGetDependenciesCommand : BaseCommand<PublishSettings>
{
    // This record represents data from project.assets.json files.
    private record ProjectAssetsPackage(string Type, Dictionary<string, string>? Dependencies);

    // This record represents data from project.assets.json files.
    private record ProjectAssets(Dictionary<string, Dictionary<string, ProjectAssetsPackage>> Targets);

    // This record represents data from .nupkg.metadata files.
    private record NuGetPackageMetadata(string Source);

    // This record represents data from dotnet-tools.json files.
    private record DotNetTool(string Version);

    // This record represents data from dotnet-tools.json files.
    private record DotNetTools(Dictionary<string, DotNetTool> Tools);

    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected override bool ExecuteCore(BuildContext context, PublishSettings settings)
    {
        context.Console.WriteHeading("Pushing packages");

        var success = true;
        var tasks = new Task<bool>[4];

        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.FromResult(true);
        }

        var cancellationToken = ConsoleHelper.CancellationToken;

        var packageHashPathsVisited = new HashSet<string>();
        var packagePathsToUpload = new ConcurrentBag<string>();

        context.Console.WriteImportantMessage($"Listing packages.");
        using (var httpClient = new HttpClient())
        {
            // Don't push packages, that are available at nuget.org.
            async Task<bool> FilterPackageAsync(string packagePath)
            {
                var packageFile = Path.GetFileName(packagePath);
                var packageDirectory = Path.GetDirectoryName(packagePath)!;
                var metadataPath = Path.Combine(packageDirectory, ".nupkg.metadata");
                var metadataJson = await File.ReadAllTextAsync(metadataPath, cancellationToken);
                var metadata = JsonSerializer.Deserialize<NuGetPackageMetadata>(metadataJson, jsonSerializerOptions)!;

                if (metadata.Source.StartsWith("https://api.nuget.org"))
                {
                    context.Console.WriteMessage($"Package '{packageFile}' comes from NuGet.org.");

                    return true;
                }

                // Assertion.
                if (metadata.Source.Contains("nuget.org"))
                {
                    context.Console.WriteError(
                        $"Package '{packageFile}' shouldn't come from NuGet.org, but the source path contains 'nuget.org': '{metadata.Source}'");

                    return false;
                }

                // Some packages may come from outside of nuget.org, but can still be listed at nuget.org.
                var packageVersion = Path.GetFileName(packageDirectory);
                var packageName = Path.GetFileName(Path.GetDirectoryName(packageDirectory));
                var packageMetadataPath =
                    $"https://api.nuget.org/v3-flatcontainer/{packageName}/{packageVersion}/{packageName}.nuspec";

                // ReSharper disable once AccessToDisposedClosure
                using (var httpResult = await httpClient.SendAsync(
                           new HttpRequestMessage(HttpMethod.Head, packageMetadataPath),
                           HttpCompletionOption.ResponseHeadersRead,
                           cancellationToken))
                {
                    if (httpResult.StatusCode == HttpStatusCode.OK)
                    {
                        context.Console.WriteMessage($"Package '{packageFile}' found at NuGet.org.");

                        return true;
                    }

                    // Assertion.
                    if (httpResult.StatusCode != HttpStatusCode.NotFound)
                    {
                        context.Console.WriteError(
                            $"'{packageMetadataPath}' returned '{httpResult.StatusCode} {httpResult.ReasonPhrase}' unexpectedly.");

                        return false;
                    }
                }

                packagePathsToUpload.Add(packagePath);

                return true;
            }

            var packagePathsToCheck = new HashSet<string>();

            static string GetPackagePath(string name, string version)
            {
                var lowerName = name.ToLowerInvariant();
                return Environment.ExpandEnvironmentVariables(Path.Combine("%UserProfile%", ".nuget",
                    "packages", lowerName, version, $"{lowerName}.{version}.nupkg"));
            }

            // List all packages restored from projects.
            foreach (var projectAssetsPath in Directory.EnumerateFiles(context.RepoDirectory, "project.assets.json",
                         SearchOption.AllDirectories))
            {
                context.Console.WriteMessage($"Processing {projectAssetsPath}.");
            
                var projectAssetsJson = File.ReadAllText(projectAssetsPath);
                var projectAssets = JsonSerializer.Deserialize<ProjectAssets>(projectAssetsJson, jsonSerializerOptions)!;

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
                            packagePathsToCheck.Add(packagePath);
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

            // List all .NET tool packages.
            var toolsPath = Path.Combine(context.RepoDirectory, "dotnet-tools.json");
            var toolsJson = File.ReadAllText(toolsPath);
            var tools = JsonSerializer.Deserialize<DotNetTools>(toolsJson, jsonSerializerOptions)!;
            
            foreach (var tool in tools.Tools)
            {
                var packageName = tool.Key.ToLowerInvariant();
                var packageVersion = tool.Value.Version;
                var toolPackagePath = GetPackagePath(packageName, packageVersion);
            
                packagePathsToCheck.Add(toolPackagePath);
            }
            
            // List Arcade package. (The other MSBuild SDKs are either from NuGet, or aren't restored.)
            var globalJsonPath = Path.Combine(context.RepoDirectory, "global.json");
            var globalJsonJson = File.ReadAllText(globalJsonPath);
            var globalJson = JsonSerializer.Deserialize<JsonDocument>(globalJsonJson, jsonSerializerOptions)!;
            const string ArcadePackageName = "Microsoft.DotNet.Arcade.Sdk";
            var arcadePackageVersion = globalJson.RootElement.EnumerateObject().Single(e => e.Name == "msbuild-sdks")
                .Value.EnumerateObject().Single(e => e.Name == ArcadePackageName).Value.GetString()!;
            var arcadePackagePath = GetPackagePath(ArcadePackageName, arcadePackageVersion);
            packagePathsToCheck.Add(arcadePackagePath);
            
            // Collect dependencies recursively.
            var nextPackagesToListDependencies = packagePathsToCheck.ToHashSet();
            var packagesWithListedDependencies = new HashSet<string>();

            while (nextPackagesToListDependencies.Count > 0)
            {
                var currentPackagesToListDependencies = nextPackagesToListDependencies;
                nextPackagesToListDependencies = new();

                foreach (var packagePath in currentPackagesToListDependencies)
                {
                    if (!packagesWithListedDependencies.Add(packagePath))
                    {
                        continue;
                    }
                    
                    var packageDirectory = Path.GetDirectoryName(packagePath)!;
                    var nuspecFiles = Directory.GetFiles(packageDirectory, "*.nuspec");

                    if (nuspecFiles.Length != 1)
                    {
                        context.Console.WriteError(
                            $"There's {nuspecFiles.Length} nuspec files instead of one for '{packagePath}' package.");

                        success = false;

                        continue;
                    }

                    context.Console.WriteMessage($"Processing '{nuspecFiles[0]}' of '{packagePath}'.");

                    var nuspec = XDocument.Load(nuspecFiles[0]).Root!;
                    XNamespace ns = nuspec.Attribute("xmlns")!.Value;
                    var dependenciesElement = nuspec.Element(ns + "metadata")!.Element(ns + "dependencies");

                    if (dependenciesElement == null)
                    {
                        continue;
                    }

                    var dependenciesWithoutGroups = dependenciesElement.Elements(ns + "dependency");
                    var dependenciesWithGroups = dependenciesElement.Elements(ns + "group").Elements(ns + "dependency");
                    var dependencyPackagePaths = dependenciesWithoutGroups.Concat(dependenciesWithGroups).Select(d =>
                        GetPackagePath(d.Attribute("id")!.Value, d.Attribute("version")!.Value)).ToArray();

                    foreach (var dependencyPackagePath in dependencyPackagePaths)
                    {
                        if (File.Exists(dependencyPackagePath))
                        {
                            nextPackagesToListDependencies.Add(dependencyPackagePath);
                            packagePathsToCheck.Add(dependencyPackagePath);
                        }
                        else
                        {
                            context.Console.WriteImportantMessage(
                                $"'{dependencyPackagePath}' doesn't exist. Origin: {nuspecFiles[0]}");
                        }
                    }
                }
            }
            
            // Filter out packages that are present at nuget.org.
            foreach (var packagePath in packagePathsToCheck)
            {
                // ReSharper disable once CoVariantArrayConversion
                var freeTaskSlot = Task.WaitAny(tasks, cancellationToken);

                if (!tasks[freeTaskSlot].Result)
                {
                    success = false;
                }

                tasks[freeTaskSlot] = FilterPackageAsync(packagePath);
            }

            // ReSharper disable once CoVariantArrayConversion
            Task.WaitAll(tasks, cancellationToken);

            if (tasks.Any(t => !t.Result))
            {
                success = false;
            }
        }

        if (!success)
        {
            return false;
        }

        context.Console.WriteMessage("");
        context.Console.WriteImportantMessage("Pushing packages.");

        var publisher = new NugetPublisher(Pattern.Empty, "MetalamaCompilerDependencies", "az");

        var j = 1;
        foreach (var packagePath in packagePathsToUpload)
        {
            context.Console.WriteImportantMessage($"{j}/{packagePathsToUpload.Count} {packagePath}");
            var publisherSuccess = publisher.PublishFile(context, settings, packagePath, null!, null!);

            if (publisherSuccess != SuccessCode.Success)
            {
                return false;
            }

            j++;
        }

        context.Console.WriteSuccess("Packages pushed.");

        return true;
    }
}
