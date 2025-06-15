// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class RenameBenchmarks
    {
        private Solution? _solution;
        private ISymbol? _symbol;
        private string? _solutionPath;
        private const string RenamedTypeName = "Microsoft.CodeAnalysis.SyntaxNode";

        [GlobalSetup]
        public void GlobalSetup()
        {
            var msBuildInstance = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(x => x.Version).First();
            MSBuildLocator.RegisterInstance(msBuildInstance);
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName)!;
            _solutionPath = Path.Combine(roslynRoot, @"src\Tools\IdeCoreBenchmarks\Assets\Microsoft.CodeAnalysis.sln");

            if (!File.Exists(_solutionPath))
            {
                throw new ArgumentException();
            }

            var assemblies = MSBuildMefHostServices.DefaultAssemblies;
            var hostService = MefHostServices.Create(assemblies);
            using var workspace = MSBuildWorkspace.Create(hostService);
            _solution = workspace.OpenSolutionAsync(_solutionPath!).Result;

            // Microsoft.CodeAnalysis is multi-targeting
            var project = _solution.Projects.First(project => project.Name.StartsWith("Microsoft.CodeAnalysis"));
            var compilation = project.GetRequiredCompilationAsync(CancellationToken.None).Result;
            _symbol = compilation.GetBestTypeByMetadataName(RenamedTypeName);
        }


        [Benchmark]
        public async Task RenameNodes()
        {
            await Renamer.RenameSymbolAsync(_solution!, _symbol!, new SymbolRenameOptions(), "SyntaxNode2");
        }
    }
}
