using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoUtil
{
    internal sealed class ConsumesUtil
    {
        private readonly RepoData _repoData;

        internal ConsumesUtil(RepoData repoData)
        {
            _repoData = repoData;
        }

        internal static string Go(RepoConfig repoConfig, string sourcesPath)
        {
            var repoData = RepoData.Create(repoConfig, sourcesPath);
            var util = new ConsumesUtil(repoData);
            var obj = util.GoCore();
            return obj.ToString(Formatting.Indented);
        }

        private JObject GoCore()
        {
            var obj = new JObject();
            obj.Add(GetStaticPackages());
            obj.Add(GetBuildPackages());
            obj.Add(GetToolsetPackages());
            return obj;
        }

        private JProperty GetStaticPackages()
        {
            var obj = new JObject();
            foreach (var pair in _repoData.StaticPackagesMap.OrderBy(x => x.Key))
            {
                obj.Add(GetProperty(pair.Key, pair.Value));
            }
            return new JProperty("static", obj);
        }

        private JProperty GetBuildPackages()
        {
            return GetFloatingPackages("build", _repoData.FloatingBuildPackages);
        }

        private JProperty GetToolsetPackages()
        {
            return GetFloatingPackages("toolset", _repoData.FloatingToolsetPackages);
        }

        private JProperty GetFloatingPackages(string name, IEnumerable<NuGetPackage> packages)
        {
            var obj = new JObject();
            foreach (var package in packages)
            {
                obj.Add(GetProperty(package));
            }

            return new JProperty(name, obj);
        }

        private static JProperty GetProperty(NuGetPackage package)
        {
            return new JProperty(package.Name, package.Version);
        }

        private static JProperty GetProperty(string packageName, ImmutableArray<string> versions)
        {
            if (versions.Length == 1)
            {
                return GetProperty(new NuGetPackage(packageName, versions[0]));
            }

            var content = JArray.FromObject(versions.ToArray());
            return new JProperty(packageName, content);
        }

        private static string GetKey(NuGetPackage nugetRef)
        {
            return $"{nugetRef.Name}:{nugetRef.Version}";
        }
    }
}
