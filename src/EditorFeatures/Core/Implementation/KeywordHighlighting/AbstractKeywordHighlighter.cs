// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
{
    internal abstract class AbstractKeywordHighlighter<TNode> : IHighlighter
        where TNode : SyntaxNode
    {
        public IEnumerable<TextSpan> GetHighlights(
            SyntaxNode root, int position, CancellationToken cancellationToken)
        {
            foreach (var token in GetTokens(root, position))
            {
                for (var parent = token.Parent; parent != null; parent = parent.Parent)
                {
                    if (parent is TNode)
                    {
                        var highlights = GetHighlights((TNode)parent, cancellationToken);

                        // Only return them if any of them matched
                        if (highlights.Any(span => span.IntersectsWith(position)))
                        {
                            // Return the non-empty spans
                            return highlights.Where(s => !s.IsEmpty).Distinct();
                        }
                    }
                }
            }

            return SpecializedCollections.EmptyEnumerable<TextSpan>();
        }

        protected abstract IEnumerable<TextSpan> GetHighlights(TNode node, CancellationToken cancellationToken);

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
