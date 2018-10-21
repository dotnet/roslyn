// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SplitOrMergeIfStatements;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.MergeNestedIfStatements), Shared]
    internal sealed class CSharpMergeNestedIfStatementsCodeRefactoringProvider
        : AbstractMergeNestedIfStatementsCodeRefactoringProvider<ExpressionSyntax>
    {
        protected override string IfKeywordText => SyntaxFacts.GetText(SyntaxKind.IfKeyword);

        protected override bool IsApplicableSpan(SyntaxNode node, TextSpan span, out SyntaxNode ifStatementNode)
        {
            if (node is IfStatementSyntax ifStatement)
            {
                // Cases:
                // 1. Position is at a direct token child of an if statement with no selection (e.g. 'if' keyword, a parenthesis)
                // 2. Selection around the 'if' keyword
                // 3. Selection around the header - from 'if' keyword to the end of the condition
                // 4. Selection around the whole if statement
                if (span.Length == 0 ||
                    span.IsAround(ifStatement.IfKeyword) ||
                    span.IsAround(ifStatement.IfKeyword, ifStatement.CloseParenToken) ||
                    span.IsAround(node))
                {
                    ifStatementNode = ifStatement;
                    return true;
                }
            }

            ifStatementNode = null;
            return false;
        }

        protected override bool IsIfStatement(SyntaxNode statement)
        {
            return statement is IfStatementSyntax;
        }

        protected override ImmutableArray<SyntaxNode> GetElseClauses(SyntaxNode ifStatement)
        {
            return ImmutableArray.Create<SyntaxNode>(((IfStatementSyntax)ifStatement).Else);
        }

        protected override SyntaxNode MergeIfStatements(
            SyntaxNode outerIfStatementNode, SyntaxNode innerIfStatementNode, ExpressionSyntax condition)
        {
            var outerIfStatement = (IfStatementSyntax)outerIfStatementNode;
            var innerIfStatement = (IfStatementSyntax)innerIfStatementNode;

            return outerIfStatement.WithCondition(condition).WithStatement(innerIfStatement.Statement);
        }
    }
}
