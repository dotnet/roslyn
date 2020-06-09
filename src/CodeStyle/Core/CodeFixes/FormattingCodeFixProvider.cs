// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
using Formatter = Microsoft.CodeAnalysis.Formatting.FormatterHelper;
#endif

namespace Microsoft.CodeAnalysis.CodeStyle
{
    using ISyntaxFormattingService = ISyntaxFormattingService;

    internal abstract class AbstractFormattingCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.FormattingDiagnosticId);

        protected abstract ISyntaxFormattingService SyntaxFormattingService { get; }

        public sealed override FixAllProvider GetFixAllProvider()
            => new FixAll(this);

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

        private async Task<Document> FixOneAsync(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var options = await GetOptionsAsync(context.Document, cancellationToken).ConfigureAwait(false);
            var tree = await context.Document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var updatedTree = await FormattingCodeFixHelper.FixOneAsync(tree, SyntaxFormattingService, options, diagnostic, cancellationToken).ConfigureAwait(false);
            return context.Document.WithText(await updatedTree.GetTextAsync(cancellationToken).ConfigureAwait(false));
        }

        private static async Task<OptionSet> GetOptionsAsync(Document document, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var analyzerConfigOptions = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree);

            return analyzerConfigOptions;
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
                => _formattingCodeFixProvider = formattingCodeFixProvider;

            protected override string CodeActionTitle => CodeStyleResources.Fix_formatting;

            protected override async Task<SyntaxNode> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
            {
                var options = await GetOptionsAsync(document, fixAllContext.CancellationToken).ConfigureAwait(false);
                var syntaxRoot = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
                var updatedSyntaxRoot = Formatter.Format(syntaxRoot, _formattingCodeFixProvider.SyntaxFormattingService, options, fixAllContext.CancellationToken);
                return updatedSyntaxRoot;
            }
        }
    }
}
