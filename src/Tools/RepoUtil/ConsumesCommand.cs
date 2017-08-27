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
            obj.Add(GetNuGetFeeds());
            obj.Add(GetFixedPackages());
            obj.Add(GetBuildPackages());
            obj.Add(GetToolsetPackages());
            return obj;
        }

        private JProperty GetNuGetFeeds()
        {
            var obj = new JObject();
            foreach (var nugetFeed in _repoData.NuGetFeeds)
            {
                obj.Add(GetProperty(nugetFeed));
            }
            return new JProperty("nugetFeeds", obj);
        }

        private JProperty GetFixedPackages()
        {
            var obj = new JObject();
            foreach (var package in _repoData.FixedPackages.GroupBy(x => x.Name))
            {
                obj.Add(GetProperty(package.Key, package.Select(x => x.Version)));
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

        private static JProperty GetProperty(NuGetFeed feed)
        {
            return new JProperty(feed.Name, feed.Url);
        }

        private static JProperty GetProperty(NuGetPackage package)
        {
            return new JProperty(package.Name, package.Version);
        }

        private static JProperty GetProperty(string packageName, IEnumerable<string> versions)
        {
            if (versions.Count() == 1)
            {
                return GetProperty(new NuGetPackage(packageName, versions.Single()));
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
