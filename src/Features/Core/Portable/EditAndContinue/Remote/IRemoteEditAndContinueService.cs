// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal interface IRemoteEditAndContinueService
    {
        internal interface ICallback
        {
            ValueTask<ImmutableArray<ActiveStatementDebugInfo>> GetActiveStatementsAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
            ValueTask<(int errorCode, string? errorMessage)?> GetEncAvailabilityAsync(RemoteServiceCallbackId callbackId, Guid mvid, CancellationToken cancellationToken);
            ValueTask PrepareModuleForUpdateAsync(RemoteServiceCallbackId callbackId, Guid mvid, CancellationToken cancellationToken);

            ValueTask<ImmutableArray<TextSpan>> GetSpansAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
            ValueTask<ImmutableArray<TextSpan>> GetSpansAsync(RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken);
        }

        ValueTask<ImmutableArray<DiagnosticData>> GetDocumentDiagnosticsAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken);
        ValueTask<bool> HasChangesAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, string? sourceFilePath, CancellationToken cancellationToken);

        ValueTask<(SolutionUpdateStatus Summary, ImmutableArray<Deltas> Deltas, ImmutableArray<DiagnosticData> Diagnostics)> EmitSolutionUpdateAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);

        ValueTask CommitSolutionUpdateAsync(CancellationToken cancellationToken);
        ValueTask DiscardSolutionUpdateAsync(CancellationToken cancellationToken);

        ValueTask StartDebuggingSessionAsync(PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken);
        ValueTask<ImmutableArray<DocumentId>> StartEditSessionAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
        ValueTask<ImmutableArray<DocumentId>> EndEditSessionAsync(CancellationToken cancellationToken);
        ValueTask<ImmutableArray<DocumentId>> EndDebuggingSessionAsync(CancellationToken cancellationToken);

        ValueTask<ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>> GetBaseActiveStatementSpansAsync(ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken);
        ValueTask<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>> GetAdjustedActiveStatementSpansAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken);

        ValueTask<bool?> IsActiveStatementInExceptionRegionAsync(ActiveInstructionId instructionId, CancellationToken cancellationToken);
        ValueTask<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, ActiveInstructionId instructionId, CancellationToken cancellationToken);
        ValueTask OnSourceFileUpdatedAsync(DocumentId documentId, CancellationToken cancellationToken);
    }
}
