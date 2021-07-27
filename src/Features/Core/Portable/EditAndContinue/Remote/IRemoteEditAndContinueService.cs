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
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal interface IRemoteEditAndContinueService
    {
        internal interface ICallback
        {
            ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
            ValueTask<ManagedEditAndContinueAvailability> GetAvailabilityAsync(RemoteServiceCallbackId callbackId, Guid mvid, CancellationToken cancellationToken);
            ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
            ValueTask PrepareModuleForUpdateAsync(RemoteServiceCallbackId callbackId, Guid mvid, CancellationToken cancellationToken);

            ValueTask<ImmutableArray<ActiveStatementSpan>> GetSpansAsync(RemoteServiceCallbackId callbackId, DocumentId? documentId, string filePath, CancellationToken cancellationToken);
        }

        ValueTask<ImmutableArray<DiagnosticData>> GetDocumentDiagnosticsAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken);
        ValueTask<bool> HasChangesAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, DebuggingSessionId sessionId, string? sourceFilePath, CancellationToken cancellationToken);
        ValueTask<EmitSolutionUpdateResults.Data> EmitSolutionUpdateAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, DebuggingSessionId sessionId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns ids of documents for which diagnostics need to be refreshed in-proc.
        /// </summary>
        ValueTask<ImmutableArray<DocumentId>> CommitSolutionUpdateAsync(DebuggingSessionId sessionId, CancellationToken cancellationToken);
        ValueTask DiscardSolutionUpdateAsync(DebuggingSessionId sessionId, CancellationToken cancellationToken);

        ValueTask<DebuggingSessionId> StartDebuggingSessionAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, ImmutableArray<DocumentId> captureMatchingDocuments, bool captureAllMatchingDocuments, bool reportDiagnostics, CancellationToken cancellationToken);

        /// <summary>
        /// Returns ids of documents for which diagnostics need to be refreshed in-proc.
        /// </summary>
        ValueTask<ImmutableArray<DocumentId>> BreakStateEnteredAsync(DebuggingSessionId sessionId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns ids of documents for which diagnostics need to be refreshed in-proc.
        /// </summary>
        ValueTask<ImmutableArray<DocumentId>> EndDebuggingSessionAsync(DebuggingSessionId sessionId, CancellationToken cancellationToken);

        ValueTask<ImmutableArray<ImmutableArray<ActiveStatementSpan>>> GetBaseActiveStatementSpansAsync(PinnedSolutionInfo solutionInfo, DebuggingSessionId sessionId, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken);
        ValueTask<ImmutableArray<ActiveStatementSpan>> GetAdjustedActiveStatementSpansAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, DebuggingSessionId sessionId, DocumentId documentId, CancellationToken cancellationToken);

        ValueTask<bool?> IsActiveStatementInExceptionRegionAsync(PinnedSolutionInfo solutionInfo, DebuggingSessionId sessionId, ManagedInstructionId instructionId, CancellationToken cancellationToken);
        ValueTask<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, DebuggingSessionId sessionId, ManagedInstructionId instructionId, CancellationToken cancellationToken);
        ValueTask OnSourceFileUpdatedAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, CancellationToken cancellationToken);
    }
}
