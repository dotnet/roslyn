// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports
{
    internal abstract class AbstractRemoveUnnecessaryImportsCodeFixProvider : CodeFixProvider
    {
        protected abstract ISyntaxFormatting GetSyntaxFormatting();

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(RemoveUnnecessaryImportsConstants.DiagnosticFixableId);

        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var title = GetTitle();
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    c => RemoveUnnecessaryImportsAsync(context.Document, context.GetOptionsProvider(), c),
                    title),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected abstract string GetTitle();

        private async Task<Document> RemoveUnnecessaryImportsAsync(
            Document document,
            CodeActionOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            var service = document.GetRequiredLanguageService<IRemoveUnnecessaryImportsService>();

            var options = await document.GetCodeFixOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
            var formattingOptions = options.GetFormattingOptions(GetSyntaxFormatting());
            return await service.RemoveUnnecessaryImportsAsync(document, formattingOptions, cancellationToken).ConfigureAwait(false);
        }
    }
}
