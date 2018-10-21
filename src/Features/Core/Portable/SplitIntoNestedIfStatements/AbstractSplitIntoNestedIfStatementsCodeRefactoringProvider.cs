// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SplitIntoNestedIfStatements
{
    internal abstract class AbstractSplitIntoNestedIfStatementsCodeRefactoringProvider<TExpressionSyntax>
        : BaseSplitIfStatementCodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
    {
        protected abstract int LogicalAndSyntaxKind { get; }

        protected abstract SyntaxNode SplitIfStatement(
            SyntaxNode currentIfStatement, TExpressionSyntax condition1, TExpressionSyntax condition2);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var token = root.FindToken(context.Span.Start);

            if (context.Span.Length > 0 &&
                context.Span != token.Span)
            {
                return;
            }

            if (IsPartOfBinaryExpressionChain(token, LogicalAndSyntaxKind, out var rootExpression) &&
                IsConditionOfIfStatement(rootExpression, out _))
            {
                context.RegisterRefactoring(
                    new MyCodeAction(
                        c => FixAsync(context.Document, context.Span, c),
                        IfKeywordText));
            }
        }

        private async Task<Document> FixAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(span.Start);

            Contract.ThrowIfFalse(IsPartOfBinaryExpressionChain(token, LogicalAndSyntaxKind, out var rootExpression));
            Contract.ThrowIfFalse(IsConditionOfIfStatement(rootExpression, out var currentIfStatement));

            var (left, right) = SplitBinaryExpressionChain(token, rootExpression, document.GetLanguageService<ISyntaxFactsService>());

            var newIfStatement = SplitIfStatement(currentIfStatement, (TExpressionSyntax)left, (TExpressionSyntax)right);

            var newRoot = root.ReplaceNode(currentIfStatement, newIfStatement.WithAdditionalAnnotations(Formatter.Annotation));
            return document.WithSyntaxRoot(newRoot);
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string ifKeywordText)
                : base(string.Format(FeaturesResources.Split_into_nested_0_statements, ifKeywordText), createChangedDocument)
            {
            }
        }
    }
}
