// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda
{
    using static UseExpressionBodyForLambdaHelpers;

    [ExportCodeRefactoringProvider(LanguageNames.CSharp,
        Name = PredefinedCodeRefactoringProviderNames.UseExpressionBody), Shared]
    internal sealed class UseExpressionBodyForLambdaCodeRefactoringProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        public UseExpressionBodyForLambdaCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            if (textSpan.Length > 0)
            {
                return;
            }

            var position = textSpan.Start;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var lambdaNode = root.FindToken(position).Parent.FirstAncestorOrSelf<LambdaExpressionSyntax>();
            if (lambdaNode == null)
            {
                return;
            }

            var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            if (CanOfferUseExpressionBody(optionSet, lambdaNode, forAnalyzer: false))
            {
                context.RegisterRefactoring(new MyCodeAction(
                    UseExpressionBodyTitle.ToString(),
                    c => UpdateDocumentAsync(
                        document, root, lambdaNode,
                        useExpressionBody: true, c)), lambdaNode.Span);
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var (canOffer, _) = CanOfferUseBlockBody(
                semanticModel, optionSet, lambdaNode, forAnalyzer: false, cancellationToken);
            if (canOffer)
            {
                context.RegisterRefactoring(new MyCodeAction(
                    UseBlockBodyTitle.ToString(),
                    c => UpdateDocumentAsync(
                        document, root, lambdaNode,
                        useExpressionBody: false, c)), lambdaNode.Span);
            }
        }

        private async Task<Document> UpdateDocumentAsync(
            Document document, SyntaxNode root, LambdaExpressionSyntax declaration,
            bool useExpressionBody, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // We're only replacing a single declaration in the refactoring.  So pass 'declaration'
            // as both the 'original' and 'current' declaration.
            var updatedDeclaration = Update(semanticModel, useExpressionBody, declaration, declaration);

            var newRoot = root.ReplaceNode(declaration, updatedDeclaration);
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
