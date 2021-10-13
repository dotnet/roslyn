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

#nullable disable

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class LSPSemanticTokensBenchmarks
    {
        private int[] _semanticTokens;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            var csFilePath = Path.Combine(roslynRoot, @"src\Compilers\CSharp\Portable\Generated\BoundNodes.xml.Generated.cs");

            if (!File.Exists(csFilePath))
            {
                Console.Write("Could not find file: " + csFilePath.ToString());
                throw new ArgumentException();
            }

            var text = File.ReadAllText(csFilePath);
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var solution = new AdhocWorkspace().CurrentSolution
                .AddProject(projectId, "ProjectName", "AssemblyName", LanguageNames.CSharp)
                .AddDocument(documentId, "DocumentName", text);

            var document = solution.GetDocument(documentId);

            var (semanticTokens, isFinalized) = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                document, SemanticTokensCache.TokenTypeToIndex, range: null, CancellationToken.None);
            while (!isFinalized)
            {
                (semanticTokens, isFinalized) = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                document, SemanticTokensCache.TokenTypeToIndex, range: null, CancellationToken.None);
            }

            _semanticTokens = semanticTokens;
            solution.Workspace.Dispose();
        }

        [Benchmark]
        public void RunLSPSemanticTokensBenchmark()
        {
            try
            {
                LongestCommonSemanticTokensSubsequence.GetEdits(new int[] { 0, 1, 2, 3, 4 }, _semanticTokens);
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine(ex);
            }
        }

        [Benchmark]
        public void RunLSPSemanticTokensBenchmark_RazorImpl()
        {
            try
            {
                SemanticTokensEditsDiffer.ComputeSemanticTokensEdits(new int[] { 0, 1, 2, 3, 4 }, _semanticTokens);
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
