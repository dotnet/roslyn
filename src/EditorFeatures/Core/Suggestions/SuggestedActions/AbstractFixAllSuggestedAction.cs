// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;

/// <summary>
/// Suggested action for fix all occurrences for a code fix or a code refactoring.
/// </summary>
internal abstract class AbstractFixAllSuggestedAction(
    IThreadingContext threadingContext,
    SuggestedActionsSourceProvider sourceProvider,
    Solution originalSolution,
    ITextBuffer subjectBuffer,
    IRefactorOrFixAllState fixAllState,
    CodeAction originalCodeAction,
    AbstractFixAllCodeAction fixAllCodeAction)
    : SuggestedAction(threadingContext,
        sourceProvider,
        originalSolution,
        subjectBuffer,
        fixAllState.FixAllProvider,
        fixAllCodeAction)
{
    public CodeAction OriginalCodeAction { get; } = originalCodeAction;

    public IRefactorOrFixAllState FixAllState { get; } = fixAllState;

    public override bool TryGetTelemetryId(out Guid telemetryId)
    {
        // We get the telemetry id for the original code action we are fixing,
        // not the special 'FixAllCodeAction'.  that is the .CodeAction this
        // SuggestedAction is pointing at.
        telemetryId = OriginalCodeAction.GetTelemetryId(FixAllState.Scope);
        return true;
    }

    protected override async Task InnerInvokeAsync(
        IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
    {
        await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var fixAllKind = FixAllState.FixAllKind;
        var functionId = fixAllKind switch
        {
            FixAllKind.CodeFix => FunctionId.CodeFixes_FixAllOccurrencesSession,
            FixAllKind.Refactoring => FunctionId.Refactoring_FixAllOccurrencesSession,
            _ => throw ExceptionUtilities.UnexpectedValue(fixAllKind)
        };

        using (Logger.LogBlock(functionId, FixAllLogger.CreateCorrelationLogMessage(FixAllState.CorrelationId), cancellationToken))
        {
            await base.InnerInvokeAsync(progress, cancellationToken).ConfigureAwait(false);
        }
    }
}
