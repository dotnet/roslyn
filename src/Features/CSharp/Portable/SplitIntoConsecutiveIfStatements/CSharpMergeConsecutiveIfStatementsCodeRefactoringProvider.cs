// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SplitIntoConsecutiveIfStatements;

namespace Microsoft.CodeAnalysis.CSharp.SplitIntoConsecutiveIfStatements
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.MergeConsecutiveIfStatements), Shared]
    internal sealed class CSharpMergeConsecutiveIfStatementsCodeRefactoringProvider
        : AbstractMergeConsecutiveIfStatementsCodeRefactoringProvider<ExpressionSyntax>
    {
        protected override string IfKeywordText => SyntaxFacts.GetText(SyntaxKind.IfKeyword);

        protected override bool IsTokenOfIfStatement(SyntaxToken token, out SyntaxNode ifStatement)
        {
            if (token.Parent is IfStatementSyntax s)
            {
                ifStatement = s;
                return true;
            }

            ifStatement = null;
            return false;
        }

        protected override bool IsElseClauseOfIfStatement(SyntaxNode statement, out SyntaxNode ifStatement)
        {
            if (statement.Parent is ElseClauseSyntax elseClause &&
                elseClause.Parent is IfStatementSyntax s)
            {
                ifStatement = s;
                return true;
            }

            ifStatement = null;
            return false;
        }

        protected override bool IsIfStatement(SyntaxNode statement)
        {
            return statement is IfStatementSyntax;
        }

        protected override bool HasElseClauses(SyntaxNode ifStatement)
        {
            return ((IfStatementSyntax)ifStatement).Else != null;
        }

        protected override SyntaxNode MergeIfStatements(
            SyntaxNode parentIfStatement, SyntaxNode ifStatement, ExpressionSyntax condition)
        {
            return ((IfStatementSyntax)parentIfStatement).WithCondition(condition).WithElse(((IfStatementSyntax)ifStatement).Else);
        }
    }
}
