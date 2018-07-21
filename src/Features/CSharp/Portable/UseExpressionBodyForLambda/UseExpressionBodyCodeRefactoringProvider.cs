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
            if (!(root.FindToken(position).Parent is LambdaExpressionSyntax node))
            {
                return;
            }

            var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            await TryComputeRefactoringAsync(context, root, node, optionSet).ConfigureAwait(false);
        }

        private async Task<bool> TryComputeRefactoringAsync(
            CodeRefactoringContext context, SyntaxNode root,
            LambdaExpressionSyntax node, OptionSet optionSet)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var succeeded = false;
            if (CanOfferUseExpressionBody(optionSet, node, forAnalyzer: false))
            {
                context.RegisterRefactoring(new MyCodeAction(
                    UseExpressionBodyTitle.ToString(),
                    c => UpdateDocumentAsync(
                        document, root, node,
                        useExpressionBody: true, c)));
                succeeded = true;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var (canOffer, _) = CanOfferUseBlockBody(
                semanticModel, optionSet, node, forAnalyzer: false, cancellationToken);
            if (canOffer)
            {
                context.RegisterRefactoring(new MyCodeAction(
                    UseBlockBodyTitle.ToString(),
                    c => UpdateDocumentAsync(
                        document, root, node,
                        useExpressionBody: false, c)));
                succeeded = true;
            }

            return succeeded;
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
