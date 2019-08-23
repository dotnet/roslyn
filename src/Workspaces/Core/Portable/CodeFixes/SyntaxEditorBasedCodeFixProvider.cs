// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal abstract partial class SyntaxEditorBasedCodeFixProvider : CodeFixProvider
    {
        private readonly bool _supportsFixAll;

        protected SyntaxEditorBasedCodeFixProvider(bool supportsFixAll = true)
        {
            _supportsFixAll = supportsFixAll;
        }

        public sealed override FixAllProvider GetFixAllProvider()
            => _supportsFixAll ? new SyntaxEditorBasedFixAllProvider(this) : null;

        protected Task<Document> FixAsync(
            Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            return FixAllAsync(document, ImmutableArray.Create(diagnostic), cancellationToken);
        }

        private Task<Document> FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            return FixAllWithEditorAsync(document,
                editor => FixAllAsync(document, diagnostics, editor, cancellationToken),
                cancellationToken);
        }

        internal static async Task<Document> FixAllWithEditorAsync(
            Document document,
            Func<SyntaxEditor, Task> editAsync,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);

            await editAsync(editor).ConfigureAwait(false);

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        protected abstract Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken);

#if !CODE_STYLE
        internal abstract CodeFixCategory CodeFixCategory { get; }

        /// <summary>
        /// Whether or not this diagnostic should be included when performing a FixAll.  This is
        /// useful for providers that create multiple diagnostics for the same issue (For example,
        /// one main diagnostic and multiple 'faded out code' diagnostics).  FixAll can be invoked
        /// from any of those, but we'll only want perform an edit for only one diagnostic for each
        /// of those sets of diagnostics.
        ///
        /// This overload differs from <see cref="IncludeDiagnosticDuringFixAll(Diagnostic)"/> in
        /// that it also passes along the <see cref="FixAllState"/> in case that would be useful
        /// (for example if the <see cref="FixAllState.CodeActionEquivalenceKey"/> is used.  
        ///
        /// Only one of these two overloads needs to be overridden if you want to customize
        /// behavior.
        /// </summary>
        protected virtual bool IncludeDiagnosticDuringFixAll(FixAllState fixAllState, Diagnostic diagnostic, CancellationToken cancellationToken)
            => IncludeDiagnosticDuringFixAll(diagnostic);

        /// <summary>
        /// Whether or not this diagnostic should be included when performing a FixAll.  This is
        /// useful for providers that create multiple diagnostics for the same issue (For example,
        /// one main diagnostic and multiple 'faded out code' diagnostics).  FixAll can be invoked
        /// from any of those, but we'll only want perform an edit for only one diagnostic for each
        /// of those sets of diagnostics.
        ///
        /// By default, all diagnostics will be included in fix-all unless they are filtered out
        /// here. If only the diagnostic needs to be queried to make this determination, only this
        /// overload needs to be overridden.  However, if information from <see cref="FixAllState"/>
        /// is needed (for example <see cref="FixAllState.CodeActionEquivalenceKey"/>), then <see
        /// cref="IncludeDiagnosticDuringFixAll(FixAllState, Diagnostic, CancellationToken)"/>
        /// should be overridden instead.
        ///
        /// Only one of these two overloads needs to be overridden if you want to customize
        /// behavior.
        /// </summary>
        protected virtual bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => true;
#endif
    }
}
