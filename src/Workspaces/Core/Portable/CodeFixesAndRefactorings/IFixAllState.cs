// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using FixAllScope = Microsoft.CodeAnalysis.CodeFixes.FixAllScope;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings
{
    /// <summary>
    /// Represents internal FixAllState for code fixes or refactorings. 
    /// </summary>
    internal interface IFixAllState
    {
        int CorrelationId { get; }
        IFixAllProvider FixAllProvider { get; }
        string? CodeActionEquivalenceKey { get; }
        FixAllScope Scope { get; }
        FixAllKind FixAllKind { get; }
        Document? Document { get; }
        Project Project { get; }
        Solution Solution { get; }

        /// <summary>
        /// Underlying code fix provider or code refactoring provider for the fix all occurrences fix.
        /// </summary>
        object Provider { get; }

        CodeActionOptionsProvider CodeActionOptionsProvider { get; }

        IFixAllState With(
            Optional<(Document? document, Project project)> documentAndProject = default,
            Optional<FixAllScope> scope = default,
            Optional<string?> codeActionEquivalenceKey = default);
    }
}
