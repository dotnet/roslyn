// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Implement this interface to provide fixes for source code problems.
    /// Remember to use <see cref="ExportCodeFixProviderAttribute"/> so the host environment can offer your fixes in a UI.
    /// </summary>
    public interface ICodeFixProvider
    {
        /// <summary>
        /// A list of diagnostic ID's that this provider can provider fixes for.
        /// </summary>
        IEnumerable<string> GetFixableDiagnosticIds();

        /// <summary>
        /// Gets one or more fixes for the specified diagnostics represented as a list of <see cref="CodeAction"/>'s.
        /// </summary>
        /// <returns>A list of zero or more potential <see cref="CodeAction"/>'s. It is also safe to return null if there are none.</returns>
        Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken);

        /// <summary>
        /// Gets an optional <see cref="FixAllProvider"/> that can fix all/multiple occurrences of diagnostics fixed by this code fix provider.
        /// Return null if the provider doesn't support fix all/multiple occurrences.
        /// Otherwise, you can return any of the well known fix all providers from <see cref="WellKnownFixAllProviders"/> or implement your own fix all provider.
        /// </summary>
        /// <returns></returns>
        FixAllProvider GetFixAllProvider();
    }
}
