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
                else if (Path.GetExtension(arg) == ".xml")
                {
                    allGood &= ProcessStructuredLog(arg);
                }
                else
                {
                    allGood &= ProcessTargets(arg);
                }
            }

            if (!allGood)
            {
                Console.WriteLine("Failed");
            }

            return allGood ? 0 : 1;
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

        private static bool ProcessTargets(string targets)
        {
            var checker = new TargetsCheckerUtil(targets);
            return CheckCore(checker, $"Targets {targets}");
        }

        private static bool ProcessStructuredLog(string logFilePath)
        {
            var util = new StructuredLoggerCheckerUtil(XDocument.Load(logFilePath));
            return CheckCore(util, $"Structured log {logFilePath}");
        }

        private static void Usage()
        {
            Console.WriteLine($"BuildBoss <solution paths>");
        }
    }
}
