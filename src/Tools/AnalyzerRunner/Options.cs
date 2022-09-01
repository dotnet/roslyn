// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace AnalyzerRunner
{
    public sealed class Options
    {
        public readonly string AnalyzerPath;
        public readonly string SolutionPath;
        public readonly ImmutableHashSet<string> AnalyzerNames;
        public readonly ImmutableHashSet<string> RefactoringNodes;

        public readonly bool RunConcurrent;
        public readonly bool ReportSuppressedDiagnostics;
        public readonly bool ApplyChanges;
        public readonly bool UseAll;
        public readonly int Iterations;

        // Options specific to incremental analyzers
        public readonly bool UsePersistentStorage;
        public readonly ImmutableArray<string> IncrementalAnalyzerNames;
        public readonly bool FullSolutionAnalysis;

        // Options used by AnalyzerRunner CLI only
        internal readonly bool ShowStats;
        internal readonly bool ShowCompilerDiagnostics;
        internal readonly bool TestDocuments;
        internal readonly Func<string, bool> TestDocumentMatch;
        internal readonly int TestDocumentIterations;
        internal readonly string LogFileName;
        internal readonly string ProfileRoot;

        internal IdeAnalyzerOptions IdeOptions
            => IdeAnalyzerOptions.Default;

        internal BackgroundAnalysisScope AnalysisScope
            => FullSolutionAnalysis ? BackgroundAnalysisScope.FullSolution : BackgroundAnalysisScope.OpenFiles;

        public Options(
            string analyzerPath,
            string solutionPath,
            ImmutableHashSet<string> analyzerIds,
            ImmutableHashSet<string> refactoringNodes,
            bool runConcurrent,
            bool reportSuppressedDiagnostics,
            bool applyChanges,
            bool useAll,
            int iterations,
            bool usePersistentStorage,
            bool fullSolutionAnalysis,
            ImmutableArray<string> incrementalAnalyzerNames)
            : this(analyzerPath,
                  solutionPath,
                  analyzerIds,
                  refactoringNodes,
                  runConcurrent,
                  reportSuppressedDiagnostics,
                  applyChanges,
                  showStats: false,
                  showCompilerDiagnostics: false,
                  useAll,
                  iterations,
                  testDocuments: false,
                  testDocumentMatch: _ => false,
                  testDocumentIterations: 0,
                  logFileName: null,
                  profileRoot: null,
                  usePersistentStorage,
                  fullSolutionAnalysis,
                  incrementalAnalyzerNames)
        { }

        internal Options(
            string analyzerPath,
            string solutionPath,
            ImmutableHashSet<string> analyzerIds,
            ImmutableHashSet<string> refactoringNodes,
            bool runConcurrent,
            bool reportSuppressedDiagnostics,
            bool applyChanges,
            bool showStats,
            bool showCompilerDiagnostics,
            bool useAll,
            int iterations,
            bool testDocuments,
            Func<string, bool> testDocumentMatch,
            int testDocumentIterations,
            string logFileName,
            string profileRoot,
            bool usePersistentStorage,
            bool fullSolutionAnalysis,
            ImmutableArray<string> incrementalAnalyzerNames)
        {
            AnalyzerPath = analyzerPath;
            SolutionPath = solutionPath;
            AnalyzerNames = analyzerIds;
            RefactoringNodes = refactoringNodes;
            RunConcurrent = runConcurrent;
            ReportSuppressedDiagnostics = reportSuppressedDiagnostics;
            ApplyChanges = applyChanges;
            ShowStats = showStats;
            ShowCompilerDiagnostics = showCompilerDiagnostics;
            UseAll = useAll;
            Iterations = iterations;
            TestDocuments = testDocuments;
            TestDocumentMatch = testDocumentMatch;
            TestDocumentIterations = testDocumentIterations;
            LogFileName = logFileName;
            ProfileRoot = profileRoot;
            UsePersistentStorage = usePersistentStorage;
            FullSolutionAnalysis = fullSolutionAnalysis;
            IncrementalAnalyzerNames = incrementalAnalyzerNames;
        }

        internal static Options Create(string[] args)
        {
            string analyzerPath = null;
            string solutionPath = null;
            var builder = ImmutableHashSet.CreateBuilder<string>();
            var refactoringBuilder = ImmutableHashSet.CreateBuilder<string>();
            bool runConcurrent = false;
            bool reportSuppressedDiagnostics = false;
            bool applyChanges = false;
            bool showStats = false;
            bool showCompilerDiagnostics = false;
            bool useAll = false;
            int iterations = 1;
            bool testDocuments = false;
            Func<string, bool> testDocumentMatch = _ => true;
            int testDocumentIterations = 10;
            string logFileName = null;
            string profileRoot = null;
            var usePersistentStorage = false;
            var fullSolutionAnalysis = false;
            var incrementalAnalyzerNames = ImmutableArray.CreateBuilder<string>();

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
                    case "/compilerStats":
                        showCompilerDiagnostics = true;
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
                    case "/apply":
                        applyChanges = true;
                        break;
                    case "/a":
                        builder.Add(ReadValue());
                        break;
                    case "/refactor":
                        refactoringBuilder.Add(ReadValue());
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
                        fullSolutionAnalysis = true;
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
                refactoringNodes: refactoringBuilder.ToImmutableHashSet(),
                runConcurrent: runConcurrent,
                reportSuppressedDiagnostics: reportSuppressedDiagnostics,
                applyChanges: applyChanges,
                showStats: showStats,
                showCompilerDiagnostics: showCompilerDiagnostics,
                useAll: useAll,
                iterations: iterations,
                testDocuments: testDocuments,
                testDocumentMatch: testDocumentMatch,
                testDocumentIterations: testDocumentIterations,
                logFileName: logFileName,
                profileRoot: profileRoot,
                usePersistentStorage: usePersistentStorage,
                fullSolutionAnalysis: fullSolutionAnalysis,
                incrementalAnalyzerNames: incrementalAnalyzerNames.ToImmutable());
        }
    }
}
