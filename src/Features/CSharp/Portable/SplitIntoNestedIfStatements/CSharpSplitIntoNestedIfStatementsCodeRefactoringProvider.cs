// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SplitIntoNestedIfStatements;

namespace Microsoft.CodeAnalysis.CSharp.SplitIntoNestedIfStatements
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.SplitIntoNestedIfStatements), Shared]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.InvertLogical, Before = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
    internal sealed class CSharpSplitIntoNestedIfStatementsCodeRefactoringProvider
        : AbstractSplitIntoNestedIfStatementsCodeRefactoringProvider<IfStatementSyntax, ExpressionSyntax>
    {
        protected override string IfKeywordText => SyntaxFacts.GetText(SyntaxKind.IfKeyword);

        protected override int LogicalAndSyntaxKind => (int)SyntaxKind.LogicalAndExpression;

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

        protected override IfStatementSyntax SplitIfStatement(
            IfStatementSyntax currentIfStatement, ExpressionSyntax condition1, ExpressionSyntax condition2)
        {
            var innerIfStatement = SyntaxFactory.IfStatement(condition2, currentIfStatement.Statement, currentIfStatement.Else);
            var outerIfStatement = currentIfStatement
                .WithCondition(condition1)
                .WithStatement(SyntaxFactory.Block(innerIfStatement));

            return outerIfStatement;
        }
    }
}
