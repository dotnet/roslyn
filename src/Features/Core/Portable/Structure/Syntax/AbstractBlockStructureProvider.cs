// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Threading;

namespace Microsoft.CodeAnalysis.Structure;

/// <summary>
/// Note: this type is for subclassing by the VB and C# provider only.
/// It presumes that the language supports Syntax Trees.
/// </summary>
internal abstract class AbstractBlockStructureProvider : BlockStructureProvider
{
    private static readonly IComparer<BlockSpan> s_blockSpanComparer = Comparer<BlockSpan>.Create(static (x, y) => y.TextSpan.Start.CompareTo(x.TextSpan.Start));

    private readonly ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> _nodeProviderMap;
    private readonly ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> _triviaProviderMap;

    protected AbstractBlockStructureProvider(
        ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> defaultNodeOutlinerMap,
        ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> defaultTriviaOutlinerMap)
    {
        _nodeProviderMap = defaultNodeOutlinerMap;
        _triviaProviderMap = defaultTriviaOutlinerMap;
    }

    public override void ProvideBlockStructure(in BlockStructureContext context)
    {
        try
        {
            var syntaxRoot = context.SyntaxTree.GetRoot(context.CancellationToken);
            var initialContextCount = context.Spans.Count;
            BlockSpanCollector.CollectBlockSpans(
                syntaxRoot, context.Options, _nodeProviderMap, _triviaProviderMap, context.Spans, context.CancellationToken);

            // Sort descending, and keep track of the "last added line".
            // Then, ignore if we found a span on the same line.
            // The effect for this is if we have something like:
            //
            // M1(M2(
            //     ...
            //     ...
            // )
            //
            // We only collapse the "inner" span which has larger start.
            context.Spans.Sort(initialContextCount, s_blockSpanComparer);
            var text = context.SyntaxTree.GetText(context.CancellationToken);
            BlockSpan? lastSpan = null;

            context.Spans.RemoveAll((span, index, _) =>
                {
                    // do not remove items before the first item that we added
                    if (index < initialContextCount)
                        return false;

                    if (span.IsOverlappingBlockSpan(text.Lines, lastSpan))
                        return true;

                    lastSpan = span;
                    return false;
                },
                arg: default(VoidResult));
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
