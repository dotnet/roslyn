// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Structure;

/// <summary>
/// Note: this type is for subclassing by the VB and C# provider only.
/// It presumes that the language supports Syntax Trees.
/// </summary>
internal abstract class AbstractBlockStructureProvider : BlockStructureProvider
{
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
            using var spans = TemporaryArray<BlockSpan>.Empty;
            BlockSpanCollector.CollectBlockSpans(
                syntaxRoot, context.Options, _nodeProviderMap, _triviaProviderMap, ref spans.AsRef(), context.CancellationToken);

            context.Spans.EnsureCapacity(context.Spans.Count + spans.Count);

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
            spans.Sort(static (x, y) => y.TextSpan.Start.CompareTo(x.TextSpan.Start));

            var lastAddedLineStart = -1;
            var lastAddedLineEnd = -1;
            var text = context.SyntaxTree.GetText(context.CancellationToken);

            foreach (var span in spans)
            {
                var lineStart = text.Lines.GetLinePosition(span.TextSpan.Start).Line;
                var lineEnd = text.Lines.GetLinePosition(span.TextSpan.End).Line;
                if (lineStart == lastAddedLineStart && lastAddedLineEnd == lineEnd)
                    continue;

                lastAddedLineStart = lineStart;
                lastAddedLineEnd = lineEnd;
                context.Spans.Add(span);
            }
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
