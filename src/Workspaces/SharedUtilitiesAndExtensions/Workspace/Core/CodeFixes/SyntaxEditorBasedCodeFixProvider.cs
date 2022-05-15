// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal abstract partial class SyntaxEditorBasedCodeFixProvider : CodeFixProvider
    {
        private static readonly ImmutableArray<FixAllScope> s_defaultSupportedFixAllScopes =
            ImmutableArray.Create(FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution,
                FixAllScope.ContainingMember, FixAllScope.ContainingType);

        private readonly bool _supportsFixAll;

        protected SyntaxEditorBasedCodeFixProvider(bool supportsFixAll = true)
            => _supportsFixAll = supportsFixAll;

        public sealed override FixAllProvider? GetFixAllProvider()
        {
            if (!_supportsFixAll)
                return null;

            return FixAllProvider.Create(
                async (fixAllContext, document, diagnostics) =>
                {
                    var model = await document.GetRequiredSemanticModelAsync(fixAllContext.CancellationToken).ConfigureAwait(false);

                    // Ensure that diagnostics for this document are always in document location order.  This provides a
                    // consistent and deterministic order for fixers that want to update a document.
                    //
                    // Also ensure that we do not pass in duplicates by invoking Distinct.  See
                    // https://github.com/dotnet/roslyn/issues/31381, that seems to be causing duplicate diagnostics.
                    var filteredDiagnostics = diagnostics.Distinct()
                                                         .WhereAsArray(d => this.IncludeDiagnosticDuringFixAll(d, document, model, fixAllContext.CodeActionEquivalenceKey, fixAllContext.CancellationToken))
                                                         .Sort((d1, d2) => d1.Location.SourceSpan.Start - d2.Location.SourceSpan.Start);

                    if (filteredDiagnostics.Length == 0)
                        return document;

                    return await FixAllAsync(document, filteredDiagnostics, fixAllContext.GetOptionsProvider(), fixAllContext.CancellationToken).ConfigureAwait(false);
                },
                s_defaultSupportedFixAllScopes);
        }

        protected void RegisterCodeFix(CodeFixContext context, string title, string equivalenceKey, Diagnostic? diagnostic = null)
            => context.RegisterCodeFix(CodeAction.Create(title, GetDocumentUpdater(context, diagnostic), equivalenceKey), context.Diagnostics);

        protected void RegisterCodeFix(CodeFixContext context, string title, string equivalenceKey, CodeActionPriority priority, Diagnostic? diagnostic = null)
            => context.RegisterCodeFix(new CustomCodeActions.DocumentChangeAction(title, GetDocumentUpdater(context, diagnostic), equivalenceKey, priority), context.Diagnostics);

        protected Func<CancellationToken, Task<Document>> GetDocumentUpdater(CodeFixContext context, Diagnostic? diagnostic = null)
        {
            var diagnostics = ImmutableArray.Create(diagnostic ?? context.Diagnostics[0]);
            return cancellationToken => FixAllAsync(context.Document, diagnostics, context.GetOptionsProvider(), cancellationToken);
        }

        private Task<Document> FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, CodeActionOptionsProvider options, CancellationToken cancellationToken)
        {
            return FixAllWithEditorAsync(
                document,
                editor => FixAllAsync(document, diagnostics, editor, options, cancellationToken),
                cancellationToken);
        }

        internal static async Task<Document> FixAllWithEditorAsync(
            Document document,
            Func<SyntaxEditor, Task> editAsync,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace.Services);

            await editAsync(editor).ConfigureAwait(false);

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        protected abstract Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken);

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
        /// (for example if the <see cref="IFixAllState.CodeActionEquivalenceKey"/> is used.
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
        /// is needed (for example <see cref="IFixAllState.CodeActionEquivalenceKey"/>), then <see
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
