// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnalyzerRunner;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class ClassificationBenchmarks
    {
        string _solutionPath;
        MSBuildWorkspace _workspace;
        Solution _solution;

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
            _solutionPath = Path.Combine(roslynRoot, @"Roslyn.sln");
            var restoreOperation = Process.Start("dotnet", $"restore /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1 /p:Deterministic=true /p:Optimize=true {_solutionPath}");
            restoreOperation.WaitForExit();
            if (restoreOperation.ExitCode != 0)
                throw new ArgumentException($"Unable to restore {_solutionPath}");
        }

        private async Task LoadSolutionAsync()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            var solutionPath = Path.Combine(roslynRoot, "Roslyn.sln");

            if (!File.Exists(solutionPath))
                throw new ArgumentException("Couldn't find Roslyn.sln");

            Console.WriteLine("Found Roslyn.sln: " + Process.GetCurrentProcess().Id);

            var assemblies = MSBuildMefHostServices.DefaultAssemblies
                .Add(typeof(AnalyzerRunnerHelper).Assembly)
                .Add(typeof(FindReferencesBenchmarks).Assembly);
            var services = MefHostServices.Create(assemblies);

            _workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
                {
                    // Use the latest language version to force the full set of available analyzers to run on the project.
                    { "LangVersion", "preview" },
                }, services);

            if (_workspace == null)
                throw new ArgumentException("Couldn't create workspace");

            Console.WriteLine("Opening roslyn.  Attach to: " + Process.GetCurrentProcess().Id);

            var start = DateTime.Now;
            _solution = await _workspace.OpenSolutionAsync(solutionPath, progress: null, CancellationToken.None);
            Console.WriteLine("Finished opening roslyn: " + (DateTime.Now - start));
        }

        protected static async Task<ImmutableArray<ClassifiedSpan>> GetSemanticClassificationsAsync(Document document, TextSpan span)
        {
            var service = document.GetRequiredLanguageService<IClassificationService>();
            using var _ = Classifier.GetPooledList(out var result);
            await service.AddSemanticClassificationsAsync(document, span, ClassificationOptions.Default, result, CancellationToken.None);
            return result.ToImmutableArray();
        }

        [Benchmark]
        public void ClassifyDocument()
        {
            var project = _solution.Projects.First(p => p.AssemblyName == "Microsoft.CodeAnalysis");
            foreach (var document in project.Documents)
            {
                var text = document.GetTextAsync().Result.ToString();
                var span = new TextSpan(0, text.Length);
                _ = GetSemanticClassificationsAsync(document, span);
            }
        }

        [IterationCleanup]
        public void Cleanup()
        {
            _workspace?.Dispose();
            _workspace = null;
            _solution = null;
        }
    }
}
