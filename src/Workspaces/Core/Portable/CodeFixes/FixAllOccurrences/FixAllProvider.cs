// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Implement this abstract type to provide fix all/multiple occurrences code fixes for source code problems.
    /// Alternatively, you can use any of the well known fix all providers from <see cref="WellKnownFixAllProviders"/>.
    /// </summary>
    public abstract class FixAllProvider
    {
        /// <summary>
        /// Gets the supported scopes for fixing all occurrences of a diagnostic.
        /// By default, it returns the following scopes:
        /// (a) <see cref="FixAllScope.Document"/>
        /// (b) <see cref="FixAllScope.Project"/> and
        /// (c) <see cref="FixAllScope.Solution"/>
        /// </summary>
        public virtual IEnumerable<FixAllScope> GetSupportedFixAllScopes()
            => ImmutableArray.Create(FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution);

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
    }
}
