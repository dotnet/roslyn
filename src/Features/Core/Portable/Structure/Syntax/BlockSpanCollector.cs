// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.Structure
{
    internal class BlockSpanCollector
    {
        private readonly BlockStructureOptionProvider _optionProvider;
        private readonly ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> _nodeProviderMap;
        private readonly ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> _triviaProviderMap;
        private readonly CancellationToken _cancellationToken;

        private BlockSpanCollector(
            BlockStructureOptionProvider optionProvider,
            ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> nodeOutlinerMap,
            ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> triviaOutlinerMap,
            CancellationToken cancellationToken)
        {
            _optionProvider = optionProvider;
            _nodeProviderMap = nodeOutlinerMap;
            _triviaProviderMap = triviaOutlinerMap;
            _cancellationToken = cancellationToken;
        }

        public static void CollectBlockSpans(
            SyntaxNode syntaxRoot,
            BlockStructureOptionProvider optionProvider,
            ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxStructureProvider>> nodeOutlinerMap,
            ImmutableDictionary<int, ImmutableArray<AbstractSyntaxStructureProvider>> triviaOutlinerMap,
            ref TemporaryArray<BlockSpan> spans,
            CancellationToken cancellationToken)
        {
            var collector = new BlockSpanCollector(optionProvider, nodeOutlinerMap, triviaOutlinerMap, cancellationToken);
            collector.Collect(syntaxRoot, ref spans);
        }

        private void Collect(SyntaxNode root, ref TemporaryArray<BlockSpan> spans)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            foreach (var nodeOrToken in root.DescendantNodesAndTokensAndSelf(descendIntoTrivia: true))
            {
                if (nodeOrToken.IsNode)
                {
                    GetBlockSpans(nodeOrToken.AsNode()!, ref spans);
                }
                else
                {
                    GetBlockSpans(nodeOrToken.AsToken(), ref spans);
                }
            }
        }

        private void GetBlockSpans(SyntaxNode node, ref TemporaryArray<BlockSpan> spans)
        {
            if (_nodeProviderMap.TryGetValue(node.GetType(), out var providers))
            {
                foreach (var provider in providers)
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    provider.CollectBlockSpans(node, ref spans, _optionProvider, _cancellationToken);
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

                        provider.CollectBlockSpans(trivia, ref spans, _optionProvider, _cancellationToken);
                    }
                }
            }
        }
    }
}
