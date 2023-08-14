// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Build.Publishers;
using PostSharp.Engineering.BuildTools.Utilities;

namespace Build;

internal class PushNuGetDependenciesCommand : BaseCommand<PublishSettings>
{
    // This record represents data from project.nuget.cache files. 
    private record ProjectNuGetCache(string[] expectedPackageFiles);

    // This record represents data from .nupkg.metadata files.
    private record NuGetPackageMetadata(string source);
    
    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
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

                if (metadata.source.StartsWith("https://api.nuget.org"))
                {
                    context.Console.WriteMessage($"Package '{packageFile}' comes from NuGet.org.");

                    return true;
                }

                // Assertion.
                if (metadata.source.Contains("nuget.org"))
                {
                    context.Console.WriteError(
                        $"Package '{packageFile}' shouldn't come from NuGet.org, but the source path contains 'nuget.org': '{metadata.source}'");

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

            foreach (var cachePath in Directory.EnumerateFiles(context.RepoDirectory, "project.nuget.cache",
                         SearchOption.AllDirectories))
            {
                context.Console.WriteMessage($"Processing {cachePath}.");

                var cacheJson = File.ReadAllText(cachePath);
                var cache = JsonSerializer.Deserialize<ProjectNuGetCache>(cacheJson, jsonSerializerOptions)!;

                foreach (var packageHashPath in cache.expectedPackageFiles)
                {
                    if (!packageHashPathsVisited.Add(packageHashPath))
                    {
                        continue;
                    }

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

                    if (packagePathsToUpload.Contains(packagePath))
                    {
                        continue;
                    }

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
        }

        if (!success)
        {
            return false;
        }

        context.Console.WriteMessage("");
        context.Console.WriteImportantMessage("Pushing packages.");

        var publisher = new NugetPublisher(Pattern.Empty, "MetalamaDependencies", "az");

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
