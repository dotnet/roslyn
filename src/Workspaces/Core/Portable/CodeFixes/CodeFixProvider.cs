// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Implement this type to provide fixes for source code problems.
    /// Remember to use <see cref="ExportCodeFixProviderAttribute"/> so the host environment can offer your fixes in a UI.
    /// </summary>
    public abstract class CodeFixProvider
    {
        /// <summary>
        /// A list of diagnostic IDs that this provider can provide fixes for.
        /// </summary>
        public abstract ImmutableArray<string> FixableDiagnosticIds { get; }

        /// <summary>
        /// Computes one or more fixes for the specified <see cref="CodeFixContext"/>.
        /// </summary>
        /// <param name="context">
        /// A <see cref="CodeFixContext"/> containing context information about the diagnostics to fix.
        /// The context must only contain diagnostics with a <see cref="Diagnostic.Id"/> included in the <see cref="FixableDiagnosticIds"/> for the current provider.
        /// </param>
        public abstract Task RegisterCodeFixesAsync(CodeFixContext context);

        /// <summary>
        /// Gets an optional <see cref="FixAllProvider"/> that can fix all/multiple occurrences of diagnostics fixed by this code fix provider.
        /// Return null if the provider doesn't support fix all/multiple occurrences.
        /// Otherwise, you can return any of the well known fix all providers from <see cref="WellKnownFixAllProviders"/> or implement your own fix all provider.
        /// </summary>
        public virtual FixAllProvider GetFixAllProvider()
        {
            return null;
        }
    }
}
