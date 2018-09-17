// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal abstract class AbstractInvertLogicalCodeRefactoringProvider<
        TSyntaxKind, 
        TExpressionSyntax,
        TBinaryExpressionSyntax>
        : CodeRefactoringProvider
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TBinaryExpressionSyntax : TExpressionSyntax
    {
        private static readonly SyntaxAnnotation s_annotation = new SyntaxAnnotation();

        protected abstract TSyntaxKind GetKind(int rawKind);
        protected abstract TSyntaxKind InvertedKind(TSyntaxKind binaryExprKind);

        protected abstract TSyntaxKind GetOperatorTokenKind(TSyntaxKind binaryExprKind);
        protected abstract SyntaxToken CreateOperatorToken(TSyntaxKind operatorTokenKind);

        protected abstract TBinaryExpressionSyntax BinaryExpression(
            TSyntaxKind syntaxKind, TExpressionSyntax newLeft, SyntaxToken newOp, TExpressionSyntax newRight);

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
            Document document, int position, CancellationToken cancellationToken)
        {
            var updatedDocument1 = await InvertLeftAndRightOfBinaryAsync(document, position, cancellationToken).ConfigureAwait(false);
            var updatedDocument2 = await InvertBinaryAsync(updatedDocument1, cancellationToken).ConfigureAwait(false);
            return updatedDocument2;
        }

        private async Task<Document> InvertBinaryAsync(
            Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var node = root.GetAnnotatedNodes(s_annotation).Single();

            // Walk up parens and !'s.  That way we don't end up with something like !!.
            // It also ensures that this refactoring is reversible by invoking it again.
            while (syntaxFacts.IsParenthesizedExpression(node.Parent) ||
                   syntaxFacts.IsLogicalNotExpression(node.Parent))
            {
                node = node.Parent;
            }

            var generator = SyntaxGenerator.GetGenerator(document);

            // Negate the containing binary expr.  Pass the 'negateBinary:false' flag so we don't
            // just negate the work we're actually doing right now.
            var updatedNode = generator.Negate(node, semanticModel, negateBinary: false, cancellationToken);
            
            var updatedRoot = root.ReplaceNode(node, updatedNode);

            return document.WithSyntaxRoot(updatedRoot);
        }

        private async Task<Document> InvertLeftAndRightOfBinaryAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var generator = SyntaxGenerator.GetGenerator(document);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var token = root.FindToken(position);
            var binary = token.Parent;

            syntaxFacts.GetPartsOfBinaryExpression(binary,
                out var left, out var op, out var right);

            var invertedKind = InvertedKind(GetKind(binary.RawKind));

            var newBinary = BinaryExpression(
                invertedKind,
                (TExpressionSyntax)generator.Negate(left, semanticModel, cancellationToken),
                CreateOperatorToken(GetOperatorTokenKind(invertedKind)).WithTriviaFrom(op),
                (TExpressionSyntax)generator.Negate(right, semanticModel, cancellationToken));

            return document.WithSyntaxRoot(
                root.ReplaceNode(binary.WithAdditionalAnnotations(s_annotation), newBinary));
        }

        private string GetTitle(TSyntaxKind binaryExprKind)
            => string.Format(FeaturesResources.Replace_0_with_1,
                    GetOperatorText(binaryExprKind), GetOperatorText(InvertedKind(binaryExprKind)));

        private string GetOperatorText(TSyntaxKind binaryExprKind)
            => CreateOperatorToken(GetOperatorTokenKind(binaryExprKind)).Text;

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
