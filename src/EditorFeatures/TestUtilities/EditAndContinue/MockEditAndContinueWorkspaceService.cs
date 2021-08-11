// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal delegate void ActionOut<T>(out T arg);

    internal class MockEditAndContinueWorkspaceService : IEditAndContinueWorkspaceService
    {
        public Func<Solution, ImmutableArray<DocumentId>, ImmutableArray<ImmutableArray<ActiveStatementSpan>>>? GetBaseActiveStatementSpansImpl;
        public Func<Solution, ActiveStatementSpanProvider, ManagedInstructionId, LinePositionSpan?>? GetCurrentActiveStatementPositionImpl;

        public Func<TextDocument, ActiveStatementSpanProvider, ImmutableArray<ActiveStatementSpan>>? GetAdjustedActiveStatementSpansImpl;
        public Func<Solution, IManagedEditAndContinueDebuggerService, ImmutableArray<DocumentId>, bool, bool, DebuggingSessionId>? StartDebuggingSessionImpl;

        public ActionOut<ImmutableArray<DocumentId>>? EndDebuggingSessionImpl;
        public Func<Solution, ActiveStatementSpanProvider, string?, bool>? HasChangesImpl;
        public Func<Solution, ActiveStatementSpanProvider, EmitSolutionUpdateResults>? EmitSolutionUpdateImpl;
        public Func<Solution, ManagedInstructionId, bool?>? IsActiveStatementInExceptionRegionImpl;
        public Action<Document>? OnSourceFileUpdatedImpl;
        public ActionOut<ImmutableArray<DocumentId>>? CommitSolutionUpdateImpl;
        public ActionOut<ImmutableArray<DocumentId>>? BreakStateEnteredImpl;
        public Action? DiscardSolutionUpdateImpl;
        public Func<Document, ActiveStatementSpanProvider, ImmutableArray<Diagnostic>>? GetDocumentDiagnosticsImpl;

        public void BreakStateEntered(DebuggingSessionId sessionId, out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            documentsToReanalyze = ImmutableArray<DocumentId>.Empty;
            BreakStateEnteredImpl?.Invoke(out documentsToReanalyze);
        }

        public void CommitSolutionUpdate(DebuggingSessionId sessionId, out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            documentsToReanalyze = ImmutableArray<DocumentId>.Empty;
            CommitSolutionUpdateImpl?.Invoke(out documentsToReanalyze);
        }

        public void DiscardSolutionUpdate(DebuggingSessionId sessionId)
            => DiscardSolutionUpdateImpl?.Invoke();

        public ValueTask<EmitSolutionUpdateResults> EmitSolutionUpdateAsync(DebuggingSessionId sessionId, Solution solution, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
            => new((EmitSolutionUpdateImpl ?? throw new NotImplementedException()).Invoke(solution, activeStatementSpanProvider));

        public void EndDebuggingSession(DebuggingSessionId sessionId, out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            documentsToReanalyze = ImmutableArray<DocumentId>.Empty;
            EndDebuggingSessionImpl?.Invoke(out documentsToReanalyze);
        }

        public ValueTask<ImmutableArray<ImmutableArray<ActiveStatementSpan>>> GetBaseActiveStatementSpansAsync(DebuggingSessionId sessionId, Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
            => new((GetBaseActiveStatementSpansImpl ?? throw new NotImplementedException()).Invoke(solution, documentIds));

        public ValueTask<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(DebuggingSessionId sessionId, Solution solution, ActiveStatementSpanProvider activeStatementSpanProvider, ManagedInstructionId instructionId, CancellationToken cancellationToken)
            => new((GetCurrentActiveStatementPositionImpl ?? throw new NotImplementedException()).Invoke(solution, activeStatementSpanProvider, instructionId));

        public ValueTask<ImmutableArray<ActiveStatementSpan>> GetAdjustedActiveStatementSpansAsync(DebuggingSessionId sessionId, TextDocument document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
            => new((GetAdjustedActiveStatementSpansImpl ?? throw new NotImplementedException()).Invoke(document, activeStatementSpanProvider));

        public ValueTask<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
            => new((GetDocumentDiagnosticsImpl ?? throw new NotImplementedException()).Invoke(document, activeStatementSpanProvider));

        public ValueTask<bool> HasChangesAsync(DebuggingSessionId sessionId, Solution solution, ActiveStatementSpanProvider activeStatementSpanProvider, string? sourceFilePath, CancellationToken cancellationToken)
            => new((HasChangesImpl ?? throw new NotImplementedException()).Invoke(solution, activeStatementSpanProvider, sourceFilePath));

        public ValueTask<bool?> IsActiveStatementInExceptionRegionAsync(DebuggingSessionId sessionId, Solution solution, ManagedInstructionId instructionId, CancellationToken cancellationToken)
            => new((IsActiveStatementInExceptionRegionImpl ?? throw new NotImplementedException()).Invoke(solution, instructionId));

        public void OnSourceFileUpdated(Document document)
            => OnSourceFileUpdatedImpl?.Invoke(document);

        public ValueTask<DebuggingSessionId> StartDebuggingSessionAsync(Solution solution, IManagedEditAndContinueDebuggerService debuggerService, ImmutableArray<DocumentId> captureMatchingDocuments, bool captureAllMatchingDocuments, bool reportDiagnostics, CancellationToken cancellationToken)
            => new((StartDebuggingSessionImpl ?? throw new NotImplementedException()).Invoke(solution, debuggerService, captureMatchingDocuments, captureAllMatchingDocuments, reportDiagnostics));
    }
}
