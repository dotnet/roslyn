// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.NewLines.ArrowExpressionClausePlacement
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ArrowExpressionClausePlacement), Shared]
    internal sealed class ArrowExpressionClausePlacementCodeFixProvider : CodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ArrowExpressionClausePlacementCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.ArrowExpressionClausePlacementDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(
                CodeAction.Create(
                    CSharpCodeFixesResources.Place_token_on_following_line,
                    c => UpdateDocumentAsync(document, ImmutableArray.Create(diagnostic), c),
                    nameof(CSharpCodeFixesResources.Place_token_on_following_line)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        private static async Task<Document> UpdateDocumentAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            using var _ = ArrayBuilder<TextChange>.GetInstance(out var edits);

            foreach (var diagnostic in diagnostics)
            {
                var arrowToken = root.FindToken(diagnostic.Location.SourceSpan.Start);
                Contract.ThrowIfTrue(arrowToken.Kind() != SyntaxKind.EqualsGreaterThanToken);

                var arrowExpression = (ArrowExpressionClauseSyntax)arrowToken.GetRequiredParent();

                AddEdits(text, arrowExpression.ArrowToken, arrowExpression.Expression, edits);
            }

            var changedText = text.WithChanges(edits);
            return document.WithText(changedText);
        }

        private static void AddEdits(
            SourceText text,
            SyntaxToken token,
            ExpressionSyntax nextExpression,
            ArrayBuilder<TextChange> edits)
        {
            // Cases to consider
            // x =>
            // x => 
            // x => /* comment */
            // x /* comment */ =>
            // x /* comment */ => /* comment */
            //
            // in all these cases, we want to grab the arrow, and any spaces that follow and remove that, but we
            // leave the rest where it is. We then move the arrow right before the start of the next token.

            var start = token.SpanStart;
            var end = token.Span.End;

            while (end < text.Length && text[end] == ' ')
                end++;

            if (end < text.Length && SyntaxFacts.IsNewLine(text[end]))
            {
                while (start > 0 && text[start - 1] == ' ')
                    start--;
            }

            edits.Add(new TextChange(TextSpan.FromBounds(start, end), ""));
            edits.Add(new TextChange(new TextSpan(nextExpression.SpanStart, 0), token.Text + " "));
        }

        public override FixAllProvider? GetFixAllProvider()
            => FixAllProvider.Create(async (context, document, diagnostics) => await UpdateDocumentAsync(document, diagnostics, context.CancellationToken).ConfigureAwait(false));
    }
}
