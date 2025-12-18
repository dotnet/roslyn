// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements;

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
        => CodeAction.Create(
            string.Format(FeaturesResources.Split_into_nested_0_statements, ifKeywordText),
            createChangedDocument,
            nameof(FeaturesResources.Split_into_nested_0_statements) + "_" + ifKeywordText);

    protected sealed override async ValueTask<SyntaxNode> GetChangedRootAsync(
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

        return root.ReplaceNode(ifOrElseIf, outerIfOrElseIf.WithAdditionalAnnotations(Formatter.Annotation));
    }
}
