// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    internal abstract class AbstractIntroduceLocalForExpressionCodeRefactoringProvider<
        TExpressionSyntax,
        TStatementSyntax,
        TExpressionStatementSyntax,
        TLocalDeclarationStatementSyntax> : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TExpressionStatementSyntax : TStatementSyntax
        where TLocalDeclarationStatementSyntax : TStatementSyntax
    {
        protected abstract bool IsValid(TExpressionStatementSyntax expressionStatement, TextSpan span);
        protected abstract TLocalDeclarationStatementSyntax FixupLocalDeclaration(TExpressionStatementSyntax expressionStatement, TLocalDeclarationStatementSyntax localDeclaration);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var expressionStatement = await GetExpressionStatementAsync(context).ConfigureAwait(false);
            if (expressionStatement == null)
            {
                return;
            }

            var (document, _, cancellationToken) = context;
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var expression = syntaxFacts.GetExpressionOfExpressionStatement(expressionStatement);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var type = semanticModel.GetTypeInfo(expression).Type;
            if (type == null ||
                type.SpecialType == SpecialType.System_Void)
            {
                return;
            }

            var singleLineExpression = syntaxFacts.ConvertToSingleLine(expression);
            var nodeString = singleLineExpression.ToString();

            context.RegisterRefactoring(
                CodeAction.Create(
                    string.Format(FeaturesResources.Introduce_local_for_0, nodeString),
                    c => IntroduceLocalAsync(document, expressionStatement, c),
                    nameof(FeaturesResources.Introduce_local_for_0) + "_" + nodeString),
                expressionStatement.Span);
        }

        protected async Task<TExpressionStatementSyntax> GetExpressionStatementAsync(CodeRefactoringContext context)
        {
            var expressionStatement = await context.TryGetRelevantNodeAsync<TExpressionStatementSyntax>().ConfigureAwait(false);
            return expressionStatement != null && IsValid(expressionStatement, context.Span)
                ? expressionStatement
                : null;
        }

        private async Task<Document> IntroduceLocalAsync(
            Document document, TExpressionStatementSyntax expressionStatement, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var generator = SyntaxGenerator.GetGenerator(document);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var expression = (TExpressionSyntax)syntaxFacts.GetExpressionOfExpressionStatement(expressionStatement);

            var nameToken = await GenerateUniqueNameAsync(document, expression, cancellationToken).ConfigureAwait(false);
            var type = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
            var localDeclaration = (TLocalDeclarationStatementSyntax)generator.LocalDeclarationStatement(
                generator.TypeExpression(type),
                nameToken.WithAdditionalAnnotations(RenameAnnotation.Create()),
                expression.WithoutLeadingTrivia());

            localDeclaration = localDeclaration.WithLeadingTrivia(expression.GetLeadingTrivia());

            // Because expr-statements and local decl statements are so close, we can allow
            // each language to do a little extra work to ensure the resultant local decl 
            // feels right. For example, C# will want to transport the semicolon from the
            // expr statement to the local decl if it has one.
            localDeclaration = FixupLocalDeclaration(expressionStatement, localDeclaration);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(expressionStatement, localDeclaration);

            return document.WithSyntaxRoot(newRoot);
        }

        protected static async Task<SyntaxToken> GenerateUniqueNameAsync(
            Document document, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();
            var baseName = semanticFacts.GenerateNameForExpression(semanticModel, expression, capitalize: false, cancellationToken);
            var uniqueName = semanticFacts.GenerateUniqueLocalName(semanticModel, expression, containerOpt: null, baseName, cancellationToken)
                                          .WithAdditionalAnnotations(RenameAnnotation.Create());
            return uniqueName;
        }
    }
}
