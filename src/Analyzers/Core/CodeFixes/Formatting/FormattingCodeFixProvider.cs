﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;

using Formatter = Microsoft.CodeAnalysis.Formatting.FormatterHelper;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract class AbstractFormattingCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.FormattingDiagnosticId);

        protected abstract ISyntaxFormatting SyntaxFormatting { get; }

#if !CODE_STYLE

        /// <summary>
        /// Fixing formatting is high priority.  It's something the user wants to be able to fix quickly, is driven by
        /// them acting on an error reported in code, and can be computed fast as it only uses syntax not semantics.
        /// It's also the 8th most common fix that people use, and is picked almost all the times it is shown.
        /// </summary>
        private protected override CodeActionRequestPriority ComputeRequestPriority()
            => CodeActionRequestPriority.High;

#endif

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
#if CODE_STYLE
                context.RegisterCodeFix(
                    CodeAction.Create(
                        AnalyzersResources.Fix_formatting,
                        c => FixOneAsync(context, diagnostic, c),
                        nameof(AbstractFormattingCodeFixProvider)),
                    diagnostic);
#else
                context.RegisterCodeFix(
                    CodeAction.DocumentChangeAction.Create(
                        AnalyzersResources.Fix_formatting,
                        c => FixOneAsync(context, diagnostic, c),
                        nameof(AbstractFormattingCodeFixProvider),
                        CodeActionPriority.High),
                    diagnostic);
#endif
            }

            return Task.CompletedTask;
        }

        private async Task<Document> FixOneAsync(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var options = await context.Document.GetCodeFixOptionsAsync(context.GetOptionsProvider(), cancellationToken).ConfigureAwait(false);
            var formattingOptions = options.GetFormattingOptions(SyntaxFormatting);
            var tree = await context.Document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var updatedTree = await FormattingCodeFixHelper.FixOneAsync(tree, SyntaxFormatting, formattingOptions, diagnostic, cancellationToken).ConfigureAwait(false);
            return context.Document.WithText(await updatedTree.GetTextAsync(cancellationToken).ConfigureAwait(false));
        }

        protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var options = await document.GetCodeFixOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
            var formattingOptions = options.GetFormattingOptions(SyntaxFormatting);
            var updatedRoot = Formatter.Format(editor.OriginalRoot, SyntaxFormatting, formattingOptions, cancellationToken);
            editor.ReplaceNode(editor.OriginalRoot, updatedRoot);
        }
    }
}
