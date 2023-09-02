// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class SymbolDisplayBenchmarks
    {
        private ISymbol _classSymbol;

        [ParamsAllValues]
        public bool OptimizeDisplayString { get; set; }

        [GlobalSetup]
        public async Task SetupAsync()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            var csFilePath = Path.Combine(roslynRoot, @"src\Compilers\CSharp\Portable\Generated\BoundNodes.xml.Generated.cs");

            var text = File.ReadAllText(csFilePath);

            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var solution = new AdhocWorkspace().CurrentSolution
                .AddProject(projectId, "ProjectName", "AssemblyName", LanguageNames.CSharp)
                .AddDocument(documentId, "DocumentName", text);

            var document = solution.GetDocument(documentId)!;
            var root = (await document.GetSyntaxRootAsync().ConfigureAwait(false))!;

            var nsNode = root.ChildNodes().First(n => n.Kind() == SyntaxKind.NamespaceDeclaration);
            var classNode = nsNode.ChildNodes().First(n => n.Kind() == SyntaxKind.ClassDeclaration);

            var semanticModel = (await document.GetSemanticModelAsync(CancellationToken.None).ConfigureAwait(false))!;
            _classSymbol = semanticModel.GetDeclaredSymbol(classNode)!;
        }

        [Benchmark]
        public void CallToDisplayString()
        {
            Microsoft.CodeAnalysis.CSharp.SymbolDisplay.OptimizeDisplayString = OptimizeDisplayString;
            _classSymbol.ToDisplayString();
        }
    }
}
