// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp,
        Name = PredefinedCodeRefactoringProviderNames.UseExpressionBody), Shared]
    internal class UseExpressionBodyCodeRefactoringProvider : CodeRefactoringProvider
    {
        private static readonly ImmutableArray<UseExpressionBodyHelper> _helpers = UseExpressionBodyHelper.Helpers;

        [ImportingConstructor]
        public UseExpressionBodyCodeRefactoringProvider()
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
            var node = root.FindToken(position).Parent;
            if (node == null)
            {
                return;
            }

            var containingLambda = node.FirstAncestorOrSelf<LambdaExpressionSyntax>();
            if (containingLambda != null &&
                node.AncestorsAndSelf().Contains(containingLambda.Body))
            {
                // don't offer inside a lambda.  Lambdas can be quite large, and it will be very noisy
                // inside the body of one to be offering to use a block/expression body for the containing
                // class member.
                return;
            }

            var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            foreach (var helper in _helpers)
            {
                var succeeded = TryComputeRefactoring(context, root, node, optionSet, helper);
                if (succeeded)
                {
                    return;
                }
            }
        }

        private bool TryComputeRefactoring(
            CodeRefactoringContext context,
            SyntaxNode root, SyntaxNode node, OptionSet optionSet,
            UseExpressionBodyHelper helper)
        {
            var declaration = GetDeclaration(node, helper);
            if (declaration == null)
            {
                return false;
            }

            var document = context.Document;

            var succeeded = false;
            if (helper.CanOfferUseExpressionBody(optionSet, declaration, forAnalyzer: false))
            {
                context.RegisterRefactoring(new MyCodeAction(
                    helper.UseExpressionBodyTitle.ToString(),
                    c => UpdateDocumentAsync(
                        document, root, declaration, helper,
                        useExpressionBody: true, cancellationToken: c)));
                succeeded = true;
            }

            var (canOffer, _) = helper.CanOfferUseBlockBody(optionSet, declaration, forAnalyzer: false);
            if (canOffer)
            {
                context.RegisterRefactoring(new MyCodeAction(
                    helper.UseBlockBodyTitle.ToString(),
                    c => UpdateDocumentAsync(
                        document, root, declaration, helper,
                        useExpressionBody: false, cancellationToken: c)));
                succeeded = true;
            }

            return succeeded;
        }

        private SyntaxNode GetDeclaration(SyntaxNode node, UseExpressionBodyHelper helper)
        {
            for (var current = node; current != null; current = current.Parent)
            {
                if (helper.SyntaxKinds.Contains(current.Kind()))
                {
                    return current;
                }
            }

            return null;
        }

        private async Task<Document> UpdateDocumentAsync(
            Document document, SyntaxNode root, SyntaxNode declaration,
            UseExpressionBodyHelper helper, bool useExpressionBody,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var updatedDeclaration = helper.Update(semanticModel, declaration, useExpressionBody);

            var parent = declaration is AccessorDeclarationSyntax
                ? declaration.Parent
                : declaration;
            var updatedParent = parent.ReplaceNode(declaration, updatedDeclaration)
                                      .WithAdditionalAnnotations(Formatter.Annotation);

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
