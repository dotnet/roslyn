// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
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

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress +=
                (sender, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                };

            // QueryVisualStudioInstances returns Visual Studio installations on .NET Framework, and .NET Core SDK
            // installations on .NET Core. We use the one with the most recent version.
            var msBuildInstance = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(x => x.Version).First();

#if NETCOREAPP
            // Since we do not inherit msbuild.deps.json when referencing the SDK copy
            // of MSBuild and because the SDK no longer ships with version matched assemblies, we
            // register an assembly loader that will load assemblies from the msbuild path with
            // equal or higher version numbers than requested.
            LooseVersionAssemblyLoader.Register(msBuildInstance.MSBuildPath);
#endif

            MSBuildLocator.RegisterInstance(msBuildInstance);

            var incrementalAnalyzerRunner = new IncrementalAnalyzerRunner(options);
            var diagnosticAnalyzerRunner = new DiagnosticAnalyzerRunner(options);
            var codeRefactoringRunner = new CodeRefactoringRunner(options);
            if (!incrementalAnalyzerRunner.HasAnalyzers && !diagnosticAnalyzerRunner.HasAnalyzers && !codeRefactoringRunner.HasRefactorings)
            {
                WriteLine("No analyzers found", ConsoleColor.Red);
                PrintHelp();
                return;
            }

            var cancellationToken = cts.Token;

            if (!string.IsNullOrEmpty(options.ProfileRoot))
            {
                Directory.CreateDirectory(options.ProfileRoot);
                ProfileOptimization.SetProfileRoot(options.ProfileRoot);
            }

            var stopwatch = PerformanceTracker.StartNew();
            var properties = new Dictionary<string, string>
            {
#if NETCOREAPP
                // This property ensures that XAML files will be compiled in the current AppDomain
                // rather than a separate one. Any tasks isolated in AppDomains or tasks that create
                // AppDomains will likely not work due to https://github.com/Microsoft/MSBuildLocator/issues/16.
                { "AlwaysCompileMarkupFilesInSeparateDomain", bool.FalseString },
#endif
                // Use the latest language version to force the full set of available analyzers to run on the project.
                { "LangVersion", "latest" },
            };

            if (!string.IsNullOrEmpty(options.ProfileRoot))
            {
                ProfileOptimization.StartProfile(nameof(MSBuildWorkspace.OpenSolutionAsync));
            }

            using (MSBuildWorkspace workspace = MSBuildWorkspace.Create(properties, AnalyzerRunnerMefHostServices.DefaultServices))
            {
                Solution solution = await workspace.OpenSolutionAsync(options.SolutionPath, cancellationToken: cancellationToken).ConfigureAwait(false);
                var projectIds = solution.ProjectIds;

                foreach (var workspaceDiagnostic in workspace.Diagnostics)
                {
                    if (workspaceDiagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                    {
                        Console.WriteLine(workspaceDiagnostic.Message);
                    }
                }

                foreach (var projectId in projectIds)
                {
                    solution = solution.WithProjectAnalyzerReferences(projectId, ImmutableArray<AnalyzerReference>.Empty);
                }

                Console.WriteLine($"Loaded solution in {stopwatch.GetSummary(preciseMemory: true)}");

                if (options.ShowStats)
                {
                    stopwatch = PerformanceTracker.StartNew();

                    List<Project> projects = solution.Projects.Where(project => project.Language == LanguageNames.CSharp || project.Language == LanguageNames.VisualBasic).ToList();

                    Console.WriteLine("Number of projects:\t\t" + projects.Count);
                    Console.WriteLine("Number of documents:\t\t" + projects.Sum(x => x.DocumentIds.Count));

                    var statistics = GetSolutionStatistics(projects, cancellationToken);

                    Console.WriteLine("Number of syntax nodes:\t\t" + statistics.NumberofNodes);
                    Console.WriteLine("Number of syntax tokens:\t" + statistics.NumberOfTokens);
                    Console.WriteLine("Number of syntax trivia:\t" + statistics.NumberOfTrivia);

                    Console.WriteLine($"Statistics gathered in {stopwatch.GetSummary(preciseMemory: true)}");
                }

                if (options.ShowCompilerDiagnostics)
                {
                    var projects = solution.Projects.Where(project => project.Language == LanguageNames.CSharp || project.Language == LanguageNames.VisualBasic).ToList();

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

                Console.WriteLine("Pausing 5 seconds before starting analysis...");
                await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

                await incrementalAnalyzerRunner.RunAsync(workspace, cancellationToken).ConfigureAwait(false);
                await diagnosticAnalyzerRunner.RunAsync(workspace, cancellationToken).ConfigureAwait(false);
                await codeRefactoringRunner.RunAsync(workspace, cancellationToken).ConfigureAwait(false);
            }
        }

        private static Statistic GetSolutionStatistics(IEnumerable<Project> projects, CancellationToken cancellationToken)
        {
            ConcurrentBag<Statistic> sums = new ConcurrentBag<Statistic>();

            Parallel.ForEach(projects.SelectMany(project => project.Documents), document =>
            {
                var documentStatistics = GetSolutionStatisticsAsync(document, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
                sums.Add(documentStatistics);
            });

            Statistic sum = sums.Aggregate(new Statistic(0, 0, 0), (currentResult, value) => currentResult + value);
            return sum;
        }

        // TODO consider removing this and using GetAnalysisResultAsync
        // https://github.com/dotnet/roslyn/issues/23108
        private static async Task<Statistic> GetSolutionStatisticsAsync(Document document, CancellationToken cancellationToken)
        {
            SyntaxTree tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            SyntaxNode root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            var tokensAndNodes = root.DescendantNodesAndTokensAndSelf(descendIntoTrivia: true);

            int numberOfNodes = tokensAndNodes.Count(x => x.IsNode);
            int numberOfTokens = tokensAndNodes.Count(x => x.IsToken);
            int numberOfTrivia = root.DescendantTrivia(descendIntoTrivia: true).Count();

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
