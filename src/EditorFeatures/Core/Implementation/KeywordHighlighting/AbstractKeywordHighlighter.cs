// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
{
    internal abstract class AbstractKeywordHighlighter<TNode> : AbstractKeywordHighlighter where TNode : SyntaxNode
    {
        protected sealed override bool IsHighlightableNode(SyntaxNode node) => node is TNode;

        protected sealed override void AddHighlightsForNode(SyntaxNode node, List<TextSpan> highlights, CancellationToken cancellationToken)
            => AddHighlights((TNode)node, highlights, cancellationToken);

        protected abstract void AddHighlights(TNode node, List<TextSpan> highlights, CancellationToken cancellationToken);
    }

    internal abstract class AbstractKeywordHighlighter : IHighlighter
    {
        private static readonly ObjectPool<List<TextSpan>> s_listPool = new ObjectPool<List<TextSpan>>(() => new List<TextSpan>());

        protected abstract bool IsHighlightableNode(SyntaxNode node);

        public void AddHighlights(
            SyntaxNode root, int position, List<TextSpan> highlights, CancellationToken cancellationToken)
        {
            var _ = s_listPool.GetPooledObject();
            var tempHighlights = _.Object;

            foreach (var token in GetTokens(root, position))
            {
                for (var parent = token.Parent; parent != null; parent = parent.Parent)
                {
                    if (IsHighlightableNode(parent))
                    {
                        tempHighlights.Clear();
                        AddHighlightsForNode(parent, tempHighlights, cancellationToken);

                        if (AnyIntersects(position, tempHighlights))
                        {
                            foreach (var highlight in tempHighlights)
                            {
                                highlights.Add(highlight);
                            }
                        }
                    }
                }
            }
        }

        private static bool AnyIntersects(int position, List<TextSpan> highlights)
        {
            foreach (var highlight in highlights)
            {
                if (highlight.IntersectsWith(position))
                {
                    return true;
                }
            }

            return false;
        }

        protected abstract void AddHighlightsForNode(SyntaxNode node, List<TextSpan> highlights, CancellationToken cancellationToken);

        protected TextSpan EmptySpan(int position)
        {
            return new TextSpan(position, 0);
        }

        internal static IEnumerable<SyntaxToken> GetTokens(
            SyntaxNode root,
            int position)
        {
            var tokens1 = GetTokens(root, position, findInsideTrivia: true);
            var tokens2 = GetTokens(root, position, findInsideTrivia: false);
            return tokens1.Concat(tokens2);
        }

        private static IEnumerable<SyntaxToken> GetTokens(
            SyntaxNode root,
            int position,
            bool findInsideTrivia)
        {
            yield return root.FindToken(position - 0, findInsideTrivia);

            if (position > 0)
            {
                yield return root.FindToken(position - 1, findInsideTrivia);
            }
        }
    }
}
