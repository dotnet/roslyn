// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
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
        ArrayBuilder<BlockSpan> spans,
        CancellationToken cancellationToken)
    {
        var collector = new BlockSpanCollector(options, nodeOutlinerMap, triviaOutlinerMap, cancellationToken);
        collector.Collect(syntaxRoot, spans);
    }

    private void Collect(SyntaxNode root, ArrayBuilder<BlockSpan> spans)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        SyntaxToken previousToken = default;
        foreach (var nodeOrToken in root.DescendantNodesAndTokensAndSelf(descendIntoTrivia: true))
        {
            if (nodeOrToken.AsNode(out var childNode))
            {
                GetBlockSpans(previousToken, childNode, spans);
            }
            else
            {
                GetBlockSpans(nodeOrToken.AsToken(), spans);
                previousToken = nodeOrToken.AsToken();
            }
        }
    }

    private void GetBlockSpans(SyntaxToken previousToken, SyntaxNode node, ArrayBuilder<BlockSpan> spans)
    {
        if (_nodeProviderMap.TryGetValue(node.GetType(), out var providers))
        {
            foreach (var provider in providers)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                provider.CollectBlockSpans(previousToken, node, spans, _options, _cancellationToken);
            }
        }
    }

    private void GetBlockSpans(SyntaxToken token, ArrayBuilder<BlockSpan> spans)
    {
        GetOutliningSpans(token.LeadingTrivia, spans);
        GetOutliningSpans(token.TrailingTrivia, spans);
    }

    private void GetOutliningSpans(SyntaxTriviaList triviaList, ArrayBuilder<BlockSpan> spans)
    {
        foreach (var trivia in triviaList)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (_triviaProviderMap.TryGetValue(trivia.RawKind, out var providers))
            {
                foreach (var provider in providers)
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    provider.CollectBlockSpans(trivia, spans, _options, _cancellationToken);
                }
            }
        }
    }
}
