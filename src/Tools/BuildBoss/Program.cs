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
            string sourcePath;
            string configPath;
            if (!ParseArgs(args, out sourcePath, out configPath))
            {
                Usage();
                return 1;
            }

            var config = JsonConvert.DeserializeObject<BuildBossConfig>(File.ReadAllText(configPath));
            var allGood = true;
            var list = new List<string>();

            foreach (var projectPath in Directory.EnumerateFiles(Path.Combine(sourcePath, "src"), "*proj", SearchOption.AllDirectories))
            {
                var relativePath = GetRelativePath(sourcePath, projectPath);
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

            var path = fullPath.Substring(basePath.Length);
            if (path.Length > 0 && path[0] == '\\')
            {
                path = path.Substring(1);
            }

            return path;
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

        private static bool ParseArgs(string[] args, out string sourcePath, out string configPath)
        {
            configPath = null;
            sourcePath = null;

            var i = 0;
            while (i < args.Length)
            {
                var current = args[i];
                if (current == "-config")
                {
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("The -config option requires a parameter");
                        return false;
                    }

                    configPath = args[i + 1];
                    i += 2;
                }
                else
                {
                    break;
                }
            }

            if (i + 1 != args.Length)
            {
                return false;
            }

            sourcePath = args[i];
            configPath = configPath ?? Path.Combine(sourcePath, @"build\config\BuildBossData.json");
            return true;
        }

        private static void Usage()
        {
            Console.WriteLine($"BuildBoss [-config <config path>] <source path>");
        }
    }
}
