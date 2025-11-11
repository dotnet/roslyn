// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnalyzerRunner;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace IdeCoreBenchmarks
{
    // [GcServer(true)]
    [MemoryDiagnoser]
    [SimpleJob(launchCount: 1, warmupCount: 0, targetCount: 0, invocationCount: 1, id: "QuickJob")]
    public class IncrementalSourceGeneratorBenchmarks
    {
        string _solutionPath;
        MSBuildWorkspace _workspace;

        [GlobalSetup]
        public void GlobalSetup()
        {
            RestoreCompilerSolution();
        }

        [IterationSetup]
        public void IterationSetup() => LoadSolutionAsync().Wait();

        private void RestoreCompilerSolution()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            _solutionPath = Path.Combine(roslynRoot, @"Compilers.slnf");
            var restoreOperation = Process.Start("dotnet", $"restore /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1 /p:Deterministic=true /p:Optimize=true {_solutionPath}");
            restoreOperation.WaitForExit();
            if (restoreOperation.ExitCode != 0)
                throw new ArgumentException($"Unable to restore {_solutionPath}");
        }

        private Task LoadSolutionAsync()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            _solutionPath = Path.Combine(roslynRoot, @"Roslyn.slnx");

            if (!File.Exists(_solutionPath))
                throw new ArgumentException("Couldn't find Roslyn.slnx");

            Console.WriteLine("Found Roslyn.slnx: " + Process.GetCurrentProcess().Id);
            var assemblies = MSBuildMefHostServices.DefaultAssemblies
                .Add(typeof(AnalyzerRunnerHelper).Assembly)
                .Add(typeof(FindReferencesBenchmarks).Assembly);
            var services = MefHostServices.Create(assemblies);

            _workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
                {
                    // Use the latest language version to force the full set of available analyzers to run on the project.
                    { "LangVersion", "9.0" },
                }, services);

            if (_workspace == null)
                throw new ArgumentException("Couldn't create workspace");

            Console.WriteLine("Opening roslyn.  Attach to: " + Process.GetCurrentProcess().Id);

            var start = DateTime.Now;

            var solution = _workspace.OpenSolutionAsync(_solutionPath, progress: null, CancellationToken.None).Result;
            Console.WriteLine("Finished opening roslyn: " + (DateTime.Now - start));

            //foreach (var diag in _workspace.Diagnostics)
            //    Console.WriteLine(diag);
            return Task.CompletedTask;
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _workspace.Dispose();
            _workspace = null;
        }

        [Benchmark]
        public async Task RunGenerator()
        {
            var generator = (new PipelineCallbackGenerator(ctx =>
            {
                Console.WriteLine("Registering");
#if false
                var input = ctx.SyntaxProvider.CreateSyntaxProvider<ClassDeclarationSyntax>(
                    (c, _) => 
                    {
                        return c is ClassDeclarationSyntax classDecl && classDecl.AttributeLists.Count > 0;
                    },
                    (ctx, _) =>
                    {
                        var node = (ClassDeclarationSyntax)ctx.Node;
                        var symbol = ctx.SemanticModel.GetDeclaredSymbol(node);
                        foreach (var attribute in symbol.GetAttributes())
                        {
                            if (attribute.AttributeClass.ToDisplayString() == "System.Text.Json.Serialization.JsonSerializationAttribute")
                            {

                            }
                        }
                        return node;
                    });
#else
                var input = ctx.SyntaxProvider.ForAttributeWithMetadataName(
                    "System.Text.Json.Serialization.JsonSerializableAttribute",
                    (n, _) => n is ClassDeclarationSyntax,
                    (ctx, _) => 0);
                // var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("JsonSerializableAttribute");
#endif
                ctx.RegisterSourceOutput(input, (spc, node) => { });
            })).AsSourceGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
               new ISourceGenerator[] { generator }, parseOptions: CSharpParseOptions.Default);

            //foreach (var proj in _workspace.CurrentSolution.Projects)
            //{
            //    Console.WriteLine(proj.Name);
            //}

            var project = _workspace.CurrentSolution.Projects.Single(p => p.Name == "Microsoft.CodeAnalysis.Workspaces(netstandard2.0)");

            var start = DateTime.Now;
            Console.WriteLine("Getting compilation: " + project.Name);
            var compilation = await project.GetCompilationAsync();
            Console.WriteLine("Compilation time: " + (DateTime.Now - start));
            Console.WriteLine("Syntax tree count: " + compilation.SyntaxTrees.Count());

            start = DateTime.Now;
            driver = driver.RunGenerators(compilation);

            Console.WriteLine("First generator run: " + (DateTime.Now - start));

            var syntaxTree = compilation.SyntaxTrees.Single(t => t.FilePath.Contains("AbstractCaseCorrectionService"));
            var sourceText = syntaxTree.GetText();

            Console.WriteLine("Start profiling now");
            Thread.Sleep(5000);

            var totalIncrementalTime = TimeSpan.Zero;
            for (var i = 0; i < 50000; i++)
            {
                var changedText = sourceText.WithChanges(new TextChange(sourceText.Lines[0].Span, $"// added text{i}"));
                var changedTree = syntaxTree.WithChangedText(changedText);
                compilation = compilation.ReplaceSyntaxTree(syntaxTree, changedTree);
                sourceText = changedText;
                syntaxTree = changedTree;

                start = DateTime.Now;
                driver = driver.RunGenerators(compilation);
                var incrementalTime = DateTime.Now - start;
                if (i % 5000 == 0)
                    Console.WriteLine("Incremental time: " + incrementalTime);
                totalIncrementalTime += incrementalTime;
            }

            Console.WriteLine("Total incremental time: " + totalIncrementalTime);
            Environment.Exit(0);
        }
    }

    internal sealed class PipelineCallbackGenerator : IIncrementalGenerator
    {
        private readonly Action<IncrementalGeneratorInitializationContext> _registerPipelineCallback;

        public PipelineCallbackGenerator(Action<IncrementalGeneratorInitializationContext> registerPipelineCallback)
        {
            _registerPipelineCallback = registerPipelineCallback;
        }

        public void Initialize(IncrementalGeneratorInitializationContext context) => _registerPipelineCallback(context);
    }
}
