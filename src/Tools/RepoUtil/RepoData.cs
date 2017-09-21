using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoUtil
{
    internal sealed class RepoData
    {
        internal string SourcesDirectory { get; }
        internal RepoConfig RepoConfig { get; }
        internal ImmutableArray<NuGetFeed> NuGetFeeds { get; }
        internal ImmutableArray<NuGetPackage> FloatingBuildPackages { get; }
        internal ImmutableArray<NuGetPackage> FloatingToolsetPackages { get; }
        internal ImmutableArray<NuGetPackage> FloatingPackages { get; }
        internal ImmutableArray<NuGetPackage> FixedPackages => RepoConfig.FixedPackages;
        internal ImmutableArray<NuGetPackage> AllPackages { get; }

        private RepoData(RepoConfig config, string sourcesDir, IEnumerable<NuGetFeed> nugetFeeds, IEnumerable<NuGetPackage> floatingPackages)
        {
            SourcesDirectory = sourcesDir;
            RepoConfig = config;
            NuGetFeeds = nugetFeeds.ToImmutableArray();
            FloatingToolsetPackages = floatingPackages
                .Where(x => RepoConfig.ToolsetPackages.Contains(x.Name, Constants.NugetPackageNameComparer))
                .OrderBy(x => x.Name)
                .ToImmutableArray();
            FloatingBuildPackages = floatingPackages
                .Where(x => !RepoConfig.ToolsetPackages.Contains(x.Name, Constants.NugetPackageNameComparer))
                .OrderBy(x => x.Name)
                .ToImmutableArray();
            FloatingPackages = floatingPackages
                .OrderBy(x => x.Name)
                .ToImmutableArray();
            AllPackages = Combine(
                FloatingBuildPackages,
                FloatingToolsetPackages,
                FixedPackages.Select(x => x).ToImmutableArray());
        }

        private static ImmutableArray<NuGetPackage> Combine(params ImmutableArray<NuGetPackage>[] args)
        {
            return args
                .SelectMany(x => x)
                .OrderBy(x => x.Name)
                .ToImmutableArray();
        }

        /// <summary>
        /// The raw RepoData contains only the fixed + toolset packages that we need to track.  This method will examine the current
        /// state of the repo and add in the current data.  If any conflicting package definitions are detected this method 
        /// will throw.
        /// </summary>
        internal static RepoData Create(RepoConfig config, string sourcesDir, bool ignoreConflicts)
        {
            var repoData = Create(config, sourcesDir, out var conflicts);
            if (conflicts?.Count > 0 && !ignoreConflicts)
            {
                throw new ConflictingPackagesException(conflicts);
            }

            return repoData;
        }

        internal static RepoData Create(RepoConfig config, string sourcesDir, out List<NuGetPackageConflict> conflicts)
        {
            var nugetFeeds = new List<NuGetFeed>();
            foreach (var nugetConfig in NuGetConfigUtil.GetNuGetConfigFiles(sourcesDir))
            {
                var nugetFeed = NuGetConfigUtil.GetNuGetFeeds(nugetConfig);
                nugetFeeds.AddRange(nugetFeed);
            }

            conflicts = null;

            var fixedPackageSet = new HashSet<NuGetPackage>(config.FixedPackages, default(Constants.IgnoreGenerateNameComparer));
            var floatingPackageMap = new Dictionary<string, NuGetPackageSource>(Constants.NugetPackageNameComparer);
            foreach (var filePath in ProjectJsonUtil.GetProjectJsonFiles(sourcesDir))
            {
                var fileName = FileName.FromFullPath(sourcesDir, filePath);

                if (config.ProjectJsonExcludes.Any(x => x.IsMatch(fileName.RelativePath)))
                {
                    continue;
                }

                foreach (var package in ProjectJsonUtil.GetDependencies(filePath))
                {
                    if (fixedPackageSet.Contains(package))
                    {
                        continue;
                    }

                    // If this is the first time we've seen the package then record where it was found.  Need the source
                    // information to provide better error messages later.
                    var packageSource = new NuGetPackageSource(package, fileName);
                    if (floatingPackageMap.TryGetValue(package.Name, out var originalSource))
                    {
                        if (originalSource.NuGetPackage.Version != package.Version)
                        {
                            var conflict = new NuGetPackageConflict(original: originalSource, conflict: packageSource);
                            conflicts = conflicts ?? new List<NuGetPackageConflict>();
                            conflicts.Add(conflict);
                        }
                    }
                    else
                    {
                        floatingPackageMap.Add(package.Name, packageSource);
                    }
                }
            }

            return new RepoData(config, sourcesDir, nugetFeeds, floatingPackageMap.Values.Select(x => x.NuGetPackage));
        }
    }
}
