// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Structure
{
    internal class BlockSpanCollector
    {
        private readonly Document _document;
        private readonly ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> _nodeProviderMap;
        private readonly ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> _triviaProviderMap;
        private readonly ArrayBuilder<BlockSpan> _spans;
        private readonly CancellationToken _cancellationToken;

        private BlockSpanCollector(
            Document document,
            ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> nodeOutlinerMap,
            ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> triviaOutlinerMap,
            ArrayBuilder<BlockSpan> spans,
            CancellationToken cancellationToken)
        {
            _document = document;
            _nodeProviderMap = nodeOutlinerMap;
            _triviaProviderMap = triviaOutlinerMap;
            _spans = spans;
            _cancellationToken = cancellationToken;
        }

        public static void CollectBlockSpans(
            Document document,
            SyntaxNode syntaxRoot,
            ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> nodeOutlinerMap,
            ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> triviaOutlinerMap,
            ArrayBuilder<BlockSpan> spans,
            CancellationToken cancellationToken)
        {
            var collector = new BlockSpanCollector(document, nodeOutlinerMap, triviaOutlinerMap, spans, cancellationToken);
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
            ImmutableArray<AbstractSyntaxStructureProvider> providers;
            if (_nodeProviderMap.TryGetValue(node.GetType(), out providers))
            {
                foreach (var provider in providers)
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    provider.CollectBlockSpans(_document, node, _spans, _cancellationToken);
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

                ImmutableArray<AbstractSyntaxStructureProvider> providers;
                if (_triviaProviderMap.TryGetValue(trivia.RawKind, out providers))
                {
                    foreach (var provider in providers)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();

                        provider.CollectBlockSpans(_document, trivia, _spans, _cancellationToken);
                    }
                }
            }
        }
    }
}