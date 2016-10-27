﻿using Newtonsoft.Json;
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
            var allGood = true;
            var count = 0;
            foreach (var projectData in projectDataList)
            {
                if (projectData.IsFolder)
                {
                    continue;
                }

                var projectFilePath = Path.Combine(solutionPath, projectData.RelativeFilePath);
                allGood &= ProcessProject(projectFilePath, projectData);
                count++;
            }

            var result = allGood ? "passed" : "FAILED";
            Console.WriteLine($"Processing {Path.GetFileName(solutionFilePath)} ... {result} ({count} projects processed)");
            return allGood;
        }

        private static bool ProcessProject(string projectFilePath, ProjectData projectData)
        {
            var doc = XDocument.Load(projectFilePath);
            var projectType = projectData.ProjectType;
            var util = new ProjectUtil(projectType, projectFilePath, doc);
            var textWriter = new StringWriter();
            if (!util.CheckAll(textWriter))
            {
                Console.WriteLine($"Checking {projectData.RelativeFilePath} failed");
                Console.WriteLine(textWriter.ToString());
                return false;
            }

            return true;
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
