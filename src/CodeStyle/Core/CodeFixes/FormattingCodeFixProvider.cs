// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.FixFormatting)]
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
                        c => FormattingCodeFixHelper.FixOneAsync(context.Document, diagnostic, c),
                        nameof(FormattingCodeFixProvider)),
                    diagnostic);
            }

            return Task.CompletedTask;
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
