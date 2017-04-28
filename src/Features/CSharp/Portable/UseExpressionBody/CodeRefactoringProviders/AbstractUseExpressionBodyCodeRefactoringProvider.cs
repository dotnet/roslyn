// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
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
    internal abstract class AbstractUseExpressionBodyCodeRefactoringProvider<TDeclaration> :
        CodeRefactoringProvider
        where TDeclaration : SyntaxNode
    {
        private readonly AbstractUseExpressionBodyHelper<TDeclaration> _helper;

        protected AbstractUseExpressionBodyCodeRefactoringProvider(
            AbstractUseExpressionBodyHelper<TDeclaration> helper)
        {
            _helper = helper;
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

            var declaration = node.FirstAncestorOrSelf<TDeclaration>();
            if (declaration == null)
            {
                return;
            }

            var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            if (_helper.CanOfferUseExpressionBody(optionSet, declaration, forAnalyzer: false))
            {
                context.RegisterRefactoring(new MyCodeAction(
                    _helper.UseExpressionBodyTitle.ToString(),
                    c => UpdateDocumentAsync(document, root, declaration, optionSet, c)));
            }

            if (_helper.CanOfferUseBlockBody(optionSet, declaration, forAnalyzer: false))
            {
                context.RegisterRefactoring(new MyCodeAction(
                    _helper.UseBlockBodyTitle.ToString(),
                    c => UpdateDocumentAsync(document, root, declaration, optionSet, c)));
            }
        }

        private Task<Document> UpdateDocumentAsync(
            Document document, SyntaxNode root, TDeclaration declaration,
            DocumentOptionSet options, CancellationToken cancellationToken)
        {
            var updatedDeclaration = _helper.Update(declaration, options)
                                            .WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(declaration, updatedDeclaration);

            return Task.FromResult(document.WithSyntaxRoot(newRoot));
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