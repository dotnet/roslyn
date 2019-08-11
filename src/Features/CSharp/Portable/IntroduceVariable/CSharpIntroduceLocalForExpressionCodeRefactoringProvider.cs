// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.IntroduceVariable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class CSharpIntroduceLocalForExpressionCodeRefactoringProvider :
        AbstractIntroduceLocalForExpressionCodeRefactoringProvider<
            ExpressionSyntax,
            StatementSyntax,
            ExpressionStatementSyntax,
            LocalDeclarationStatementSyntax>
    {
        protected override bool IsValid(ExpressionStatementSyntax expressionStatement, TextSpan span)
        {
            // Expression is likely too simple to want to offer to generate a local for.
            // This leads to too many false cases where this is offered.
            if (span.IsEmpty &&
                expressionStatement.SemicolonToken.IsMissing &&
                expressionStatement.Expression.IsKind(SyntaxKind.IdentifierName))
            {
                return false;
            }

            return true;
        }

        protected override LocalDeclarationStatementSyntax FixupLocalDeclaration(
            ExpressionStatementSyntax expressionStatement, LocalDeclarationStatementSyntax localDeclaration)
        {
            // If there wasn't a semicolon before, ensure the trailing trivia of the expression
            // becomes the trailing trivia of a new semicolon that we add.
            var semicolonToken = expressionStatement.SemicolonToken;
            if (expressionStatement.SemicolonToken.IsMissing)
            {
                var expression = expressionStatement.Expression;
                localDeclaration = localDeclaration.ReplaceNode(localDeclaration.Declaration.Variables[0].Initializer.Value, expression.WithoutLeadingTrivia());
                semicolonToken = SyntaxFactory.Token(SyntaxKind.SemicolonToken).WithTrailingTrivia(expression.GetTrailingTrivia());
            }

            return localDeclaration.WithSemicolonToken(semicolonToken);
        }
    }
}
