// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.FixFormatting)]
    [Shared]
    internal class FormattingCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.FormattingDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixOneAsync(context.Document, context.Diagnostics, c)),
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
                if (!diagnostic.Properties.TryGetValue(FormattingDiagnosticAnalyzer.ReplaceTextKey, out var replacement))
                {
                    continue;
                }

                changes.Add(new TextChange(diagnostic.Location.SourceSpan, replacement));
            }

            changes.Sort((left, right) => left.Span.Start.CompareTo(right.Span.Start));

            return document.WithText(text.WithChanges(changes));
        }

        protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var updatedDocument = await Formatter.FormatAsync(document, options, cancellationToken).ConfigureAwait(false);
            editor.ReplaceNode(editor.OriginalRoot, await updatedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false));
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Formatting_analyzer_code_fix, createChangedDocument, FeaturesResources.Formatting_analyzer_code_fix)
            {
            }
        }
    }
}
