// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.MisplacedUsings
{
    /// <summary>
    /// Implements a code fix for all misplaced using statements.
    /// </summary>
    internal partial class MisplacedUsingsCodeFixProvider
    {
        internal static class FormattingHelper
        {
            /// <summary>
            /// Transforms a <see cref="SyntaxNode"/> to ensure no formatting operations will be applied to the node or any
            /// of its descendants when a <see cref="CodeAction"/> is applied.
            /// </summary>
            /// <typeparam name="TNode">The specific type of syntax node.</typeparam>
            /// <param name="node">The syntax node.</param>
            /// <returns>
            /// A syntax node which is equivalent to the input <paramref name="node"/>, but which will not be subject to
            /// automatic code formatting operations when applied as part of a <see cref="CodeAction"/>.
            /// </returns>
            public static TNode WithoutFormatting<TNode>(TNode node)
                where TNode : SyntaxNode
            {
                /* Strategy:
                 *  1. Transform all descendants of the node (nodes, tokens, and trivia), but not the node itself
                 *  2. Transform the resulting node itself
                 */
                TNode result = node.ReplaceSyntax(
                    node.DescendantNodes(descendIntoTrivia: true),
                    (originalNode, rewrittenNode) => WithoutFormattingImpl(rewrittenNode),
                    node.DescendantTokens(descendIntoTrivia: true),
                    (originalToken, rewrittenToken) => WithoutFormattingImpl(rewrittenToken),
                    node.DescendantTrivia(descendIntoTrivia: true),
                    (originalTrivia, rewrittenTrivia) => WithoutFormattingImpl(rewrittenTrivia));

                return WithoutFormattingImpl(result);
            }

            /// <summary>
            /// Remove formatting from a single <see cref="SyntaxNode"/>. The descendants of the node, including its leading
            /// and trailing trivia, are not altered by this method.
            /// </summary>
            /// <typeparam name="TNode">The specific type of syntax node.</typeparam>
            /// <param name="node">The syntax node.</param>
            /// <returns>
            /// A syntax node which is equivalent to the input <paramref name="node"/>, but which will not be subject to
            /// automatic code formatting operations when applied as part of a <see cref="CodeAction"/>.
            /// </returns>
            private static TNode WithoutFormattingImpl<TNode>(TNode node)
                where TNode : SyntaxNode
            {
                return node.WithoutAnnotations(Formatter.Annotation, SyntaxAnnotation.ElasticAnnotation);
            }

            /// <summary>
            /// Remove formatting from a single <see cref="SyntaxToken"/>. The descendants of the token, including its
            /// leading and trailing trivia, are not altered by this method.
            /// </summary>
            /// <param name="token">The syntax token.</param>
            /// <returns>
            /// A syntax token which is equivalent to the input <paramref name="token"/>, but which will not be subject to
            /// automatic code formatting operations when applied as part of a <see cref="CodeAction"/>.
            /// </returns>
            private static SyntaxToken WithoutFormattingImpl(SyntaxToken token)
            {
                return token.WithoutAnnotations(Formatter.Annotation, SyntaxAnnotation.ElasticAnnotation);
            }

            /// <summary>
            /// Remove formatting from a single <see cref="SyntaxTrivia"/>. The descendants of the trivia, including any
            /// structure it contains, are not altered by this method.
            /// </summary>
            /// <param name="trivia">The syntax trivia.</param>
            /// <returns>
            /// A syntax trivia which is equivalent to the input <paramref name="trivia"/>, but which will not be subject to
            /// automatic code formatting operations when applied as part of a <see cref="CodeAction"/>.
            /// </returns>
            private static SyntaxTrivia WithoutFormattingImpl(SyntaxTrivia trivia)
            {
                return trivia.WithoutAnnotations(Formatter.Annotation, SyntaxAnnotation.ElasticAnnotation);
            }
        }
    }
}
