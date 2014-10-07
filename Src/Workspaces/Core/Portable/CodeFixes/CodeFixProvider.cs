// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Implement this type to provide fixes for source code problems.
    /// Remember to use <see cref="ExportCodeFixProviderAttribute"/> so the host environment can offer your fixes in a UI.
    /// </summary>
    public abstract class CodeFixProvider
    {
        /// <summary>
        /// A list of diagnostic IDs that this provider can provider fixes for.
        /// </summary>
        public abstract ImmutableArray<string> GetFixableDiagnosticIds();

        /// <summary>
        /// Gets one or more fixes for the specified code fix context represented as a list of <see cref="CodeAction"/>'s.
        /// </summary>
        /// <returns>A list of zero or more potential <see cref="CodeAction"/>. It is also safe to return null if there are none.</returns>
        public abstract Task<IEnumerable<CodeAction>> GetFixesAsync(CodeFixContext context);

        /// <summary>
        /// Gets an optional <see cref="FixAllProvider"/> that can fix all/multiple occurrences of diagnostics fixed by this code fix provider.
        /// Return null if the provider doesn't support fix all/multiple occurrences.
        /// Otherwise, you can return any of the well known fix all providers from <see cref="WellKnownFixAllProviders"/> or implement your own fix all provider.
        /// </summary>
        /// <returns></returns>
        public abstract FixAllProvider GetFixAllProvider();
    }
}
