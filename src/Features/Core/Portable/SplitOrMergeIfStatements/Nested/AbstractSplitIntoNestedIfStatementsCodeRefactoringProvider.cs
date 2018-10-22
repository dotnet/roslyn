// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal abstract class AbstractSplitIntoNestedIfStatementsCodeRefactoringProvider<TExpressionSyntax>
        : AbstractSplitIfStatementCodeRefactoringProvider<TExpressionSyntax>
        where TExpressionSyntax : SyntaxNode
    {
        protected abstract int LogicalAndSyntaxKind { get; }
        protected sealed override int LogicalExpressionSyntaxKind => LogicalAndSyntaxKind;

        protected abstract SyntaxNode SplitIfStatement(
            SyntaxNode currentIfStatement, TExpressionSyntax condition1, TExpressionSyntax condition2);

        protected sealed override CodeAction CreateCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
            => new MyCodeAction(createChangedDocument, IfKeywordText);

        protected sealed override Task<SyntaxNode> GetChangedRootAsync(
            Document document,
            SyntaxNode root,
            SyntaxNode currentIfStatement,
            TExpressionSyntax left,
            TExpressionSyntax right,
            CancellationToken cancellationToken)
        {
            var newIfStatement = SplitIfStatement(currentIfStatement, left, right);

            return Task.FromResult(
                root.ReplaceNode(currentIfStatement, newIfStatement.WithAdditionalAnnotations(Formatter.Annotation)));
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
