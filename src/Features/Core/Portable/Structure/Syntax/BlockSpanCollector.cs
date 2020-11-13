// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Structure
{
    internal class BlockSpanCollector
    {
        private readonly BlockStructureOptionProvider _optionProvider;
        private readonly ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> _nodeProviderMap;
        private readonly ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> _triviaProviderMap;
        private readonly ArrayBuilder<BlockSpan> _spans;
        private readonly CancellationToken _cancellationToken;

        private BlockSpanCollector(
            BlockStructureOptionProvider optionProvider,
            ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> nodeOutlinerMap,
            ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> triviaOutlinerMap,
            ArrayBuilder<BlockSpan> spans,
            CancellationToken cancellationToken)
        {
            _optionProvider = optionProvider;
            _nodeProviderMap = nodeOutlinerMap;
            _triviaProviderMap = triviaOutlinerMap;
            _spans = spans;
            _cancellationToken = cancellationToken;
        }

        public static void CollectBlockSpans(
            SyntaxNode syntaxRoot,
            BlockStructureOptionProvider optionProvider,
            ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> nodeOutlinerMap,
            ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> triviaOutlinerMap,
            ArrayBuilder<BlockSpan> spans,
            CancellationToken cancellationToken)
        {
            var collector = new BlockSpanCollector(optionProvider, nodeOutlinerMap, triviaOutlinerMap, spans, cancellationToken);
            collector.Collect(syntaxRoot);
        }

        private void Collect(SyntaxNode root)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            foreach (var nodeOrToken in root.DescendantNodesAndTokensAndSelf(descendIntoTrivia: true))
            {
                if (nodeOrToken.IsNode)
                {
                    GetBlockSpans(nodeOrToken.AsNode());
                }
                else
                {
                    GetBlockSpans(nodeOrToken.AsToken());
                }
            }
        }

        private void GetBlockSpans(SyntaxNode node)
        {
            if (_nodeProviderMap.TryGetValue(node.GetType(), out var providers))
            {
                foreach (var provider in providers)
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    provider.CollectBlockSpans(node, _spans, _optionProvider, _cancellationToken);
                }
            }
        }

        private void GetBlockSpans(SyntaxToken token)
        {
            GetOutliningSpans(token.LeadingTrivia);
            GetOutliningSpans(token.TrailingTrivia);
        }

        private void GetOutliningSpans(SyntaxTriviaList triviaList)
        {
            foreach (var trivia in triviaList)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                if (_triviaProviderMap.TryGetValue(trivia.RawKind, out var providers))
                {
                    foreach (var provider in providers)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();

                        provider.CollectBlockSpans(trivia, _spans, _optionProvider, _cancellationToken);
                    }
                }
            }
        }
    }
}
