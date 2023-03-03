// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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
        protected abstract TExpressionStatementSyntax FixupDeconstruction(TExpressionStatementSyntax expressionStatement, TExpressionStatementSyntax localDeclaration);
        protected abstract Task<TExpressionStatementSyntax> CreateTupleDeconstructionAsync(
            Document document, CodeActionOptionsProvider optionsProvider, INamedTypeSymbol tupleType, TExpressionSyntax expression, CancellationToken cancellationToken);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var expressionStatement = await GetExpressionStatementAsync(context).ConfigureAwait(false);
            if (expressionStatement == null)
                return;

            var (document, _, cancellationToken) = context;
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var expression = syntaxFacts.GetExpressionOfExpressionStatement(expressionStatement);

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var type = semanticModel.GetTypeInfo(expression).Type;
            if (type == null ||
                type.SpecialType == SpecialType.System_Void)
            {
                return;
            }

            var nodeString = syntaxFacts.ConvertToSingleLine(expression).ToString();
            if (type.IsTupleType && syntaxFacts.SupportsTupleDeconstruction(expression.SyntaxTree.Options))
            {
                // prefer to emit as `var (x, y) = ...` or `(T x, T y) = ...`
                context.RegisterRefactoring(
                    CodeAction.Create(
                        string.Format(FeaturesResources.Deconstruct_locals_for_0, nodeString),
                        cancellationToken => IntroduceLocalAsync(document, context.Options, expressionStatement, type, deconstruct: true, cancellationToken),
                        nameof(FeaturesResources.Deconstruct_locals_for_0) + "_" + nodeString),
                    expressionStatement.Span);
            }

            context.RegisterRefactoring(
                CodeAction.Create(
                    string.Format(FeaturesResources.Introduce_local_for_0, nodeString),
                    cancellationToken => IntroduceLocalAsync(document, context.Options, expressionStatement, type, deconstruct: false, cancellationToken),
                    nameof(FeaturesResources.Introduce_local_for_0) + "_" + nodeString),
                expressionStatement.Span);
        }

        protected async Task<TExpressionStatementSyntax?> GetExpressionStatementAsync(CodeRefactoringContext context)
        {
            var expressionStatement = await context.TryGetRelevantNodeAsync<TExpressionStatementSyntax>().ConfigureAwait(false);
            return expressionStatement != null && IsValid(expressionStatement, context.Span)
                ? expressionStatement
                : null;
        }

        private async Task<Document> IntroduceLocalAsync(
            Document document,
            CodeActionOptionsProvider optionsProvider,
            TExpressionStatementSyntax expressionStatement,
            ITypeSymbol type,
            bool deconstruct,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var generator = SyntaxGenerator.GetGenerator(document);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var expression = (TExpressionSyntax)syntaxFacts.GetExpressionOfExpressionStatement(expressionStatement);

            var localStatement = await CreateLocalDeclarationAsync().ConfigureAwait(false);

            localStatement = localStatement.WithLeadingTrivia(expression.GetLeadingTrivia());

            // Because expr-statements and local decl statements are so close, we can allow
            // each language to do a little extra work to ensure the resultant local decl 
            // feels right. For example, C# will want to transport the semicolon from the
            // expr statement to the local decl if it has one.
            localStatement = localStatement is TLocalDeclarationStatementSyntax localDeclaration
                ? FixupLocalDeclaration(expressionStatement, localDeclaration)
                : FixupDeconstruction(expressionStatement, (TExpressionStatementSyntax)localStatement);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(expressionStatement, localStatement);

            return document.WithSyntaxRoot(newRoot);

            async Task<TStatementSyntax> CreateLocalDeclarationAsync()
            {
                if (deconstruct)
                {
                    Contract.ThrowIfNull(type);
                    return await this.CreateTupleDeconstructionAsync(
                        document, optionsProvider, (INamedTypeSymbol)type, expression, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var nameToken = await GenerateUniqueNameAsync(document, expression, cancellationToken).ConfigureAwait(false);
                    return (TLocalDeclarationStatementSyntax)generator.LocalDeclarationStatement(
                        generator.TypeExpression(type ?? semanticModel.Compilation.ObjectType),
                        nameToken.WithAdditionalAnnotations(RenameAnnotation.Create()),
                        expression.WithoutLeadingTrivia());
                }
            }
        }

        protected static async Task<SyntaxToken> GenerateUniqueNameAsync(
            Document document, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();

            var baseName = semanticFacts.GenerateNameForExpression(semanticModel, expression, capitalize: false, cancellationToken);
            return semanticFacts.GenerateUniqueLocalName(semanticModel, expression, container: null, baseName, cancellationToken);
        }
    }
}
