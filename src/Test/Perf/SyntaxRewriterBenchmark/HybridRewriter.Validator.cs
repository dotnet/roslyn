// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias Baseline;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static Roslyn.SyntaxRewriterBenchmark.Program;

#if ParseWithBothCSharpAssemblies
using Baseline::Microsoft.CodeAnalysis;
using Baseline::Microsoft.CodeAnalysis.CSharp;
using Baseline::Microsoft.CodeAnalysis.Text;
#else
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
#endif

namespace Roslyn.SyntaxRewriterBenchmark;

public partial class HybridRewriter
{
    /// <summary>
    /// Validates the equivalency of the methods being performance tested.
    /// </summary>
    private class Validator : CSharpSyntaxRewriter
    {
        internal Tests Tests { get; init; }

        internal Validator(Tests tests)
        {
            Tests = tests;
        }

        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            if (Tests.HasFlag(Tests.IndexOfNodeInParent) && node != null)
            {
                int x, y;

                x = node.BaselineIndexOfNodeInParent();

                y = Unsafe.As<Microsoft.CodeAnalysis.SyntaxNode>(node).IndexOfNodeInParent();

                if (x != y)
                {
                    throw new InvalidOperationException($"baselineIndexOfNodeInParent: {x}, indexOfNodeInParent: {y}");
                }
            }
            return base.Visit(node);
        }

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            var latestToken = token.ToLatest();
            var baselineToken = token.ToBaseline();

            if (Tests.HasFlag(Tests.GetNextToken))
            {
                var x = baselineToken.GetNextToken();
                var y = latestToken.GetNextToken();
                if (x != y.ToBaseline())
                {
                    throw new InvalidOperationException($"baselineNextTokenStart: {x.FullSpan.Start}, nextTokenStart: {y.FullSpan.Start}; baselineNextToken: {x.ValueText}, nextToken: {y.ValueText}");
                }
            }

            if (Tests.HasFlag(Tests.GetPreviousToken))
            {
                var x = baselineToken.GetPreviousToken();
                var y = latestToken.GetPreviousToken();
                if (x != y.ToBaseline())
                {
                    throw new InvalidOperationException($"baselinePrevTokenStart: {x.FullSpan.Start}, prevTokenStart: {y.FullSpan.Start}; baselinePrevToken: {x.ValueText}, prevToken: {y.ValueText}");
                }
            }

            return base.VisitToken(token);
        }
    }
}
