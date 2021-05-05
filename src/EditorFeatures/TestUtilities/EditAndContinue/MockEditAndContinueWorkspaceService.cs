// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal delegate void ActionOut<T>(out T arg);

    internal class MockEditAndContinueWorkspaceService : IEditAndContinueWorkspaceService
    {
        public Func<Solution, ImmutableArray<DocumentId>, ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>>? GetBaseActiveStatementSpansImpl;
        public Func<Solution, SolutionActiveStatementSpanProvider, ManagedInstructionId, LinePositionSpan?>? GetCurrentActiveStatementPositionImpl;

        public Func<Document, DocumentActiveStatementSpanProvider, ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>? GetAdjustedActiveStatementSpansImpl;
        public Action<Solution, IManagedEditAndContinueDebuggerService, bool>? StartDebuggingSessionImpl;

        public ActionOut<ImmutableArray<DocumentId>>? EndDebuggingSessionImpl;
        public Func<Solution, SolutionActiveStatementSpanProvider, string?, bool>? HasChangesImpl;
        public Func<Solution, SolutionActiveStatementSpanProvider, EmitSolutionUpdateResults>? EmitSolutionUpdateImpl;
        public Func<Solution, ManagedInstructionId, bool?>? IsActiveStatementInExceptionRegionImpl;
        public Action<Document>? OnSourceFileUpdatedImpl;
        public ActionOut<ImmutableArray<DocumentId>>? CommitSolutionUpdateImpl;
        public ActionOut<ImmutableArray<DocumentId>>? BreakStateEnteredImpl;
        public Action? DiscardSolutionUpdateImpl;
        public Func<Document, DocumentActiveStatementSpanProvider, ImmutableArray<Diagnostic>>? GetDocumentDiagnosticsImpl;

        public void BreakStateEntered(out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            documentsToReanalyze = ImmutableArray<DocumentId>.Empty;
            BreakStateEnteredImpl?.Invoke(out documentsToReanalyze);
        }

        public void CommitSolutionUpdate(out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            documentsToReanalyze = ImmutableArray<DocumentId>.Empty;
            CommitSolutionUpdateImpl?.Invoke(out documentsToReanalyze);
        }

        public void DiscardSolutionUpdate()
            => DiscardSolutionUpdateImpl?.Invoke();

        public ValueTask<EmitSolutionUpdateResults> EmitSolutionUpdateAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
            => new((EmitSolutionUpdateImpl ?? throw new NotImplementedException()).Invoke(solution, activeStatementSpanProvider));

        public void EndDebuggingSession(out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            documentsToReanalyze = ImmutableArray<DocumentId>.Empty;
            EndDebuggingSessionImpl?.Invoke(out documentsToReanalyze);
        }

        public ValueTask<ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>> GetBaseActiveStatementSpansAsync(Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
            => new((GetBaseActiveStatementSpansImpl ?? throw new NotImplementedException()).Invoke(solution, documentIds));

        public ValueTask<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, ManagedInstructionId instructionId, CancellationToken cancellationToken)
            => new((GetCurrentActiveStatementPositionImpl ?? throw new NotImplementedException()).Invoke(solution, activeStatementSpanProvider, instructionId));

        public ValueTask<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>> GetAdjustedActiveStatementSpansAsync(Document document, DocumentActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
            => new((GetAdjustedActiveStatementSpansImpl ?? throw new NotImplementedException()).Invoke(document, activeStatementSpanProvider));

        public ValueTask<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, DocumentActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
            => new((GetDocumentDiagnosticsImpl ?? throw new NotImplementedException()).Invoke(document, activeStatementSpanProvider));

        public ValueTask<bool> HasChangesAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, string? sourceFilePath, CancellationToken cancellationToken)
            => new((HasChangesImpl ?? throw new NotImplementedException()).Invoke(solution, activeStatementSpanProvider, sourceFilePath));

        public ValueTask<bool?> IsActiveStatementInExceptionRegionAsync(Solution solution, ManagedInstructionId instructionId, CancellationToken cancellationToken)
            => new((IsActiveStatementInExceptionRegionImpl ?? throw new NotImplementedException()).Invoke(solution, instructionId));

        public void OnSourceFileUpdated(Document document)
            => OnSourceFileUpdatedImpl?.Invoke(document);

        public ValueTask StartDebuggingSessionAsync(Solution solution, IManagedEditAndContinueDebuggerService debuggerService, bool captureMatchingDocuments, CancellationToken cancellationToken)
        {
            StartDebuggingSessionImpl?.Invoke(solution, debuggerService, captureMatchingDocuments);
            return default;
        }
    }
}
