// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.MSBuild;
using File = System.IO.File;
using Path = System.IO.Path;

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

            var analyzers = GetDiagnosticAnalyzers(options.AnalyzerPath);
            analyzers = FilterAnalyzers(analyzers, options).ToImmutableArray();

            if (analyzers.Length == 0)
            {
                WriteLine("No analyzers found", ConsoleColor.Red);
                PrintHelp();
                return;
            }
            var cancellationToken = cts.Token;

            Stopwatch stopwatch = Stopwatch.StartNew();
            var properties = new Dictionary<string, string>
            {
                { "VSToolsPath", Environment.GetEnvironmentVariable("VSToolsPath") },
                { "LangVersion", "latest" }
            };
            using (MSBuildWorkspace workspace = MSBuildWorkspace.Create(properties))
            {
                Solution solution = await workspace.OpenSolutionAsync(options.SolutionPath, cancellationToken).ConfigureAwait(false);
                var projectIds = solution.ProjectIds;

                foreach (var projectId in projectIds)
                {
                    solution = solution.WithProjectAnalyzerReferences(projectId, ImmutableArray<AnalyzerReference>.Empty);
                }

                Console.WriteLine($"Loaded solution in {stopwatch.ElapsedMilliseconds}ms");

                if (options.ShowStats)
                {
                    // TODO The tool support CSharp projects for now. It should support VB/FSharp as well
                    // https://github.com/dotnet/roslyn/issues/23108
                    List<Project> csharpProjects = solution.Projects.Where(project => project.Language == LanguageNames.CSharp).ToList();

                    Console.WriteLine("Number of projects:\t\t" + csharpProjects.Count);
                    Console.WriteLine("Number of documents:\t\t" + csharpProjects.Sum(x => x.DocumentIds.Count));

                    var statistics = GetSolutionStatistics(csharpProjects, cancellationToken);

                    Console.WriteLine("Number of syntax nodes:\t\t" + statistics.NumberofNodes);
                    Console.WriteLine("Number of syntax tokens:\t" + statistics.NumberOfTokens);
                    Console.WriteLine("Number of syntax trivia:\t" + statistics.NumberOfTrivia);
                }

                Console.WriteLine("Pausing 5 seconds before starting analysis...");
                await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

                stopwatch.Restart();

                var analysisResult = await GetAnalysisResultAsync(solution, analyzers, options, cancellationToken).ConfigureAwait(true);
                var allDiagnostics = analysisResult.Where(pair => pair.Value != null).SelectMany(pair => pair.Value.GetAllDiagnostics()).ToImmutableArray();

                Console.WriteLine($"Found {allDiagnostics.Length} diagnostics in {stopwatch.ElapsedMilliseconds}ms");
                WriteTelemetry(analysisResult);

                if (options.TestDocuments)
                {
                    var projectPerformance = new Dictionary<ProjectId, double>();
                    var documentPerformance = new Dictionary<DocumentId, DocumentAnalyzerPerformance>();
                    foreach (var projectId in solution.ProjectIds)
                    {
                        var project = solution.GetProject(projectId);
                        if (project.Language != LanguageNames.CSharp)
                        {
                            continue;
                        }

                        foreach (var documentId in project.DocumentIds)
                        {
                            var document = project.GetDocument(documentId);
                            if (!options.TestDocumentMatch(document.FilePath))
                            {
                                continue;
                            }

                            var currentDocumentPerformance = await TestDocumentPerformanceAsync(analyzers, project, documentId, options, cancellationToken).ConfigureAwait(false);
                            Console.WriteLine($"{document.FilePath ?? document.Name}: {currentDocumentPerformance.EditsPerSecond:0.00}");
                            documentPerformance.Add(documentId, currentDocumentPerformance);
                        }

                        var sumOfDocumentAverages = documentPerformance.Where(x => x.Key.ProjectId == projectId).Sum(x => x.Value.EditsPerSecond);
                        double documentCount = documentPerformance.Where(x => x.Key.ProjectId == projectId).Count();
                        if (documentCount > 0)
                        {
                            projectPerformance[project.Id] = sumOfDocumentAverages / documentCount;
                        }
                    }

                    var slowestFiles = documentPerformance.OrderBy(pair => pair.Value.EditsPerSecond).GroupBy(pair => pair.Key.ProjectId);
                    Console.WriteLine("Slowest files in each project:");
                    foreach (var projectGroup in slowestFiles)
                    {
                        Console.WriteLine($"  {solution.GetProject(projectGroup.Key).Name}");
                        foreach (var pair in projectGroup.Take(5))
                        {
                            var document = solution.GetDocument(pair.Key);
                            Console.WriteLine($"    {document.FilePath ?? document.Name}: {pair.Value.EditsPerSecond:0.00}");
                        }
                    }

                    foreach (var projectId in solution.ProjectIds)
                    {
                        if (!projectPerformance.TryGetValue(projectId, out var averageEditsInProject))
                        {
                            continue;
                        }

                        var project = solution.GetProject(projectId);
                        Console.WriteLine($"{project.Name} ({project.DocumentIds.Count} documents): {averageEditsInProject:0.00} edits per second");
                    }
                }

                foreach (var group in allDiagnostics.GroupBy(diagnostic => diagnostic.Id).OrderBy(diagnosticGroup => diagnosticGroup.Key, StringComparer.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"  {group.Key}: {group.Count()} instances");

                    // Print out analyzer diagnostics like AD0001 for analyzer exceptions
                    if (group.Key.StartsWith("AD", StringComparison.Ordinal))
                    {
                        foreach (var item in group)
                        {
                            Console.WriteLine(item);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(options.LogFileName))
                {
                    WriteDiagnosticResults(analysisResult.SelectMany(pair => pair.Value.GetAllDiagnostics().Select(j => Tuple.Create(pair.Key, j))).ToImmutableArray(), options.LogFileName);
                }
            }
        }

        private static async Task<DocumentAnalyzerPerformance> TestDocumentPerformanceAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, Project project, DocumentId documentId, Options analyzerOptionsInternal, CancellationToken cancellationToken)
        {
            // update the project compilation options
            var modifiedSpecificDiagnosticOptions = project.CompilationOptions.SpecificDiagnosticOptions
                .Add("AD0001", ReportDiagnostic.Error)
                .Add("AD0002", ReportDiagnostic.Error);
            // Report exceptions during the analysis process as errors
            var modifiedCompilationOptions = project.CompilationOptions.WithSpecificDiagnosticOptions(modifiedSpecificDiagnosticOptions);
            var processedProject = project.WithCompilationOptions(modifiedCompilationOptions);

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < analyzerOptionsInternal.TestDocumentIterations; i++)
            {
                Compilation compilation = await processedProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                var workspaceAnalyzerOptions = new WorkspaceAnalyzerOptions(project.AnalyzerOptions, project.Solution.Options, project.Solution);
                CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(analyzers, new CompilationWithAnalyzersOptions(workspaceAnalyzerOptions, null, analyzerOptionsInternal.RunConcurrent, logAnalyzerExecutionTime: true, reportSuppressedDiagnostics: analyzerOptionsInternal.ReportSuppressedDiagnostics));

                SyntaxTree tree = await project.GetDocument(documentId).GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                await compilationWithAnalyzers.GetAnalyzerSyntaxDiagnosticsAsync(tree, cancellationToken).ConfigureAwait(false);
                await compilationWithAnalyzers.GetAnalyzerSemanticDiagnosticsAsync(compilation.GetSemanticModel(tree), null, cancellationToken).ConfigureAwait(false);
            }

            return new DocumentAnalyzerPerformance(analyzerOptionsInternal.TestDocumentIterations / stopwatch.Elapsed.TotalSeconds);
        }

        private static void WriteDiagnosticResults(ImmutableArray<Tuple<ProjectId, Diagnostic>> diagnostics, string fileName)
        {
            var orderedDiagnostics =
                diagnostics
                .OrderBy(tuple => tuple.Item2.Id)
                .ThenBy(tuple => tuple.Item2.Location.SourceTree?.FilePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(tuple => tuple.Item2.Location.SourceSpan.Start)
                .ThenBy(tuple => tuple.Item2.Location.SourceSpan.End);

            var uniqueLines = new HashSet<string>();
            StringBuilder completeOutput = new StringBuilder();
            StringBuilder uniqueOutput = new StringBuilder();
            foreach (var diagnostic in orderedDiagnostics)
            {
                string message = diagnostic.Item2.ToString();
                string uniqueMessage = $"{diagnostic.Item1}: {diagnostic.Item2}";
                completeOutput.AppendLine(message);
                if (uniqueLines.Add(uniqueMessage))
                {
                    uniqueOutput.AppendLine(message);
                }
            }

            string directoryName = Path.GetDirectoryName(fileName);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            string uniqueFileName = Path.Combine(directoryName, $"{fileNameWithoutExtension}-Unique{extension}");

            File.WriteAllText(fileName, completeOutput.ToString(), Encoding.UTF8);
            File.WriteAllText(uniqueFileName, uniqueOutput.ToString(), Encoding.UTF8);
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

        private static IEnumerable<DiagnosticAnalyzer> FilterAnalyzers(IEnumerable<DiagnosticAnalyzer> analyzers, Options options)
        {
            var analyzerTypes = new HashSet<Type>();

            foreach (var analyzer in analyzers)
            {
                if (!analyzerTypes.Add(analyzer.GetType()))
                {
                    // Avoid running the same analyzer multiple times
                    continue;
                }

                if (options.UseAll)
                {
                    yield return analyzer;
                }
                else if (options.AnalyzerNames.Count == 0)
                {
                    if (analyzer.SupportedDiagnostics.Any(diagnosticDescriptor => diagnosticDescriptor.IsEnabledByDefault))
                    {
                        yield return analyzer;
                    }
                }
                else if (options.AnalyzerNames.Contains(analyzer.GetType().Name))
                {
                    yield return analyzer;
                }
            }
        }

        private static ImmutableArray<DiagnosticAnalyzer> GetDiagnosticAnalyzers(string path)
        {
            if (File.Exists(path))
            {
                return GetDiagnosticAnalyzersFromFile(path);
            }
            else if (Directory.Exists(path))
            {
                return Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories).SelectMany(file => GetDiagnosticAnalyzersFromFile(file)).ToImmutableArray();
            }

            throw new InvalidDataException($"Cannot find {path}.");
        }

        private static ImmutableArray<DiagnosticAnalyzer> GetDiagnosticAnalyzersFromFile(string path)
        {
            var analyzerReference = new AnalyzerFileReference(path, AssemblyLoader.Instance);
            var analyzers = analyzerReference.GetAnalyzersForAllLanguages();
            return analyzers;
        }

        private static async Task<ImmutableDictionary<ProjectId, AnalysisResult>> GetAnalysisResultAsync(
            Solution solution,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            Options options,
            CancellationToken cancellationToken)
        {
            var projectDiagnosticBuilder = ImmutableDictionary.CreateBuilder<ProjectId, AnalysisResult>();

            for (var i = 0; i < options.Iterations; i++)
            {
                var projectDiagnosticTasks = new List<KeyValuePair<ProjectId, Task<AnalysisResult>>>();

                // Make sure we analyze the projects in parallel
                foreach (var project in solution.Projects)
                {
                    // TODO The tool support CSharp projects for now. It should support VB/FSharp as well
                    // https://github.com/dotnet/roslyn/issues/23108
                    if (project.Language != LanguageNames.CSharp)
                    {
                        continue;
                    }

                    projectDiagnosticTasks.Add(new KeyValuePair<ProjectId, Task<AnalysisResult>>(
                        project.Id,
                        GetProjectAnalysisResultAsync(
                            analyzers,
                            project,
                            options,
                            cancellationToken)));
                }

                foreach (var task in projectDiagnosticTasks)
                {
                    var result = await task.Value.ConfigureAwait(false);
                    if (result == null)
                    {
                        continue;
                    }

                    if (projectDiagnosticBuilder.TryGetValue(task.Key, out var previousResult))
                    {
                        foreach (var pair in previousResult.AnalyzerTelemetryInfo)
                        {
                            result.AnalyzerTelemetryInfo[pair.Key].ExecutionTime += pair.Value.ExecutionTime;
                        }
                    }

                    projectDiagnosticBuilder[task.Key] = result;
                }
            }

            return projectDiagnosticBuilder.ToImmutable();
        }

        /// <summary>
        /// Returns a list of all analysis results inside the specific project. This is an asynchronous operation.
        /// </summary>
        /// <param name="analyzers">The list of analyzers that should be used</param>
        /// <param name="project">The project that should be analyzed
        /// <see langword="false"/> to use the behavior configured for the specified <paramref name="project"/>.</param>
        /// <param name="cancellationToken">The cancellation token that the task will observe.</param>
        /// <returns>A list of analysis results inside the project</returns>
        private static async Task<AnalysisResult> GetProjectAnalysisResultAsync(
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            Project project,
            Options analyzerOptionsInternal,
            CancellationToken cancellationToken)
        {
            WriteLine($"Running analyzers for {project.Name}", ConsoleColor.Gray);

            // update the project compilation options
            var modifiedSpecificDiagnosticOptions = project.CompilationOptions.SpecificDiagnosticOptions
                .Add("AD0001", ReportDiagnostic.Error)
                .Add("AD0002", ReportDiagnostic.Error);
            // Report exceptions during the analysis process as errors
            var modifiedCompilationOptions = project.CompilationOptions.WithSpecificDiagnosticOptions(modifiedSpecificDiagnosticOptions);
            var processedProject = project.WithCompilationOptions(modifiedCompilationOptions);

            try
            {
                Compilation compilation = await processedProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var newCompilation = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(compilation.SyntaxTrees);

                var workspaceAnalyzerOptions = new WorkspaceAnalyzerOptions(project.AnalyzerOptions, project.Solution.Options, project.Solution);
                CompilationWithAnalyzers compilationWithAnalyzers = newCompilation.WithAnalyzers(analyzers, new CompilationWithAnalyzersOptions(workspaceAnalyzerOptions, null, analyzerOptionsInternal.RunConcurrent, logAnalyzerExecutionTime: true, reportSuppressedDiagnostics: analyzerOptionsInternal.ReportSuppressedDiagnostics));
                var analystResult = await compilationWithAnalyzers.GetAnalysisResultAsync(cancellationToken).ConfigureAwait(false);
                return analystResult;
            }
            catch (Exception e)
            {
                WriteLine($"Failed to analyze {project.Name} with {e.ToString()}", ConsoleColor.Red);
                return null;
            }
        }

        private static void WriteTelemetry(ImmutableDictionary<ProjectId, AnalysisResult> dictionary)
        {
            var telemetryInfoDictionary = new Dictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo>();
            foreach (var analysisResult in dictionary.Values)
            {
                foreach (var pair in analysisResult.AnalyzerTelemetryInfo)
                {
                    if (!telemetryInfoDictionary.TryGetValue(pair.Key, out var telemetry))
                    {
                        telemetryInfoDictionary.Add(pair.Key, pair.Value);
                    }
                    else
                    {
                        telemetry.Add(pair.Value);
                    }
                }
            }

            foreach (var pair in telemetryInfoDictionary.OrderBy(x => x.Key.GetType().Name, StringComparer.OrdinalIgnoreCase))
            {
                WriteTelemetry(pair.Key.GetType().Name, pair.Value);
            }

            WriteLine($"Execution times (ms):", ConsoleColor.DarkCyan);
            foreach (var pair in telemetryInfoDictionary.OrderBy(x => x.Key.GetType().Name, StringComparer.OrdinalIgnoreCase))
            {
                WriteExecutionTimes(pair.Key.GetType().Name, pair.Value);
            }
        }

        private static void WriteTelemetry(string analyzerName, AnalyzerTelemetryInfo telemetry)
        {
            WriteLine($"Statistics for {analyzerName}:", ConsoleColor.DarkCyan);
            WriteLine($"Concurrent:                     {telemetry.Concurrent}", telemetry.Concurrent ? ConsoleColor.White : ConsoleColor.DarkRed);
            WriteLine($"Execution time (ms):            {telemetry.ExecutionTime.TotalMilliseconds}", ConsoleColor.White);

            WriteLine($"Code Block Actions:             {telemetry.CodeBlockActionsCount}", ConsoleColor.White);
            WriteLine($"Code Block Start Actions:       {telemetry.CodeBlockStartActionsCount}", ConsoleColor.White);
            WriteLine($"Code Block End Actions:         {telemetry.CodeBlockEndActionsCount}", ConsoleColor.White);

            WriteLine($"Compilation Actions:            {telemetry.CompilationActionsCount}", ConsoleColor.White);
            WriteLine($"Compilation Start Actions:      {telemetry.CompilationStartActionsCount}", ConsoleColor.White);
            WriteLine($"Compilation End Actions:        {telemetry.CompilationEndActionsCount}", ConsoleColor.White);

            WriteLine($"Operation Actions:              {telemetry.OperationActionsCount}", ConsoleColor.White);
            WriteLine($"Operation Block Actions:        {telemetry.OperationBlockActionsCount}", ConsoleColor.White);
            WriteLine($"Operation Block Start Actions:  {telemetry.OperationBlockStartActionsCount}", ConsoleColor.White);
            WriteLine($"Operation Block End Actions:    {telemetry.OperationBlockEndActionsCount}", ConsoleColor.White);

            WriteLine($"Semantic Model Actions:         {telemetry.SemanticModelActionsCount}", ConsoleColor.White);
            WriteLine($"Symbol Actions:                 {telemetry.SymbolActionsCount}", ConsoleColor.White);
            WriteLine($"Syntax Node Actions:            {telemetry.SyntaxNodeActionsCount}", ConsoleColor.White);
            WriteLine($"Syntax Tree Actions:            {telemetry.SyntaxTreeActionsCount}", ConsoleColor.White);
        }

        private static void WriteExecutionTimes(string analyzerName, AnalyzerTelemetryInfo telemetry)
        {
            WriteLine($"{analyzerName + ":",-60} {telemetry.ExecutionTime.TotalMilliseconds,7:0}", ConsoleColor.White);
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
        }

        private struct DocumentAnalyzerPerformance
        {
            public DocumentAnalyzerPerformance(double editsPerSecond)
            {
                EditsPerSecond = editsPerSecond;
            }

            public double EditsPerSecond
            {
                get;
            }
        }
    }
}
