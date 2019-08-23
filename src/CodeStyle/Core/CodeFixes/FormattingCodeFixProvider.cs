// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    extern alias CodeStyle;
    using Formatter = CodeStyle::Microsoft.CodeAnalysis.Formatting.Formatter;
    using ISyntaxFormattingService = ISyntaxFormattingService;

    internal abstract class AbstractFormattingCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.FormattingDiagnosticId);

        protected abstract ISyntaxFormattingService SyntaxFormattingService { get; }

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
            var tree = await context.Document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var updatedTree = await FormattingCodeFixHelper.FixOneAsync(tree, SyntaxFormattingService, options, diagnostic, cancellationToken).ConfigureAwait(false);
            return context.Document.WithText(await updatedTree.GetTextAsync(cancellationToken).ConfigureAwait(false));
        }

        private async Task<OptionSet> GetOptionsAsync(Document document, CancellationToken cancellationToken)
        {
            OptionSet options = CompilerAnalyzerConfigOptions.Empty;

            // The in-IDE workspace supports .editorconfig without special handling. However, the AdhocWorkspace used
            // in testing requires manual handling of .editorconfig.
            if (File.Exists(document.FilePath ?? document.Name))
            {
                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var codingConventionsManager = new AnalyzerConfigCodingConventionsManager(tree, document.Project.AnalyzerOptions);
                var codingConventionContext = await codingConventionsManager.GetConventionContextAsync(document.FilePath ?? document.Name, cancellationToken).ConfigureAwait(false);
                options = ApplyFormattingOptions(options, codingConventionContext);
            }

            return options;
        }

        /// <summary>
        /// Provide an optimized Fix All implementation that runs
        /// <see cref="Formatter.Format(SyntaxNode, ISyntaxFormattingService, OptionSet, CancellationToken)"/> on the document(s)
        /// included in the Fix All scope.
        /// </summary>
        private sealed class FixAll : DocumentBasedFixAllProvider
        {
            private readonly AbstractFormattingCodeFixProvider _formattingCodeFixProvider;

            public FixAll(AbstractFormattingCodeFixProvider formattingCodeFixProvider)
            {
                _formattingCodeFixProvider = formattingCodeFixProvider;
            }

            protected override string GetCodeActionTitle(FixAllContext fixAllContext) => CodeStyleResources.Fix_formatting;

            protected override async Task<Document> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
            {
                var options = await _formattingCodeFixProvider.GetOptionsAsync(document, fixAllContext.CancellationToken).ConfigureAwait(false);
                var syntaxRoot = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
                var updatedSyntaxRoot = Formatter.Format(syntaxRoot, _formattingCodeFixProvider.SyntaxFormattingService, options, fixAllContext.CancellationToken);
                return document.WithSyntaxRoot(updatedSyntaxRoot);
            }
        }
    }
}
