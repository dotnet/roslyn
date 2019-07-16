// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.IntroduceVariable;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

        protected override async Task<LocalDeclarationStatementSyntax> CreateLocalDeclarationAsync(
            Document document, ExpressionStatementSyntax expressionStatement, CancellationToken cancellationToken)
        {
            var expression = expressionStatement.Expression;
            var semicolon = expressionStatement.SemicolonToken;

            var uniqueName = await GenerateUniqueNameAsync(document, expression, cancellationToken).ConfigureAwait(false);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var type = semanticModel.GetTypeInfo(expression).Type;

            if (semicolon.IsMissing)
            {
                semicolon = SyntaxFactory.Token(SyntaxKind.SemicolonToken)
                                         .WithTrailingTrivia(expression.GetTrailingTrivia());
                expression = expression.WithoutTrailingTrivia();
            }

            var variableDeclaration =
                SyntaxFactory.VariableDeclaration(
                    type.GenerateTypeSyntax(),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(uniqueName)
                                     .WithInitializer(SyntaxFactory.EqualsValueClause(
                                         expression.WithoutLeadingTrivia()))));
            var localDeclaration =
                SyntaxFactory.LocalDeclarationStatement(variableDeclaration)
                             .WithSemicolonToken(semicolon)
                             .WithLeadingTrivia(expression.GetLeadingTrivia());

            return localDeclaration;
        }
    }
}
