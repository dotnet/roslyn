// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
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
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpIntroduceLocalForExpressionCodeRefactoringProvider()
        {
        }

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

            // We don't want to offer new local for an assignmentExpression `a = 42` -> `int newA = a = 42`
            if (expressionStatement.Expression is AssignmentExpressionSyntax)
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
