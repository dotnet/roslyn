// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    /// <summary>
    /// Implement this abstract type to provide fix all occurrences support for code refactorings.
    /// </summary>
    public abstract class FixAllProvider
    {
        /// <summary>
        /// Gets the supported scopes for applying multiple occurrences of a code refactoring.
        /// By default, it returns the following scopes:
        /// (a) <see cref="FixAllScope.Document"/>
        /// (b) <see cref="FixAllScope.Project"/> and
        /// (c) <see cref="FixAllScope.Solution"/>
        /// </summary>
        public virtual IEnumerable<FixAllScope> GetSupportedFixAllScopes()
            => ImmutableArray.Create(FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution);

        /// <summary>
        /// Gets fix all occurrences fix for the given fixAllContext.
        /// </summary>
        public abstract Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext);

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
        /// <param name="supportsFixAllForSelection">Indicates if <see cref="FixAllScope.Selection"/> is supported or not.</param>
        /// <param name="supportsFixAllForContainingMember">Indicates if <see cref="FixAllScope.ContainingMember"/> is supported or not.</param>
        /// <param name="supportsFixAllForContainingType">Indicates if <see cref="FixAllScope.ContainingType"/> is supported or not.</param>
        public static FixAllProvider Create(
            Func<FixAllContext, Task<Document?>> fixAllAsync,
            bool supportsFixAllForSelection,
            bool supportsFixAllForContainingMember,
            bool supportsFixAllForContainingType)
        {
            if (fixAllAsync == null)
                throw new ArgumentNullException(nameof(fixAllAsync));

            return new CallbackDocumentBasedFixAllProvider(fixAllAsync,
                supportsFixAllForSelection, supportsFixAllForContainingMember, supportsFixAllForContainingType);
        }

        private sealed class CallbackDocumentBasedFixAllProvider : DocumentBasedFixAllProvider
        {
            private readonly Func<FixAllContext, Task<Document?>> _fixAllAsync;

            public CallbackDocumentBasedFixAllProvider(
                Func<FixAllContext, Task<Document?>> fixAllAsync,
                bool supportsFixAllForSelection,
                bool supportsFixAllForContainingMember,
                bool supportsFixAllForContainingType)
            {
                _fixAllAsync = fixAllAsync;
                SupportsFixAllForSelection = supportsFixAllForSelection;
                SupportsFixAllForContainingMember = supportsFixAllForContainingMember;
                SupportsFixAllForContainingType = supportsFixAllForContainingType;
            }

            protected override bool SupportsFixAllForSelection { get; }

            protected override bool SupportsFixAllForContainingMember { get; }

            protected override bool SupportsFixAllForContainingType { get; }

            protected override Task<Document?> FixAllAsync(FixAllContext context)
                => _fixAllAsync(context);
        }
    }
}
