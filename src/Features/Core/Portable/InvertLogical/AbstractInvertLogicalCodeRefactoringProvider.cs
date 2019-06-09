﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.InvertLogical
{
    /// <summary>
    /// Code refactoring to help convert code like `!a || !b` to `!(a &amp;&amp; b)`
    /// </summary>
    internal abstract class AbstractInvertLogicalCodeRefactoringProvider<
        TSyntaxKind,
        TExpressionSyntax,
        TBinaryExpressionSyntax>
        : CodeRefactoringProvider
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TBinaryExpressionSyntax : TExpressionSyntax
    {
        /// <summary>
        /// See comment in <see cref="InvertLogicalAsync"/> to understand the need for this annotation.
        /// </summary>
        private static readonly SyntaxAnnotation s_annotation = new SyntaxAnnotation();

        protected abstract TSyntaxKind GetKind(int rawKind);
        protected abstract TSyntaxKind InvertedKind(TSyntaxKind binaryExprKind);
        protected abstract string GetOperatorText(TSyntaxKind binaryExprKind);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            var position = span.Start;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);

            if (span.Length > 0 && span != token.Span)
            {
                return;
            }

            if (span.Length == 0 && !token.Span.IntersectsWith(position))
            {
                return;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var parent = token.Parent;
            if (!syntaxFacts.IsLogicalAndExpression(parent) &&
                !syntaxFacts.IsLogicalOrExpression(parent))
            {
                return;
            }

            context.RegisterRefactoring(new MyCodeAction(
                GetTitle(GetKind(parent.RawKind)),
                c => InvertLogicalAsync(document, position, c)));
        }

        private async Task<Document> InvertLogicalAsync(
            Document document1, int position, CancellationToken cancellationToken)
        {
            // We invert in two steps.  To invert `a op b` we are effectively generating two negations:
            // `!(!(a op b)`.  The inner `!` will distribute on the inside to make `!a op' !b` leaving
            // us with `!(!a op' !b)`.

            // Because we need to do two negations, we actually perform the inner one, marking the
            // result with an annotation, then we do the outer one (making sure we don't descend in
            // and undo the work we just did).  Because our negation helper needs semantics, we generate
            // a new document at each step so that we'll be able to properly analyze things as we go
            // along.
            var document2 = await InvertInnerExpressionAsync(document1, position, cancellationToken).ConfigureAwait(false);
            var document3 = await InvertOuterExpressionAsync(document2, cancellationToken).ConfigureAwait(false);
            return document3;
        }

        private async Task<Document> InvertInnerExpressionAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Walk up to the topmost binary of the same type.  When converting || to && (or vice versa)
            // we want to grab the entire set.  i.e.  `!a && !b && !c` should become `!(a || b || c)` not
            // `!(a || b) && !c`
            var binaryExpression = root.FindToken(position).Parent;
            while (binaryExpression.Parent?.RawKind == binaryExpression.RawKind)
            {
                binaryExpression = binaryExpression.Parent;
            }

            var generator = SyntaxGenerator.GetGenerator(document);
            var newBinary = generator.Negate(binaryExpression, semanticModel, cancellationToken);

            return document.WithSyntaxRoot(root.ReplaceNode(
                binaryExpression,
                newBinary.WithAdditionalAnnotations(s_annotation)));
        }

        private async Task<Document> InvertOuterExpressionAsync(
            Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var expression = root.GetAnnotatedNodes(s_annotation).Single();

            // Walk up parens and !'s.  That way we don't end up with something like !!.
            // It also ensures that this refactoring reverses itself when invoked twice.
            while (syntaxFacts.IsParenthesizedExpression(expression.Parent) ||
                   syntaxFacts.IsLogicalNotExpression(expression.Parent))
            {
                expression = expression.Parent;
            }

            var generator = SyntaxGenerator.GetGenerator(document);

            // Negate the containing binary expr.  Pass the 'negateBinary:false' flag so we don't
            // just negate the work we're actually doing right now.
            return document.WithSyntaxRoot(root.ReplaceNode(
                expression,
                generator.Negate(expression, semanticModel, negateBinary: false, cancellationToken)));
        }

        private string GetTitle(TSyntaxKind binaryExprKind)
            => string.Format(FeaturesResources.Replace_0_with_1,
                    GetOperatorText(binaryExprKind), GetOperatorText(InvertedKind(binaryExprKind)));

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
