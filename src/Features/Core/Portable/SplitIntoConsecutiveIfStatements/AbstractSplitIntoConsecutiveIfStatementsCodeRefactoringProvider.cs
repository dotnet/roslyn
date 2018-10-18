// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SplitIntoConsecutiveIfStatements
{
    internal abstract class AbstractSplitIntoConsecutiveIfStatementsCodeRefactoringProvider<
        TIfStatementSyntax, TExpressionSyntax> : CodeRefactoringProvider
        where TIfStatementSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
    {
        protected abstract string IfKeywordText { get; }

        protected abstract int LogicalOrSyntaxKind { get; }

        protected abstract bool IsConditionOfIfStatement(SyntaxNode expression, out TIfStatementSyntax ifStatement);

        protected abstract bool HasElseClauses(TIfStatementSyntax ifStatement);

        protected abstract TIfStatementSyntax SplitIfStatementIntoElseClause(
            TIfStatementSyntax currentIfStatement, TExpressionSyntax condition1, TExpressionSyntax condition2);

        protected abstract (TIfStatementSyntax, TIfStatementSyntax) SplitIfStatementIntoSeparateStatements(
            TIfStatementSyntax currentIfStatement, TExpressionSyntax condition1, TExpressionSyntax condition2);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var token = root.FindToken(context.Span.Start);

            if (context.Span.Length > 0 &&
                context.Span != token.Span)
            {
                return;
            }

            if (IsPartOfBinaryExpressionChain(token, LogicalOrSyntaxKind, out var rootExpression) &&
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

            Contract.ThrowIfFalse(IsPartOfBinaryExpressionChain(token, LogicalOrSyntaxKind, out var rootExpression));
            Contract.ThrowIfFalse(IsConditionOfIfStatement(rootExpression, out var currentIfStatement));

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var (left, right) = SplitBinaryExpressionChain(token, rootExpression, syntaxFacts);

            if (await CanBeSeparateStatementsAsync(document, syntaxFacts, currentIfStatement, cancellationToken))
            {
                var (firstIfStatement, secondIfStatement) = SplitIfStatementIntoSeparateStatements(currentIfStatement, left, right);

                var newRoot = root.ReplaceNode(currentIfStatement, new[]
                {
                    firstIfStatement.WithAdditionalAnnotations(Formatter.Annotation),
                    secondIfStatement.WithAdditionalAnnotations(Formatter.Annotation)
                });
                return document.WithSyntaxRoot(newRoot);
            }
            else
            {
                var newIfStatement = SplitIfStatementIntoElseClause(currentIfStatement, left, right);

                var newRoot = root.ReplaceNode(currentIfStatement, newIfStatement.WithAdditionalAnnotations(Formatter.Annotation));
                return document.WithSyntaxRoot(newRoot);
            }
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

            // (((a || b) || c) || d) || e
            var left = (TExpressionSyntax)parentLeft;
            var right = (TExpressionSyntax)rootExpression.ReplaceNode(token.Parent, parentRight);

            return (left, right);
        }

        private async Task<bool> CanBeSeparateStatementsAsync(
            Document document,
            ISyntaxFactsService syntaxFacts,
            TIfStatementSyntax ifStatement,
            CancellationToken cancellationToken)
        {
            if (HasElseClauses(ifStatement))
            {
                return false;
            }

            if (!syntaxFacts.IsExecutableBlock(ifStatement.Parent))
            {
                return false;
            }

            var insideStatements = syntaxFacts.GetStatementContainerStatements(ifStatement);
            if (insideStatements.Count == 0)
            {
                return false;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var controlFlow = semanticModel.AnalyzeControlFlow(insideStatements.First(), insideStatements.Last());

            return !controlFlow.EndPointIsReachable;
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string ifKeywordText)
                : base(string.Format(FeaturesResources.Split_into_consecutive_0_statements, ifKeywordText), createChangedDocument)
            {
            }
        }
    }
}
