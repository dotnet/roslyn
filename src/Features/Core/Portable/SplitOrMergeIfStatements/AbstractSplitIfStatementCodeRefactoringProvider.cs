// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal abstract class AbstractSplitIfStatementCodeRefactoringProvider<TExpressionSyntax> : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
    {
        protected abstract string IfKeywordText { get; }

        protected abstract int LogicalExpressionSyntaxKind { get; }

        protected abstract bool IsConditionOfIfStatement(SyntaxNode expression, out SyntaxNode ifStatement);

        protected abstract CodeAction CreateCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument);

        protected abstract Task<SyntaxNode> GetChangedRootAsync(
            Document document,
            SyntaxNode root,
            SyntaxNode currentIfStatement,
            TExpressionSyntax left,
            TExpressionSyntax right,
            CancellationToken cancellationToken);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var token = root.FindToken(context.Span.Start);

            if (context.Span.Length > 0 &&
                context.Span != token.Span)
            {
                return;
            }

            if (IsPartOfBinaryExpressionChain(token, LogicalExpressionSyntaxKind, out var rootExpression) &&
                IsConditionOfIfStatement(rootExpression, out _))
            {
                context.RegisterRefactoring(
                    CreateCodeAction(
                        c => RefactorAsync(context.Document, context.Span, c)));
            }
        }

        private async Task<Document> RefactorAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(span.Start);

            Contract.ThrowIfFalse(IsPartOfBinaryExpressionChain(token, LogicalExpressionSyntaxKind, out var rootExpression));
            Contract.ThrowIfFalse(IsConditionOfIfStatement(rootExpression, out var currentIfStatement));

            var (left, right) = SplitBinaryExpressionChain(token, rootExpression, document.GetLanguageService<ISyntaxFactsService>());

            var newRoot = await GetChangedRootAsync(document, root, currentIfStatement, left, right, cancellationToken).ConfigureAwait(false);
            return document.WithSyntaxRoot(newRoot);
        }

        private static bool IsPartOfBinaryExpressionChain(SyntaxToken token, int syntaxKind, out SyntaxNode rootExpression)
        {
            SyntaxNodeOrToken current = token;

            while (current.Parent?.RawKind == syntaxKind)
            {
                current = current.Parent;
            }

            rootExpression = current.AsNode();
            return current.IsNode;
        }

        private static (TExpressionSyntax left, TExpressionSyntax right) SplitBinaryExpressionChain(
            SyntaxToken token, SyntaxNode rootExpression, ISyntaxFactsService syntaxFacts)
        {
            syntaxFacts.GetPartsOfBinaryExpression(token.Parent, out var parentLeft, out _, out var parentRight);

            // (((a && b) && c) && d) && e
            var left = (TExpressionSyntax)parentLeft;
            var right = (TExpressionSyntax)rootExpression.ReplaceNode(token.Parent, parentRight);

            return (left, right);
        }
    }
}
