// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class SyntaxListBenchmarks
    {
        private SyntaxNode _root;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var csFilePath = @"d:\src\Roslyn2\src\Compilers\CSharp\Portable\Parser\LanguageParser.cs";

            if (!File.Exists(csFilePath))
                throw new FileNotFoundException(csFilePath);

            var textContents = File.ReadAllText(csFilePath);

            var text = SourceText.From(textContents);
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            _root = tree.GetCompilationUnitRoot();
        }

        [Benchmark]
        public void WalkTree()
        {
            for (int i = 0; i < 100; i++)
            {
                foreach (var token in _root.DescendantTokens())
                {
                }
            }
        }
    }
}
