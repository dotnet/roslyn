// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = "FixFormatting")]
    [Shared]
    internal class FormattingCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.FormattingDiagnosticId);

        public override FixAllProvider GetFixAllProvider()
        {
            return new FixAll();
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        CodeStyleFixesResources.Formatting_analyzer_code_fix,
                        c => FixOneAsync(context.Document, diagnostic, c),
                        nameof(FormattingCodeFixProvider)),
                    diagnostic);
            }

            return Task.CompletedTask;
        }

        protected async Task<Document> FixOneAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            // The span to format is the full line(s) containing the diagnostic
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var diagnosticLinePositionSpan = text.Lines.GetLinePositionSpan(diagnosticSpan);
            var spanToFormat = TextSpan.FromBounds(
                text.Lines[diagnosticLinePositionSpan.Start.Line].Start,
                text.Lines[diagnosticLinePositionSpan.End.Line].End);

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return await Formatter.FormatAsync(document, spanToFormat, options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Provide an optimized Fix All implementation that runs
        /// <see cref="Formatter.FormatAsync(Document, Options.OptionSet, CancellationToken)"/> on the document(s)
        /// included in the Fix All scope.
        /// </summary>
        private class FixAll : DocumentBasedFixAllProvider
        {
            protected override string CodeActionTitle => CodeStyleFixesResources.Formatting_analyzer_code_fix;

            protected override async Task<SyntaxNode> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
            {
                var options = await document.GetOptionsAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
                var updatedDocument = await Formatter.FormatAsync(document, options, fixAllContext.CancellationToken).ConfigureAwait(false);
                return await updatedDocument.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            }
        }
    }
}
