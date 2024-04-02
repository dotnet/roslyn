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
using Roslyn.Utilities;
using FixAllScope = Microsoft.CodeAnalysis.CodeFixes.FixAllScope;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

/// <summary>
/// Implement this abstract type to provide fix all occurrences support for code refactorings.
/// </summary>
/// <remarks>
/// TODO: Make public, tracked with https://github.com/dotnet/roslyn/issues/60703
/// </remarks>
internal abstract class FixAllProvider : IFixAllProvider
{
    private protected static ImmutableArray<FixAllScope> DefaultSupportedFixAllScopes
        = [FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution];

    /// <summary>
    /// Gets the supported scopes for applying multiple occurrences of a code refactoring.
    /// By default, it returns the following scopes:
    /// (a) <see cref="FixAllScope.Document"/>
    /// (b) <see cref="FixAllScope.Project"/> and
    /// (c) <see cref="FixAllScope.Solution"/>
    /// </summary>
    public virtual IEnumerable<FixAllScope> GetSupportedFixAllScopes()
        => DefaultSupportedFixAllScopes;

    /// <summary>
    /// Gets fix all occurrences fix for the given fixAllContext.
    /// </summary>
    public abstract Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext);

    #region IFixAllProvider implementation
    Task<CodeAction?> IFixAllProvider.GetFixAsync(IFixAllContext fixAllContext)
        => this.GetFixAsync((FixAllContext)fixAllContext);
    #endregion

    /// <summary>
    /// Create a <see cref="FixAllProvider"/> that fixes documents independently.
    /// This can be used in the case where refactoring(s) registered by this provider
    /// only affect a single <see cref="Document"/>.
    /// </summary>
    /// <param name="fixAllAsync">
    /// Callback that will apply the refactorings present in the provided document.  The document returned will only be
    /// examined for its content (e.g. it's <see cref="SyntaxTree"/> or <see cref="SourceText"/>.  No other aspects
    /// of it (like attributes), or changes to the <see cref="Project"/> or <see cref="Solution"/> it points at
    /// will be considered.
    /// </param>
    public static FixAllProvider Create(Func<FixAllContext, Document, Optional<ImmutableArray<TextSpan>>, Task<Document?>> fixAllAsync)
        => Create(fixAllAsync, DefaultSupportedFixAllScopes);

    /// <summary>
    /// Create a <see cref="FixAllProvider"/> that fixes documents independently.
    /// This can be used in the case where refactoring(s) registered by this provider
    /// only affect a single <see cref="Document"/>.
    /// </summary>
    /// <param name="fixAllAsync">
    /// Callback that will apply the refactorings present in the provided document.  The document returned will only be
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
        Func<FixAllContext, Document, Optional<ImmutableArray<TextSpan>>, Task<Document?>> fixAllAsync,
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

    private sealed class CallbackDocumentBasedFixAllProvider(
        Func<FixAllContext, Document, Optional<ImmutableArray<TextSpan>>, Task<Document?>> fixAllAsync,
        ImmutableArray<FixAllScope> supportedFixAllScopes) : DocumentBasedFixAllProvider(supportedFixAllScopes)
    {
        protected override Task<Document?> FixAllAsync(FixAllContext context, Document document, Optional<ImmutableArray<TextSpan>> fixAllSpans)
            => fixAllAsync(context, document, fixAllSpans);
    }
}
