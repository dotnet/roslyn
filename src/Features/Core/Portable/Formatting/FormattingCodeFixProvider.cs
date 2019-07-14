// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.Formatting
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.FixFormatting)]
    [Shared]
    internal class FormattingCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        public FormattingCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.FormattingDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    new MyCodeAction(c => FixOneAsync(context, diagnostic, c)),
                    diagnostic);
            }

            return Task.CompletedTask;
        }

        private static async Task<Document> FixOneAsync(CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var options = await context.Document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var tree = await context.Document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var formattedTree = await FormattingCodeFixHelper.FixOneAsync(tree, context.Document.Project.Solution.Workspace, options, diagnostic, cancellationToken).ConfigureAwait(false);
            return context.Document.WithSyntaxRoot(await formattedTree.GetRootAsync(cancellationToken).ConfigureAwait(false));
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
                : base(FeaturesResources.Fix_formatting, createChangedDocument, FeaturesResources.Fix_formatting)
            {
            }
        }
    }
}
