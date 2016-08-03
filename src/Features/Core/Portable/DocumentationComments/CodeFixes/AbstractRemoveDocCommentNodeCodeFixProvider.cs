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
        protected abstract SyntaxNode GetDocCommentElementNode(SyntaxNode fullDocComentNode, TextSpan span);

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();

            context.RegisterCodeFix(
                new MyCodeAction(
                    FeaturesResources.Remove_tag,
                    c => RemoveDuplicateParamTagAsync(context)),
                context.Diagnostics);

            return SpecializedTasks.EmptyTask;
        }

        private async Task<Document> RemoveDuplicateParamTagAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var commentParent = root.FindNode(span);
            var triviaNode = commentParent.GetLeadingTrivia().Single(s => s.GetLocation().SourceSpan.Contains(span));

            var triviaNodeStructure = triviaNode.GetStructure();

            var paramNode = GetDocCommentElementNode(triviaNodeStructure, span);
            var removedNodes = new List<SyntaxNode> { paramNode };

            var triviaNodeStructureChildren = triviaNodeStructure.ChildNodes().ToList();

            var paramNodeIndex = triviaNodeStructureChildren.IndexOf(paramNode);
            var previousNodeTextTrimmed = triviaNodeStructureChildren[paramNodeIndex - 1].ToFullString().Trim();

            if (previousNodeTextTrimmed == string.Empty || previousNodeTextTrimmed == DocCommentSignifierToken)
            {
                removedNodes.Add(triviaNodeStructureChildren[paramNodeIndex - 1]);
            }

            var newCommentNode = triviaNodeStructure.RemoveNodes(removedNodes, SyntaxRemoveOptions.KeepNoTrivia);
            var newRoot = root.ReplaceTrivia(triviaNode, GetRevisedDocCommentTrivia(newCommentNode.ToFullString()));
            return document.WithSyntaxRoot(newRoot);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}