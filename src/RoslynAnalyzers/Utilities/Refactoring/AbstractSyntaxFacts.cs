// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Analyzer.Utilities
{
    internal abstract class AbstractSyntaxFacts
    {
        public abstract ISyntaxKinds SyntaxKinds { get; }

        public bool IsOnHeader(SyntaxNode root, int position, SyntaxNode ownerOfHeader, SyntaxNodeOrToken lastTokenOrNodeOfHeader)
            => IsOnHeader(root, position, ownerOfHeader, lastTokenOrNodeOfHeader, ImmutableArray<SyntaxNode>.Empty);

        public bool IsOnHeader<THoleSyntax>(
            SyntaxNode root,
            int position,
            SyntaxNode ownerOfHeader,
            SyntaxNodeOrToken lastTokenOrNodeOfHeader,
            ImmutableArray<THoleSyntax> holes)
            where THoleSyntax : SyntaxNode
        {
            Debug.Assert(ownerOfHeader.FullSpan.Contains(lastTokenOrNodeOfHeader.Span));

            var headerSpan = TextSpan.FromBounds(
                start: GetStartOfNodeExcludingAttributes(root, ownerOfHeader),
                end: lastTokenOrNodeOfHeader.FullSpan.End);

            // Is in header check is inclusive, being on the end edge of an header still counts
            if (!headerSpan.IntersectsWith(position))
            {
                return false;
            }

            // Holes are exclusive: 
            // To be consistent with other 'being on the edge' of Tokens/Nodes a position is 
            // in a hole (not in a header) only if it's inside _inside_ a hole, not only on the edge.
            if (holes.Any(h => h.Span.Contains(position) && position > h.Span.Start))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to get an ancestor of a Token on current position or of Token directly to left:
        /// e.g.: tokenWithWantedAncestor[||]tokenWithoutWantedAncestor
        /// </summary>
        protected TNode? TryGetAncestorForLocation<TNode>(SyntaxNode root, int position)
            where TNode : SyntaxNode
        {
            var tokenToRightOrIn = root.FindToken(position);
            var nodeToRightOrIn = tokenToRightOrIn.GetAncestor<TNode>();
            if (nodeToRightOrIn != null)
            {
                return nodeToRightOrIn;
            }

            // not at the beginning of a Token -> no (different) token to the left
            if (tokenToRightOrIn.FullSpan.Start != position && tokenToRightOrIn.RawKind != SyntaxKinds.EndOfFileToken)
            {
                return null;
            }

            return tokenToRightOrIn.GetPreviousToken().GetAncestor<TNode>();
        }

        protected int GetStartOfNodeExcludingAttributes(SyntaxNode root, SyntaxNode node)
        {
            var attributeList = GetAttributeLists(node);
            if (attributeList.Any())
            {
                var endOfAttributeLists = attributeList.Last().Span.End;
                var afterAttributesToken = root.FindTokenOnRightOfPosition(endOfAttributeLists);

                return Math.Min(afterAttributesToken.Span.Start, node.Span.End);
            }

            return node.SpanStart;
        }

        public abstract SyntaxList<SyntaxNode> GetAttributeLists(SyntaxNode node);
    }
}
