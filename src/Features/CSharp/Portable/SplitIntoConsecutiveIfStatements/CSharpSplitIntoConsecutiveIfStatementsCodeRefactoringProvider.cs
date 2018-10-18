// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SplitIntoConsecutiveIfStatements;

namespace Microsoft.CodeAnalysis.CSharp.SplitIntoConsecutiveIfStatements
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.SplitIntoConsecutiveIfStatements), Shared]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.InvertLogical, Before = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
    internal sealed class CSharpSplitIntoConsecutiveIfStatementsCodeRefactoringProvider
        : AbstractSplitIntoConsecutiveIfStatementsCodeRefactoringProvider<IfStatementSyntax, ExpressionSyntax>
    {
        protected override string IfKeywordText => SyntaxFacts.GetText(SyntaxKind.IfKeyword);

        protected override int LogicalOrSyntaxKind => (int)SyntaxKind.LogicalOrExpression;

        protected override bool IsConditionOfIfStatement(SyntaxNode expression, out IfStatementSyntax ifStatement)
        {
            if (expression.Parent is IfStatementSyntax s && s.Condition == expression)
            {
                ifStatement = s;
                return true;
            }

            ifStatement = null;
            return false;
        }

        protected override bool HasElseClauses(IfStatementSyntax ifStatement)
        {
            return ifStatement.Else != null;
        }

        protected override IfStatementSyntax SplitIfStatementIntoElseClause(
            IfStatementSyntax currentIfStatement, ExpressionSyntax condition1, ExpressionSyntax condition2)
        {
            var secondIfStatement = SyntaxFactory.IfStatement(condition2, currentIfStatement.Statement, currentIfStatement.Else);
            var firstIfStatement = currentIfStatement
                .WithCondition(condition1)
                .WithElse(SyntaxFactory.ElseClause(secondIfStatement));

            return firstIfStatement;
        }

        protected override (IfStatementSyntax, IfStatementSyntax) SplitIfStatementIntoSeparateStatements(
            IfStatementSyntax currentIfStatement, ExpressionSyntax condition1, ExpressionSyntax condition2)
        {
            var secondIfStatement = SyntaxFactory.IfStatement(condition2, currentIfStatement.Statement);
            var firstIfStatement = currentIfStatement.WithCondition(condition1);

            return (firstIfStatement, secondIfStatement);
        }
    }
}
