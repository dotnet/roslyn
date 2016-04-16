// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveUnnecessaryCast
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryCast), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
    internal partial class RemoveUnnecessaryCastCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return RemoveUnnecessaryCastFixAllProvider.Instance;
        }

        private static CastExpressionSyntax GetCastNode(SyntaxNode root, SemanticModel model, TextSpan span, CancellationToken cancellationToken)
        {
            var token = root.FindToken(span.Start);
            if (!token.Span.IntersectsWith(span))
            {
                return null;
            }

            return token.GetAncestors<CastExpressionSyntax>()
                .FirstOrDefault(c => c.Span.IntersectsWith(span) && c.IsUnnecessaryCast(model, cancellationToken));
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var node = GetCastNode(root, model, span, cancellationToken);
            if (node == null)
            {
                return;
            }

            context.RegisterCodeFix(
                new MyCodeAction(
                    CSharpFeaturesResources.RemoveUnnecessaryCast,
                    (c) => RemoveUnnecessaryCastAsync(document, node, c)),
                context.Diagnostics);
        }

        private static async Task<Document> RemoveUnnecessaryCastAsync(Document document, CastExpressionSyntax cast, CancellationToken cancellationToken)
        {
            var annotatedCast = cast.WithAdditionalAnnotations(Simplifier.Annotation);

            if (annotatedCast.Expression is ParenthesizedExpressionSyntax)
            {
                annotatedCast = annotatedCast.WithExpression(
                    annotatedCast.Expression.WithAdditionalAnnotations(Simplifier.Annotation));
            }
            else
            {
                annotatedCast = annotatedCast.WithExpression(
                    annotatedCast.Expression.Parenthesize());
            }

            ExpressionSyntax oldNode = cast;
            ExpressionSyntax newNode = annotatedCast;

            // Ensure that we simplify any parenting parenthesized expressions not just on the syntax tree level but also on Token based
            // Case 1:
            //  In the syntax, (((Task<Action>)x).Result)() 
            //                 oldNode = (Task<Action>)x
            //                 newNode = (Task<Action>)(x)
            //                 Final newNode will be (((Task<Action>)(x)).Result)
            while (oldNode.Parent.IsKind(SyntaxKind.ParenthesizedExpression) || oldNode.GetFirstToken().GetPreviousToken().Parent.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                var parenthesizedExpression = (ParenthesizedExpressionSyntax)oldNode.GetFirstToken().GetPreviousToken().Parent;
                newNode = parenthesizedExpression.ReplaceNode(oldNode, newNode)
                    .WithAdditionalAnnotations(Simplifier.Annotation);
                oldNode = parenthesizedExpression;
            }

            newNode = newNode.WithAdditionalAnnotations(Formatter.Annotation);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(oldNode, newNode);

            return document.WithSyntaxRoot(newRoot);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}
