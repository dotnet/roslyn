// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.InvertLogical
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class CSharpInvertLogicalCodeRefactoringProvider : CodeRefactoringProvider
    {
        private static readonly SyntaxAnnotation s_annotation = new SyntaxAnnotation();

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

            var parent = token.Parent;
            if (!parent.IsKind(SyntaxKind.LogicalAndExpression) && 
                !parent.IsKind(SyntaxKind.LogicalOrExpression))
            {
                return;
            }

            context.RegisterRefactoring(new MyCodeAction(
                GetTitle(parent.Kind()),
                c => InvertLogicalAsync(document, position, c)));
        }

        private async Task<Document> InvertLogicalAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var updatedDocument1 = await InvertLeftAndRightOfBinaryAsync(document, position, cancellationToken).ConfigureAwait(false);
            var updatedDocument2 = await InvertBinaryAsync(updatedDocument1, cancellationToken).ConfigureAwait(false);
            return updatedDocument2;
        }

        private async Task<Document> InvertBinaryAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var node = root.GetAnnotatedNodes(s_annotation).Single();

            while (node.IsParentKind(SyntaxKind.ParenthesizedExpression) ||
                   node.IsParentKind(SyntaxKind.LogicalNotExpression))
            {
                node = node.Parent;
            }

            var generator = SyntaxGenerator.GetGenerator(document);
            var updatedNode = generator.Negate(node, semanticModel, negateBinary: false, cancellationToken);
            
            var updatedRoot = root.ReplaceNode(node, updatedNode);

            return document.WithSyntaxRoot(updatedRoot);
        }

        private static async Task<Document> InvertLeftAndRightOfBinaryAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var generator = SyntaxGenerator.GetGenerator(document);

            var token = root.FindToken(position);
            var binary = (BinaryExpressionSyntax)token.Parent;

            var left = binary.Left;
            var op = binary.OperatorToken;
            var right = binary.Right;

            var newLeft = (ExpressionSyntax)generator.Negate(left, semanticModel, cancellationToken);
            var newRight = (ExpressionSyntax)generator.Negate(right, semanticModel, cancellationToken);

            var newOp = binary.Kind() == SyntaxKind.LogicalAndExpression
                ? SyntaxFactory.Token(SyntaxKind.BarBarToken)
                : SyntaxFactory.Token(SyntaxKind.AmpersandAmpersandToken);

            var newBinary = SyntaxFactory.BinaryExpression(
                InvertedKind(binary.Kind()), newLeft, newOp, newRight).WithAdditionalAnnotations(s_annotation);
            var newRoot = root.ReplaceNode(binary, newBinary);

            var updatedDocument = document.WithSyntaxRoot(newRoot);
            return updatedDocument;
        }

        private string GetTitle(SyntaxKind kind)
            => string.Format(FeaturesResources.Replace_0_with_1,
                    GetText(kind), GetText(InvertedKind(kind)));

        private string GetText(SyntaxKind syntaxKind)
            => syntaxKind == SyntaxKind.LogicalAndExpression
                ? SyntaxFacts.GetText(SyntaxKind.AmpersandAmpersandToken)
                : SyntaxFacts.GetText(SyntaxKind.BarBarToken);

        private static SyntaxKind InvertedKind(SyntaxKind syntaxKind)
            => syntaxKind == SyntaxKind.LogicalAndExpression ? SyntaxKind.LogicalOrExpression : SyntaxKind.LogicalAndExpression;

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
