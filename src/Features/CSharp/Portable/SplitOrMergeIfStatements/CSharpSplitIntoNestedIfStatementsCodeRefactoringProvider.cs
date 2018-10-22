// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SplitOrMergeIfStatements;

namespace Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.SplitIntoNestedIfStatements), Shared]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.InvertLogical, Before = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
    internal sealed class CSharpSplitIntoNestedIfStatementsCodeRefactoringProvider
        : AbstractSplitIntoNestedIfStatementsCodeRefactoringProvider<ExpressionSyntax>
    {
        protected override string IfKeywordText => SyntaxFacts.GetText(SyntaxKind.IfKeyword);

        protected override int LogicalAndSyntaxKind => (int)SyntaxKind.LogicalAndExpression;

        protected override bool IsConditionOfIfStatement(SyntaxNode expression, out SyntaxNode ifStatementNode)
        {
            if (expression.Parent is IfStatementSyntax ifStatement && ifStatement.Condition == expression)
            {
                ifStatementNode = ifStatement;
                return true;
            }

            ifStatementNode = null;
            return false;
        }

        protected override SyntaxNode SplitIfStatement(
            SyntaxNode ifStatementNode, ExpressionSyntax condition1, ExpressionSyntax condition2)
        {
            var ifStatement = (IfStatementSyntax)ifStatementNode;

            var innerIfStatement = SyntaxFactory.IfStatement(condition2, ifStatement.Statement, ifStatement.Else);
            var outerIfStatement = ifStatement
                .WithCondition(condition1)
                .WithStatement(SyntaxFactory.Block(innerIfStatement));

            return outerIfStatement;
        }
    }
}
