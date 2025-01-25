// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGetLogger = NuGet.Common.NullLogger;

namespace Microsoft.RoslynTools.NuGet;

internal static partial class NuGetDependencyFinder
{
    internal enum DependencyResult
    {
        SiblingPackage,
        ReleasedPackage,
        MissingPackage,
        ReleasePackageUnavailable,
        ReleasePackageAvailable,
    }

    public static async Task<int> FindDependenciesAsync(string packageFolder, ILogger logger)
    {
        var nugetLogger = NuGetLogger.Instance;
        var cache = new SourceCacheContext();

        var packages = (from file in Directory.EnumerateFiles(packageFolder, "*.nupkg")
                        let fileName = Path.GetFileName(file)
                        let packageVersion = PackageVersion().Match(fileName)
                        where packageVersion.Success
                        select packageVersion.Groups[1].Value).ToImmutableHashSet();

        var nugetOrg = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        var nugetOrgFinder = await nugetOrg.GetResourceAsync<FindPackageByIdResource>().ConfigureAwait(false);

        try
        {
            logger.LogInformation("Finding dependencies...");

            var dependencies = new Dictionary<DependencyResult, List<(PackageDependency Dependency, NuGetVersion? DesiredVersion)>>();

            await foreach (var dependency in GetAllDependenciesAsync())
            {
                DependencyResult result;
                NuGetVersion? desiredVersion = null;

                if (packages.Contains(dependency.Id))
                {
                    result = DependencyResult.SiblingPackage;
                }
                else if (!dependency.VersionRange.MinVersion!.IsPrerelease)
                {
                    result = DependencyResult.ReleasedPackage;
                }

                else if (!await nugetOrgFinder.DoesPackageExistAsync(dependency.Id, dependency.VersionRange.MinVersion, cache, nugetLogger, CancellationToken.None).ConfigureAwait(false))
                {
                    result = DependencyResult.MissingPackage;
                }
                else
                {
                    var versions = await nugetOrgFinder.GetAllVersionsAsync(dependency.Id, cache, nugetLogger, CancellationToken.None).ConfigureAwait(false);

                    desiredVersion = (from v in versions
                                      where !v.IsPrerelease
                                      where v.Version > dependency.VersionRange.MinVersion.Version
                                      select v).FirstOrDefault();


                    result = desiredVersion is null
                        ? DependencyResult.ReleasePackageUnavailable
                        : DependencyResult.ReleasePackageAvailable;
                }

                var list = dependencies.ContainsKey(result)
                    ? dependencies[result]
                    : [];

                list.Add((dependency, desiredVersion));

                dependencies[result] = list;
            }

            logger.LogInformation("Dependencies found.");

            if (dependencies.TryGetValue(DependencyResult.SiblingPackage, out var siblingPackages))
            {
                logger.LogTrace("");
                logger.LogTrace("Dependencies in this folder:");
                foreach (var (dependency, _) in siblingPackages.OrderBy(x => x.Dependency.Id))
                {
                    logger.LogTrace("{DependencyId}, {DependencyMinVersion}", dependency.Id, dependency.VersionRange.MinVersion);
                }
            }

            if (dependencies.TryGetValue(DependencyResult.ReleasedPackage, out var releasedPackages))
            {
                logger.LogDebug("");
                logger.LogDebug("Dependencies on a release version:");
                foreach (var (dependency, _) in releasedPackages.OrderBy(x => x.Dependency.Id))
                {
                    logger.LogDebug("{DependencyId}, {DependencyMinVersion}", dependency.Id, dependency.VersionRange.MinVersion);
                }
            }

            if (dependencies.TryGetValue(DependencyResult.ReleasePackageAvailable, out var releaseAvailablePackages))
            {
                logger.LogInformation("");
                logger.LogInformation("Dependencies where a release version is available:");
                foreach (var (dependency, desiredVersion) in releaseAvailablePackages.OrderBy(x => x.Dependency.Id))
                {
                    logger.LogInformation("{DependencyId}, {DependencyMinVersion}: Upgrade to {DesiredVersion}", dependency.Id, dependency.VersionRange.MinVersion, desiredVersion);
                }
            }

            if (dependencies.TryGetValue(DependencyResult.ReleasePackageUnavailable, out var releaseUnavailablePackages))
            {
                logger.LogWarning("");
                logger.LogWarning("Dependencies where a release version is unavailable:");
                foreach (var (dependency, _) in releaseUnavailablePackages.OrderBy(x => x.Dependency.Id))
                {
                    logger.LogWarning("{DependencyId}, {DependencyMinVersion}", dependency.Id, dependency.VersionRange.MinVersion);
                }
            }

            if (dependencies.TryGetValue(DependencyResult.MissingPackage, out var missingPackages))
            {
                logger.LogError("");
                logger.LogError("Dependencies missing from NuGet.org:");
                foreach (var (dependency, _) in missingPackages.OrderBy(x => x.Dependency.Id))
                {
                    logger.LogError("{DependencyId}, {DependencyMinVersion}", dependency.Id, dependency.VersionRange.MinVersion);
                }
            }

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

    [GeneratedRegex(@"^(.*?)\.((?:\.?[0-9]+){3,}(?:[-a-z0-9]+)?)(\.final)?\.nupkg$")]
    private static partial Regex PackageVersion();
}
