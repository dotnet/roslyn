// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

        public sealed override FixAllProvider GetFixAllProvider()
            => FixAllProvider.Create(async (context, document, diagnostics) =>
            {
                var cancellationToken = context.CancellationToken;
                var options = await GetOptionsAsync(document, cancellationToken).ConfigureAwait(false);
                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var updatedSyntaxRoot = Formatter.Format(syntaxRoot, this.SyntaxFormattingService, options, cancellationToken);
                return document.WithSyntaxRoot(updatedSyntaxRoot);
            });
    }
}
