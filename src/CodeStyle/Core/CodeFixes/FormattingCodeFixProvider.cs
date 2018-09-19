// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = "FixFormatting")]
    [Shared]
    internal class FormattingCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(AbstractFormattingAnalyzer.FormattingDiagnosticId);

        public override FixAllProvider GetFixAllProvider()
        {
            return new FixAll();
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeStyleFixesResources.Formatting_analyzer_code_fix,
                    c => FixOneAsync(context.Document, context.Diagnostics, c),
                    nameof(FormattingCodeFixProvider)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected async Task<Document> FixOneAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var changes = new List<TextChange>();
            foreach (var diagnostic in diagnostics)
            {
                if (!diagnostic.Properties.TryGetValue(AbstractFormattingAnalyzerImpl.ReplaceTextKey, out var replacement))
                {
                    continue;
                }

                changes.Add(new TextChange(diagnostic.Location.SourceSpan, replacement));
            }

            changes.Sort((left, right) => left.Span.Start.CompareTo(right.Span.Start));

            return document.WithText(text.WithChanges(changes));
        }

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
