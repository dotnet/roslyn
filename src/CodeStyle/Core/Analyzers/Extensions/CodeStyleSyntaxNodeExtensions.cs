// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

namespace Microsoft.CodeAnalysis
{
    internal static class CodeStyleSyntaxNodeExtensions
    {
        /// <summary>
        /// Gets the first node of type TNode that matches the predicate.
        /// </summary>
        /// <remarks>
        /// This method was added to <see cref="SyntaxNode"/> as a public API. This extension method can be removed once
        /// the code style layer is updated to reference a version of Roslyn that includes it. It will be easy to
        /// identify since this method will show 0 references once the switch occurs.
        /// </remarks>
        internal static TNode? FirstAncestorOrSelf<TNode, TArg>(this SyntaxNode? node, Func<TNode, TArg, bool> predicate, TArg argument, bool ascendOutOfTrivia = true)
            where TNode : SyntaxNode
        {
            for (; node != null; node = GetParent(node, ascendOutOfTrivia))
            {
                if (node is TNode tnode && predicate(tnode, argument))
                {
                    return tnode;
                }
            }

            return null;
        }

        private static SyntaxNode? GetParent(SyntaxNode node, bool ascendOutOfTrivia)
        {
            var parent = node.Parent;
            if (parent == null && ascendOutOfTrivia)
            {
                var structuredTrivia = node as IStructuredTriviaSyntax;
                if (structuredTrivia != null)
                {
                    parent = structuredTrivia.ParentTrivia.Token.Parent;
                }
            }

            return parent;
        }
    }
}
