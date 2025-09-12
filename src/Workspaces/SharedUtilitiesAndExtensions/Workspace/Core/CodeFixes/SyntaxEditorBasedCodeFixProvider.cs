// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes;

internal abstract partial class SyntaxEditorBasedCodeFixProvider(bool supportsFixAll = true) : CodeFixProvider
{
    private static readonly ImmutableArray<FixAllScope> s_defaultSupportedFixAllScopes =
        [FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution, FixAllScope.ContainingMember, FixAllScope.ContainingType];

    private readonly bool _supportsFixAll = supportsFixAll;

#if WORKSPACE
    protected virtual CodeActionCleanup Cleanup => CodeActionCleanup.Default;
#endif

    public sealed override FixAllProvider? GetFixAllProvider()
    {
        if (!_supportsFixAll)
            return null;

        return FixAllProvider.Create(
            async (fixAllContext, document, diagnostics) =>
            {
                // Ensure that diagnostics for this document are always in document location order.  This provides a
                // consistent and deterministic order for fixers that want to update a document.
                //
                // Also ensure that we do not pass in duplicates by invoking Distinct.  See
                // https://github.com/dotnet/roslyn/issues/31381, that seems to be causing duplicate diagnostics.
                var filteredDiagnostics = diagnostics.Distinct()
                                                     .WhereAsArray(d => this.IncludeDiagnosticDuringFixAll(d, document, fixAllContext.CodeActionEquivalenceKey, fixAllContext.CancellationToken))
                                                     .Sort((d1, d2) => d1.Location.SourceSpan.Start - d2.Location.SourceSpan.Start);

                if (filteredDiagnostics.Length == 0)
                    return document;

                return await FixAllAsync(document, filteredDiagnostics, fixAllContext.CancellationToken).ConfigureAwait(false);
            },
            s_defaultSupportedFixAllScopes
#if WORKSPACE
            , this.Cleanup
#endif
            );
    }

    protected void RegisterCodeFix(CodeFixContext context, string title, string equivalenceKey, Diagnostic? diagnostic = null)
        => context.RegisterCodeFix(CodeAction.Create(title, GetDocumentUpdater(context, diagnostic), equivalenceKey), context.Diagnostics);

    protected void RegisterCodeFix(CodeFixContext context, string title, string equivalenceKey, CodeActionPriority priority, Diagnostic? diagnostic = null)
        => context.RegisterCodeFix(CodeAction.Create(title, GetDocumentUpdater(context, diagnostic), equivalenceKey, priority), context.Diagnostics);

    protected Func<CancellationToken, Task<Document>> GetDocumentUpdater(CodeFixContext context, Diagnostic? diagnostic = null)
    {
        var diagnostics = ImmutableArray.Create(diagnostic ?? context.Diagnostics[0]);
        return cancellationToken => FixAllAsync(context.Document, diagnostics, cancellationToken);
    }

    private Task<Document> FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        return FixAllWithEditorAsync(
            document,
            editor => FixAllAsync(document, diagnostics, editor, cancellationToken),
            cancellationToken);
    }

    internal static async Task<Document> FixAllWithEditorAsync(
        Document document,
        Func<SyntaxEditor, Task> editAsync,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var editor = new SyntaxEditor(root, document.Project.Solution.Services);

        await editAsync(editor).ConfigureAwait(false);

        var newRoot = editor.GetChangedRoot();
        return document.WithSyntaxRoot(newRoot);
    }

    /// <summary>
    /// Fixes all <paramref name="diagnostics"/> in the specified <paramref name="editor"/>.
    /// The fixes are applied to the <paramref name="document"/>'s syntax tree via <paramref name="editor"/>.
    /// The implementation may query options of any document in the <paramref name="document"/>'s solution.
    /// </summary>
    protected abstract Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken);

    /// <summary>
    /// Whether or not this diagnostic should be included when performing a FixAll.  This is useful for providers that
    /// create multiple diagnostics for the same issue (For example, one main diagnostic and multiple 'faded out code'
    /// diagnostics).  FixAll can be invoked from any of those, but we'll only want perform an edit for only one
    /// diagnostic for each of those sets of diagnostics.
    /// <para/> This overload differs from <see cref="IncludeDiagnosticDuringFixAll(Diagnostic)"/> in that it also
    /// passes along the <see cref="FixAllState"/> in case that would be useful (for example if the
    /// CodeActionEquivalenceKey is used).
    /// <para/>
    /// Only one of these two overloads needs to be overridden if you want to customize behavior.
    /// </summary>
    protected virtual bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic, Document document, string? equivalenceKey, CancellationToken cancellationToken)
        => IncludeDiagnosticDuringFixAll(diagnostic);

    /// <summary>
    /// Whether or not this diagnostic should be included when performing a FixAll.  This is useful for providers that
    /// create multiple diagnostics for the same issue (For example, one main diagnostic and multiple 'faded out code'
    /// diagnostics).  FixAll can be invoked from any of those, but we'll only want perform an edit for only one
    /// diagnostic for each of those sets of diagnostics.
    /// <para/> By default, all diagnostics will be included in fix-all unless they are filtered out here. If only the
    /// diagnostic needs to be queried to make this determination, only this overload needs to be overridden.  However,
    /// if information from <see cref="FixAllState"/> is needed (for example the CodeActionEquivalenceKey), then <see
    /// cref="IncludeDiagnosticDuringFixAll(Diagnostic, Document, string, CancellationToken)"/> should be overridden
    /// instead.
    /// <para/>
    /// Only one of these two overloads needs to be overridden if you want to customize behavior.
    /// </summary>
    protected virtual bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
        => true;
}
