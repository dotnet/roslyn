﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.LanguageServices;
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
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;
            var generatorInternal = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();
            foreach (var diagnostic in diagnostics)
            {
                var loc = diagnostic.AdditionalLocations[0];
                var interpolation = semanticModel.GetOperation(loc.FindNode(getInnermostNodeForTie: true, cancellationToken), cancellationToken) as IInterpolationOperation;
                if (interpolation?.Syntax is TInterpolationSyntax interpolationSyntax &&
                    interpolationSyntax.Parent is TInterpolatedStringExpressionSyntax interpolatedString)
                {
                    Helpers.UnwrapInterpolation<TInterpolationSyntax, TExpressionSyntax>(
                        document.GetRequiredLanguageService<IVirtualCharLanguageService>(),
                        document.GetRequiredLanguageService<ISyntaxFactsService>(), interpolation, out var unwrapped,
                        out var alignment, out var negate, out var formatString, out _);

                    if (unwrapped == null)
                        continue;

                    alignment = negate ? (TExpressionSyntax)generator.NegateExpression(alignment) : alignment;

                    editor.ReplaceNode(
                        interpolationSyntax,
                        Update(generatorInternal, interpolatedString, interpolationSyntax, unwrapped, alignment, formatString));
                }
            }
        }

        private TInterpolationSyntax Update(
            SyntaxGeneratorInternal generator, TInterpolatedStringExpressionSyntax interpolatedString,
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

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(AnalyzersResources.Simplify_interpolation, createChangedDocument, AnalyzersResources.Simplify_interpolation)
            {
            }
        }
    }
}
