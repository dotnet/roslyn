// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGetLogger = NuGet.Common.NullLogger;

namespace Microsoft.RoslynTools.NuGet
{
    internal static class NuGetDependencyFinder
    {
        public static async Task<int> FindDependenciesAsync(string packageFolder, ILogger logger)
        {
            var nugetLogger = NuGetLogger.Instance;
            var cache = new SourceCacheContext();

            var packages = from file in Directory.EnumerateFiles(packageFolder, "*.nupkg")
                           let fileName = Path.GetFileName(file)
                           let regex = Regex.Match(fileName, @"^(.*?)\.((?:\.?[0-9]+){3,}(?:[-a-z]+)?)\.nupkg$")
                           where regex.Success
                           select regex.Groups[1].Value;

            var nugetOrg = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            var nugetOrgFinder = await nugetOrg.GetResourceAsync<FindPackageByIdResource>().ConfigureAwait(false);

            try
            {
                logger.LogInformation("Finding dependencies...");

                await foreach (var dependency in GetAllDependenciesAsync())
                {
                    if (packages.Contains(dependency.Id))
                    {
                        logger.LogDebug($"{dependency.Id}, {dependency.VersionRange.MinVersion}: One of our packages, so versioned uniformly.");
                        continue;
                    }
                    else if (!dependency.VersionRange.MinVersion.IsPrerelease)
                    {
                        logger.LogDebug($"{dependency.Id}, {dependency.VersionRange.MinVersion}: Already using a released version.");
                        continue;
                    }

                    if (!await nugetOrgFinder.DoesPackageExistAsync(dependency.Id, dependency.VersionRange.MinVersion, cache, nugetLogger, CancellationToken.None).ConfigureAwait(false))
                    {
                        logger.LogError($"{dependency.Id}, {dependency.VersionRange.MinVersion}: Doesn't exist on nuget.org, nothing to do.");
                        continue;
                    }

                    var versions = await nugetOrgFinder.GetAllVersionsAsync(dependency.Id, cache, nugetLogger, CancellationToken.None).ConfigureAwait(false);

                    var desiredVersion = (from v in versions
                                          where !v.IsPrerelease
                                          where v.Version > dependency.VersionRange.MinVersion.Version
                                          select v).FirstOrDefault();

                    if (desiredVersion is null)
                    {
                        logger.LogWarning($"{dependency.Id}, {dependency.VersionRange.MinVersion}: No released version to upgrade to on nuget.org.");
                        continue;
                    }

                    logger.LogInformation($"{dependency.Id}, {dependency.VersionRange.MinVersion}: Upgrade to {desiredVersion}.");
                }

                logger.LogInformation("Dependencies found.");

                return 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return 1;
            }

            async IAsyncEnumerable<PackageDependency> GetAllDependenciesAsync()
            {
                var local = Repository.Factory.GetCoreV3(packageFolder);
                var localFinder = await local.GetResourceAsync<FindPackageByIdResource>().ConfigureAwait(false);
                var foundPackages = new HashSet<string>();

                foreach (var package in packages)
                {
                    var localVersions = await localFinder.GetAllVersionsAsync(package, cache, nugetLogger, CancellationToken.None).ConfigureAwait(false);
                    var dependencies = await localFinder.GetDependencyInfoAsync(package, localVersions.First(), cache, nugetLogger, CancellationToken.None).ConfigureAwait(false);

                    foreach (var group in dependencies.DependencyGroups)
                    {
                        foreach (var dependency in group.Packages)
                        {
                            if (foundPackages.Add(dependency.Id))
                            {
                                yield return dependency;
                            }
                        }
                    }
                }
            }
        }
    }
}
