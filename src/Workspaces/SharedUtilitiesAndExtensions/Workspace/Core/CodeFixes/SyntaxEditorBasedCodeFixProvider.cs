// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal abstract partial class SyntaxEditorBasedCodeFixProvider : CodeFixProvider
    {
        private readonly bool _supportsFixAll;

        protected SyntaxEditorBasedCodeFixProvider(bool supportsFixAll = true)
            => _supportsFixAll = supportsFixAll;

        public sealed override FixAllProvider? GetFixAllProvider()
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
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);

            await editAsync(editor).ConfigureAwait(false);

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        internal abstract CodeFixCategory CodeFixCategory { get; }

        protected abstract Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken);

        /// <summary>
        /// Whether or not this diagnostic should be included when performing a FixAll.  This is
        /// useful for providers that create multiple diagnostics for the same issue (For example,
        /// one main diagnostic and multiple 'faded out code' diagnostics).  FixAll can be invoked
        /// from any of those, but we'll only want perform an edit for only one diagnostic for each
        /// of those sets of diagnostics.
        ///
        /// This overload differs from <see cref="IncludeDiagnosticDuringFixAll(Diagnostic, Document, SemanticModel, string, CancellationToken)"/>
        /// in that it also passes along the <see cref="SemanticModel"/>.
        ///
        /// This overload differs from <see cref="IncludeDiagnosticDuringFixAll(Diagnostic)"/> in
        /// that it also passes along the <see cref="FixAllState"/> in case that would be useful
        /// (for example if the <see cref="FixAllState.CodeActionEquivalenceKey"/> is used.
        ///
        /// Only one of these three overloads needs to be overridden if you want to customize
        /// behavior.
        /// </summary>
        protected virtual bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic, Document document, SemanticModel model, string? equivalenceKey, CancellationToken cancellationToken)
            => IncludeDiagnosticDuringFixAll(diagnostic, document, equivalenceKey, cancellationToken);

        protected virtual bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic, Document document, string? equivalenceKey, CancellationToken cancellationToken)
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
        /// cref="IncludeDiagnosticDuringFixAll(Diagnostic, Document, SemanticModel, string, CancellationToken)"/>
        /// should be overridden instead.
        ///
        /// Only one of these two overloads needs to be overridden if you want to customize
        /// behavior.
        /// </summary>
        protected virtual bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => true;
    }
}
