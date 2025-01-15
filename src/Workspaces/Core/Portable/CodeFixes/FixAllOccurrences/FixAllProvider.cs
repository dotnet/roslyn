// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes;

/// <summary>
/// Implement this abstract type to provide fix all/multiple occurrences code fixes for source code problems.
/// Alternatively, you can use any of the well known fix all providers from <see cref="WellKnownFixAllProviders"/>.
/// </summary>
public abstract class FixAllProvider : IFixAllProvider
{
    private protected static ImmutableArray<FixAllScope> DefaultSupportedFixAllScopes
        = [FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution];

    /// <summary>
    /// Gets the supported scopes for fixing all occurrences of a diagnostic.
    /// By default, it returns the following scopes:
    /// (a) <see cref="FixAllScope.Document"/>
    /// (b) <see cref="FixAllScope.Project"/> and
    /// (c) <see cref="FixAllScope.Solution"/>
    /// </summary>
    public virtual IEnumerable<FixAllScope> GetSupportedFixAllScopes()
        => DefaultSupportedFixAllScopes;

    /// <summary>
    /// Gets the diagnostic IDs for which fix all occurrences is supported.
    /// By default, it returns <see cref="CodeFixProvider.FixableDiagnosticIds"/> for the given <paramref name="originalCodeFixProvider"/>.
    /// </summary>
    /// <param name="originalCodeFixProvider">Original code fix provider that returned this fix all provider from <see cref="CodeFixProvider.GetFixAllProvider"/> method.</param>
    public virtual IEnumerable<string> GetSupportedFixAllDiagnosticIds(CodeFixProvider originalCodeFixProvider)
        => originalCodeFixProvider.FixableDiagnosticIds;

    /// <summary>
    /// Gets fix all occurrences fix for the given fixAllContext.
    /// </summary>
    public abstract Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext);

    /// <summary>
    /// Create a <see cref="FixAllProvider"/> that fixes documents independently.  This should be used instead of
    /// <see cref="WellKnownFixAllProviders.BatchFixer"/> in the case where fixes for a <see cref="Diagnostic"/>
    /// only affect the <see cref="Document"/> the diagnostic was produced in.
    /// </summary>
    /// <param name="fixAllAsync">
    /// Callback that will the fix diagnostics present in the provided document.  The document returned will only be
    /// examined for its content (e.g. it's <see cref="SyntaxTree"/> or <see cref="SourceText"/>.  No other aspects
    /// of it (like attributes), or changes to the <see cref="Project"/> or <see cref="Solution"/> it points at
    /// will be considered.
    /// </param>
    public static FixAllProvider Create(Func<FixAllContext, Document, ImmutableArray<Diagnostic>, Task<Document?>> fixAllAsync)
        => Create(fixAllAsync, DefaultSupportedFixAllScopes);

    /// <summary>
    /// Create a <see cref="FixAllProvider"/> that fixes documents independently for the given <paramref name="supportedFixAllScopes"/>.
    /// This should be used instead of <see cref="WellKnownFixAllProviders.BatchFixer"/> in the case where
    /// fixes for a <see cref="Diagnostic"/> only affect the <see cref="Document"/> the diagnostic was produced in.
    /// </summary>
    /// <param name="fixAllAsync">
    /// Callback that will the fix diagnostics present in the provided document.  The document returned will only be
    /// examined for its content (e.g. it's <see cref="SyntaxTree"/> or <see cref="SourceText"/>.  No other aspects
    /// of it (like attributes), or changes to the <see cref="Project"/> or <see cref="Solution"/> it points at
    /// will be considered.
    /// </param>
    /// <param name="supportedFixAllScopes">
    /// Supported <see cref="FixAllScope"/>s for the fix all provider.
    /// Note that <see cref="FixAllScope.Custom"/> is not supported by the <see cref="DocumentBasedFixAllProvider"/>
    /// and should not be part of the supported scopes.
    /// </param>
    public static FixAllProvider Create(
        Func<FixAllContext, Document, ImmutableArray<Diagnostic>, Task<Document?>> fixAllAsync,
        ImmutableArray<FixAllScope> supportedFixAllScopes)
    {
        if (fixAllAsync is null)
            throw new ArgumentNullException(nameof(fixAllAsync));

        if (supportedFixAllScopes.IsDefault)
            throw new ArgumentNullException(nameof(supportedFixAllScopes));

        if (supportedFixAllScopes.Contains(FixAllScope.Custom))
            throw new ArgumentException(WorkspacesResources.FixAllScope_Custom_is_not_supported_with_this_API, nameof(supportedFixAllScopes));

        return new CallbackDocumentBasedFixAllProvider(fixAllAsync, supportedFixAllScopes);
    }

    #region IFixAllProvider implementation
    Task<CodeAction?> IFixAllProvider.GetFixAsync(IFixAllContext fixAllContext)
        => this.GetFixAsync((FixAllContext)fixAllContext);
    #endregion

    private sealed class CallbackDocumentBasedFixAllProvider(
        Func<FixAllContext, Document, ImmutableArray<Diagnostic>, Task<Document?>> fixAllAsync,
        ImmutableArray<FixAllScope> supportedFixAllScopes) : DocumentBasedFixAllProvider(supportedFixAllScopes)
    {
        protected override Task<Document?> FixAllAsync(FixAllContext context, Document document, ImmutableArray<Diagnostic> diagnostics)
            => fixAllAsync(context, document, diagnostics);
    }
}
