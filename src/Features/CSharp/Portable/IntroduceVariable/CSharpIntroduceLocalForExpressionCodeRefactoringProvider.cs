// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.IntroduceVariable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable
{
    using static SyntaxFactory;

    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.IntroduceLocalForExpression), Shared]
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
            return expressionStatement.Expression is not AssignmentExpressionSyntax;
        }

        protected override LocalDeclarationStatementSyntax FixupLocalDeclaration(
            ExpressionStatementSyntax expressionStatement, LocalDeclarationStatementSyntax localDeclaration)
        {
            // If there wasn't a semicolon before, ensure the trailing trivia of the expression
            // becomes the trailing trivia of a new semicolon that we add.
            var semicolonToken = expressionStatement.SemicolonToken;
            if (expressionStatement.SemicolonToken.IsMissing && localDeclaration is { Declaration.Variables: [{ Initializer.Value: { } value }, ..] })
            {
                var expression = expressionStatement.Expression;
                localDeclaration = localDeclaration.ReplaceNode(value, expression.WithoutLeadingTrivia());
                semicolonToken = Token(SyntaxKind.SemicolonToken).WithTrailingTrivia(expression.GetTrailingTrivia());
            }

            return localDeclaration.WithSemicolonToken(semicolonToken);
        }

        protected override ExpressionStatementSyntax FixupDeconstruction(
            ExpressionStatementSyntax expressionStatement, ExpressionStatementSyntax deconstruction)
        {
            // If there wasn't a semicolon before, ensure the trailing trivia of the expression
            // becomes the trailing trivia of a new semicolon that we add.
            var semicolonToken = expressionStatement.SemicolonToken;
            if (expressionStatement.SemicolonToken.IsMissing && deconstruction is { Expression: AssignmentExpressionSyntax binary })
            {
                var expression = expressionStatement.Expression;
                deconstruction = deconstruction.ReplaceNode(binary.Right, expression.WithoutLeadingTrivia());
                semicolonToken = Token(SyntaxKind.SemicolonToken).WithTrailingTrivia(expression.GetTrailingTrivia());
            }

            return deconstruction.WithSemicolonToken(semicolonToken);
        }

        protected override ExpressionStatementSyntax CreateImplicitlyTypedDeconstruction(ImmutableArray<SyntaxToken> names, ExpressionSyntax expression)
        {
            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    DeclarationExpression(
                        IdentifierName("var"),
                        ParenthesizedVariableDesignation(SeparatedList(names.SelectAsArray(n => (VariableDesignationSyntax)SingleVariableDesignation(n))))),
                    expression));
        }
    }
}
