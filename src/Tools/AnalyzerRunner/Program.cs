// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;

namespace AnalyzerRunner
{
    /// <summary>
    /// AnalyzerRunner is a tool that will analyze a solution, find diagnostics in it and will print out the number of
    /// diagnostics it could find. This is useful to easily test performance without having the overhead of visual
    /// studio running.
    /// </summary>
    class Program
    {
        public static async Task Main(string[] args)
        {
            Options options;
            try
            {
                options = Options.Create(args);
            }
            catch (InvalidDataException)
            {
                PrintHelp();
                return;
            }

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress +=
                (sender, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                };

            var cancellationToken = cts.Token;

            if (!string.IsNullOrEmpty(options.ProfileRoot))
            {
                Directory.CreateDirectory(options.ProfileRoot);
                ProfileOptimization.SetProfileRoot(options.ProfileRoot);
            }

            using var workspace = AnalyzerRunnerHelper.CreateWorkspace();

            var incrementalAnalyzerRunner = new IncrementalAnalyzerRunner(workspace, options);
            var diagnosticAnalyzerRunner = new DiagnosticAnalyzerRunner(workspace, options);
            var codeRefactoringRunner = new CodeRefactoringRunner(workspace, options);

            if (!incrementalAnalyzerRunner.HasAnalyzers && !diagnosticAnalyzerRunner.HasAnalyzers && !codeRefactoringRunner.HasRefactorings)
            {
                WriteLine("No analyzers found", ConsoleColor.Red);
                PrintHelp();
                return;
            }

            var stopwatch = PerformanceTracker.StartNew();

            if (!string.IsNullOrEmpty(options.ProfileRoot))
            {
                ProfileOptimization.StartProfile(nameof(MSBuildWorkspace.OpenSolutionAsync));
            }

            await workspace.OpenSolutionAsync(options.SolutionPath, progress: null, cancellationToken).ConfigureAwait(false);

            foreach (var workspaceDiagnostic in workspace.Diagnostics)
            {
                if (workspaceDiagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                {
                    Console.WriteLine(workspaceDiagnostic.Message);
                }
            }

            Console.WriteLine($"Loaded solution in {stopwatch.GetSummary(preciseMemory: true)}");

            if (options.ShowStats)
            {
                stopwatch = PerformanceTracker.StartNew();
                ShowSolutionStatistics(workspace.CurrentSolution, cancellationToken);
                Console.WriteLine($"Statistics gathered in {stopwatch.GetSummary(preciseMemory: true)}");
            }

            if (options.ShowCompilerDiagnostics)
            {
                await ShowCompilerDiagnosticsAsync(workspace.CurrentSolution, cancellationToken).ConfigureAwait(false);
            }

            Console.WriteLine("Pausing 5 seconds before starting analysis...");
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

            if (incrementalAnalyzerRunner.HasAnalyzers)
            {
                if (!string.IsNullOrEmpty(options.ProfileRoot))
                {
                    ProfileOptimization.StartProfile("IncrementalAnalyzer");
                }

                await incrementalAnalyzerRunner.RunAsync(cancellationToken).ConfigureAwait(false);
            }

            if (diagnosticAnalyzerRunner.HasAnalyzers)
            {
                if (!string.IsNullOrEmpty(options.ProfileRoot))
                {
                    ProfileOptimization.StartProfile(nameof(DiagnosticAnalyzerRunner));
                }

                await diagnosticAnalyzerRunner.RunAllAsync(cancellationToken).ConfigureAwait(false);
            }

            if (codeRefactoringRunner.HasRefactorings)
            {
                if (!string.IsNullOrEmpty(options.ProfileRoot))
                {
                    ProfileOptimization.StartProfile(nameof(CodeRefactoringRunner));
                }

                await codeRefactoringRunner.RunAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task ShowCompilerDiagnosticsAsync(Solution solution, CancellationToken cancellationToken)
        {
            var projectIds = solution.ProjectIds;

            foreach (var projectId in projectIds)
            {
                solution = solution.WithProjectAnalyzerReferences(projectId, ImmutableArray<AnalyzerReference>.Empty);
            }

            var projects = solution.Projects.Where(project => project.Language is LanguageNames.CSharp or LanguageNames.VisualBasic).ToList();

            var diagnosticStatistics = new Dictionary<string, (string description, DiagnosticSeverity severity, int count)>();
            foreach (var project in projects)
            {
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                foreach (var diagnostic in compilation.GetDiagnostics(cancellationToken))
                {
                    diagnosticStatistics.TryGetValue(diagnostic.Id, out var existing);
                    var description = existing.description;
                    if (string.IsNullOrEmpty(description))
                    {
                        description = diagnostic.Descriptor?.Title.ToString();
                        if (string.IsNullOrEmpty(description))
                        {
                            description = diagnostic.Descriptor?.MessageFormat.ToString();
                        }
                    }

                    diagnosticStatistics[diagnostic.Id] = (description, diagnostic.Descriptor.DefaultSeverity, existing.count + 1);
                }
            }

            foreach (var pair in diagnosticStatistics)
            {
                Console.WriteLine($"  {pair.Value.severity} {pair.Key}: {pair.Value.count} instances ({pair.Value.description})");
            }
        }

        private static void ShowSolutionStatistics(Solution solution, CancellationToken cancellationToken)
        {
            var projects = solution.Projects.Where(project => project.Language is LanguageNames.CSharp or LanguageNames.VisualBasic).ToList();

            Console.WriteLine("Number of projects:\t\t" + projects.Count);
            Console.WriteLine("Number of documents:\t\t" + projects.Sum(x => x.DocumentIds.Count));

            var statistics = GetSolutionStatistics(projects, cancellationToken);

            Console.WriteLine("Number of syntax nodes:\t\t" + statistics.NumberofNodes);
            Console.WriteLine("Number of syntax tokens:\t" + statistics.NumberOfTokens);
            Console.WriteLine("Number of syntax trivia:\t" + statistics.NumberOfTrivia);
        }

        private static Statistic GetSolutionStatistics(IEnumerable<Project> projects, CancellationToken cancellationToken)
        {
            var sums = new ConcurrentBag<Statistic>();

            Parallel.ForEach(projects.SelectMany(project => project.Documents), document =>
            {
                var documentStatistics = GetSolutionStatisticsAsync(document, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
                sums.Add(documentStatistics);
            });

            var sum = sums.Aggregate(new Statistic(0, 0, 0), (currentResult, value) => currentResult + value);
            return sum;
        }

        // TODO consider removing this and using GetAnalysisResultAsync
        // https://github.com/dotnet/roslyn/issues/23108
        private static async Task<Statistic> GetSolutionStatisticsAsync(Document document, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            var tokensAndNodes = root.DescendantNodesAndTokensAndSelf(descendIntoTrivia: true);

            var numberOfNodes = tokensAndNodes.Count(x => x.IsNode);
            var numberOfTokens = tokensAndNodes.Count(x => x.IsToken);
            var numberOfTrivia = root.DescendantTrivia(descendIntoTrivia: true).Count();

            return new Statistic(numberOfNodes, numberOfTokens, numberOfTrivia);
        }

        internal static void WriteLine(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        internal static void PrintHelp()
        {
            Console.WriteLine("Usage: AnalyzerRunner <AnalyzerAssemblyOrFolder> <Solution> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("/all                 Run all analyzers, including ones that are disabled by default");
            Console.WriteLine("/stats               Display statistics of the solution");
            Console.WriteLine("/a <analyzer name>   Enable analyzer with <analyzer name> (when this is specified, only analyzers specificed are enabled. Use: /a <name1> /a <name2>, etc.");
            Console.WriteLine("/concurrent          Executes analyzers in concurrent mode");
            Console.WriteLine("/suppressed          Reports suppressed diagnostics");
            Console.WriteLine("/log <logFile>       Write logs into the log file specified");
            Console.WriteLine("/editperf[:<match>]     Test the incremental performance of analyzers to simulate the behavior of editing files. If <match> is specified, only files matching this regular expression are evaluated for editor performance.");
            Console.WriteLine("/edititer:<iterations>  Specifies the number of iterations to use for testing documents with /editperf. When this is not specified, the default value is 10.");
            Console.WriteLine("/persist             Enable persistent storage (e.g. SQLite; only applies to IIncrementalAnalyzer testing)");
            Console.WriteLine("/fsa                 Enable full solution analysis (only applies to IIncrementalAnalyzer testing)");
            Console.WriteLine("/ia <analyzer name>  Enable incremental analyzer with <analyzer name> (when this is specified, only incremental analyzers specified are enabled. Use: /ia <name1> /ia <name2>, etc.");
        }
    }
}
