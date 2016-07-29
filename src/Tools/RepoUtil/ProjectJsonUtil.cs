using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Immutable;

namespace RepoUtil
{
    internal static class ProjectJsonUtil
    {
        /// <summary>
        /// Does the specified project.json file need to be tracked by our repo util? 
        /// </summary>
        internal static bool NeedsTracking(string filePath)
        {
            return GetDependencies(filePath).Length > 0;
        }

        internal static ImmutableArray<NuGetPackage> GetDependencies(string filePath)
        {
            // Need to track any file that has dependencies
            var obj = JObject.Parse(File.ReadAllText(filePath));
            var dependencies = (JObject)obj["dependencies"];
            if (dependencies == null)
            {
                return ImmutableArray<NuGetPackage>.Empty;
            }

            var builder = ImmutableArray.CreateBuilder<NuGetPackage>();
            foreach (var dependency in dependencies.Properties())
            {
                builder.Add(ParseDependency(dependency));
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Change the NuGet dependencies in the file to match the new packages.
        /// </summary>
        internal static bool ChangeDependencies(string filePath, ImmutableDictionary<NuGetPackage, NuGetPackage> changeMap)
        {
            var obj = JObject.Parse(File.ReadAllText(filePath), new JsonLoadSettings() { CommentHandling = CommentHandling.Load });
            var dependencies = (JObject)obj["dependencies"];
            if (dependencies == null)
            {
                return false;
            }

            var changed = false;
            foreach (var prop in dependencies.Properties())
            {
                var currentPackage = ParseDependency(prop);
                NuGetPackage newPackage;
                if (!changeMap.TryGetValue(currentPackage, out newPackage))
                {
                    continue;
                }

                ChangeDependency(prop, newPackage.Version);
                changed = true;
            }

            if (!changed)
            {
                return false;
            }

            var data = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(filePath, data);
            return true;
        }

        private static void ChangeDependency(JProperty prop, string version)
        {
            if (prop.Value.Type == JTokenType.String)
            {
                prop.Value = version;
            }
            else
            {
                var obj = (JObject)prop.Value;
                obj["version"] = version;
            }
        }

        /// <summary>
        /// Parse out a dependency entry from the project.json file.
        /// </summary>
        internal static NuGetPackage ParseDependency(JProperty prop)
        {
            var name = prop.Name;

            string version;
            if (prop.Value.Type == JTokenType.String)
            {
                version = (string)prop.Value;
            }
            else
            {
                version = ((JObject)prop.Value).Value<string>("version");
            }

            return new NuGetPackage(name, version);
        }

        internal static bool VerifyTracked(string sourcesPath, IEnumerable<FileName> fileNames)
        {
            var set = new HashSet<FileName>(fileNames);
            var allGood = true;

            foreach (var file in Directory.EnumerateFiles(sourcesPath, "project.json", SearchOption.AllDirectories))
            {
                var relativeName = file.Substring(sourcesPath.Length + 1);
                var fileName = new FileName(sourcesPath, relativeName);
                if (set.Contains(fileName) || !NeedsTracking(file))
                {
                    continue;
                }

                Console.WriteLine($"Need to track {fileName}");
                allGood = false;
            }

            return allGood;
        }

        internal static IEnumerable<string> GetProjectJsonFiles(string sourcesPath)
        {
            return Directory.EnumerateFiles(sourcesPath, "*project.json", SearchOption.AllDirectories);
        }
    }
}
