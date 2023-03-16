// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias Baseline;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Baseline::Microsoft.CodeAnalysis;
using Baseline::Microsoft.CodeAnalysis.CSharp;
using Baseline::Microsoft.CodeAnalysis.Text;
using static Roslyn.SyntaxRewriterBenchmark.Program;

namespace Roslyn.SyntaxRewriterBenchmark;

public partial class HybridRewriter
{
    /// <summary>
    /// A C# syntax rewriter that uses assemblies from the baseline set of packages for both parsing and syntax analysis. 
    /// </summary>
    private class Baseline(HybridRewriter parent) : CSharpSyntaxRewriter
    {
        internal Tests Tests { get; init; } = parent.Tests;
        internal Stopwatch NextTokenTimer { get; init; } = parent.NextTokenTimers.Baseline;
        internal Stopwatch PreviousTokenTimer { get; init; } = parent.PreviousTokenTimers.Baseline;
        internal Stopwatch IndexOfNodeTimer { get; init; } = parent.IndexOfNodeTimers.Baseline;

        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            if (Tests.HasFlag(Tests.IndexOfNodeInParent))
            {
                IndexOfNodeTimer.Start();
                node?.BaselineIndexOfNodeInParent();
                IndexOfNodeTimer.Stop();
            }

            return base.Visit(node);
        }

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            if (Tests.HasFlag(Tests.GetNextToken))
            {
                NextTokenTimer.Start();
                token.GetNextToken();
                NextTokenTimer.Stop();
            }

            if (Tests.HasFlag(Tests.GetPreviousToken))
            {
                PreviousTokenTimer.Start();
                token.GetPreviousToken();
                PreviousTokenTimer.Stop();
            }

            return base.VisitToken(token);
        }
    }
}
