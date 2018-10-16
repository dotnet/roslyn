// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.SplitIntoNestedIfStatements;

namespace Microsoft.CodeAnalysis.CSharp.SplitIntoNestedIfStatements
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.MergeNestedIfStatements), Shared]
    internal sealed class CSharpMergeNestedIfStatementsCodeRefactoringProvider
        : AbstractMergeNestedIfStatementsCodeRefactoringProvider<IfStatementSyntax>
    {
        protected override string IfKeywordText => SyntaxFacts.GetText(SyntaxKind.IfKeyword);

        protected override bool IsTokenOfIfStatement(SyntaxToken token, out IfStatementSyntax ifStatement)
        {
            if (token.Parent is IfStatementSyntax s)
            {
                ifStatement = s;
                return true;
            }

            ifStatement = null;
            return false;
        }

        protected override bool IsFirstStatementOfIfStatement(SyntaxNode statement, out IfStatementSyntax ifStatement)
        {
            if (statement.Parent is IfStatementSyntax s1 && s1.Statement == statement)
            {
                ifStatement = s1;
                return true;
            }

            if (statement.Parent is BlockSyntax block && block.Statements.FirstOrDefault() == statement &&
                block.Parent is IfStatementSyntax s2 && s2.Statement == block)
            {
                ifStatement = s2;
                return true;
            }

            ifStatement = null;
            return false;
        }

        protected override ImmutableArray<SyntaxNode> GetElseClauses(IfStatementSyntax ifStatement)
        {
            return ImmutableArray.Create<SyntaxNode>(ifStatement.Else);
        }

        protected override IfStatementSyntax MergeIfStatements(IfStatementSyntax outerIfStatement, IfStatementSyntax innerIfStatement, SyntaxNode condition)
        {
            return outerIfStatement.WithCondition((ExpressionSyntax)condition).WithStatement(innerIfStatement.Statement);
        }
    }
}
