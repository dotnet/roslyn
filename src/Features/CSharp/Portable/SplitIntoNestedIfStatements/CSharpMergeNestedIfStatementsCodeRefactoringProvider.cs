// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SplitIntoNestedIfStatements;

namespace Microsoft.CodeAnalysis.CSharp.SplitIntoNestedIfStatements
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.MergeNestedIfStatements), Shared]
    internal sealed class CSharpMergeNestedIfStatementsCodeRefactoringProvider
        : AbstractMergeNestedIfStatementsCodeRefactoringProvider<ExpressionSyntax>
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
