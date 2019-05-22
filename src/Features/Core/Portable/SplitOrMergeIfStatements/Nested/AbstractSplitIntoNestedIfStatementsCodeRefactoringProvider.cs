// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal abstract class AbstractSplitIntoNestedIfStatementsCodeRefactoringProvider
        : AbstractSplitIfStatementCodeRefactoringProvider
    {
        // Converts:
        //    if (a && b)
        //        Console.WriteLine();
        //
        // To:
        //    if (a)
        //    {
        //        if (b)
        //            Console.WriteLine();
        //    }

        protected sealed override int GetLogicalExpressionKind(ISyntaxKindsService syntaxKinds)
            => syntaxKinds.LogicalAndExpression;

        protected sealed override CodeAction CreateCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string ifKeywordText)
            => new MyCodeAction(createChangedDocument, ifKeywordText);

        protected sealed override Task<SyntaxNode> GetChangedRootAsync(
            Document document,
            SyntaxNode root,
            SyntaxNode ifOrElseIf,
            SyntaxNode leftCondition,
            SyntaxNode rightCondition,
            CancellationToken cancellationToken)
        {
            var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();

            // If we have an else-if clause, we first convert it to an if statement. If there are any
            // else-if or else clauses following the outer if statement, they will be copied and placed inside too.

            var innerIfStatement = ifGenerator.WithCondition(ifGenerator.ToIfStatement(ifOrElseIf), rightCondition);
            var outerIfOrElseIf = ifGenerator.WithCondition(ifGenerator.WithStatementInBlock(ifOrElseIf, innerIfStatement), leftCondition);

            return Task.FromResult(
                root.ReplaceNode(ifOrElseIf, outerIfOrElseIf.WithAdditionalAnnotations(Formatter.Annotation)));
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
