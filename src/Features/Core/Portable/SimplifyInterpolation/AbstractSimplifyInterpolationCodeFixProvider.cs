// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.SimplifyInterpolation
{
    internal abstract class AbstractSimplifyInterpolationCodeFixProvider<
        TInterpolationSyntax,
        TExpressionSyntax,
        TInterpolationAlignmentClause,
        TInterpolationFormatClause,
        TInterpolatedStringExpressionSyntax> : SyntaxEditorBasedCodeFixProvider
        where TInterpolationSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
        where TInterpolationAlignmentClause : SyntaxNode
        where TInterpolationFormatClause : SyntaxNode
        where TInterpolatedStringExpressionSyntax : TExpressionSyntax
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.SimplifyInterpolationId);

        internal override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        protected abstract TInterpolationSyntax WithExpression(TInterpolationSyntax interpolation, TExpressionSyntax expression);
        protected abstract TInterpolationSyntax WithAlignmentClause(TInterpolationSyntax interpolation, TInterpolationAlignmentClause alignmentClause);
        protected abstract TInterpolationSyntax WithFormatClause(TInterpolationSyntax interpolation, TInterpolationFormatClause? formatClause);
        protected abstract string Escape(TInterpolatedStringExpressionSyntax interpolatedString, string formatString);

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
            var semanticModel = await document.RequireSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;
            foreach (var diagnostic in diagnostics)
            {
                var loc = diagnostic.AdditionalLocations[0];
                var interpolation = semanticModel.GetOperation(loc.FindNode(getInnermostNodeForTie: true, cancellationToken)) as IInterpolationOperation;
                if (interpolation?.Syntax is TInterpolationSyntax interpolationSyntax &&
                    interpolationSyntax.Parent is TInterpolatedStringExpressionSyntax interpolatedString)
                {
                    Helpers.UnwrapInterpolation<TInterpolationSyntax, TExpressionSyntax>(
                        document.GetRequiredLanguageService<IVirtualCharService>(), interpolation, out var unwrapped,
                        out var alignment, out var negate, out var formatString, out _);

                    if (unwrapped == null)
                        continue;

                    alignment = negate ? (TExpressionSyntax)generator.NegateExpression(alignment) : alignment;

                    editor.ReplaceNode(
                        interpolationSyntax,
                        Update(generator, interpolatedString, interpolationSyntax, unwrapped, alignment, formatString));
                }
            }
        }

        private TInterpolationSyntax Update(
            SyntaxGenerator generator, TInterpolatedStringExpressionSyntax interpolatedString,
            TInterpolationSyntax interpolation, TExpressionSyntax unwrapped,
            TExpressionSyntax? alignment, string? formatString)
        {
            var result = WithExpression(interpolation, unwrapped);
            if (alignment != null)
            {
                result = WithAlignmentClause(
                    result,
                    (TInterpolationAlignmentClause)generator.InterpolationAlignmentClause(alignment));
            }

            if (!string.IsNullOrEmpty(formatString))
            {
                result = WithFormatClause(
                    result,
                    (TInterpolationFormatClause?)generator.InterpolationFormatClause(Escape(interpolatedString, formatString!)));
            }

            return result;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Simplify_interpolation, createChangedDocument, FeaturesResources.Simplify_interpolation)
            {
            }
        }
    }
}
