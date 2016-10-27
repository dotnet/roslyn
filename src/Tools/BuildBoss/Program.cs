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
            string configFile;
            string basePath;
            List<string> solutionFilePaths;
            if (!ParseCommandLine(args, out configFile, out basePath, out solutionFilePaths))
            { 
                Usage();
                return 1;
            }

            if (configFile != null)
            {
                var config = JsonConvert.DeserializeObject<BuildBossConfig>(File.ReadAllText(configFile));
                foreach (var target in config.Targets)
                {
                    solutionFilePaths.Add(Path.IsPathRooted(target)
                        ? target
                        : Path.Combine(basePath, target));
                }
            }

            var allGood = true;
            foreach (var solutionFilePath in solutionFilePaths)
            {
                allGood &= ProcessSolution(solutionFilePath);
            }

            return allGood ? 0 : 1;
        }

        private static bool ProcessSolution(string solutionFilePath)
        {
            var solutionPath = Path.GetDirectoryName(solutionFilePath);
            var projectDataList = SolutionUtil.ParseProjects(solutionFilePath);
            var map = new Dictionary<ProjectKey, ProjectData>();
            foreach (var projectEntry in projectDataList)
            {
                if (projectEntry.IsFolder)
                {
                    continue;
                }

                var projectFilePath = Path.Combine(solutionPath, projectEntry.RelativeFilePath);
                var projectData = new ProjectData(projectFilePath);
                map.Add(projectData.Key, projectData);
            }

            var allGood = true;
            var count = 0;
            foreach (var projectData in map.Values.OrderBy(x => x.FileName))
            {
                allGood &= ProcessProject(solutionPath, projectData, map);
                count++;

                var element = projectData.ProjectUtil.FindSingleProperty("Configuration");
                if (element != null)
                {
                    element.Remove();
                    projectData.Document.Save(projectData.FilePath);
                }
            }

            var result = allGood ? "passed" : "FAILED";
            Console.WriteLine($"Processing {Path.GetFileName(solutionFilePath)} ... {result} ({count} projects processed)");
            return allGood;
        }

        private static bool ProcessProject(string solutionPath, ProjectData projectData, Dictionary<ProjectKey, ProjectData> map)
        {
            var util = new ProjectCheckerUtil(projectData, map);
            var textWriter = new StringWriter();
            if (!util.CheckAll(textWriter))
            {
                Console.WriteLine($"Checking {projectData.FilePath} failed");
                Console.WriteLine(textWriter.ToString());
                return false;
            }

            return true;
        }

        private static bool ParseCommandLine(string[] args, out string configFile, out string basePath, out List<string> solutionFilePaths)
        {
            configFile = null;
            basePath = AppContext.BaseDirectory;
            solutionFilePaths = new List<string>();

            var i = 0;
            while (i < args.Length)
            {
                var current = args[i];
                switch (current.ToLower())
                {
                    case "-config":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("config requires an argument");
                            return false;
                        }

                        configFile = args[i + 1];
                        i += 2;
                        break;
                    case "-basepath":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("basePath requise and argument");
                            return false;
                        }

                        basePath = args[i + 1];
                        i += 2;
                        break;
                    default:
                        solutionFilePaths.Add(current);
                        i++;
                        break;
                }
            }

            return true;
        }

        private static void Usage()
        {
            Console.WriteLine($"BuildBoss [-config <config file path] [-basePath <base path>] <solution paths>");
        }
    }
}
