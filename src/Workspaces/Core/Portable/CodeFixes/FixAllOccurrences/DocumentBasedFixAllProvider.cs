// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes;

/// <summary>
/// Provides a base class to write a <see cref="FixAllProvider"/> that fixes documents independently. This type
/// should be used instead of <see cref="WellKnownFixAllProviders.BatchFixer"/> in the case where fixes for a <see
/// cref="Diagnostic"/> only affect the <see cref="Document"/> the diagnostic was produced in.
/// </summary>
/// <remarks>
/// This type provides suitable logic for fixing large solutions in an efficient manner.  Projects are serially
/// processed, with all the documents in the project being processed in parallel.  Diagnostics are computed for the
/// project and then appropriately bucketed by document.  These are then passed to <see
/// cref="FixAllAsync(FixAllContext, Document, ImmutableArray{Diagnostic})"/> for implementors to process.
/// </remarks>
public abstract class DocumentBasedFixAllProvider(ImmutableArray<FixAllScope> supportedFixAllScopes) : FixAllProvider
{
    private readonly ImmutableArray<FixAllScope> _supportedFixAllScopes = supportedFixAllScopes;

    protected DocumentBasedFixAllProvider()
        : this(DefaultSupportedFixAllScopes)
    {
    }

    /// <summary>
    /// Produce a suitable title for the fix-all <see cref="CodeAction"/> this type creates in <see
    /// cref="GetFixAsync(FixAllContext)"/>.  Override this if customizing that title is desired.
    /// </summary>
    protected virtual string GetFixAllTitle(FixAllContext fixAllContext)
        => fixAllContext.GetDefaultFixAllTitle();

    /// <summary>
    /// Fix all the <paramref name="diagnostics"/> present in <paramref name="document"/>.  The document returned
    /// will only be examined for its content (e.g. it's <see cref="SyntaxTree"/> or <see cref="SourceText"/>.  No
    /// other aspects of (like it's properties), or changes to the <see cref="Project"/> or <see cref="Solution"/>
    /// it points at will be considered.
    /// </summary>
    /// <param name="fixAllContext">The context for the Fix All operation.</param>
    /// <param name="document">The document to fix.</param>
    /// <param name="diagnostics">The diagnostics to fix in the document.</param>
    /// <returns>
    /// <para>The new <see cref="Document"/> representing the content fixed document.</para>
    /// <para>-or-</para>
    /// <para><see langword="null"/>, if no changes were made to the document.</para>
    /// </returns>
    protected abstract Task<Document?> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics);

    public sealed override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
        => _supportedFixAllScopes;

    public sealed override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        => DefaultFixAllProviderHelpers.GetFixAsync(
            fixAllContext.GetDefaultFixAllTitle(), fixAllContext, FixAllContextsHelperAsync);

    private Task<Solution?> FixAllContextsHelperAsync(FixAllContext originalFixAllContext, ImmutableArray<FixAllContext> fixAllContexts)
        => DocumentBasedFixAllProviderHelpers.FixAllContextsAsync(
            originalFixAllContext,
            fixAllContexts,
            originalFixAllContext.Progress,
            this.GetFixAllTitle(originalFixAllContext),
            DetermineDiagnosticsAndGetFixedDocumentsAsync);

    private async Task DetermineDiagnosticsAndGetFixedDocumentsAsync(
        FixAllContext fixAllContext, Func<Document, Document?, ValueTask> onDocumentFixed)
    {
        var cancellationToken = fixAllContext.CancellationToken;

        // First, determine the diagnostics to fix.
        var documentToDiagnostics = await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);

        // Second, get the fixes for each document+diagnostics pair in parallel, and apply them to determine the new
        // root/text for each doc.
        await RoslynParallel.ForEachAsync(
            source: documentToDiagnostics,
            cancellationToken,
            async (kvp, cancellationToken) =>
            {
                var (document, documentDiagnostics) = kvp;
                if (documentDiagnostics.IsDefaultOrEmpty)
                    return;

                var newDocument = await this.FixAllAsync(fixAllContext, document, documentDiagnostics).ConfigureAwait(false);
                await onDocumentFixed(document, newDocument).ConfigureAwait(false);
            }).ConfigureAwait(false);
    }
}
