﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract class AbstractHeaderFacts : IHeaderFacts
    {
        protected abstract ISyntaxFacts SyntaxFacts { get; }

        public abstract bool IsOnTypeHeader(SyntaxNode root, int position, bool fullHeader, [NotNullWhen(true)] out SyntaxNode? typeDeclaration);
        public abstract bool IsOnPropertyDeclarationHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? propertyDeclaration);
        public abstract bool IsOnParameterHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? parameter);
        public abstract bool IsOnMethodHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? method);
        public abstract bool IsOnLocalFunctionHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? localFunction);
        public abstract bool IsOnLocalDeclarationHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? localDeclaration);
        public abstract bool IsOnIfStatementHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? ifStatement);
        public abstract bool IsOnWhileStatementHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? whileStatement);
        public abstract bool IsOnForeachHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? foreachStatement);

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
        protected TNode? TryGetAncestorForLocation<TNode>(SyntaxNode root, int position) where TNode : SyntaxNode
        {
            var tokenToRightOrIn = root.FindToken(position);
            var nodeToRightOrIn = tokenToRightOrIn.GetAncestor<TNode>();
            if (nodeToRightOrIn != null)
            {
                return nodeToRightOrIn;
            }

            // not at the beginning of a Token -> no (different) token to the left
            if (tokenToRightOrIn.FullSpan.Start != position && tokenToRightOrIn.RawKind != SyntaxFacts.SyntaxKinds.EndOfFileToken)
            {
                return null;
            }

            return tokenToRightOrIn.GetPreviousToken().GetAncestor<TNode>();
        }

        protected int GetStartOfNodeExcludingAttributes(SyntaxNode root, SyntaxNode node)
        {
            var attributeList = SyntaxFacts.GetAttributeLists(node);
            if (attributeList.Any())
            {
                var endOfAttributeLists = attributeList.Last().Span.End;
                var afterAttributesToken = root.FindTokenOnRightOfPosition(endOfAttributeLists);

                return Math.Min(afterAttributesToken.Span.Start, node.Span.End);
            }

            return node.SpanStart;
        }
    }
}
