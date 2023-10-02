// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Implement this abstract type to provide fix all/multiple occurrences code fixes for source code problems.
    /// Alternatively, you can use any of the well known fix all providers from <see cref="WellKnownFixAllProviders"/>.
    /// </summary>
    public abstract class FixAllProvider : IFixAllProvider
    {
        private static readonly Dictionary<Type, bool> s_isNonProgressGetFixAsyncOverridden = new();

        private protected static ImmutableArray<FixAllScope> DefaultSupportedFixAllScopes
            = ImmutableArray.Create(FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution);

        private bool IsNonProgressApiOverridden(Dictionary<Type, bool> dictionary, Func<FixAllProvider, bool> computeResult)
        {
            var type = this.GetType();
            lock (dictionary)
            {
                return dictionary.GetOrAdd(type, computeResult(this));
            }
        }

        private bool IsNonProgressGetFixAsyncOverridden()
        {
#pragma warning disable RS0030 // Do not use banned APIs
            return IsNonProgressApiOverridden(
                s_isNonProgressGetFixAsyncOverridden,
                static codeAction => new Func<FixAllContext, Task<CodeAction?>>(codeAction.GetFixAsync).Method.DeclaringType != typeof(CodeAction));
#pragma warning restore RS0030 // Do not use banned APIs
        }

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

#pragma warning disable CS0618 // Type or member is obsolete
        /// <summary>
        /// Gets fix all occurrences fix for the given fixAllContext.
        /// </summary>
        public virtual Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            => GetFixAsync(fixAllContext, CodeAnalysisProgress.None, fixAllContext.CancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete

#pragma warning disable RS0030 // Do not use banned APIs
        /// <summary>
        /// Gets fix all occurrences fix for the given fixAllContext. Prefer overriding this method over <see
        /// cref="GetFixAsync(FixAllContext)"/> when computation is long running and progress should be shown to the
        /// user.
        /// </summary>
        public virtual async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext, IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
        {
            // If the subclass overrode `ComputeOperationsAsync(CancellationToken)` then we must call into that in
            // order to preserve whatever logic our subclass had had for determining the new solution.
            if (IsNonProgressGetFixAsyncOverridden())
            {
                return await GetFixAsync(fixAllContext).ConfigureAwait(false);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
#pragma warning restore RS0030 // Do not use banned APIs

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
        [Obsolete("Use overload that takes a callback that accepts a cancellation token instead.", error: false)]
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
        [Obsolete("Use overload that takes a callback that accepts a cancellation token instead.", error: false)]
        public static FixAllProvider Create(
            Func<FixAllContext, Document, ImmutableArray<Diagnostic>, Task<Document?>> fixAllAsync,
            ImmutableArray<FixAllScope> supportedFixAllScopes)
        {
            return Create((ctx, doc, dxs, _) => fixAllAsync(ctx, doc, dxs), supportedFixAllScopes);
        }

        /// <inheritdoc cref="Create(Func{FixAllContext, Document, ImmutableArray{Diagnostic}, Task{Document?}}, ImmutableArray{FixAllScope})"/>
        public static FixAllProvider Create(
            Func<FixAllContext, Document, ImmutableArray<Diagnostic>, CancellationToken, Task<Document?>> fixAllAsync,
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
        Task<CodeAction?> IFixAllProvider.GetFixAsync(IFixAllContext fixAllContext, IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
            => this.GetFixAsync((FixAllContext)fixAllContext, progress, cancellationToken);
        #endregion

        private class CallbackDocumentBasedFixAllProvider(
            Func<FixAllContext, Document, ImmutableArray<Diagnostic>, CancellationToken, Task<Document?>> fixAllAsync,
            ImmutableArray<FixAllScope> supportedFixAllScopes) : DocumentBasedFixAllProvider(supportedFixAllScopes)
        {
            protected override Task<Document?> FixAllAsync(FixAllContext context, Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
                => fixAllAsync(context, document, diagnostics, cancellationToken);
        }
    }
}
