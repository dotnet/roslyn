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
        private readonly RepoConfig _repoConfig;

        internal ImmutableArray<NuGetPackage> FloatingBuildPackages { get; }
        internal ImmutableArray<NuGetPackage> FloatingToolsetPackages { get; }
        internal ImmutableArray<NuGetPackage> FloatingPackages { get; }
        internal ImmutableArray<NuGetPackage> StaticPackages => _repoConfig.StaticPackages;
        internal ImmutableDictionary<string, ImmutableArray<string>> StaticPackagesMap => _repoConfig.StaticPackagesMap;

        internal RepoData(RepoConfig config, IEnumerable<NuGetPackage> floatingPackages)
        {
            _repoConfig = config;
            FloatingToolsetPackages = floatingPackages
                .Where(x => _repoConfig.ToolsetPackages.Contains(x.Name, Constants.NugetPackageNameComparer))
                .OrderBy(x => x.Name)
                .ToImmutableArray();
            FloatingBuildPackages = floatingPackages
                .Where(x => !_repoConfig.ToolsetPackages.Contains(x.Name, Constants.NugetPackageNameComparer))
                .OrderBy(x => x.Name)
                .ToImmutableArray();
            FloatingPackages = floatingPackages
                .OrderBy(x => x.Name)
                .ToImmutableArray();
        }

        /// <summary>
        /// The raw RepoData contains only the static + toolset packages that we need to track.  This method will examine the current
        /// state of the repo and add in the current data.
        /// </summary>
        internal static RepoData Create(RepoConfig config, string sourcesPath)
        {
            var set = new HashSet<NuGetPackage>();
            foreach (var fileName in ProjectJsonUtil.GetProjectJsonFiles(sourcesPath))
            {
                foreach (var nuget in ProjectJsonUtil.GetDependencies(fileName))
                {
                    if (config.StaticPackagesMap.ContainsKey(nuget.Name))
                    {
                        continue;
                    }

                    set.Add(nuget);
                }
            }

            return new RepoData(
                config,
                set);
        }
    }
}
