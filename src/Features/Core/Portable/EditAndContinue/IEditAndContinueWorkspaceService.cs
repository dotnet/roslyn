﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal interface IEditAndContinueWorkspaceService : IWorkspaceService
    {
        ValueTask<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken);
        ValueTask<bool> HasChangesAsync(DebuggingSessionId sessionId, Solution solution, ActiveStatementSpanProvider activeStatementSpanProvider, string? sourceFilePath, CancellationToken cancellationToken);
        ValueTask<EmitSolutionUpdateResults> EmitSolutionUpdateAsync(DebuggingSessionId sessionId, Solution solution, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken);

        void CommitSolutionUpdate(DebuggingSessionId sessionId, out ImmutableArray<DocumentId> documentsToReanalyze);
        void DiscardSolutionUpdate(DebuggingSessionId sessionId);

        void OnSourceFileUpdated(Document document);

        ValueTask<DebuggingSessionId> StartDebuggingSessionAsync(Solution solution, IManagedEditAndContinueDebuggerService debuggerService, bool captureMatchingDocuments, bool reportDiagnostics, CancellationToken cancellationToken);
        void BreakStateEntered(DebuggingSessionId sessionId, out ImmutableArray<DocumentId> documentsToReanalyze);
        void EndDebuggingSession(DebuggingSessionId sessionId, out ImmutableArray<DocumentId> documentsToReanalyze);

        ValueTask<bool?> IsActiveStatementInExceptionRegionAsync(DebuggingSessionId sessionId, Solution solution, ManagedInstructionId instructionId, CancellationToken cancellationToken);
        ValueTask<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(DebuggingSessionId sessionId, Solution solution, ActiveStatementSpanProvider activeStatementSpanProvider, ManagedInstructionId instructionId, CancellationToken cancellationToken);

        ValueTask<ImmutableArray<ImmutableArray<ActiveStatementSpan>>> GetBaseActiveStatementSpansAsync(DebuggingSessionId sessionId, Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken);
        ValueTask<ImmutableArray<ActiveStatementSpan>> GetAdjustedActiveStatementSpansAsync(DebuggingSessionId sessionId, TextDocument document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken);
    }
}
