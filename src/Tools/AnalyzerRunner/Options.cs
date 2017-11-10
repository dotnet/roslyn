// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;

namespace AnalyzerRunner
{
    internal class Options
    {
        public readonly string AnalyzerPath;
        public readonly string SolutionPath;
        public readonly ImmutableHashSet<string> AnalyzerNames;

        public readonly bool RunConcurrent;
        public readonly bool ReportSuppressedDiagnostics;
        public readonly bool ShowStats;
        public readonly bool UseAll;
        public readonly string LogFileName;

        public Options(
            string analyzerPath,
            string solutionPath,
            ImmutableHashSet<string> analyzerIds,
            bool runConcurrent,
            bool reportSuppressedDiagnostics,
            bool showStats,
            bool useAll,
            string logFileName)
        {
            AnalyzerPath = analyzerPath;
            SolutionPath = solutionPath;
            AnalyzerNames = analyzerIds;
            RunConcurrent = runConcurrent;
            ReportSuppressedDiagnostics = reportSuppressedDiagnostics;
            ShowStats = showStats;
            UseAll = useAll;
            LogFileName = logFileName;
        }

        internal static Options TryCreate(string[] args)
        {
            string analyzerPath = null;
            string solutionPath = null;
            var builder = ImmutableHashSet.CreateBuilder<string>();
            bool runConcurrent = false;
            bool reportSuppressedDiagnostics = false;
            bool showStats = false;
            bool useAll = false;
            string logFileName = null;

            int i = 0;
            while (i < args.Length)
            {
                var arg = args[i++];
                string ReadValue() => (i < args.Length) ? args[i++] : throw new InvalidDataException($"Missing value for option {arg}");

                switch (arg)
                {
                    case "/all":
                        useAll = true;
                        break;
                    case "/stats":
                        showStats = true;
                        break;
                    case "/concurrent":
                        runConcurrent = true;
                        break;
                    case "/suppressed":
                        reportSuppressedDiagnostics = true;
                        break;
                    case "/a":
                        builder.Add(ReadValue());
                        break;
                    case "/log":
                        logFileName = ReadValue();
                        break;
                    default:
                        if (analyzerPath == null)
                        {
                            analyzerPath = arg;
                        }
                        else if (solutionPath == null)
                        {
                            solutionPath = arg;
                        }
                        else
                        {
                            throw new InvalidDataException((arg.StartsWith("/", StringComparison.Ordinal) ?
                             $"Unrecognized option {arg}" :
                             $"Unrecognized parameter {arg}"));
                        }
                        break;
                }
            }

            if (analyzerPath == null)
            {
                throw new InvalidDataException("Missing analyzer path");
            }

            if (solutionPath == null)
            {
                throw new InvalidDataException("Missing solution path");
            }

            return new Options(
                analyzerPath,
                solutionPath,
                builder.ToImmutableHashSet(),
                useAll,
                showStats,
                runConcurrent,
                reportSuppressedDiagnostics,
                logFileName);
        }
    }
}
