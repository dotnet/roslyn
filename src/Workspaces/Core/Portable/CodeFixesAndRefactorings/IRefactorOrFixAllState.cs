// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

/// <summary>
/// Represents internal <see cref="FixAllState"/> or <see cref="RefactorAllState"/>.
/// </summary>
internal interface IRefactorOrFixAllState
{
    int CorrelationId { get; }
    IRefactorOrFixAllProvider FixAllProvider { get; }
    string? CodeActionEquivalenceKey { get; }
    FixAllScope Scope { get; }
    FixAllKind FixAllKind { get; }
    Document? Document { get; }
    Project Project { get; }
    Solution Solution { get; }

    /// <summary>
    /// Underlying code fix provider or code refactoring provider for the fix all occurrences fix.
    /// </summary>
    IRefactorOrFixProvider Provider { get; }

    IRefactorOrFixAllState With(
        Optional<(Document? document, Project project)> documentAndProject = default,
        Optional<FixAllScope> scope = default,
        Optional<string?> codeActionEquivalenceKey = default);
}
