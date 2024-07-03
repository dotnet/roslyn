// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Structure;

internal sealed class BlockSpanCollector
{
    private readonly BlockStructureOptions _options;
    private readonly ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> _nodeProviderMap;
    private readonly ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> _triviaProviderMap;
    private readonly CancellationToken _cancellationToken;

    private BlockSpanCollector(
        BlockStructureOptions options,
        ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> nodeOutlinerMap,
        ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> triviaOutlinerMap,
        CancellationToken cancellationToken)
    {
        _options = options;
        _nodeProviderMap = nodeOutlinerMap;
        _triviaProviderMap = triviaOutlinerMap;
        _cancellationToken = cancellationToken;
    }

    public static void CollectBlockSpans(
        SyntaxNode syntaxRoot,
        BlockStructureOptions options,
        ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> nodeOutlinerMap,
        ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> triviaOutlinerMap,
        ref TemporaryArray<BlockSpan> spans,
        CancellationToken cancellationToken)
    {
        var collector = new BlockSpanCollector(options, nodeOutlinerMap, triviaOutlinerMap, cancellationToken);
        collector.Collect(syntaxRoot, ref spans);
    }

    private void Collect(SyntaxNode root, ref TemporaryArray<BlockSpan> spans)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        SyntaxToken previousToken = default;
        foreach (var nodeOrToken in root.DescendantNodesAndTokensAndSelf(descendIntoTrivia: true))
        {
            if (nodeOrToken.AsNode(out var childNode))
            {
                GetBlockSpans(previousToken, childNode, ref spans);
            }
            else
            {
                GetBlockSpans(nodeOrToken.AsToken(), ref spans);
                previousToken = nodeOrToken.AsToken();
            }
        }
    }

    private void GetBlockSpans(SyntaxToken previousToken, SyntaxNode node, ref TemporaryArray<BlockSpan> spans)
    {
        if (_nodeProviderMap.TryGetValue(node.GetType(), out var providers))
        {
            foreach (var provider in providers)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                provider.CollectBlockSpans(previousToken, node, ref spans, _options, _cancellationToken);
            }
        }
    }

    private void GetBlockSpans(SyntaxToken token, ref TemporaryArray<BlockSpan> spans)
    {
        GetOutliningSpans(token.LeadingTrivia, ref spans);
        GetOutliningSpans(token.TrailingTrivia, ref spans);
    }

    private void GetOutliningSpans(SyntaxTriviaList triviaList, ref TemporaryArray<BlockSpan> spans)
    {
        foreach (var trivia in triviaList)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (_triviaProviderMap.TryGetValue(trivia.RawKind, out var providers))
            {
                foreach (var provider in providers)
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    provider.CollectBlockSpans(trivia, ref spans, _options, _cancellationToken);
                }
            }
        }
    }
}
