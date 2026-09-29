// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Locator;
using Mono.Options;

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
            VisualStudioInstance instance = MSBuildLocator.RegisterDefaults();
            Console.WriteLine($"Version: {instance.Version}");
            string repositoryDirectory = null;
            string configuration = "Debug";
            string primarySolution = null;
            bool checkPackageInstall = false;
            List<string> solutionFiles;

            var options = new OptionSet
            {
                { "r|root=", "The repository root", value => repositoryDirectory = value },
                { "c|configuration=", "Build configuration", value => configuration = value },
                { "p|primary=", "Primary solution file name (which contains all projects)", value => primarySolution = value },
                // The trailing ':' makes the value optional, so a bare --check-package-install turns the
                // check on. Without it Mono.Options silently discards an explicit value and
                // --check-package-install=false would still run the check.
                { "check-package-install:", "Verify our NuGet packages can be installed", value => checkPackageInstall = value is null || bool.Parse(value) },
            };

            if (configuration is not "Debug" and not "Release")
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

            return Go(repositoryDirectory, configuration, primarySolution, solutionFiles, checkPackageInstall);
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

        private static bool Go(string repositoryDirectory, string configuration, string primarySolution, List<string> solutionFileNames, bool checkPackageInstall)
        {
            var allGood = true;
            var artifactsDirectory = Path.Combine(repositoryDirectory, "artifacts");
            var logDirectory = Path.Combine(artifactsDirectory, "log", configuration, "BuildBoss");
            if (Directory.Exists(logDirectory))
            {
                Directory.Delete(logDirectory, recursive: true);
            }

            Directory.CreateDirectory(logDirectory);

            foreach (var solutionFileName in solutionFileNames)
            {
                allGood &= ProcessSolution(
                    Path.Combine(repositoryDirectory, solutionFileName),
                    solutionFileName == primarySolution,
                    Path.Combine(logDirectory, $"Solution-{Path.GetFileName(solutionFileName)}.log"));
            }

            allGood &= ProcessGeneratedFiles(repositoryDirectory, Path.Combine(logDirectory, "GeneratedFiles.log"));
            allGood &= ProcessTargets(repositoryDirectory, Path.Combine(logDirectory, "Targets.log"));
            allGood &= ProcessPackages(repositoryDirectory, artifactsDirectory, configuration, Path.Combine(logDirectory, "PackageContents.log"));
            allGood &= ProcessStructuredLog(artifactsDirectory, configuration, Path.Combine(logDirectory, "StructuredLog.log"));
            allGood &= ProcessOptProf(repositoryDirectory, artifactsDirectory, configuration, Path.Combine(logDirectory, "OptProf.log"));

            if (checkPackageInstall)
            {
                allGood &= ProcessPackageInstall(artifactsDirectory, configuration, Path.Combine(logDirectory, "PackageInstall.log"));
            }

            if (!allGood)
            {
                Console.WriteLine("Failed");
            }

            return allGood;
        }

        private static bool CheckCore(ICheckerUtil util, string title, string logFilePath)
        {
            Console.Write($"Processing {title} ... ");
            var textWriter = new StringWriter();
            var succeeded = util.Check(textWriter);
            var output = textWriter.ToString();

            using (var logWriter = new StreamWriter(logFilePath, append: false, SharedUtil.Encoding))
            {
                logWriter.WriteLine($"Check: {title}");
                logWriter.WriteLine($"Result: {(succeeded ? "passed" : "FAILED")}");
                logWriter.Write(output);
            }

            if (succeeded)
            {
                Console.WriteLine("passed");
                return true;
            }
            else
            {
                Console.WriteLine("FAILED");
                Console.WriteLine(output);
                return false;
            }
        }

        private static bool ProcessSolution(string solutionFilePath, bool isPrimarySolution, string logFilePath)
        {
            var util = new SolutionCheckerUtil(solutionFilePath, isPrimarySolution);
            return CheckCore(util, $"Solution {solutionFilePath}", logFilePath);
        }

        private static bool ProcessGeneratedFiles(string repositoryDirectory, string logFilePath)
        {
            var checker = new GeneratedFilesCheckerUtil(repositoryDirectory);
            return CheckCore(checker, $"Generated files {repositoryDirectory}", logFilePath);
        }

        private static bool ProcessTargets(string repositoryDirectory, string logFilePath)
        {
            var targetsDirectory = Path.Combine(repositoryDirectory, @"eng\targets");
            var checker = new TargetsCheckerUtil(targetsDirectory);
            return CheckCore(checker, $"Targets {targetsDirectory}", logFilePath);
        }

        private static bool ProcessStructuredLog(string artifactsDirectory, string configuration, string checkLogFilePath)
        {
            var binaryLogFilePath = Path.Combine(artifactsDirectory, $@"log\{configuration}\Build.binlog");
            var util = new StructuredLoggerCheckerUtil(binaryLogFilePath);
            return CheckCore(util, $"Structured log {binaryLogFilePath}", checkLogFilePath);
        }

        private static bool ProcessPackages(string repositoryDirectory, string artifactsDirectory, string configuration, string logFilePath)
        {
            var util = new PackageContentsChecker(repositoryDirectory, artifactsDirectory, configuration);
            return CheckCore(util, $"NuPkg and VSIX files", logFilePath);
        }

        private static bool ProcessOptProf(string repositoryDirectory, string artifactsDirectory, string configuration, string logFilePath)
        {
            var util = new OptProfCheckerUtil(repositoryDirectory, artifactsDirectory, configuration);
            return CheckCore(util, $"OptProf inputs", logFilePath);
        }

        private static bool ProcessPackageInstall(string artifactsDirectory, string configuration, string logFilePath)
        {
            var util = new PackageInstallChecker(artifactsDirectory, configuration);
            return CheckCore(util, "NuGet package install", logFilePath);
        }
    }
}
