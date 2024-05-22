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
        private SyntaxNode _smallRoot;
        private SyntaxNode _mediumRoot;
        private SyntaxNode _largeRoot;
        private const int IterationCount = 1000;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _smallRoot = GetRootFrom(@"d:\src\Roslyn2\src\Compilers\CSharp\Portable\Parser\AbstractLexer.cs");
            _mediumRoot = GetRootFrom(@"d:\src\Roslyn2\src\Compilers\CSharp\Portable\Parser\SyntaxParser.cs");
            _largeRoot = GetRootFrom(@"d:\src\Roslyn2\src\Compilers\CSharp\Portable\Parser\LanguageParser.cs");
        }

        private SyntaxNode GetRootFrom(string path)
        {
            var textContents = File.ReadAllText(path);

            var text = SourceText.From(textContents);
            var tree = SyntaxFactory.ParseSyntaxTree(text);

            return tree.GetCompilationUnitRoot();
        }

        [Benchmark]
        public void WalkTree_Small()
        {
            for (int i = 0; i < IterationCount; i++)
            {
                foreach (var token in _smallRoot.DescendantTokens())
                {
                }
            }
        }

        [Benchmark]
        public void WalkTree_Medium()
        {
            for (int i = 0; i < IterationCount; i++)
            {
                foreach (var token in _mediumRoot.DescendantTokens())
                {
                }
            }
        }

        [Benchmark]
        public void WalkTree_Large()
        {
            for (int i = 0; i < IterationCount; i++)
            {
                foreach (var token in _largeRoot.DescendantTokens())
                {
                }
            }
        }
    }
}
