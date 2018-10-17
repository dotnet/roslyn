// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract class AbstractFormattingCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.FormattingDiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return new FixAll(this);
        }

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        CodeStyleResources.Fix_formatting,
                        c => FixOneAsync(context, diagnostic, c),
                        nameof(AbstractFormattingCodeFixProvider)),
                    diagnostic);
            }

            return Task.CompletedTask;
        }

        protected abstract OptionSet ApplyFormattingOptions(OptionSet optionSet, ICodingConventionContext codingConventionContext);

        private async Task<Document> FixOneAsync(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var options = await GetOptionsAsync(context.Document, cancellationToken).ConfigureAwait(false);
            return await FormattingCodeFixHelper.FixOneAsync(context.Document, options, diagnostic, cancellationToken).ConfigureAwait(false);
        }

        private async Task<OptionSet> GetOptionsAsync(Document document, CancellationToken cancellationToken)
        {
            OptionSet options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            // The in-IDE workspace supports .editorconfig without special handling. However, the AdhocWorkspace used
            // in testing requires manual handling of .editorconfig.
            if (document.Project.Solution.Workspace is AdhocWorkspace && File.Exists(document.FilePath ?? document.Name))
            {
                var codingConventionsManager = CodingConventionsManagerFactory.CreateCodingConventionsManager();
                var codingConventionContext = await codingConventionsManager.GetConventionContextAsync(document.FilePath ?? document.Name, cancellationToken).ConfigureAwait(false);
                options = ApplyFormattingOptions(options, codingConventionContext);
            }

            return options;
        }

        /// <summary>
        /// Provide an optimized Fix All implementation that runs
        /// <see cref="Formatter.FormatAsync(Document, Options.OptionSet, CancellationToken)"/> on the document(s)
        /// included in the Fix All scope.
        /// </summary>
        private class FixAll : DocumentBasedFixAllProvider
        {
            private readonly AbstractFormattingCodeFixProvider _formattingCodeFixProvider;

            public FixAll(AbstractFormattingCodeFixProvider formattingCodeFixProvider)
            {
                _formattingCodeFixProvider = formattingCodeFixProvider;
            }

            protected override string CodeActionTitle => CodeStyleResources.Fix_formatting;

            protected override async Task<SyntaxNode> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
            {
                var options = await _formattingCodeFixProvider.GetOptionsAsync(document, fixAllContext.CancellationToken).ConfigureAwait(false);
                var updatedDocument = await Formatter.FormatAsync(document, options, fixAllContext.CancellationToken).ConfigureAwait(false);
                return await updatedDocument.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            }
        }
    }
}
