using Mono.Options;
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
            bool isRelease = false;
            List<string> solutionFiles;

            var options = new OptionSet
            {
                { "r|root=", "The repository root", r => repositoryDirectory = r },
                { "release", "Use a release build", r => isRelease = true }
            };

            try
            {
                solutionFiles = options.Parse(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                options.WriteOptionDescriptions(Console.Out);
                return false;
            }

            if (string.IsNullOrEmpty(repositoryDirectory))
            {
                repositoryDirectory = AppContext.BaseDirectory;
            }

            return Go(repositoryDirectory, isRelease, solutionFiles);
        }

        private static bool Go(string repositoryDirectory, bool isRelease, List<string> solutionFileNames)
        { 
            var allGood = true;
            foreach (var solutionFileName in solutionFileNames)
            {
                allGood &= ProcessSolution(Path.Combine(repositoryDirectory, solutionFileName));
            }

            var configDirectory = Path.Combine(repositoryDirectory, "Binaries");
            configDirectory = Path.Combine(configDirectory, isRelease ? "Release" : "Debug");

            allGood &= ProcessStructuredLog(configDirectory);
            allGood &= ProcessTargets(repositoryDirectory);
            allGood &= ProcessCompilerNuGet(repositoryDirectory, configDirectory);

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
            var targetsDirectory = Path.Combine(repositoryDirectory, @"build\Targets");
            var checker = new TargetsCheckerUtil(targetsDirectory);
            return CheckCore(checker, $"Targets {targetsDirectory}");
        }

        private static bool ProcessStructuredLog(string configDirectory)
        {
            var logFilePath = Path.Combine(configDirectory, @"Logs\Roslyn.binlog");
            var util = new StructuredLoggerCheckerUtil(logFilePath);
            return CheckCore(util, $"Structured log {logFilePath}");
        }

        private static bool ProcessCompilerNuGet(string repositoryDirectory, string configDirectory)
        {
            var util = new CompilerNuGetCheckerUtil(repositoryDirectory, configDirectory);
            return CheckCore(util, $"Compiler NuGets");
        }
    }
}
