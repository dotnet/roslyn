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
        private readonly ImmutableArray<string> _buildPackages;

        internal ImmutableArray<NuGetPackage> StaticPackages => _repoConfig.StaticPackages;
        internal ImmutableDictionary<string, ImmutableArray<string>> StaticPackagesMap => _repoConfig.StaticPackagesMap;
        internal ImmutableArray<string> FloatingBuildPackages => _buildPackages;
        internal ImmutableArray<string> FloatingToolsetPackages => _repoConfig.ToolsetPackages;
        internal ImmutableArray<string> FloatingPackages => FloatingBuildPackages.Concat(FloatingToolsetPackages).ToImmutableArray();

        internal RepoData(RepoConfig config, IEnumerable<string> floatingPackages)
        {
            _repoConfig = config;
            _buildPackages = floatingPackages.OrderBy(x => x).ToImmutableArray();
        }

        /// <summary>
        /// The raw RepoData contains only the static + toolset packages that we need to track.  This method will examine the current
        /// state of the repo and add in the current data.
        /// </summary>
        internal static RepoData Create(RepoConfig config, string sourcesPath)
        {
            var set = new HashSet<string>(Constants.NugetPackageNameComparer);
            foreach (var fileName in ProjectJsonUtil.GetProjectJsonFiles(sourcesPath))
            {
                foreach (var nuget in ProjectJsonUtil.GetDependencies(fileName))
                {
                    if (config.StaticPackagesMap.ContainsKey(nuget.Name) || config.ToolsetPackages.Contains(nuget.Name, Constants.NugetPackageNameComparer))
                    {
                        continue;
                    }

                    set.Add(nuget.Name);
                }
            }

            return new RepoData(
                config,
                set);
        }
    }
}
