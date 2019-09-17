// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Shared.Options;

namespace AnalyzerRunner
{
    internal sealed class Options
    {
        public readonly string AnalyzerPath;
        public readonly string SolutionPath;
        public readonly ImmutableHashSet<string> AnalyzerNames;

        public readonly bool RunConcurrent;
        public readonly bool ReportSuppressedDiagnostics;
        public readonly bool ShowStats;
        public readonly bool UseAll;
        public readonly int Iterations;
        public readonly bool TestDocuments;
        public readonly Func<string, bool> TestDocumentMatch;
        public readonly int TestDocumentIterations;
        public readonly string LogFileName;
        public readonly string ProfileRoot;

        // Options specific to incremental analyzers
        public readonly bool UsePersistentStorage;
        public readonly BackgroundAnalysisScope AnalysisScope;
        public readonly ImmutableList<string> IncrementalAnalyzerNames;

        private Options(
            string analyzerPath,
            string solutionPath,
            ImmutableHashSet<string> analyzerIds,
            bool runConcurrent,
            bool reportSuppressedDiagnostics,
            bool showStats,
            bool useAll,
            int iterations,
            bool testDocuments,
            Func<string, bool> testDocumentMatch,
            int testDocumentIterations,
            string logFileName,
            string profileRoot,
            bool usePersistentStorage,
            BackgroundAnalysisScope analysisScope,
            ImmutableList<string> incrementalAnalyzerNames)
        {
            AnalyzerPath = analyzerPath;
            SolutionPath = solutionPath;
            AnalyzerNames = analyzerIds;
            RunConcurrent = runConcurrent;
            ReportSuppressedDiagnostics = reportSuppressedDiagnostics;
            ShowStats = showStats;
            UseAll = useAll;
            Iterations = iterations;
            TestDocuments = testDocuments;
            TestDocumentMatch = testDocumentMatch;
            TestDocumentIterations = testDocumentIterations;
            LogFileName = logFileName;
            ProfileRoot = profileRoot;
            UsePersistentStorage = usePersistentStorage;
            AnalysisScope = analysisScope;
            IncrementalAnalyzerNames = incrementalAnalyzerNames;
        }

        internal static Options Create(string[] args)
        {
            string analyzerPath = null;
            string solutionPath = null;
            var builder = ImmutableHashSet.CreateBuilder<string>();
            bool runConcurrent = false;
            bool reportSuppressedDiagnostics = false;
            bool showStats = false;
            bool useAll = false;
            int iterations = 1;
            bool testDocuments = false;
            Func<string, bool> testDocumentMatch = _ => true;
            int testDocumentIterations = 10;
            string logFileName = null;
            string profileRoot = null;
            var usePersistentStorage = false;
            var analysisScope = BackgroundAnalysisScope.OpenFiles;
            var incrementalAnalyzerNames = ImmutableList.CreateBuilder<string>();

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
                    case "/editperf":
                        testDocuments = true;
                        break;
                    case var _ when arg.StartsWith("/editperf:"):
                        testDocuments = true;
                        var expression = new Regex(arg.Substring("/editperf:".Length), RegexOptions.Compiled | RegexOptions.IgnoreCase);
                        testDocumentMatch = documentPath => expression.IsMatch(documentPath);
                        break;
                    case var _ when arg.StartsWith("/edititer:"):
                        testDocumentIterations = int.Parse(arg.Substring("/edititer:".Length));
                        break;
                    case var _ when arg.StartsWith("/iter:"):
                        iterations = int.Parse(arg.Substring("/iter:".Length));
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
                    case "/profileroot":
                        profileRoot = ReadValue();
                        break;
                    case "/persist":
                        usePersistentStorage = true;
                        break;
                    case "/fsa":
                        analysisScope = BackgroundAnalysisScope.FullSolution;
                        break;
                    case "/ia":
                        incrementalAnalyzerNames.Add(ReadValue());
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
                             "Unrecognized option " + arg :
                             "Unrecognized parameter " + arg));
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
                analyzerPath: analyzerPath,
                solutionPath: solutionPath,
                analyzerIds: builder.ToImmutableHashSet(),
                runConcurrent: runConcurrent,
                reportSuppressedDiagnostics: reportSuppressedDiagnostics,
                showStats: showStats,
                useAll: useAll,
                iterations: iterations,
                testDocuments: testDocuments,
                testDocumentMatch: testDocumentMatch,
                testDocumentIterations: testDocumentIterations,
                logFileName: logFileName,
                profileRoot: profileRoot,
                usePersistentStorage: usePersistentStorage,
                analysisScope: analysisScope,
                incrementalAnalyzerNames: incrementalAnalyzerNames.ToImmutable());
        }
    }
}
