// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Roslyn.Utilities;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes
{
    internal abstract class AbstractRemoveDocCommentNodeCodeFixProvider : CodeFixProvider
    {
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public abstract override ImmutableArray<string> FixableDiagnosticIds { get; }
        protected abstract string DocCommentSignifierToken { get; }

        protected abstract SyntaxTriviaList GetRevisedDocCommentTrivia(string docCommentText);

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(
                    c => RemoveDuplicateParamTagAsync(context.Document, context.Span, c)),
                context.Diagnostics);

            return SpecializedTasks.EmptyTask;
        }

        private async Task<Document> RemoveDuplicateParamTagAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var commentParent = root.FindNode(span);

            // There *should* always be one node here if the diagnostic fires,
            // but we will use `FirstOrDefault` just to safeguard against a sly bug
            var triviaNode = commentParent.GetLeadingTrivia().FirstOrDefault(s => s.Span.Contains(span));
            if (triviaNode == null)
            {
                return document;
            }

            var triviaNodeStructure = triviaNode.GetStructure();

            // See comment above
            var paramNode = triviaNodeStructure.ChildNodes().FirstOrDefault(s => s.Span.Contains(span));
            if (paramNode == null)
            {
                return document;
            }

            var removedNodes = new List<SyntaxNode> { paramNode };

            var triviaNodeStructureChildren = triviaNodeStructure.ChildNodes().ToList();

            // This should not cause a crash because the diagnostics are only thrown in
            // doc comment XML nodes, which, by definition, start with `///` (C#) or `'''` (VB.NET)
            var paramNodeIndex = triviaNodeStructureChildren.IndexOf(paramNode);
            var previousNodeTextTrimmed = triviaNodeStructureChildren[paramNodeIndex - 1].ToFullString().Trim();

            if (previousNodeTextTrimmed == string.Empty || previousNodeTextTrimmed == DocCommentSignifierToken)
            {
                removedNodes.Add(triviaNodeStructureChildren[paramNodeIndex - 1]);
            }

            // Remove all trivia attached to the nodes I am removing.
            // Really, any option should work here because the leading/trailing text
            // around these nodes are not attached to them as trivia.
            var newCommentNode = triviaNodeStructure.RemoveNodes(removedNodes, SyntaxRemoveOptions.KeepNoTrivia);
            var newRoot = root.ReplaceTrivia(triviaNode, GetRevisedDocCommentTrivia(newCommentNode.ToFullString()));
            return document.WithSyntaxRoot(newRoot);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(FeaturesResources.Remove_tag, createChangedDocument)
            {
            }
        }
    }
}