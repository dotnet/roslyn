// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal delegate void StartEditSession(ActiveStatementProvider activeStatementProvider, IDebuggeeModuleMetadataProvider debuggeeModuleMetadataProvider, out ImmutableArray<DocumentId> documentsToReanalyze);
    internal delegate void EndSession(out ImmutableArray<DocumentId> documentsToReanalyze);

    internal class MockEditAndContinueWorkspaceService : IEditAndContinueWorkspaceService
    {
        public Func<ImmutableArray<DocumentId>, ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>>? GetBaseActiveStatementSpansImpl;
        public Func<Solution, SolutionActiveStatementSpanProvider, ActiveInstructionId, LinePositionSpan?>? GetCurrentActiveStatementPositionImpl;
        public Func<Document, DocumentActiveStatementSpanProvider, ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>? GetAdjustedActiveStatementSpansImpl;
        public Action<Solution>? StartDebuggingSessionImpl;
        public StartEditSession? StartEditSessionImpl;
        public EndSession? EndDebuggingSessionImpl;
        public EndSession? EndEditSessionImpl;
        public Func<Solution, SolutionActiveStatementSpanProvider, string?, bool>? HasChangesImpl;
        public Func<Solution, SolutionActiveStatementSpanProvider, (SolutionUpdateStatus, ImmutableArray<Deltas>, ImmutableArray<DiagnosticData>)>? EmitSolutionUpdateImpl;
        public Func<ActiveInstructionId, bool?>? IsActiveStatementInExceptionRegionImpl;
        public Action<DocumentId>? OnSourceFileUpdatedImpl;
        public Action? CommitSolutionUpdateImpl;
        public Action? DiscardSolutionUpdateImpl;
        public Func<Document, DocumentActiveStatementSpanProvider, ImmutableArray<Diagnostic>>? GetDocumentDiagnosticsImpl;

        public void CommitSolutionUpdate()
            => CommitSolutionUpdateImpl?.Invoke();

        public void DiscardSolutionUpdate()
            => DiscardSolutionUpdateImpl?.Invoke();

        public Task<(SolutionUpdateStatus Summary, ImmutableArray<Deltas> Deltas, ImmutableArray<DiagnosticData> Diagnostics)>
            EmitSolutionUpdateAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
            => Task.FromResult((EmitSolutionUpdateImpl ?? throw new NotImplementedException()).Invoke(solution, activeStatementSpanProvider));

        public void EndDebuggingSession(out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            documentsToReanalyze = ImmutableArray<DocumentId>.Empty;
            EndDebuggingSessionImpl?.Invoke(out documentsToReanalyze);
        }

        public void EndEditSession(out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            documentsToReanalyze = ImmutableArray<DocumentId>.Empty;
            EndEditSessionImpl?.Invoke(out documentsToReanalyze);
        }

        public Task<ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>> GetBaseActiveStatementSpansAsync(ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
            => Task.FromResult((GetBaseActiveStatementSpansImpl ?? throw new NotImplementedException()).Invoke(documentIds));

        public Task<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, ActiveInstructionId instructionId, CancellationToken cancellationToken)
            => Task.FromResult((GetCurrentActiveStatementPositionImpl ?? throw new NotImplementedException()).Invoke(solution, activeStatementSpanProvider, instructionId));

        public Task<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>> GetAdjustedActiveStatementSpansAsync(Document document, DocumentActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
            => Task.FromResult((GetAdjustedActiveStatementSpansImpl ?? throw new NotImplementedException()).Invoke(document, activeStatementSpanProvider));

        public Task<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, DocumentActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
            => Task.FromResult((GetDocumentDiagnosticsImpl ?? throw new NotImplementedException()).Invoke(document, activeStatementSpanProvider));

        public Task<bool> HasChangesAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, string? sourceFilePath, CancellationToken cancellationToken)
            => Task.FromResult((HasChangesImpl ?? throw new NotImplementedException()).Invoke(solution, activeStatementSpanProvider, sourceFilePath));

        public Task<bool?> IsActiveStatementInExceptionRegionAsync(ActiveInstructionId instructionId, CancellationToken cancellationToken)
            => Task.FromResult((IsActiveStatementInExceptionRegionImpl ?? throw new NotImplementedException()).Invoke(instructionId));

        public void OnSourceFileUpdated(DocumentId documentId)
            => OnSourceFileUpdatedImpl?.Invoke(documentId);

        public void StartDebuggingSession(Solution solution)
            => StartDebuggingSessionImpl?.Invoke(solution);

        public void StartEditSession(ActiveStatementProvider activeStatementsProvider, IDebuggeeModuleMetadataProvider debuggeeModuleMetadataProvider, out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            documentsToReanalyze = ImmutableArray<DocumentId>.Empty;
            StartEditSessionImpl?.Invoke(activeStatementsProvider, debuggeeModuleMetadataProvider, out documentsToReanalyze);
        }
    }
}
