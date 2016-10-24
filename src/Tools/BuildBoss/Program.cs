using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BuildBoss
{
    internal static class Program
    {
        internal static int Main(string[] args)
        {
            var sourceDir = args[0].Trim('"');
            var configPath = Path.Combine(sourceDir, @"build\config\BuildBossData.json");
            var config = JsonConvert.DeserializeObject<BuildBossConfig>(File.ReadAllText(configPath));
            var allGood = true;
            var list = new List<string>();

            foreach (var projectPath in Directory.EnumerateFiles(Path.Combine(sourceDir, "src"), "*proj", SearchOption.AllDirectories))
            {
                var relativePath = GetRelativePath(sourceDir, projectPath);
                if (Exclude(config, relativePath))
                {
                    continue;
                }

                var doc = XDocument.Load(projectPath);
                var projectType = GetProjectType(projectPath);
                var util = new ProjectUtil(projectType, projectPath, doc);
                var textWriter = new StringWriter();
                if (!util.CheckAll(textWriter))
                {
                    Console.WriteLine($"Checking {relativePath} failed");
                    Console.WriteLine(textWriter.ToString());
                    list.Add(relativePath);
                    allGood = false;
                }
            }

            // Print it all out in the end for easy editting.
            foreach (var item in list)
            {
                Console.WriteLine(item);
            }

            return allGood ? 0 : 1;
        }

        private static ProjectType GetProjectType(string path)
        {
            switch (Path.GetExtension(path))
            {
                case ".csproj": return ProjectType.CSharp;
                case ".vbproj": return ProjectType.Basic;
                case ".shproj": return ProjectType.Shared;
                default:
                    return ProjectType.Unknown;
            }
        }

        private static string GetRelativePath(string basePath, string fullPath)
        {
            if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"{fullPath} doesn't begin with {basePath}");
            }

            return fullPath.Substring(basePath.Length + 1);
        }

        private static bool Exclude(BuildBossConfig config, string projectRelativePath)
        {
            foreach (var exclude in config.Exclude)
            {
                if (projectRelativePath.StartsWith(exclude, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
