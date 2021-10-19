// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using System.Threading;
using System.Linq;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class LSPSemanticTokensBenchmarks
    {
        private int[] _semanticTokens1;
        private int[] _semanticTokens2;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            var filePath1 = Path.Combine(roslynRoot, @"src\Compilers\CSharp\Portable\Generated\BoundNodes.xml.Generated.cs");
            var filePath2 = Path.Combine(roslynRoot, @"src\Compilers\CSharp\Portable\Generated\CSharpSyntaxGenerator\CSharpSyntaxGenerator.SourceGenerator\Syntax.xml.Syntax.Generated.cs");

            // Generates the two token sets we later compare against each other.
            // The two chosen files are both around 15k lines of code and approximately 600k tokens each.
            _semanticTokens1 = await GetSemanticTokensForFilePathAsync(filePath1).ConfigureAwait(false);
            _semanticTokens2 = await GetSemanticTokensForFilePathAsync(filePath2).ConfigureAwait(false);

            Console.WriteLine("Completed setup.");

            static async Task<int[]> GetSemanticTokensForFilePathAsync(string filePath)
            {
                if (!File.Exists(filePath))
                {
                    throw new ArgumentException("Invalid file path: " + filePath.ToString());
                }

                var text = File.ReadAllText(filePath);
                var projectId = ProjectId.CreateNewId();
                var documentId = DocumentId.CreateNewId(projectId);

                var solution = new AdhocWorkspace().CurrentSolution
                    .AddProject(projectId, "ProjectName", "AssemblyName", LanguageNames.CSharp)
                    .AddDocument(documentId, "DocumentName", text);

                var document = solution.GetDocument(documentId);

                var (semanticTokens, isFinalized) = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                    document, SemanticTokensCache.TokenTypeToIndex, range: null, CancellationToken.None).ConfigureAwait(false);
                while (!isFinalized)
                {
                    (semanticTokens, isFinalized) = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                    document, SemanticTokensCache.TokenTypeToIndex, range: null, CancellationToken.None).ConfigureAwait(false);
                }

                solution.Workspace.Dispose();

                return semanticTokens;
            }
        }

        [Benchmark]
        public Task RunLSPSemanticTokensBenchmark_10k() => RunLSPSemanticTokensBenchmark(10000);

        [Benchmark]
        public Task RunLSPSemanticTokensBenchmark_20k() => RunLSPSemanticTokensBenchmark(20000);

        [Benchmark]
        public Task RunLSPSemanticTokensBenchmark_100k() => RunLSPSemanticTokensBenchmark(100000);

        [Benchmark]
        public Task RunLSPSemanticTokensBenchmark_250k() => RunLSPSemanticTokensBenchmark(250000);

        [Benchmark]
        public Task RunLSPSemanticTokensBenchmark_AllTokens() => RunLSPSemanticTokensBenchmark();

        private async Task RunLSPSemanticTokensBenchmark(int? numTokens = null)
        {
            try
            {
                if (numTokens is null)
                {
                    await SemanticTokensEditsDiffer.ComputeSemanticTokensEditsAsync(_semanticTokens1, _semanticTokens2).ConfigureAwait(false);
                }
                else
                {
                    await SemanticTokensEditsDiffer.ComputeSemanticTokensEditsAsync(
                        _semanticTokens1.Take(numTokens.Value).ToArray(), _semanticTokens2.Take(numTokens.Value).ToArray()).ConfigureAwait(false);
                }
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
