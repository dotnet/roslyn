// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        private SourceText _smallText;
        private SourceText _mediumText;
        private SourceText _largeText;

        private const int IterationCountWalkTree = 1000;
        private const int IterationCountWalkLines = 10;

        [GlobalSetup]
        public void GlobalSetup()
        {
            (_smallRoot, _smallText) = GetRootAndTextFrom(@"d:\src\Roslyn2\src\Compilers\CSharp\Portable\Parser\AbstractLexer.cs");
            (_mediumRoot, _mediumText) = GetRootAndTextFrom(@"d:\src\Roslyn2\src\Compilers\CSharp\Portable\Parser\SyntaxParser.cs");
            (_largeRoot, _largeText) = GetRootAndTextFrom(@"d:\src\Roslyn2\src\Compilers\CSharp\Portable\Parser\LanguageParser.cs");
        }

        private (SyntaxNode, SourceText) GetRootAndTextFrom(string path)
        {
            var textContents = File.ReadAllText(path);

            var text = SourceText.From(textContents);
            var tree = SyntaxFactory.ParseSyntaxTree(text);

            return (tree.GetCompilationUnitRoot(), text);
        }

        [Benchmark]
        public void WalkLines_Small()
        {
            WalkLines(_smallText, _smallRoot);
        }

        [Benchmark]
        public void WalkTree_Small()
        {
            WalkTree(_smallRoot);
        }

        [Benchmark]
        public void WalkLines_Medium()
        {
            WalkLines(_mediumText, _mediumRoot);
        }

        [Benchmark]
        public void WalkTree_Medium()
        {
            WalkTree(_mediumRoot);
        }

        [Benchmark]
        public void WalkLines_Large()
        {
            WalkLines(_largeText, _largeRoot);
        }

        [Benchmark]
        public void WalkTree_Large()
        {
            WalkTree(_largeRoot);
        }

        private static void WalkLines(SourceText sourceText, SyntaxNode root)
        {
            for (var i = 0; i < IterationCountWalkLines; i++)
            {
                foreach (var line in sourceText.Lines)
                {
                    foreach (var token in root.DescendantTokens(line.Span))
                    {
                    }
                }
            }
        }

        private static void WalkTree(SyntaxNode root)
        {
            for (var i = 0; i < IterationCountWalkTree; i++)
            {
                foreach (var token in root.DescendantTokens())
                {
                }
            }
        }
    }
}
