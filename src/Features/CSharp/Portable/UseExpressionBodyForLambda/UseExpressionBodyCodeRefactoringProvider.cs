// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda
{
    using static UseExpressionBodyForLambdaHelpers;

    [ExportCodeRefactoringProvider(LanguageNames.CSharp,
        Name = PredefinedCodeRefactoringProviderNames.UseExpressionBody), Shared]
    internal class UseExpressionBodyForLambdaCodeRefactoringProvider : CodeRefactoringProvider
    {
        public UseExpressionBodyForLambdaCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            if (context.Span.Length > 0)
            {
                return;
            }

            var position = context.Span.Start;
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);
            var lambdaNode = token.Parent.FirstAncestorOrSelf<LambdaExpressionSyntax>();
            if (lambdaNode == null)
            {
                return;
            }

            // Caret has to be in the signature portion of the lambda.  We don't want it showing up
            // arbitrarily deep in the body.
            if (position < lambdaNode.SpanStart || position > lambdaNode.ArrowToken.Span.End)
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
                        useExpressionBody: true, c)));
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
                        useExpressionBody: false, c)));
            }
        }

        private async Task<Document> UpdateDocumentAsync(
            Document document, SyntaxNode root, LambdaExpressionSyntax declaration,
            bool useExpressionBody, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var updatedDeclaration = Update(semanticModel, declaration, useExpressionBody);

            var parent = declaration.Parent;
            var updatedParent = parent.ReplaceNode(declaration, updatedDeclaration);

            var newRoot = root.ReplaceNode(parent, updatedParent);
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
