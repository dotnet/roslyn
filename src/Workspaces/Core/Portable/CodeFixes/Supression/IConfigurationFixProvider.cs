// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes;

/// <summary>
/// Provides suppression or configuration code fixes.
/// </summary>
internal interface IConfigurationFixProvider
{
    /// <summary>
    /// Returns true if the given diagnostic can be configured, suppressed or unsuppressed by this provider.
    /// </summary>
    bool IsFixableDiagnostic(Diagnostic diagnostic);

    /// <summary>
    /// Gets one or more add suppression, remove suppression, or configuration fixes for the specified diagnostics represented as a list of <see cref="CodeAction"/>'s.
    /// </summary>
    /// <returns>A list of zero or more potential <see cref="CodeFix"/>'es. It is also safe to return null if there are none.</returns>
    Task<ImmutableArray<CodeFix>> GetFixesAsync(TextDocument document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken);

    /// <summary>
    /// Gets one or more add suppression, remove suppression, or configuration fixes for the specified no-location diagnostics represented as a list of <see cref="CodeAction"/>'s.
    /// </summary>
    /// <returns>A list of zero or more potential <see cref="CodeFix"/>'es. It is also safe to return null if there are none.</returns>
    Task<ImmutableArray<CodeFix>> GetFixesAsync(Project project, IEnumerable<Diagnostic> diagnostics, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken);

    /// <summary>
    /// Gets an optional <see cref="FixAllProvider"/> that can fix all/multiple occurrences of diagnostics fixed by this fix provider.
    /// Return null if the provider doesn't support fix all/multiple occurrences.
    /// Otherwise, you can return any of the well known fix all providers from <see cref="WellKnownFixAllProviders"/> or implement your own fix all provider.
    /// </summary>
    FixAllProvider? GetFixAllProvider();
}
