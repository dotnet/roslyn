// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

/// <summary>
/// Implement this abstract type to provide refactor all occurrences support for code refactorings.
/// </summary>
/// <remarks>
/// TODO: Make public, tracked with https://github.com/dotnet/roslyn/issues/60703
/// </remarks>
internal abstract class RefactorAllProvider : IFixAllProvider
{
    private protected static ImmutableArray<RefactorAllScope> DefaultSupportedRefactorAllScopes
        = [RefactorAllScope.Document, RefactorAllScope.Project, RefactorAllScope.Solution];

    public virtual IEnumerable<RefactorAllScope> GetSupportedRefactorAllScopes()
        => DefaultSupportedRefactorAllScopes;

    IEnumerable<FixAllScope> IFixAllProvider.GetSupportedFixAllScopes()
        => GetSupportedRefactorAllScopes().Select(s => s.ToFixAllScope());

    public virtual CodeActionCleanup Cleanup => CodeActionCleanup.Default;

    /// <summary>
    /// Gets refactor all occurrences for the given <paramref name="refactorAllContext"/>.
    /// </summary>
    public abstract Task<CodeAction?> GetRefactoringAsync(RefactorAllContext refactorAllContext);

    #region IFixAllProvider implementation
    Task<CodeAction?> IFixAllProvider.GetFixAsync(IFixAllContext fixAllContext)
        => this.GetRefactoringAsync((RefactorAllContext)fixAllContext);
    #endregion

    /// <summary>
    /// Create a <see cref="RefactorAllProvider"/> that refactors documents independently. This can be used in the case
    /// where refactoring(s) registered by this provider only affect a single <see cref="Document"/>.
    /// </summary>
    /// <param name="refactorAllAsync">
    /// Callback that will apply the refactorings present in the provided document.  The document returned will only be
    /// examined for its content (e.g. it's <see cref="SyntaxTree"/> or <see cref="SourceText"/>.  No other aspects
    /// of it (like attributes), or changes to the <see cref="Project"/> or <see cref="Solution"/> it points at
    /// will be considered.
    /// </param>
    public static RefactorAllProvider Create(Func<RefactorAllContext, Document, Optional<ImmutableArray<TextSpan>>, Task<Document?>> refactorAllAsync)
        => Create(refactorAllAsync, DefaultSupportedRefactorAllScopes);

    /// <summary>
    /// Create a <see cref="RefactorAllProvider"/> that refactors documents independently. This can be used in the case
    /// where refactoring(s) registered by this provider only affect a single <see cref="Document"/>.
    /// </summary>
    /// <param name="refactorAllAsync">
    /// Callback that will apply the refactorings present in the provided document.  The document returned will only be
    /// examined for its content (e.g. it's <see cref="SyntaxTree"/> or <see cref="SourceText"/>.  No other aspects
    /// of it (like attributes), or changes to the <see cref="Project"/> or <see cref="Solution"/> it points at
    /// will be considered.
    /// </param>
    /// <param name="supportedRefactorAllScopes">
    /// Supported <see cref="RefactorAllScope"/>s for the fix all provider.
    /// Note that <see cref="RefactorAllScope.Custom"/> is not supported by the <see cref="DocumentBasedFixAllProvider"/>
    /// and should not be part of the supported scopes.
    /// </param>
    public static RefactorAllProvider Create(
        Func<RefactorAllContext, Document, Optional<ImmutableArray<TextSpan>>, Task<Document?>> refactorAllAsync,
        ImmutableArray<RefactorAllScope> supportedRefactorAllScopes)
    {
        return Create(refactorAllAsync, supportedRefactorAllScopes, CodeActionCleanup.Default);
    }

    internal static RefactorAllProvider Create(
        Func<RefactorAllContext, Document, Optional<ImmutableArray<TextSpan>>, Task<Document?>> refactorAllAsync,
        ImmutableArray<RefactorAllScope> supportedFixAllScopes,
        CodeActionCleanup cleanup)
    {
        if (refactorAllAsync is null)
            throw new ArgumentNullException(nameof(refactorAllAsync));

        if (supportedFixAllScopes.IsDefault)
            throw new ArgumentNullException(nameof(supportedFixAllScopes));

        if (supportedFixAllScopes.Contains(RefactorAllScope.Custom))
            throw new ArgumentException(WorkspacesResources.FixAllScope_Custom_is_not_supported_with_this_API, nameof(supportedFixAllScopes));

        return new CallbackDocumentBasedRefactorAllProvider(refactorAllAsync, supportedFixAllScopes, cleanup);
    }

    private sealed class CallbackDocumentBasedRefactorAllProvider(
        Func<RefactorAllContext, Document, Optional<ImmutableArray<TextSpan>>, Task<Document?>> refactorAllAsync,
        ImmutableArray<RefactorAllScope> supportedFixAllScopes,
        CodeActionCleanup cleanup) : DocumentBasedRefactorAllProvider(supportedFixAllScopes)
    {
        public override CodeActionCleanup Cleanup { get; } = cleanup;

        protected override Task<Document?> RefactorAllAsync(RefactorAllContext context, Document document, Optional<ImmutableArray<TextSpan>> refactorAllSpans)
            => refactorAllAsync(context, document, refactorAllSpans);
    }
}
