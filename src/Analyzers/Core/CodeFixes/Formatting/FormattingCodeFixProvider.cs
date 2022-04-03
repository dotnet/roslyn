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
using Microsoft.CodeAnalysis.Shared.Extensions;

using Formatter = Microsoft.CodeAnalysis.Formatting.FormatterHelper;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract class AbstractFormattingCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.FormattingDiagnosticId);

        protected abstract ISyntaxFormatting SyntaxFormatting { get; }

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
#pragma warning disable RS0005 // Do not use generic 'CodeAction.Create' to create 'CodeAction'
                context.RegisterCodeFix(
                    CodeAction.Create(
                        AnalyzersResources.Fix_formatting,
                        c => FixOneAsync(context, diagnostic, c),
                        nameof(AbstractFormattingCodeFixProvider)),
                    diagnostic);
#pragma warning restore RS0005 // Do not use generic 'CodeAction.Create' to create 'CodeAction'
            }

            return Task.CompletedTask;
        }

        private async Task<Document> FixOneAsync(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var options = await GetOptionsAsync(context.Document, cancellationToken).ConfigureAwait(false);
            var tree = await context.Document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var updatedTree = await FormattingCodeFixHelper.FixOneAsync(tree, this.SyntaxFormatting, options, diagnostic, cancellationToken).ConfigureAwait(false);
            return context.Document.WithText(await updatedTree.GetTextAsync(cancellationToken).ConfigureAwait(false));
        }

        private async Task<SyntaxFormattingOptions> GetOptionsAsync(Document document, CancellationToken cancellationToken)
        {
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var analyzerConfigOptions = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree);
            return this.SyntaxFormatting.GetFormattingOptions(analyzerConfigOptions);
        }

        public sealed override FixAllProvider GetFixAllProvider()
            => FixAllProvider.Create(async (context, document, diagnostics) =>
            {
                var cancellationToken = context.CancellationToken;
                var options = await GetOptionsAsync(document, cancellationToken).ConfigureAwait(false);
                var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var updatedSyntaxRoot = Formatter.Format(syntaxRoot, this.SyntaxFormatting, options, cancellationToken);
                return document.WithSyntaxRoot(updatedSyntaxRoot);
            });
    }
}
