// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SplitOrMergeIfStatements;

namespace Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.SplitIntoConsecutiveIfStatements), Shared]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.InvertLogical, Before = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
    internal sealed class CSharpSplitIntoConsecutiveIfStatementsCodeRefactoringProvider
        : AbstractSplitIntoConsecutiveIfStatementsCodeRefactoringProvider<ExpressionSyntax>
    {
        protected override string IfKeywordText => SyntaxFacts.GetText(SyntaxKind.IfKeyword);

        protected override int LogicalOrSyntaxKind => (int)SyntaxKind.LogicalOrExpression;

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

        protected override bool HasElseClauses(SyntaxNode ifStatementNode)
        {
            var ifStatement = (IfStatementSyntax)ifStatementNode;

            return ifStatement.Else != null;
        }

        protected override (SyntaxNode, SyntaxNode) SplitIfStatementIntoElseClause(
            SyntaxNode ifStatementNode, ExpressionSyntax condition1, ExpressionSyntax condition2)
        {
            var ifStatement = (IfStatementSyntax)ifStatementNode;

            if (ContainsEmbeddedIfStatement(ifStatement))
            {
                ifStatement = ifStatement.WithStatement(SyntaxFactory.Block(ifStatement.Statement));
            }

            var secondIfStatement = SyntaxFactory.IfStatement(condition2, ifStatement.Statement, ifStatement.Else);
            var firstIfStatement = ifStatement
                .WithCondition(condition1)
                .WithElse(SyntaxFactory.ElseClause(secondIfStatement));

            return (firstIfStatement, null);
        }

        protected override (SyntaxNode, SyntaxNode) SplitIfStatementIntoSeparateStatements(
            SyntaxNode ifStatementNode, ExpressionSyntax condition1, ExpressionSyntax condition2)
        {
            var ifStatement = (IfStatementSyntax)ifStatementNode;

            var secondIfStatement = SyntaxFactory.IfStatement(condition2, ifStatement.Statement);
            var firstIfStatement = ifStatement.WithCondition(condition1);

            return (firstIfStatement, secondIfStatement);
        }

        private static bool ContainsEmbeddedIfStatement(IfStatementSyntax ifStatement)
        {
            for (var statement = ifStatement.Statement; statement.IsEmbeddedStatementOwner(); statement = statement.GetEmbeddedStatement())
            {
                if (statement.IsKind(SyntaxKind.IfStatement))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
