﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;

/// <summary>
/// Service to compute and apply <see cref="FixMultipleCodeAction"/> code fixes.
/// </summary>
[ExportWorkspaceService(typeof(IFixMultipleOccurrencesService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class FixMultipleOccurrencesService() : IFixMultipleOccurrencesService
{
    public Task<Solution> GetFixAsync(
        ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnosticsToFix,
        Workspace workspace,
        CodeFixProvider fixProvider,
        FixAllProvider fixAllProvider,
        string equivalenceKey,
        string waitDialogTitle,
        string waitDialogMessage,
        IProgress<CodeAnalysisProgress> progress,
        CancellationToken cancellationToken)
    {
        var fixMultipleState = FixAllState.Create(
            fixAllProvider, diagnosticsToFix, fixProvider, equivalenceKey);

        return GetFixedSolutionAsync(
            fixMultipleState, workspace, waitDialogTitle, waitDialogMessage, progress, cancellationToken);
    }

    public Task<Solution> GetFixAsync(
        ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsToFix,
        Workspace workspace,
        CodeFixProvider fixProvider,
        FixAllProvider fixAllProvider,
        string equivalenceKey,
        string waitDialogTitle,
        string waitDialogMessage,
        IProgress<CodeAnalysisProgress> progress,
        CancellationToken cancellationToken)
    {
        var fixMultipleState = FixAllState.Create(
            fixAllProvider, diagnosticsToFix, fixProvider, equivalenceKey);

        return GetFixedSolutionAsync(
            fixMultipleState, workspace, waitDialogTitle, waitDialogMessage, progress, cancellationToken);
    }

    private static async Task<Solution> GetFixedSolutionAsync(
        FixAllState fixAllState,
        Workspace workspace,
        string title,
        string waitDialogMessage,
        IProgress<CodeAnalysisProgress> progress,
        CancellationToken cancellationToken)
    {
        var fixMultipleCodeAction = new FixMultipleCodeAction(
            fixAllState, title, waitDialogMessage);

        Solution newSolution = null;
        var extensionManager = workspace.Services.GetService<IExtensionManager>();
        await extensionManager.PerformActionAsync(fixAllState.FixAllProvider, async () =>
        {
            // We don't need to post process changes here as the inner code action created for Fix multiple code fix already executes.
            newSolution = await fixMultipleCodeAction.GetChangedSolutionInternalAsync(
                fixAllState.Solution, progress, postProcessChanges: false, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);

        return newSolution;
    }
}
