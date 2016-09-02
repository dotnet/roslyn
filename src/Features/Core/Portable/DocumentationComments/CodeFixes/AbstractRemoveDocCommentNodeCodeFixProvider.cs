// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal abstract class AbstractRemoveDocCommentNodeCodeFixProvider<TXmlElementSyntax> : CodeFixProvider
        where TXmlElementSyntax : SyntaxNode
    {
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public abstract override ImmutableArray<string> FixableDiagnosticIds { get; }

        protected abstract string DocCommentSignifierToken { get; }

        protected abstract SyntaxTriviaList GetRevisedDocCommentTrivia(string docCommentText);

        public async sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (GetParamNode(root, context.Span) != null)
            {
                context.RegisterCodeFix(
                    new MyCodeAction(
                        c => RemoveDuplicateParamTagAsync(context.Document, context.Span, c)),
                    context.Diagnostics);
            }
        }

        private TXmlElementSyntax GetParamNode(SyntaxNode root, TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
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
            var paramNode = GetParamNode(root, span, cancellationToken);

            var removedNodes = new List<SyntaxNode> { paramNode };
            var paramNodeSiblings = paramNode.Parent.ChildNodes().ToList();

            // This should not cause a crash because the diagnostics are only thrown in
            // doc comment XML nodes, which, by definition, start with `///` (C#) or `'''` (VB.NET)
            // If, perhaps, this specific node is not directly preceded by the comment marker node,
            // it will be preceded by another XML node
            var paramNodeIndex = paramNodeSiblings.IndexOf(paramNode);
            var previousNodeTextTrimmed = paramNodeSiblings[paramNodeIndex - 1].ToFullString().Trim();

            if (previousNodeTextTrimmed == string.Empty || previousNodeTextTrimmed == DocCommentSignifierToken)
            {
                removedNodes.Add(paramNodeSiblings[paramNodeIndex - 1]);
            }

            // Remove all trivia attached to the nodes I am removing.
            // Really, any option should work here because the leading/trailing text
            // around these nodes are not attached to them as trivia.
            var newRoot = root.RemoveNodes(removedNodes, SyntaxRemoveOptions.KeepNoTrivia);
            return document.WithSyntaxRoot(newRoot);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Remove_tag, createChangedDocument)
            {
            }
        }
    }
}