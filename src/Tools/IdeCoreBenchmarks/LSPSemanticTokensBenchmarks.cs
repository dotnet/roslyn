// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.CodeAnalysis.Classification;
using System.Threading;
using static Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens.SemanticTokensEditsHandler;
using System.Linq;

#nullable disable

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class LSPSemanticTokensBenchmarks
    {
        private int[] _semanticTokens;
        private int[] _semanticTokens2;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            var csFilePath = Path.Combine(roslynRoot, @"src\Compilers\CSharp\Portable\Generated\BoundNodes.xml.Generated.cs");
            var csFilePath2 = Path.Combine(roslynRoot, @"src\Compilers\CSharp\Portable\Generated\CSharpSyntaxGenerator\CSharpSyntaxGenerator.SourceGenerator\Syntax.xml.Syntax.Generated.cs");

            if (!File.Exists(csFilePath))
            {
                Console.Write("Could not find file: " + csFilePath.ToString());
                throw new ArgumentException();
            }

            if (!File.Exists(csFilePath2))
            {
                Console.Write("Could not find file: " + csFilePath.ToString());
                throw new ArgumentException();
            }

            var text = File.ReadAllText(csFilePath);
            var text2 = File.ReadAllText(csFilePath2);
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            var documentId2 = DocumentId.CreateNewId(projectId);

            var solution = new AdhocWorkspace().CurrentSolution
                .AddProject(projectId, "ProjectName", "AssemblyName", LanguageNames.CSharp)
                .AddDocument(documentId, "DocumentName", text)
                .AddDocument(documentId2, "DocumentName2", text2);

            var document = solution.GetDocument(documentId);

            var (semanticTokens, isFinalized) = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                document, SemanticTokensCache.TokenTypeToIndex, range: null, CancellationToken.None);
            while (!isFinalized)
            {
                (semanticTokens, isFinalized) = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                document, SemanticTokensCache.TokenTypeToIndex, range: null, CancellationToken.None);
            }

            var document2 = solution.GetDocument(documentId2);

            var (semanticTokens2, isFinalized2) = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                document2, SemanticTokensCache.TokenTypeToIndex, range: null, CancellationToken.None);
            while (!isFinalized2)
            {
                (semanticTokens2, isFinalized2) = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                document2, SemanticTokensCache.TokenTypeToIndex, range: null, CancellationToken.None);
            }

            _semanticTokens = semanticTokens;
            _semanticTokens2 = semanticTokens2;
            solution.Workspace.Dispose();

            Console.WriteLine("Completed setup.");
        }

        [Benchmark]
        public async Task RunLSPSemanticTokensBenchmarkAsync_10()
        {
            try
            {
                await LongestCommonSemanticTokensSubsequence.GetEditsAsync(_semanticTokens.Take(10000).ToArray(), _semanticTokens2.Take(10000).ToArray());
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine(ex);
            }
        }

        [Benchmark]
        public async Task RunLSPSemanticTokensBenchmarkAsync_20()
        {
            try
            {
                await LongestCommonSemanticTokensSubsequence.GetEditsAsync(_semanticTokens.Take(20000).ToArray(), _semanticTokens2.Take(20000).ToArray());
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine(ex);
            }
        }

        [Benchmark]
        public async Task RunLSPSemanticTokensBenchmarkAsync_100()
        {
            try
            {
                await LongestCommonSemanticTokensSubsequence.GetEditsAsync(_semanticTokens.Take(100000).ToArray(), _semanticTokens2.Take(100000).ToArray());
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine(ex);
            }
        }

        [Benchmark]
        public async Task RunLSPSemanticTokensBenchmarkAsync_250()
        {
            try
            {
                await LongestCommonSemanticTokensSubsequence.GetEditsAsync(_semanticTokens.Take(250000).ToArray(), _semanticTokens2.Take(250000).ToArray());
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine(ex);
            }
        }

        [Benchmark]
        public async Task RunLSPSemanticTokensBenchmarkAsync_500()
        {
            try
            {
                await LongestCommonSemanticTokensSubsequence.GetEditsAsync(_semanticTokens, _semanticTokens2);
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine(ex);
            }
        }

        [Benchmark]
        public async Task RunLSPSemanticTokensBenchmark_RazorImpl_10()
        {
            try
            {
                await SemanticTokensEditsDiffer.ComputeSemanticTokensEditsAsync(_semanticTokens.Take(10000).ToArray(), _semanticTokens2.Take(10000).ToArray());
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine(ex);
            }
        }

        [Benchmark]
        public async Task RunLSPSemanticTokensBenchmark_RazorImpl_20()
        {
            try
            {
                await SemanticTokensEditsDiffer.ComputeSemanticTokensEditsAsync(_semanticTokens.Take(20000).ToArray(), _semanticTokens2.Take(20000).ToArray());
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine(ex);
            }
        }

        [Benchmark]
        public async Task RunLSPSemanticTokensBenchmark_RazorImpl_100()
        {
            try
            {
                await SemanticTokensEditsDiffer.ComputeSemanticTokensEditsAsync(_semanticTokens.Take(100000).ToArray(), _semanticTokens2.Take(100000).ToArray());
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine(ex);
            }
        }

        [Benchmark]
        public async Task RunLSPSemanticTokensBenchmark_RazorImpl_250()
        {
            try
            {
                await SemanticTokensEditsDiffer.ComputeSemanticTokensEditsAsync(_semanticTokens.Take(250000).ToArray(), _semanticTokens2.Take(250000).ToArray());
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine(ex);
            }
        }

        [Benchmark]
        public async Task RunLSPSemanticTokensBenchmark_RazorImpl_500()
        {
            try
            {
                await SemanticTokensEditsDiffer.ComputeSemanticTokensEditsAsync(_semanticTokens, _semanticTokens2);
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
