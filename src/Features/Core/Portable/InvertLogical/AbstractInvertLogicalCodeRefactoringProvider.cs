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
        protected abstract string GetOperatorText(TSyntaxKind binaryExprKind);
        protected abstract TSyntaxKind InvertedKind(TSyntaxKind binaryExprKind);
        protected abstract SyntaxToken CreateOpToken(TSyntaxKind binaryExprKind);

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

            while (syntaxFacts.IsParenthesizedExpression(node.Parent) ||
                   syntaxFacts.IsLogicalNotExpression(node.Parent))
            {
                node = node.Parent;
            }

            var generator = SyntaxGenerator.GetGenerator(document);
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

            var newLeft = (TExpressionSyntax)generator.Negate(left, semanticModel, cancellationToken);
            var newRight = (TExpressionSyntax)generator.Negate(right, semanticModel, cancellationToken);

            var invertedKind = InvertedKind(GetKind(binary.RawKind));
            var newOp = CreateOpToken(invertedKind).WithTriviaFrom(op);

            var newBinary = BinaryExpression(
                invertedKind, newLeft, newOp, newRight).WithAdditionalAnnotations(s_annotation);
            var newRoot = root.ReplaceNode(binary, newBinary);

            var updatedDocument = document.WithSyntaxRoot(newRoot);
            return updatedDocument;
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
