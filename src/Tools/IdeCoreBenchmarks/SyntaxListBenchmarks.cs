// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
        private const int IterationCountWalkClassification = 10;

        [GlobalSetup]
        public void GlobalSetup()
        {
            (_smallRoot, _smallText) = GetRootAndTextFrom(@"src\Compilers\CSharp\Portable\Parser\AbstractLexer.cs");
            (_mediumRoot, _mediumText) = GetRootAndTextFrom(@"src\Compilers\CSharp\Portable\Parser\SyntaxParser.cs");
            (_largeRoot, _largeText) = GetRootAndTextFrom(@"src\Compilers\CSharp\Portable\Parser\LanguageParser.cs");
        }

        private (SyntaxNode, SourceText) GetRootAndTextFrom(string roslynRelativePath)
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);

            var textContents = File.ReadAllText(Path.Combine(roslynRoot, roslynRelativePath));

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
        public void WalkLines_Medium()
        {
            WalkLines(_mediumText, _mediumRoot);
        }

        [Benchmark]
        public void WalkLines_Large()
        {
            WalkLines(_largeText, _largeRoot);
        }

        [Benchmark]
        public void WalkTree_Small()
        {
            WalkTree(_smallRoot);
        }

        [Benchmark]
        public void WalkTree_Medium()
        {
            WalkTree(_mediumRoot);
        }

        [Benchmark]
        public void WalkTree_Large()
        {
            WalkTree(_largeRoot);
        }

        [Benchmark]
        public void WalkClassifications_Small()
        {
            WalkClassification(_smallText, _smallRoot);
        }

        [Benchmark]
        public void WalkClassifications_Medium()
        {
            WalkClassification(_mediumText, _mediumRoot);
        }

        [Benchmark]
        public void WalkClassifications_Large()
        {
            WalkClassification(_largeText, _largeRoot);
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

        private static void WalkClassification(SourceText sourceText, SyntaxNode root)
        {
            for (var i = 0; i < IterationCountWalkClassification; i++)
            {
                foreach (var line in sourceText.Lines)
                {
                    ClassificationTokens(line.Span, root, default);
                }
            }
        }

        private static void ClassificationTokens(TextSpan span, SyntaxNode root, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var stack);
            stack.Push(root);

            var textSpanStart = span.Start;
            var textSpanEnd = span.End;

            while (stack.TryPop(out var current))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // It's ok that we're not pushing in reverse.  The caller (TotalClassificationTaggerProvider) will be
                // sorting the results before doing anything with them.
                foreach (var child in current.ChildNodesAndTokens())
                {
                    if (child.AsNode(out var childNode))
                    {
                        var childSpan = childNode.FullSpan;

                        // If we haven't reached the start of the span we care about, then we can skip this node, going to
                        // the next.  Once we go past that span, we can stop immediately.  Otherwise, we must be
                        // intersecting the span, and we should recurse into this child.
                        if (childSpan.End < textSpanStart)
                            continue;
                        else if (childSpan.Start > textSpanEnd)
                            break;
                        else
                            stack.Push(childNode);
                    }
                    else
                    {
                        ClassifyToken(child);
                    }
                }
            }
        }

        private static void ClassifyToken(SyntaxNodeOrToken child)
        {
        }
    }
}
