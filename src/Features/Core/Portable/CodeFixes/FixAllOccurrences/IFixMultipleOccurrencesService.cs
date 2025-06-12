// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CodeFixes;

internal interface IFixMultipleOccurrencesService : IWorkspaceService
{
    /// <summary>
    /// Get the fix multiple occurrences code fix for the given diagnostics with source locations.
    /// NOTE: This method does not apply the fix to the workspace.
    /// </summary>
    Task<Solution> GetFixAsync(
        ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnosticsToFix,
        Workspace workspace,
        CodeFixProvider fixProvider,
        FixAllProvider fixAllProvider,
        string equivalenceKey,
        string waitDialogTitle,
        string waitDialogMessage,
        IProgress<CodeAnalysisProgress> progressTracker,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get the fix multiple occurrences code fix for the given diagnostics with source locations.
    /// NOTE: This method does not apply the fix to the workspace.
    /// </summary>
    Task<Solution> GetFixAsync(
        ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsToFix,
        Workspace workspace,
        CodeFixProvider fixProvider,
        FixAllProvider fixAllProvider,
        string equivalenceKey,
        string waitDialogTitle,
        string waitDialogMessage,
        IProgress<CodeAnalysisProgress> progressTracker,
        CancellationToken cancellationToken);
}
