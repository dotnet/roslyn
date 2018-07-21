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
    [ExportCodeRefactoringProvider(LanguageNames.CSharp,
        Name = PredefinedCodeRefactoringProviderNames.UseExpressionBody), Shared]
    internal class UseExpressionBodyForLambdaCodeRefactoringProvider : CodeRefactoringProvider
    {
        private static readonly ImmutableArray<UseExpressionBodyHelper> _helpers = 
            ImmutableArray.Create(UseExpressionBodyHelper.Instance);

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

            foreach (var helper in _helpers)
            {
                var succeeded = await TryComputeRefactoringAsync(context, root, node, optionSet, helper);
                if (succeeded)
                {
                    return;
                }
            }
        }

        private async Task<bool> TryComputeRefactoringAsync(
            CodeRefactoringContext context,
            SyntaxNode root, LambdaExpressionSyntax node, OptionSet optionSet,
            UseExpressionBodyHelper helper)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var succeeded = false;
            if (helper.CanOfferUseExpressionBody(optionSet, node, forAnalyzer: false))
            {
                context.RegisterRefactoring(new MyCodeAction(
                    helper.UseExpressionBodyTitle.ToString(),
                    c => UpdateDocumentAsync(
                        document, root, node, helper,
                        useExpressionBody: true, c)));
                succeeded = true;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var (canOffer, _) = helper.CanOfferUseBlockBody(
                semanticModel, optionSet, node, forAnalyzer: false, cancellationToken);
            if (canOffer)
            {
                context.RegisterRefactoring(new MyCodeAction(
                    helper.UseBlockBodyTitle.ToString(),
                    c => UpdateDocumentAsync(
                        document, root, node, helper,
                        useExpressionBody: false, c)));
                succeeded = true;
            }

            return succeeded;
        }

        private async Task<Document> UpdateDocumentAsync(
            Document document, SyntaxNode root, LambdaExpressionSyntax declaration,
            UseExpressionBodyHelper helper, bool useExpressionBody, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var updatedDeclaration = helper.Update(semanticModel, declaration, useExpressionBody);

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
