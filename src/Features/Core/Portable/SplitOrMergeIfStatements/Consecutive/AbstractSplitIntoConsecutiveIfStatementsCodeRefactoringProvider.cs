// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal abstract class AbstractSplitIntoConsecutiveIfStatementsCodeRefactoringProvider<TExpressionSyntax>
        : AbstractSplitIfStatementCodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
    {
        protected abstract bool HasElseClauses(SyntaxNode ifStatement);

        protected abstract (SyntaxNode, SyntaxNode) SplitIfStatementIntoElseClause(
            SyntaxNode currentIfStatement, TExpressionSyntax condition1, TExpressionSyntax condition2);

        protected abstract (SyntaxNode, SyntaxNode) SplitIfStatementIntoSeparateStatements(
            SyntaxNode currentIfStatement, TExpressionSyntax condition1, TExpressionSyntax condition2);

        protected sealed override CodeAction CreateCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
            => new MyCodeAction(createChangedDocument, IfKeywordText);

        protected sealed override async Task<SyntaxNode> GetChangedRootAsync(
            Document document, SyntaxNode root, SyntaxNode currentIfStatement, SyntaxNode left, SyntaxNode right, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var (firstIfStatement, secondIfStatement) =
                await CanBeSeparateStatementsAsync(document, syntaxFacts, currentIfStatement, cancellationToken).ConfigureAwait(false)
                ? SplitIfStatementIntoSeparateStatements(currentIfStatement, (TExpressionSyntax)left, (TExpressionSyntax)right)
                : SplitIfStatementIntoElseClause(currentIfStatement, (TExpressionSyntax)left, (TExpressionSyntax)right);

            return secondIfStatement != null
                ? root.ReplaceNode(currentIfStatement, ImmutableArray.Create(
                    firstIfStatement.WithAdditionalAnnotations(Formatter.Annotation),
                    secondIfStatement.WithAdditionalAnnotations(Formatter.Annotation)))
                : root.ReplaceNode(currentIfStatement,
                    firstIfStatement.WithAdditionalAnnotations(Formatter.Annotation));
        }

        private async Task<bool> CanBeSeparateStatementsAsync(
            Document document,
            ISyntaxFactsService syntaxFacts,
            SyntaxNode ifStatement,
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
