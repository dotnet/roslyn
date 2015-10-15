// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal interface ISuppressionFixProvider
    {
        /// <summary>
        /// Returns true if the given diagnostic can be suppressed or unsuppressed.
        /// </summary>
        bool CanBeSuppressedOrUnsuppressed(Diagnostic diagnostic);

        /// <summary>
        /// Gets one or more add suppression or remove suppression fixes for the specified diagnostics represented as a list of <see cref="CodeAction"/>'s.
        /// </summary>
        /// <returns>A list of zero or more potential <see cref="CodeFix"/>'es. It is also safe to return null if there are none.</returns>
        Task<IEnumerable<CodeFix>> GetSuppressionsAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken);

        /// <summary>
        /// Gets one or more add suppression or remove suppression fixes for the specified no-location diagnostics represented as a list of <see cref="CodeAction"/>'s.
        /// </summary>
        /// <returns>A list of zero or more potential <see cref="CodeFix"/>'es. It is also safe to return null if there are none.</returns>
        Task<IEnumerable<CodeFix>> GetSuppressionsAsync(Project project, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken);

        /// <summary>
        /// Gets an optional <see cref="FixAllProvider"/> that can fix all/multiple occurrences of diagnostics fixed by this fix provider.
        /// Return null if the provider doesn't support fix all/multiple occurrences.
        /// Otherwise, you can return any of the well known fix all providers from <see cref="WellKnownFixAllProviders"/> or implement your own fix all provider.
        /// </summary>
        FixAllProvider GetFixAllProvider();
    }
}
