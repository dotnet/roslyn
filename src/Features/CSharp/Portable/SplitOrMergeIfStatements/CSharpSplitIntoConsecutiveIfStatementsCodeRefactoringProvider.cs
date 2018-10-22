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

        protected override bool IsConditionOfIfStatement(SyntaxNode expression, out SyntaxNode ifStatement)
        {
            if (expression.Parent is IfStatementSyntax s && s.Condition == expression)
            {
                ifStatement = s;
                return true;
            }

            ifStatement = null;
            return false;
        }

        protected override bool HasElseClauses(SyntaxNode ifStatement)
        {
            return ((IfStatementSyntax)ifStatement).Else != null;
        }

        protected override (SyntaxNode, SyntaxNode) SplitIfStatementIntoElseClause(
            SyntaxNode currentIfStatementNode, ExpressionSyntax condition1, ExpressionSyntax condition2)
        {
            var currentIfStatement = (IfStatementSyntax)currentIfStatementNode;

            if (ContainsEmbeddedIfStatement(currentIfStatement))
            {
                currentIfStatement = currentIfStatement.WithStatement(SyntaxFactory.Block(currentIfStatement.Statement));
            }

            var secondIfStatement = SyntaxFactory.IfStatement(condition2, currentIfStatement.Statement, currentIfStatement.Else);
            var firstIfStatement = currentIfStatement
                .WithCondition(condition1)
                .WithElse(SyntaxFactory.ElseClause(secondIfStatement));

            return (firstIfStatement, null);
        }

        protected override (SyntaxNode, SyntaxNode) SplitIfStatementIntoSeparateStatements(
            SyntaxNode currentIfStatementNode, ExpressionSyntax condition1, ExpressionSyntax condition2)
        {
            var currentIfStatement = (IfStatementSyntax)currentIfStatementNode;

            var secondIfStatement = SyntaxFactory.IfStatement(condition2, currentIfStatement.Statement);
            var firstIfStatement = currentIfStatement.WithCondition(condition1);

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
