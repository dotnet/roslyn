// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace AnalyzerRunner
{
    internal class Options
    {
        public bool ConcurrentAnalysis;
        public bool ReportSuppressedDiagnostics;
        public HashSet<string> AnalyzerIds;
        public bool Stats;
        public string AnalyzerPath;
        public string SolutionPath;
        public bool UseAll;
        public bool ApplyChanges;
        public bool CodeFixes;
        public bool FixAll;
        public string LogFileName;

        private Options() { }

        internal static Options Create(string[] args)
        {
            // A valid call must have at least one parameter (a solution file). Optionally it can include /all or /id:SAXXXX.
            if (args.Length < 1)
            {
                Utilities.PrintHelp();
                return null;
            }

            var requiredArguments = args.Where(i => !i.StartsWith("/", StringComparison.Ordinal)).ToArray();
            if (requiredArguments.Length < 2)
            {
                Utilities.WriteLine("The application requires the following arguments: analyzer assembly path and solution path.", ConsoleColor.Red);
                return null;
            }

            var options = new Options()
            {
                ApplyChanges = args.Contains("/apply"),
                UseAll = args.Contains("/all"),
                Stats = args.Contains("/stats"),
                ConcurrentAnalysis = args.Contains("/concurrent"),
                ReportSuppressedDiagnostics = args.Contains("/suppressed"),
                AnalyzerPath = requiredArguments[0],
                SolutionPath = requiredArguments[1],
                CodeFixes = args.Contains("/codefixes"),
                FixAll = args.Contains("/fixall"),
            };

            if (options.ApplyChanges)
            {
                if (!args.Contains("/fixall"))
                {
                    Console.Error.WriteLine("Error: /apply can only be used with /fixall");
                    return null;
                }
            }

            string logArgument = args.FirstOrDefault(x => x.StartsWith("/log:"));
            if (logArgument != null)
            {
                options.LogFileName = logArgument.Substring(logArgument.IndexOf(':') + 1);
            }

            HashSet<string> ids = new HashSet<string>(args.Where(y => y.StartsWith("/id:", StringComparison.Ordinal)).Select(y => y.Substring(4)));
            options.AnalyzerIds = ids;

            return options;
        }
    }
}
