// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias Baseline;
using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.CompilerServices;
using static Roslyn.SyntaxRewriterBenchmark.Program;
using SyntaxNode0 = Baseline::Microsoft.CodeAnalysis.SyntaxNode;
using SyntaxToken0 = Baseline::Microsoft.CodeAnalysis.SyntaxToken;

namespace Roslyn.SyntaxRewriterBenchmark;

/// <summary>
/// A syntax rewriter with different ways to parse and analyze C#.
/// </summary>
public partial class HybridRewriter : CSharpSyntaxRewriter
{
    private readonly Random _randomGen = new();

    public Tests Tests { get; init; } = DefaultTests;

    public (TimeSpan Baseline, TimeSpan Latest) NextTokenTimes => (NextTokenTimers.Baseline.Elapsed, NextTokenTimers.Latest.Elapsed);

    public (TimeSpan Baseline, TimeSpan Latest) PreviousTokenTimes => (PreviousTokenTimers.Baseline.Elapsed, PreviousTokenTimers.Latest.Elapsed);

    public (TimeSpan Baseline, TimeSpan Latest) IndexOfNodeTimes => (IndexOfNodeTimers.Baseline.Elapsed, IndexOfNodeTimers.Latest.Elapsed);

    public (TimeSpan Baseline, TimeSpan Latest) OverallTraversalTimes => (OverallTraversalTimers.Baseline.Elapsed, OverallTraversalTimers.Latest.Elapsed);

    /// <summary>
    /// If parsing with both roslyn C# assemblies,
    /// the validation time represents the time it takes for the baseline C# syntax rewriter to run the latest and baseline versions of each test,
    /// while ensuring that they produce the same results.
    /// </summary>
    public TimeSpan ValidationTime => ValidationTimer.Elapsed;

    public (Stopwatch Baseline, Stopwatch Latest) NextTokenTimers { private get; init; } = (new(), new());

    public (Stopwatch Baseline, Stopwatch Latest) PreviousTokenTimers { private get; init; } = (new(), new());

    public (Stopwatch Baseline, Stopwatch Latest) IndexOfNodeTimers { private get; init; } = (new(), new());

    public (Stopwatch Baseline, Stopwatch Latest) OverallTraversalTimers { private get; init; } = (new(), new());

    /// <inheritdoc cref="ValidationTime"/>
    public Stopwatch ValidationTimer { private get; init; } = new();

#if ParseWithBothCSharpAssemblies

    /// <summary>
    /// Uses both roslyn C# assemblies (from the current source code and from the baseline set of packages) for parsing, <br/>
    /// while measuring the syntax analysis performance of the core assemblies (from the current source code and from the baseline set of packages).
    /// </summary>
    public (SyntaxNode0? Baseline, SyntaxNode? Latest) Visit(SyntaxNode0? baselineRoot, SyntaxNode? latestRoot)
    {
        if (_randomGen.Next(2) == 0)
        {
            OverallTraversalTimers.Baseline.Start();
            baselineRoot = new Baseline(this).Visit(baselineRoot);
            OverallTraversalTimers.Baseline.Stop();

            OverallTraversalTimers.Latest.Start();
            latestRoot = new Latest(this).Visit(latestRoot);
            OverallTraversalTimers.Latest.Stop();
        }
        else
        {
            OverallTraversalTimers.Latest.Start();
            latestRoot = new Latest(this).Visit(latestRoot);
            OverallTraversalTimers.Latest.Stop();

            OverallTraversalTimers.Baseline.Start();
            baselineRoot = new Baseline(this).Visit(baselineRoot);
            OverallTraversalTimers.Baseline.Stop();
        }

        if (Tests.HasFlag(Tests.Validation))
        {
            ValidationTimer.Start();
            new Validator(Tests).Visit(baselineRoot);
            ValidationTimer.Stop();
        }

        return (baselineRoot, latestRoot);
    }

#endif

    /// <summary>
    /// Uses only the lastest roslyn C# assembly (from the current source code) for parsing, <br/>
    /// while measuring the syntax analysis performance of both the latest and the baseline core assemblies.
    /// </summary>
    public override SyntaxNode? Visit(SyntaxNode? node)
    {
        if (Tests.HasFlag(Tests.IndexOfNodeInParent) && node != null)
        {
            int x, y;

            if (_randomGen.Next(2) == 0)
            {
                IndexOfNodeTimers.Baseline.Start();
                x = node.BaselineIndexOfNodeInParent();
                IndexOfNodeTimers.Baseline.Stop();

                IndexOfNodeTimers.Latest.Start();
                y = node.IndexOfNodeInParent();
                IndexOfNodeTimers.Latest.Stop();
            }
            else
            {
                IndexOfNodeTimers.Latest.Start();
                y = node.IndexOfNodeInParent();
                IndexOfNodeTimers.Latest.Stop();

                IndexOfNodeTimers.Baseline.Start();
                x = node.BaselineIndexOfNodeInParent();
                IndexOfNodeTimers.Baseline.Stop();
            }

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
            SyntaxToken0 x;
            SyntaxToken y;

            if (_randomGen.Next(2) == 0)
            {
                NextTokenTimers.Baseline.Start();
                x = baselineToken.GetNextToken();
                NextTokenTimers.Baseline.Stop();

                NextTokenTimers.Latest.Start();
                y = latestToken.GetNextToken();
                NextTokenTimers.Latest.Stop();
            }
            else
            {
                NextTokenTimers.Latest.Start();
                y = latestToken.GetNextToken();
                NextTokenTimers.Latest.Stop();

                NextTokenTimers.Baseline.Start();
                x = baselineToken.GetNextToken();
                NextTokenTimers.Baseline.Stop();
            }

            if (x.FromBaseline() != y)
            {
                throw new InvalidOperationException($"baselineNextTokenStart: {x.FullSpan.Start}, nextTokenStart: {y.FullSpan.Start}; baselineNextToken: {x.ValueText}, nextToken: {y.ValueText}");
            }
        }

        if (Tests.HasFlag(Tests.GetPreviousToken))
        {
            SyntaxToken0 x;
            SyntaxToken y;

            if (_randomGen.Next(2) == 0)
            {
                PreviousTokenTimers.Baseline.Start();
                x = baselineToken.GetPreviousToken();
                PreviousTokenTimers.Baseline.Stop();

                PreviousTokenTimers.Latest.Start();
                y = latestToken.GetPreviousToken();
                PreviousTokenTimers.Latest.Stop();
            }
            else
            {
                PreviousTokenTimers.Latest.Start();
                y = latestToken.GetPreviousToken();
                PreviousTokenTimers.Latest.Stop();

                PreviousTokenTimers.Baseline.Start();
                x = baselineToken.GetPreviousToken();
                PreviousTokenTimers.Baseline.Stop();
            }

            if (x.FromBaseline() != y)
            {
                throw new InvalidOperationException($"baselinePrevTokenStart: {x.FullSpan.Start}, prevTokenStart: {y.FullSpan.Start}; baselinePrevToken: {x.ValueText}, prevToken: {y.ValueText}");
            }
        }

        return base.VisitToken(token);
    }

}
