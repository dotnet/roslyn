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
using Microsoft.VisualStudio.Services.Common;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Build.Publishers;
using PostSharp.Engineering.BuildTools.Utilities;

namespace Build.NuGetDependencies;

internal class PushNuGetDependenciesCommand : BaseCommand<PublishSettings>
{
    // This record represents data from .nupkg.metadata files.
    private record NuGetPackageMetadata(string Source);

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
                var metadata =
                    JsonSerializer.Deserialize<NuGetPackageMetadata>(metadataJson,
                        NuGetJsonSerializerOptions.Instance)!;

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

            var dependencySources = new NuGetDependenciesSourceBase[]
            {
                new ProjectAssetsJsonDependenciesSource(),
                new DotNetToolDependenciesSource(),
                new SdkDependenciesSource(),
                new NuGetCacheDependenciesSource()
            };

            foreach (var source in dependencySources)
            {
                success &= source.GetDependencies(context, out var dependencies);
                packagePathsToCheck.AddRange(dependencies);
            }
           
            // Collect dependencies recursively.
            success &= new NuspecDependenciesSource(packagePathsToCheck).GetDependencies(context, out var nuspecDependencies);
            packagePathsToCheck.AddRange(nuspecDependencies);
            
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
