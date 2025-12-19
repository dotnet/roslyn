// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
{
    public abstract class PreferIsKindFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(PreferIsKindAnalyzer.Rule.Id);

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        CodeAnalysisDiagnosticsResources.PreferIsKindFix,
                        cancellationToken => ConvertKindToIsKindAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
                        equivalenceKey: nameof(PreferIsKindFix)),
                    diagnostic);
            }
        }

        private async Task<Document> ConvertKindToIsKindAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            if (TryGetNodeToFix(editor.OriginalRoot, sourceSpan) is { } nodeToFix)
            {
                FixDiagnostic(editor, nodeToFix);
            }

            return editor.GetChangedDocument();
        }

        protected abstract SyntaxNode? TryGetNodeToFix(SyntaxNode root, TextSpan span);

        protected abstract void FixDiagnostic(DocumentEditor editor, SyntaxNode nodeToFix);

        private sealed class CustomFixAllProvider : DocumentBasedFixAllProvider
        {
            private readonly PreferIsKindFix _fixer;

            public CustomFixAllProvider(PreferIsKindFix fixer)
            {
                _fixer = fixer;
            }

            protected override string GetFixAllTitle(FixAllContext fixAllContext) => CodeAnalysisDiagnosticsResources.PreferIsKindFix;

            protected override async Task<Document?> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
            {
                var editor = await DocumentEditor.CreateAsync(document, fixAllContext.CancellationToken).ConfigureAwait(false);
                foreach (var diagnostic in diagnostics)
                {
                    var nodeToFix = _fixer.TryGetNodeToFix(editor.OriginalRoot, diagnostic.Location.SourceSpan);
                    if (nodeToFix is null)
                        continue;

                    _fixer.FixDiagnostic(editor, nodeToFix);
                }

                return editor.GetChangedDocument();
            }
        }
    }
}
