// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SplitOrMergeIfStatements;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.MergeConsecutiveIfStatements), Shared]
    internal sealed class CSharpMergeConsecutiveIfStatementsCodeRefactoringProvider
        : AbstractMergeConsecutiveIfStatementsCodeRefactoringProvider<ExpressionSyntax>
    {
        protected override bool IsApplicableSpan(SyntaxNode node, TextSpan span, out SyntaxNode ifStatementNode)
        {
            if (node is IfStatementSyntax ifStatement)
            {
                // Cases:
                // 1. Position is at a direct token child of an if statement with no selection (e.g. 'if' keyword, a parenthesis)
                // 2. Selection around the 'if' keyword
                // 3. Selection around the header - from 'if' keyword to the end of the condition
                // 4. Selection around the whole if statement *excluding* its else clause - from 'if' keyword to the end of its statement
                if (span.Length == 0 ||
                    span.IsAround(ifStatement.IfKeyword) ||
                    span.IsAround(ifStatement.IfKeyword, ifStatement.CloseParenToken) ||
                    span.IsAround(ifStatement.IfKeyword, ifStatement.Statement))
                {
                    ifStatementNode = ifStatement;
                    return true;
                }
            }

            if (node is ElseClauseSyntax elseClause && elseClause.Statement is IfStatementSyntax elseIfStatement)
            {
                // 5. Position is at a direct token child of an else clause with no selection ('else' keyword)
                // 6. Selection around the header including the 'else' keyword - from 'else' keyword to the end of the condition
                // 7. Selection from the 'else' keyword to the end of the if statement's statement
                if (span.Length == 0 ||
                    span.IsAround(elseClause.ElseKeyword, elseIfStatement.CloseParenToken) ||
                    span.IsAround(elseClause.ElseKeyword, elseIfStatement.Statement))
                {
                    ifStatementNode = elseIfStatement;
                    return true;
                }
            }

            ifStatementNode = null;
            return false;
        }

        protected override bool IsElseClauseOfIfStatement(SyntaxNode node, out SyntaxNode ifStatementNode)
        {
            if (node.Parent is ElseClauseSyntax elseClause &&
                elseClause.Parent is IfStatementSyntax ifStatement)
            {
                ifStatementNode = ifStatement;
                return true;
            }

            ifStatementNode = null;
            return false;
        }

        protected override SyntaxNode MergeIfStatements(
            SyntaxNode firstIfStatementNode, SyntaxNode secondIfStatementNode, ExpressionSyntax condition)
        {
            var firstIfStatement = (IfStatementSyntax)firstIfStatementNode;
            var secondIfStatement = (IfStatementSyntax)secondIfStatementNode;

            return firstIfStatement.WithCondition(condition).WithElse(secondIfStatement.Else);
        }
    }
}
