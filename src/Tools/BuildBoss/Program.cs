﻿using Mono.Options;
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
            try
            {
                return MainCore(args) ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled exception: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        private static bool MainCore(string[] args)
        {
            string repositoryDirectory = null;
            string configuration = "Debug";
            List<string> solutionFiles;

            var options = new OptionSet
            {
                { "r|root=", "The repository root", value => repositoryDirectory = value },
                { "c|configuration=", "Build configuration", value => configuration = value }
            };

            if (configuration != "Debug" && configuration != "Release")
            {
                Console.Error.WriteLine($"Invalid configuration: '{configuration}'");
                return false;
            }

            try
            {
                solutionFiles = options.Parse(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                options.WriteOptionDescriptions(Console.Error);
                return false;
            }

            if (string.IsNullOrEmpty(repositoryDirectory))
            {
                repositoryDirectory = FindRepositoryRoot(
                    (solutionFiles.Count > 0) ? Path.GetDirectoryName(solutionFiles[0]) : AppContext.BaseDirectory);

                if (repositoryDirectory == null)
                {
                    Console.Error.WriteLine("Unable to find repository root");
                    return false;
                }
            }

            if (solutionFiles.Count == 0)
            {
                solutionFiles = Directory.EnumerateFiles(repositoryDirectory, "*.sln").ToList();
            }

            return Go(repositoryDirectory, configuration, solutionFiles);
        }

        private static string FindRepositoryRoot(string startDirectory)
        {
            string dir = startDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "global.json")))
            {
                dir = Path.GetDirectoryName(dir);
            }

            return dir;
        }

        private static bool Go(string repositoryDirectory, string configuration, List<string> solutionFileNames)
        {
            var allGood = true;
            foreach (var solutionFileName in solutionFileNames)
            {
                allGood &= ProcessSolution(Path.Combine(repositoryDirectory, solutionFileName));
            }

            var artifactsDirectory = Path.Combine(repositoryDirectory, "artifacts");

            allGood &= ProcessStructuredLog(artifactsDirectory, configuration);
            allGood &= ProcessTargets(repositoryDirectory);
            allGood &= ProcessPackages(repositoryDirectory, artifactsDirectory, configuration);

            if (!allGood)
            {
                Console.WriteLine("Failed");
            }

            return allGood;
        }

        private static bool CheckCore(ICheckerUtil util, string title)
        {
            Console.Write($"Processing {title} ... ");
            var textWriter = new StringWriter();
            if (util.Check(textWriter))
            {
                Console.WriteLine("passed");
                return true;
            }
            else
            {
                Console.WriteLine("FAILED");
                Console.WriteLine(textWriter.ToString());
                return false;
            }
        }

        private static bool ProcessSolution(string solutionFilePath)
        {
            var util = new SolutionCheckerUtil(solutionFilePath);
            return CheckCore(util, $"Solution {solutionFilePath}");
        }

        private static bool ProcessTargets(string repositoryDirectory)
        {
            var targetsDirectory = Path.Combine(repositoryDirectory, @"eng\targets");
            var checker = new TargetsCheckerUtil(targetsDirectory);
            return CheckCore(checker, $"Targets {targetsDirectory}");
        }

        private static bool ProcessStructuredLog(string artifactsDirectory, string configuration)
        {
            var logFilePath = Path.Combine(artifactsDirectory, $@"log\{configuration}\Build.binlog");
            var util = new StructuredLoggerCheckerUtil(logFilePath);
            return CheckCore(util, $"Structured log {logFilePath}");
        }

        private static bool ProcessPackages(string repositoryDirectory, string artifactsDirectory, string configuration)
        {
            var util = new PackageContentsChecker(repositoryDirectory, artifactsDirectory, configuration);
            return CheckCore(util, $"NuPkg and VSIX files");
        }
    }
}
