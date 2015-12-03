// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Outlining
{
    internal class RegionCollector
    {
        private readonly SyntacticDocument _document;
        private readonly ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxOutliner>> _nodeOutlinerMap;
        private readonly ImmutableDictionary<int, ImmutableArray<AbstractSyntaxOutliner>> _triviaOutlinerMap;
        private readonly List<OutliningSpan> _regions;
        private readonly CancellationToken _cancellationToken;

        private RegionCollector(
            SyntacticDocument document,
            ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxOutliner>> nodeOutlinerMap,
            ImmutableDictionary<int, ImmutableArray<AbstractSyntaxOutliner>> triviaOutlinerMap,
            List<OutliningSpan> spans,
            CancellationToken cancellationToken)
        {
            _document = document;
            _nodeOutlinerMap = nodeOutlinerMap;
            _triviaOutlinerMap = triviaOutlinerMap;
            _regions = spans;
            _cancellationToken = cancellationToken;
        }

        public static void CollectOutliningSpans(
            SyntacticDocument document,
            ImmutableDictionary<Type, ImmutableArray<AbstractSyntaxOutliner>> nodeOutlinerMap,
            ImmutableDictionary<int, ImmutableArray<AbstractSyntaxOutliner>> triviaOutlinerMap,
            List<OutliningSpan> spans,
            CancellationToken cancellationToken)
        {
            var collector = new RegionCollector(document, nodeOutlinerMap, triviaOutlinerMap, spans, cancellationToken);
            collector.Collect(document.Root);
        }

        private void Collect(SyntaxNode root)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            foreach (var nodeOrToken in root.DescendantNodesAndTokensAndSelf(descendIntoTrivia: true))
            {
                if (nodeOrToken.IsNode)
                {
                    GetOutliningSpans(nodeOrToken.AsNode());
                }
                else
                {
                    GetOutliningSpans(nodeOrToken.AsToken());
                }
            }
        }

        private void GetOutliningSpans(SyntaxNode node)
        {
            ImmutableArray<AbstractSyntaxOutliner> outliners;
            if (_nodeOutlinerMap.TryGetValue(node.GetType(), out outliners))
            {
                foreach (var outliner in outliners)
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    outliner.CollectOutliningSpans(_document.Document, node, _regions, _cancellationToken);
                }
            }
        }

        private void GetOutliningSpans(SyntaxToken token)
        {
            GetOutliningSpans(token.LeadingTrivia);
            GetOutliningSpans(token.TrailingTrivia);
        }

        private void GetOutliningSpans(SyntaxTriviaList triviaList)
        {
            foreach (var trivia in triviaList)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                ImmutableArray<AbstractSyntaxOutliner> outliners;
                if (_triviaOutlinerMap.TryGetValue(trivia.RawKind, out outliners))
                {
                    foreach (var outliner in outliners)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();

                        outliner.CollectOutliningSpans(_document.Document, trivia, _regions, _cancellationToken);
                    }
                }
            }
        }
    }
}
