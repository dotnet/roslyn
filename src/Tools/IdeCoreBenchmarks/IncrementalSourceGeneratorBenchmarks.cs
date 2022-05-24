// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnalyzerRunner;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

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
            SetUpWorkspace();
        }

        [IterationSetup]
        public void IterationSetup() => LoadSolutionAsync().Wait();

        private void RestoreCompilerSolution()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            _solutionPath = Path.Combine(roslynRoot, @"Compilers.sln");
            var restoreOperation = Process.Start("dotnet", $"restore /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1 /p:Deterministic=true /p:Optimize=true {_solutionPath}");
            restoreOperation.WaitForExit();
            if (restoreOperation.ExitCode != 0)
                throw new ArgumentException($"Unable to restore {_solutionPath}");
        }

        private static void SetUpWorkspace()
        {
            // QueryVisualStudioInstances returns Visual Studio installations on .NET Framework, and .NET Core SDK
            // installations on .NET Core. We use the one with the most recent version.
            var msBuildInstance = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(x => x.Version).First();

            MSBuildLocator.RegisterInstance(msBuildInstance);
        }

        private Task LoadSolutionAsync()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            _solutionPath = Path.Combine(roslynRoot, @"Roslyn.sln");

            if (!File.Exists(_solutionPath))
                throw new ArgumentException("Couldn't find Roslyn.sln");

            Console.WriteLine("Found Roslyn.sln: " + Process.GetCurrentProcess().Id);
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
#if true
                var input = ctx.SyntaxProvider.CreateSyntaxProvider<ClassDeclarationSyntax>(
                    (c, _) =>
                    {
                        return c is ClassDeclarationSyntax classDecl && classDecl.AttributeLists.Count > 0;
                    },
                    (ctx, _) =>
                    {
                        var node = (ClassDeclarationSyntax)ctx.Node;
                        return node;
                    });
#else
                var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("System.Text.Json.Serialization.JsonSerializableAttribute");
#endif
                ctx.RegisterSourceOutput(input, (spc, node) => { });
            })).AsSourceGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
               new ISourceGenerator[] { generator }, parseOptions: CSharpParseOptions.Default);

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

            var totalIncrementalTime = TimeSpan.Zero;
            for (var i = 0; i < 10000; i++)
            {
                var changedText = sourceText.WithChanges(new TextChange(new TextSpan(0, 0), $"// added text{i}\r\n"));
                var changedTree = syntaxTree.WithChangedText(changedText);
                var changedCompilation = compilation.ReplaceSyntaxTree(syntaxTree, changedTree);

                start = DateTime.Now;
                driver = driver.RunGenerators(changedCompilation);
                var incrementalTime = DateTime.Now - start;
                Console.WriteLine("Incremental time: " + incrementalTime);
                totalIncrementalTime += incrementalTime;
            }

            Console.WriteLine("Total incremental time: " + totalIncrementalTime);
            Console.ReadLine();
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
