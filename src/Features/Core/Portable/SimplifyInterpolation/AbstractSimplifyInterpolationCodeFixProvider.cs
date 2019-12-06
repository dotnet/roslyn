// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.SimplifyInterpolation
{
    internal abstract class AbstractSimplifyInterpolationCodeFixProvider<
        TInterpolationSyntax,
        TExpressionSyntax> : SyntaxEditorBasedCodeFixProvider
        where TInterpolationSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.SimplifyInterpolationId);

        internal override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;
            foreach (var diagnostic in diagnostics)
            {
                var loc = diagnostic.AdditionalLocations[0];
                var interpolation = semanticModel.GetOperation(loc.FindNode(getInnermostNodeForTie: true, cancellationToken)) as IInterpolationOperation;
                if (interpolation?.Syntax is TInterpolationSyntax interpolationSyntax)
                {
                    Helpers.UnwrapInterpolation<TInterpolationSyntax, TExpressionSyntax>(
                        interpolation, out var unwrapped, out var alignment, out var negate, out var formatString);

                    alignment = negate ? (TExpressionSyntax)generator.NegateExpression(alignment) : alignment;
                    editor.ReplaceNode(
                        interpolationSyntax,
                        Update(interpolationSyntax, unwrapped, alignment, formatString));
                }
            }
        }

        protected abstract TInterpolationSyntax Update(
            TInterpolationSyntax interpolation, TExpressionSyntax unwrapped, TExpressionSyntax alignment, string formatString);

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Simplify_interpolation, createChangedDocument, FeaturesResources.Simplify_interpolation)
            {
            }
        }
    }
}
