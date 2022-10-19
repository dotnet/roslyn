// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using static AnalyzerRunner.Program;

namespace AnalyzerRunner
{
    public sealed class DiagnosticAnalyzerRunner
    {
        private readonly Workspace _workspace;
        private readonly Options _options;
        private readonly ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> _analyzers;

        public DiagnosticAnalyzerRunner(Workspace workspace, Options options)
        {
            _workspace = workspace;
            _options = options;

            var analyzers = GetDiagnosticAnalyzers(options.AnalyzerPath);
            _analyzers = FilterAnalyzers(analyzers, options);
        }

        public bool HasAnalyzers => _analyzers.Any(pair => pair.Value.Any());

        private static Solution SetOptions(Solution solution)
        {
            // Make sure AD0001 and AD0002 are reported as errors
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId)!;
                if (project.Language is not LanguageNames.CSharp and not LanguageNames.VisualBasic)
                    continue;

                var modifiedSpecificDiagnosticOptions = project.CompilationOptions.SpecificDiagnosticOptions
                    .SetItem("AD0001", ReportDiagnostic.Error)
                    .SetItem("AD0002", ReportDiagnostic.Error);
                var modifiedCompilationOptions = project.CompilationOptions.WithSpecificDiagnosticOptions(modifiedSpecificDiagnosticOptions);
                solution = solution.WithProjectCompilationOptions(projectId, modifiedCompilationOptions);
            }

            return solution;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            if (!HasAnalyzers)
            {
                return;
            }

            var solution = _workspace.CurrentSolution;
            solution = SetOptions(solution);

            await GetAnalysisResultAsync(solution, _analyzers, _options, cancellationToken).ConfigureAwait(false);
        }

        // Also runs per document analysis, used by AnalyzerRunner CLI tool
        internal async Task RunAllAsync(CancellationToken cancellationToken)
        {
            if (!HasAnalyzers)
            {
                return;
            }

            var solution = _workspace.CurrentSolution;

            solution = SetOptions(solution);

            var stopwatch = PerformanceTracker.StartNew();

            var analysisResult = await GetAnalysisResultAsync(solution, _analyzers, _options, cancellationToken).ConfigureAwait(false);
            var allDiagnostics = analysisResult.Where(pair => pair.Value != null).SelectMany(pair => pair.Value.GetAllDiagnostics()).ToImmutableArray();

            Console.WriteLine($"Found {allDiagnostics.Length} diagnostics in {stopwatch.GetSummary(preciseMemory: true)}");
            WriteTelemetry(analysisResult);

            if (_options.TestDocuments)
            {
                // Make sure we have a compilation for each project
                foreach (var project in solution.Projects)
                {
                    if (project.Language is not LanguageNames.CSharp and not LanguageNames.VisualBasic)
                        continue;

                    _ = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                }

                var projectPerformance = new Dictionary<ProjectId, double>();
                var documentPerformance = new Dictionary<DocumentId, DocumentAnalyzerPerformance>();
                foreach (var projectId in solution.ProjectIds)
                {
                    var project = solution.GetProject(projectId);
                    if (project.Language is not LanguageNames.CSharp and not LanguageNames.VisualBasic)
                    {
                        continue;
                    }

                    foreach (var documentId in project.DocumentIds)
                    {
                        var document = project.GetDocument(documentId);
                        if (!_options.TestDocumentMatch(document.FilePath))
                        {
                            continue;
                        }

                        var currentDocumentPerformance = await TestDocumentPerformanceAsync(_analyzers, project, documentId, _options, cancellationToken).ConfigureAwait(false);
                        Console.WriteLine($"{document.FilePath ?? document.Name}: {currentDocumentPerformance.EditsPerSecond:0.00} ({currentDocumentPerformance.AllocatedBytesPerEdit} bytes)");
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
                        Console.WriteLine($"    {document.FilePath ?? document.Name}: {pair.Value.EditsPerSecond:0.00} ({pair.Value.AllocatedBytesPerEdit} bytes)");
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

            if (!string.IsNullOrWhiteSpace(_options.LogFileName))
            {
                WriteDiagnosticResults(analysisResult.SelectMany(pair => pair.Value.GetAllDiagnostics().Select(j => Tuple.Create(pair.Key, j))).ToImmutableArray(), _options.LogFileName);
            }
        }

        private static async Task<DocumentAnalyzerPerformance> TestDocumentPerformanceAsync(ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzers, Project project, DocumentId documentId, Options analyzerOptionsInternal, CancellationToken cancellationToken)
        {
            if (!analyzers.TryGetValue(project.Language, out var languageAnalyzers))
            {
                languageAnalyzers = ImmutableArray<DiagnosticAnalyzer>.Empty;
            }

            Compilation compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            var stopwatch = PerformanceTracker.StartNew();
            for (int i = 0; i < analyzerOptionsInternal.TestDocumentIterations; i++)
            {
                var workspaceAnalyzerOptions = new WorkspaceAnalyzerOptions(project.AnalyzerOptions, project.Solution, analyzerOptionsInternal.IdeOptions);
                CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(languageAnalyzers, new CompilationWithAnalyzersOptions(workspaceAnalyzerOptions, null, analyzerOptionsInternal.RunConcurrent, logAnalyzerExecutionTime: true, reportSuppressedDiagnostics: analyzerOptionsInternal.ReportSuppressedDiagnostics));

                SyntaxTree tree = await project.GetDocument(documentId).GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                await compilationWithAnalyzers.GetAnalyzerSyntaxDiagnosticsAsync(tree, cancellationToken).ConfigureAwait(false);
                await compilationWithAnalyzers.GetAnalyzerSemanticDiagnosticsAsync(compilation.GetSemanticModel(tree), null, cancellationToken).ConfigureAwait(false);
            }

            return new DocumentAnalyzerPerformance(analyzerOptionsInternal.TestDocumentIterations / stopwatch.Elapsed.TotalSeconds, stopwatch.AllocatedBytes / Math.Max(1, analyzerOptionsInternal.TestDocumentIterations));
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

        private static ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> FilterAnalyzers(ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzers, Options options)
        {
            return analyzers.ToImmutableDictionary(
                pair => pair.Key,
                pair => FilterAnalyzers(pair.Value, options).ToImmutableArray());
        }

        private static IEnumerable<DiagnosticAnalyzer> FilterAnalyzers(IEnumerable<DiagnosticAnalyzer> analyzers, Options options)
        {
            if (options.IncrementalAnalyzerNames.Any())
            {
                // AnalyzerRunner is running for IIncrementalAnalyzer testing. DiagnosticAnalyzer testing is disabled
                // unless /all or /a was used.
                if (!options.UseAll && options.AnalyzerNames.IsEmpty)
                {
                    yield break;
                }
            }

            if (options.RefactoringNodes.Any())
            {
                // AnalyzerRunner is running for CodeRefactoringProvider testing. DiagnosticAnalyzer testing is disabled.
                yield break;
            }

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

        private static ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> GetDiagnosticAnalyzers(string path)
        {
            if (File.Exists(path))
            {
                return GetDiagnosticAnalyzersFromFile(path);
            }
            else if (Directory.Exists(path))
            {
                return Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories)
                    .SelectMany(file => GetDiagnosticAnalyzersFromFile(file))
                    .ToLookup(analyzers => analyzers.Key, analyzers => analyzers.Value)
                    .ToImmutableDictionary(
                        group => group.Key,
                        group => group.SelectMany(analyzer => analyzer).ToImmutableArray());
            }

            throw new InvalidDataException($"Cannot find {path}.");
        }

        private static ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> GetDiagnosticAnalyzersFromFile(string path)
        {
            var analyzerReference = new AnalyzerFileReference(Path.GetFullPath(path), AssemblyLoader.Instance);
            var csharpAnalyzers = analyzerReference.GetAnalyzers(LanguageNames.CSharp);
            var basicAnalyzers = analyzerReference.GetAnalyzers(LanguageNames.VisualBasic);
            return ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>>.Empty
                .Add(LanguageNames.CSharp, csharpAnalyzers)
                .Add(LanguageNames.VisualBasic, basicAnalyzers);
        }

        private static async Task<ImmutableDictionary<ProjectId, AnalysisResult>> GetAnalysisResultAsync(
            Solution solution,
            ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzers,
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
                    if (project.Language is not LanguageNames.CSharp and not LanguageNames.VisualBasic)
                    {
                        continue;
                    }

                    if (!analyzers.TryGetValue(project.Language, out var languageAnalyzers) || languageAnalyzers.IsEmpty)
                    {
                        continue;
                    }

                    var resultTask = GetProjectAnalysisResultAsync(languageAnalyzers, project, options, cancellationToken);
                    if (!options.RunConcurrent)
                    {
                        await resultTask.ConfigureAwait(false);
                    }

                    projectDiagnosticTasks.Add(new KeyValuePair<ProjectId, Task<AnalysisResult>>(project.Id, resultTask));
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
            if (analyzerOptionsInternal.RunConcurrent)
            {
                await Task.Yield();
            }

            try
            {
                Compilation compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var newCompilation = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(compilation.SyntaxTrees);

                var workspaceAnalyzerOptions = new WorkspaceAnalyzerOptions(project.AnalyzerOptions, project.Solution, analyzerOptionsInternal.IdeOptions);
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

        internal static void WriteTelemetry(ImmutableDictionary<ProjectId, AnalysisResult> dictionary)
        {
            if (dictionary.IsEmpty)
            {
                return;
            }

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
            var longestAnalyzerName = telemetryInfoDictionary.Select(x => x.Key.GetType().Name.Length).Max();
            foreach (var pair in telemetryInfoDictionary.OrderBy(x => x.Key.GetType().Name, StringComparer.OrdinalIgnoreCase))
            {
                WriteExecutionTimes(pair.Key.GetType().Name, longestAnalyzerName, pair.Value);
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
            WriteLine($"Symbol Start Actions:           {telemetry.SymbolStartActionsCount}", ConsoleColor.White);
            WriteLine($"Symbol End Actions:             {telemetry.SymbolEndActionsCount}", ConsoleColor.White);
            WriteLine($"Syntax Node Actions:            {telemetry.SyntaxNodeActionsCount}", ConsoleColor.White);
            WriteLine($"Syntax Tree Actions:            {telemetry.SyntaxTreeActionsCount}", ConsoleColor.White);
            WriteLine($"Additional File Actions:        {telemetry.AdditionalFileActionsCount}", ConsoleColor.White);

            WriteLine($"Suppression Actions:            {telemetry.SuppressionActionsCount}", ConsoleColor.White);
        }

        private static void WriteExecutionTimes(string analyzerName, int longestAnalyzerName, AnalyzerTelemetryInfo telemetry)
        {
            var padding = new string(' ', longestAnalyzerName - analyzerName.Length);
            WriteLine($"{analyzerName}:{padding} {telemetry.ExecutionTime.TotalMilliseconds,7:0}", ConsoleColor.White);
        }

        private struct DocumentAnalyzerPerformance
        {
            public DocumentAnalyzerPerformance(double editsPerSecond, long allocatedBytesPerEdit)
            {
                EditsPerSecond = editsPerSecond;
                AllocatedBytesPerEdit = allocatedBytesPerEdit;
            }

            public double EditsPerSecond { get; }
            public long AllocatedBytesPerEdit { get; }
        }
    }
}
