using Newtonsoft.Json;
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
    internal sealed class ConsumesCommand : ICommand
    {
        private readonly RepoData _repoData;

        internal ConsumesCommand(RepoData repoData)
        {
            _repoData = repoData;
        }

        public bool Run(TextWriter writer, string[] args)
        {
            var obj = GoCore();
            var text = obj.ToString(Formatting.Indented);
            writer.WriteLine(text);
            return true;
        }

        private JObject GoCore()
        {
            var obj = new JObject();
            obj.Add(GetFixedPackages());
            obj.Add(GetBuildPackages());
            obj.Add(GetToolsetPackages());
            return obj;
        }

        private JProperty GetFixedPackages()
        {
            var obj = new JObject();
            foreach (var pair in _repoData.FixedPackagesMap.OrderBy(x => x.Key))
            {
                obj.Add(GetProperty(pair.Key, pair.Value));
            }
            return new JProperty("fixed", obj);
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
