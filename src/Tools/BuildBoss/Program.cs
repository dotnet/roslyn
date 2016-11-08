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
            if (args.Length == 0)
            { 
                Usage();
                return 1;
            }

            var allGood = true;
            foreach (var arg in args)
            {
                if (SharedUtil.IsSolutionFile(arg))
                {
                    allGood &= ProcessSolution(arg);
                }
                else
                {
                    allGood &= ProcessTargets(arg);
                }
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

                // TODO: temporary work around util a cross cutting change can be sync'd up.  
                if (Path.GetFileName(projectEntry.RelativeFilePath) == "CompilerPerfTest.vbproj")
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

        private static bool ProcessTargets(string targets)
        {
            var checker = new TargetsCheckerUtil(targets);
            var textWriter = new StringWriter();
            if (checker.CheckAll(textWriter))
            {
                Console.WriteLine($"Processing {Path.GetFileName(targets)} passed");
                return true;
            }
            else
            {
                Console.WriteLine($"Processing {Path.GetFileName(targets)} FAILED");
                Console.WriteLine(textWriter.ToString());
                return false;
            }
        }

        private static void Usage()
        {
            Console.WriteLine($"BuildBoss <solution paths>");
        }
    }
}
