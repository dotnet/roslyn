// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes
{
    internal abstract class AbstractRemoveDocCommentNodeCodeFixProvider<TXmlElementSyntax, TXmlTextSyntax> : CodeFixProvider
        where TXmlElementSyntax : SyntaxNode
        where TXmlTextSyntax : SyntaxNode
    {
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public abstract override ImmutableArray<string> FixableDiagnosticIds { get; }

        protected abstract string DocCommentSignifierToken { get; }

        protected abstract SyntaxTriviaList GetRevisedDocCommentTrivia(string docCommentText);

        protected abstract SyntaxTokenList GetTextTokens(TXmlTextSyntax xmlText);
        protected abstract bool IsXmlNewLineToken(SyntaxToken token);
        protected abstract bool IsXmlWhitespaceToken(SyntaxToken token);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (GetParamNode(root, context.Span) != null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        FeaturesResources.Remove_tag,
                        c => RemoveDuplicateParamTagAsync(context.Document, context.Span, c),
                        nameof(FeaturesResources.Remove_tag)),
                    context.Diagnostics);
            }
        }

        private static TXmlElementSyntax GetParamNode(SyntaxNode root, TextSpan span)
        {
            // First, we get the node the diagnostic fired on
            // Then, we climb the tree to the first parent that is of the type XMLElement
            // This is to correctly handle XML nodes that are nested in other XML nodes, so we only
            // remove the node the diagnostic fired on and its children, but no parent nodes
            var paramNode = root.FindNode(span, findInsideTrivia: true);
            return paramNode.FirstAncestorOrSelf<TXmlElementSyntax>();
        }

        private async Task<Document> RemoveDuplicateParamTagAsync(
            Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var paramNode = GetParamNode(root, span);

            var removedNodes = new List<SyntaxNode> { paramNode };
            var paramNodeSiblings = paramNode.Parent.ChildNodes().ToList();

            // This should not cause a crash because the diagnostics are only thrown in
            // doc comment XML nodes, which, by definition, start with `///` (C#) or `'''` (VB.NET)
            // If, perhaps, this specific node is not directly preceded by the comment marker node,
            // it will be preceded by another XML node
            var paramNodeIndex = paramNodeSiblings.IndexOf(paramNode);

            if (ShouldRemovePreviousSibling(paramNodeSiblings, paramNodeIndex))
            {
                removedNodes.Add(paramNodeSiblings[paramNodeIndex - 1]);
            }

            // Remove all trivia attached to the nodes I am removing.
            // Really, any option should work here because the leading/trailing text
            // around these nodes are not attached to them as trivia.
            var newRoot = root.RemoveNodes(removedNodes, SyntaxRemoveOptions.KeepNoTrivia);
            return document.WithSyntaxRoot(newRoot);
        }

        private bool ShouldRemovePreviousSibling(List<SyntaxNode> paramNodeSiblings, int paramNodeIndex)
        {
            if (paramNodeIndex > 0)
            {
                var previousNodeTextTrimmed = paramNodeSiblings[paramNodeIndex - 1].ToFullString().Trim();

                if (previousNodeTextTrimmed == string.Empty ||
                    previousNodeTextTrimmed == DocCommentSignifierToken)
                {
                    // Only remove the preceding /// if this param node is also the only thing on this line.
                    if (paramNodeIndex + 1 < paramNodeSiblings.Count)
                    {
                        var nextSibling = paramNodeSiblings[paramNodeIndex + 1];
                        if (nextSibling is TXmlTextSyntax textSyntax)
                        {
                            // Walk the next text block forward, making sure we only see whitespace
                            // until we hit the next newline.  If that's all we can remove the preceding
                            // '///'.  Otherwise we'll want to keep it to keep whatever comes after
                            // this node valid.
                            foreach (var childToken in GetTextTokens(textSyntax))
                            {
                                if (IsXmlWhitespaceToken(childToken))
                                {
                                    continue;
                                }

                                if (IsXmlNewLineToken(childToken))
                                {
                                    return true;
                                }

                                return false;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
