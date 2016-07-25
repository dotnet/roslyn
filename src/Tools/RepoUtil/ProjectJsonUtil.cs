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

        internal static ImmutableArray<NuGetReference> GetDependencies(string filePath)
        {
            // Need to track any file that has dependencies
            var obj = JObject.Parse(File.ReadAllText(filePath));
            var dependencies = (JObject)obj["dependencies"];
            if (dependencies == null)
            {
                return ImmutableArray<NuGetReference>.Empty;
            }

            var builder = ImmutableArray.CreateBuilder<NuGetReference>();
            foreach (var dependency in dependencies.Properties())
            {
                builder.Add(ParseDependency(dependency));
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Parse out a dependency entry from the project.json file.
        /// </summary>
        internal static NuGetReference ParseDependency(JProperty prop)
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

            return new NuGetReference(name, version);
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
    }
}
