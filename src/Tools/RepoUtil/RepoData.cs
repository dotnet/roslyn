using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoUtil
{
    internal sealed class RepoData
    {
        internal string SourcesPath { get; }
        internal RepoConfig RepoConfig { get; }
        internal ImmutableArray<NuGetPackage> FloatingBuildPackages { get; }
        internal ImmutableArray<NuGetPackage> FloatingToolsetPackages { get; }
        internal ImmutableArray<NuGetPackage> FloatingPackages { get; }
        internal ImmutableArray<NuGetPackage> FixedPackages => RepoConfig.FixedPackages;
        internal ImmutableArray<NuGetPackage> AllPackages { get; }
        internal ImmutableDictionary<string, ImmutableArray<string>> FixedPackagesMap => RepoConfig.FixedPackagesMap;

        internal RepoData(RepoConfig config, string sourcesPath, IEnumerable<NuGetPackage> floatingPackages)
        {
            SourcesPath = sourcesPath;
            RepoConfig = config;
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
            AllPackages = Combine(FloatingBuildPackages, FloatingToolsetPackages, FixedPackages);
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
        /// state of the repo and add in the current data.
        /// </summary>
        internal static RepoData Create(RepoConfig config, string sourcesPath)
        {
            var set = new HashSet<NuGetPackage>();
            foreach (var fileName in ProjectJsonUtil.GetProjectJsonFiles(sourcesPath))
            {
                foreach (var nuget in ProjectJsonUtil.GetDependencies(fileName))
                {
                    if (config.FixedPackagesMap.ContainsKey(nuget.Name))
                    {
                        continue;
                    }

                    set.Add(nuget);
                }
            }

            return new RepoData(
                config,
                sourcesPath,
                set);
        }
    }
}
