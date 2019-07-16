// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class CSharpIntroduceLocalForExpressionCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            var expressionStatement = await GetExpressionStatementAsync(document, span, cancellationToken).ConfigureAwait(false);
            if (expressionStatement == null)
            {
                return;
            }

            var expression = expressionStatement.Expression;

            // Expression is likely too simple to want to offer to generate a local for.
            // This leads to too many false cases where this is offered.
            if (span.IsEmpty &&
                expressionStatement.SemicolonToken.IsMissing &&
                expression.IsKind(SyntaxKind.IdentifierName))
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var type = semanticModel.GetTypeInfo(expression).Type;
            if (type == null ||
                type.SpecialType == SpecialType.System_Void)
            {
                return;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var singleLineExpression = syntaxFacts.ConvertToSingleLine(expression);
            var nodeString = singleLineExpression.ToString();

            context.RegisterRefactoring(new MyCodeAction(
                string.Format(FeaturesResources.Introduce_local_for_0, nodeString),
                c => IntroduceLocalAsync(document, span, c)));
        }

        private static async Task<ExpressionStatementSyntax> GetExpressionStatementAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var helpers = document.GetLanguageService<IRefactoringHelpersService>();
            var expressionStatement = await helpers.TryGetSelectedNodeAsync<ExpressionStatementSyntax>(document, span, cancellationToken).ConfigureAwait(false);
            return expressionStatement;
        }

        private async Task<Document> IntroduceLocalAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var expressionStatement = await GetExpressionStatementAsync(document, span, cancellationToken).ConfigureAwait(false);

            var expression = expressionStatement.Expression;
            var semicolon = expressionStatement.SemicolonToken;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();
            var baseName = semanticFacts.GenerateNameForExpression(semanticModel, expression, capitalize: false, cancellationToken);
            var uniqueName = semanticFacts.GenerateUniqueLocalName(semanticModel, expression, containerOpt: null, baseName, cancellationToken)
                                          .WithAdditionalAnnotations(RenameAnnotation.Create());

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

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(expressionStatement, localDeclaration);

            return document.WithSyntaxRoot(newRoot);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
