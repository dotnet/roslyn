// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports
{
    internal abstract class AbstractRemoveUnnecessaryImportsCodeFixProvider : CodeFixProvider
    {
        protected abstract ISyntaxFormatting GetSyntaxFormatting();

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(AbstractRemoveUnnecessaryImportsDiagnosticAnalyzer.DiagnosticFixableId);

        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var title = GetTitle();
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    c => RemoveUnnecessaryImportsAsync(context.Document, c),
                    title),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected abstract string GetTitle();

#if CODE_STYLE
        private async Task<Document> RemoveUnnecessaryImportsAsync(
#else
        private static async Task<Document> RemoveUnnecessaryImportsAsync(
#endif
            Document document,
            CancellationToken cancellationToken)
        {
#if CODE_STYLE
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var formattingOptions = GetSyntaxFormatting().GetFormattingOptions(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree));
#else
            var formattingOptions = await SyntaxFormattingOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false);
#endif
            var service = document.GetRequiredLanguageService<IRemoveUnnecessaryImportsService>();
            return await service.RemoveUnnecessaryImportsAsync(document, formattingOptions, cancellationToken).ConfigureAwait(false);
        }
    }
}
