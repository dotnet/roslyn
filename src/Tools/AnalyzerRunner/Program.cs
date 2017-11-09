// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
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
            var options = Options.Create(args);
            if (options == null)
            {
                return;
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress +=
                (sender, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                };

            var analyzerAssemblies = GetAnalyzerAssemblies(options.AnalyzerPath);
            var analyzers = GetAllAnalyzers(analyzerAssemblies);
            analyzers = FilterAnalyzers(analyzers, options).ToImmutableArray();

            if (analyzers.Length == 0)
            {
                Utilities.WriteLine("No analyzers found", ConsoleColor.Red);
                Utilities.PrintHelp();
                return;
            }
            var cancellationToken = cts.Token;

            Stopwatch stopwatch = Stopwatch.StartNew();
            using (MSBuildWorkspace workspace = MSBuildWorkspace.Create())
            {
                Solution solution = await workspace.OpenSolutionAsync(options.SolutionPath, cancellationToken).ConfigureAwait(false);
                bool proceedLookingForAnalyzerReferences = true;
                while (proceedLookingForAnalyzerReferences)
                {
                    proceedLookingForAnalyzerReferences = false;
                    foreach (var project in solution.Projects)
                    {
                        if (project.AnalyzerReferences.Any())
                        {
                            proceedLookingForAnalyzerReferences = true;
                            solution = project.WithAnalyzerReferences(ImmutableArray<AnalyzerReference>.Empty).Solution;
                        }
                    }
                }

                Console.WriteLine($"Loaded solution in {stopwatch.ElapsedMilliseconds}ms");

                if (options.Stats)
                {
                    List<Project> csharpProjects = solution.Projects.Where(i => i.Language == LanguageNames.CSharp).ToList();

                    Console.WriteLine("Number of projects:\t\t" + csharpProjects.Count);
                    Console.WriteLine("Number of documents:\t\t" + csharpProjects.Sum(x => x.DocumentIds.Count));

                    var statistics = await GetAnalyzerStatisticsAsync(csharpProjects, cancellationToken).ConfigureAwait(true);

                    Console.WriteLine("Number of syntax nodes:\t\t" + statistics.NumberofNodes);
                    Console.WriteLine("Number of syntax tokens:\t" + statistics.NumberOfTokens);
                    Console.WriteLine("Number of syntax trivia:\t" + statistics.NumberOfTrivia);
                }

                stopwatch.Restart();
                var telemetryCollector = new TelemetryCollector();
                var diagnostics = await GetAnalyzerDiagnosticsAsync(solution, options.SolutionPath, analyzers, options, telemetryCollector, cancellationToken).ConfigureAwait(true);
                var allDiagnostics = diagnostics.Where(i=> i.Value != null && i.Value.Any()).SelectMany(i => i.Value).ToImmutableArray();

                Console.WriteLine($"Found {allDiagnostics.Length} diagnostics in {stopwatch.ElapsedMilliseconds}ms");
                telemetryCollector.WriteTelemetry();

                foreach (var group in allDiagnostics.GroupBy(i => i.Id).OrderBy(i => i.Key, StringComparer.OrdinalIgnoreCase))
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
                    WriteDiagnosticResults(diagnostics.SelectMany(i => i.Value.Select(j => Tuple.Create(i.Key, j))).ToImmutableArray(), options.LogFileName);
                }

                if (options.CodeFixes)
                {
                    await TestCodeFixesAsync(stopwatch, solution, analyzerAssemblies, allDiagnostics, cancellationToken).ConfigureAwait(true);
                }

                if (options.FixAll)
                {
                    await TestFixAllAsync(stopwatch, solution, analyzerAssemblies, diagnostics, options.ApplyChanges, cancellationToken).ConfigureAwait(true);
                }
            }
        }
        
        private static void WriteDiagnosticResults(ImmutableArray<Tuple<ProjectId, Diagnostic>> diagnostics, string fileName)
        {
            var orderedDiagnostics =
                diagnostics
                .OrderBy(i => i.Item2.Id)
                .ThenBy(i => i.Item2.Location.SourceTree?.FilePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.Item2.Location.SourceSpan.Start)
                .ThenBy(i => i.Item2.Location.SourceSpan.End);

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

        private static async Task TestFixAllAsync(Stopwatch stopwatch, Solution solution, IEnumerable<Assembly> analyzerAssemblies, ImmutableDictionary<ProjectId, ImmutableArray<Diagnostic>> diagnostics, bool applyChanges, CancellationToken cancellationToken)
        {
            Console.WriteLine("Calculating fixes");

            var codeFixers = GetAllCodeFixers(analyzerAssemblies).SelectMany(x => x.Value).Distinct();

            var equivalenceGroups = new List<CodeFixEquivalenceGroup>();

            foreach (var codeFixer in codeFixers)
            {
                equivalenceGroups.AddRange(await CodeFixEquivalenceGroup.CreateAsync(codeFixer, diagnostics, solution, cancellationToken).ConfigureAwait(true));
            }

            Console.WriteLine($"Found {equivalenceGroups.Count} equivalence groups.");
            if (applyChanges && equivalenceGroups.Count > 1)
            {
                Console.WriteLine("/apply can only be used with a single equivalence group.");
                return;
            }

            Console.WriteLine("Calculating changes");

            foreach (var fix in equivalenceGroups)
            {
                try
                {
                    stopwatch.Restart();
                    Console.WriteLine($"Calculating fix for {fix.CodeFixEquivalenceKey} using {fix.FixAllProvider} for {fix.NumberOfDiagnostics} instances.");
                    var operations = await fix.GetOperationsAsync(cancellationToken).ConfigureAwait(true);
                    if (applyChanges)
                    {
                        var applyOperations = operations.OfType<ApplyChangesOperation>().ToList();
                        if (applyOperations.Count > 1)
                        {
                            Console.WriteLine("/apply can only apply a single code action operation.");
                        }
                        else if (applyOperations.Count == 0)
                        {
                            Console.WriteLine("No changes were found to apply.");
                        }
                        else
                        {
                            applyOperations[0].Apply(solution.Workspace, cancellationToken);
                        }
                    }

                    Utilities.WriteLine($"Calculating changes completed in {stopwatch.ElapsedMilliseconds}ms. This is {fix.NumberOfDiagnostics / stopwatch.Elapsed.TotalSeconds:0.000} instances/second.", ConsoleColor.Yellow);
                }
                catch (Exception ex)
                {
                    // Report thrown exceptions
                    Utilities.WriteLine($"The fix '{fix.CodeFixEquivalenceKey}' threw an exception after {stopwatch.ElapsedMilliseconds}ms:", ConsoleColor.Yellow);
                    Utilities.WriteLine(ex.ToString(), ConsoleColor.Yellow);
                }
            }
        }

        private static async Task TestCodeFixesAsync(Stopwatch stopwatch, Solution solution, IEnumerable<Assembly> analyzerAssemblies, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            Console.WriteLine("Calculating fixes");

            List<CodeAction> fixes = new List<CodeAction>();

            var codeFixers = GetAllCodeFixers(analyzerAssemblies);

            foreach (var item in diagnostics)
            {
                foreach (var codeFixer in codeFixers.GetValueOrDefault(item.Id, ImmutableList.Create<CodeFixProvider>()))
                {
                    fixes.AddRange(await GetFixesAsync(solution, codeFixer, item, cancellationToken).ConfigureAwait(false));
                }
            }

            Console.WriteLine($"Found {fixes.Count} potential code fixes");

            Console.WriteLine("Calculating changes");

            stopwatch.Restart();

            object lockObject = new object();

            Parallel.ForEach(fixes, fix =>
            {
                try
                {
                    fix.GetOperationsAsync(cancellationToken).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    // Report thrown exceptions
                    lock (lockObject)
                    {
                        Utilities.WriteLine($"The fix '{fix.Title}' threw an exception:", ConsoleColor.Yellow);
                        Utilities.WriteLine(ex.ToString(), ConsoleColor.Red);
                    }
                }
            });

            Console.WriteLine($"Calculating changes completed in {stopwatch.ElapsedMilliseconds}ms");
        }

        private static async Task<IEnumerable<CodeAction>> GetFixesAsync(Solution solution, CodeFixProvider codeFixProvider, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            List<CodeAction> codeActions = new List<CodeAction>();

            await codeFixProvider.RegisterCodeFixesAsync(new CodeFixContext(solution.GetDocument(diagnostic.Location.SourceTree), diagnostic, (a, d) => codeActions.Add(a), cancellationToken)).ConfigureAwait(false);

            return codeActions;
        }

        private static Task<Statistic> GetAnalyzerStatisticsAsync(IEnumerable<Project> projects, CancellationToken cancellationToken)
        {
            ConcurrentBag<Statistic> sums = new ConcurrentBag<Statistic>();

            Parallel.ForEach(projects.SelectMany(i => i.Documents), document =>
            {
                var documentStatistics = GetAnalyzerStatisticsAsync(document, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
                sums.Add(documentStatistics);
            });

            Statistic sum = sums.Aggregate(new Statistic(0, 0, 0), (currentResult, value) => currentResult + value);
            return Task.FromResult(sum);
        }

        private static async Task<Statistic> GetAnalyzerStatisticsAsync(Document document, CancellationToken cancellationToken)
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
            foreach (var analyzer in analyzers)
            {
                if (options.UseAll)
                {
                    yield return analyzer;
                }
                else if (options.AnalyzerIds.Count == 0)
                {
                    if (analyzer.SupportedDiagnostics.Any(i => i.IsEnabledByDefault))
                    {
                        yield return analyzer;
                    }

                    continue;
                }
                else if (analyzer.SupportedDiagnostics.Any(y => options.AnalyzerIds.Contains(y.Id)))
                {
                    yield return analyzer;
                }
            }
        }

        private static ImmutableArray<Assembly> GetAnalyzerAssemblies(string path)
        {
            if (File.Exists(path))
            {
                return ImmutableArray.Create(Assembly.LoadFrom(path));
            }
            else if (Directory.Exists(path))
            {
                var builder = ImmutableArray.CreateBuilder<Assembly>();
                foreach (var file in Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories))
                {
                    builder.Add(Assembly.LoadFrom(file));
                }

                return builder.ToImmutable();
            }

            Utilities.WriteLine($"Cannot load assembliy or assemblies from {path}.", ConsoleColor.Red);
            return ImmutableArray<Assembly>.Empty;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                Console.WriteLine($"Failed to load all types from {assembly.FullName}. Will proceed with types loaded successfully.");
                return e.Types.Where(t => t != null);
            }
        }

        private static ImmutableArray<DiagnosticAnalyzer> GetAllAnalyzers(IEnumerable<Assembly> assemblies)
        {
            var analyzers = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            foreach (var diagnosticAnalyzer in GetAllSubclasses<DiagnosticAnalyzer>(assemblies))
            {
                analyzers.Add(diagnosticAnalyzer);
            }

            return analyzers.ToImmutable();
        }

        private static ImmutableDictionary<string, ImmutableList<CodeFixProvider>> GetAllCodeFixers(IEnumerable<Assembly> assemblies)
        {
            Dictionary<string, ImmutableList<CodeFixProvider>> providers = new Dictionary<string, ImmutableList<CodeFixProvider>>();
            foreach (var codeFixProvider in GetAllSubclasses<CodeFixProvider>(assemblies))
            {
                foreach (var diagnosticId in codeFixProvider.FixableDiagnosticIds)
                {
                    providers.AddToInnerList(diagnosticId, codeFixProvider);
                }
            }

            return providers.ToImmutableDictionary();
        }

        private static IEnumerable<T> GetAllSubclasses<T>(IEnumerable<Assembly> assemblies)
        {
            var returnType = typeof(T);
            foreach (var assembly in assemblies)
            {
                foreach (var type in GetLoadableTypes(assembly))
                {
                    if (type.IsSubclassOf(returnType) && !type.IsAbstract)
                    {
                        yield return (T)Activator.CreateInstance(type);
                    }
                }
            }
        }

        private static async Task<ImmutableDictionary<ProjectId, ImmutableArray<Diagnostic>>> GetAnalyzerDiagnosticsAsync(Solution solution, string solutionPath, ImmutableArray<DiagnosticAnalyzer> analyzers, Options options, TelemetryCollector telemetryCollector, CancellationToken cancellationToken)
        {
            List<KeyValuePair<ProjectId, Task<ImmutableArray<Diagnostic>>>> projectDiagnosticTasks = new List<KeyValuePair<ProjectId, Task<ImmutableArray<Diagnostic>>>>();

            // Make sure we analyze the projects in parallel
            foreach (var project in solution.Projects)
            {
                if (project.Language != LanguageNames.CSharp)
                {
                    continue;
                }

                projectDiagnosticTasks.Add(new KeyValuePair<ProjectId, Task<ImmutableArray<Diagnostic>>>(project.Id, GetProjectAnalyzerDiagnosticsAsync(analyzers, project, options, telemetryCollector, cancellationToken)));
            }

            ImmutableDictionary<ProjectId, ImmutableArray<Diagnostic>>.Builder projectDiagnosticBuilder = ImmutableDictionary.CreateBuilder<ProjectId, ImmutableArray<Diagnostic>>();
            foreach (var task in projectDiagnosticTasks)
            {
                projectDiagnosticBuilder.Add(task.Key, await task.Value.ConfigureAwait(false));
            }

            return projectDiagnosticBuilder.ToImmutable();
        }

        /// <summary>
        /// Returns a list of all analyzer diagnostics inside the specific project. This is an asynchronous operation.
        /// </summary>
        /// <param name="analyzers">The list of analyzers that should be used</param>
        /// <param name="project">The project that should be analyzed</param>
        /// <see langword="false"/> to use the behavior configured for the specified <paramref name="project"/>.</param>
        /// <param name="cancellationToken">The cancellation token that the task will observe.</param>
        /// <returns>A list of diagnostics inside the project</returns>
        private static async Task<ImmutableArray<Diagnostic>> GetProjectAnalyzerDiagnosticsAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, Project project, Options analyzerOptionsInternal, TelemetryCollector telemetryCollector, CancellationToken cancellationToken)
        {
            Utilities.WriteLine($"Running analyzers for {project.Name}", ConsoleColor.Gray);
            var supportedDiagnosticsSpecificOptions = new Dictionary<string, ReportDiagnostic>();

            // Report exceptions during the analysis process as errors
            supportedDiagnosticsSpecificOptions.Add("AD0001", ReportDiagnostic.Error);

            // update the project compilation options
            var modifiedSpecificDiagnosticOptions = supportedDiagnosticsSpecificOptions.ToImmutableDictionary().SetItems(project.CompilationOptions.SpecificDiagnosticOptions);
            var modifiedCompilationOptions = project.CompilationOptions.WithSpecificDiagnosticOptions(modifiedSpecificDiagnosticOptions);
            var processedProject = project.WithCompilationOptions(modifiedCompilationOptions);

            try
            {
                Compilation compilation = await processedProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var newCompilation = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(compilation.SyntaxTrees.Select(t => t.WithRootAndOptions(t.GetRoot(), t.Options.WithFeatures(new[] { new KeyValuePair<string, string>("IOperation", "IOperation") }))));

                CompilationWithAnalyzers compilationWithAnalyzers = newCompilation.WithAnalyzers(analyzers, new CompilationWithAnalyzersOptions(new AnalyzerOptions(ImmutableArray.Create<AdditionalText>()), null, analyzerOptionsInternal.ConcurrentAnalysis, logAnalyzerExecutionTime: true, reportSuppressedDiagnostics: analyzerOptionsInternal.ReportSuppressedDiagnostics));

                var diagnostics = await FixAllContextHelper.GetAllDiagnosticsAsync(compilation, compilationWithAnalyzers, analyzers, project.Documents, true, cancellationToken).ConfigureAwait(false);

                foreach (var analyzer in analyzers)
                {
                    var telemetry = await compilationWithAnalyzers.GetAnalyzerTelemetryInfoAsync(analyzer, cancellationToken).ConfigureAwait(false);
                    var analyzerName = analyzer.GetType().Name;

                    if (!telemetryCollector.TryGetValue(analyzerName, out var telemetryInfoList))
                    {
                        telemetryInfoList = new List<AnalyzerTelemetryInfo>();
                        telemetryCollector.Add(analyzerName, telemetryInfoList);
                    }

                    telemetryInfoList.Add(telemetry);
                }

                return diagnostics;
            }
            catch (Exception e)
            {
                Utilities.WriteLine($"Failed to analyze {project.Name} with {e.ToString()}", ConsoleColor.Red);
                return ImmutableArray<Diagnostic>.Empty;
            }
        }
    }
}
