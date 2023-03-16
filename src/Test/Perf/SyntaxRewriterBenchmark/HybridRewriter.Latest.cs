// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.CompilerServices;
using static Roslyn.SyntaxRewriterBenchmark.Program;

namespace Roslyn.SyntaxRewriterBenchmark;

public partial class HybridRewriter
{
    /// <summary>
    /// A C# syntax rewriter that uses assemblies from the current source code for both parsing and syntax analysis. 
    /// </summary>
    private class Latest(HybridRewriter parent) : CSharpSyntaxRewriter
    {
        internal Tests Tests { get; init; } = parent.Tests;
        internal Stopwatch NextTokenTimer { get; init; } = parent.NextTokenTimers.Latest;
        internal Stopwatch PreviousTokenTimer { get; init; } = parent.PreviousTokenTimers.Latest;
        internal Stopwatch IndexOfNodeTimer { get; init; } = parent.IndexOfNodeTimers.Latest;

        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            if (Tests.HasFlag(Tests.IndexOfNodeInParent))
            {
                IndexOfNodeTimer.Start();
                node?.IndexOfNodeInParent();
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
