// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal delegate void StartEditSession(ActiveStatementProvider activeStatementProvider, IDebuggeeModuleMetadataProvider debuggeeModuleMetadataProvider, out ImmutableArray<DocumentId> documentsToReanalyze);
    internal delegate void EndSession(out ImmutableArray<DocumentId> documentsToReanalyze);

    internal class MockEditAndContinueWorkspaceService : IEditAndContinueWorkspaceService
    {
        public Func<ImmutableArray<DocumentId>, ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>>? GetBaseActiveStatementSpansAsyncImpl;
        public Func<Solution, ActiveInstructionId, LinePositionSpan?>? GetCurrentActiveStatementPositionAsyncImpl;
        public Func<Document, ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>? GetDocumentActiveStatementSpansAsyncImpl;
        public Action<Solution>? StartDebuggingSessionImpl;
        public StartEditSession? StartEditSessionImpl;
        public EndSession? EndDebuggingSessionImpl;
        public EndSession? EndEditSessionImpl;
        public Func<Solution, string?, bool>? HasChangesAsyncImpl;
        public Func<Solution, (SolutionUpdateStatus, ImmutableArray<Deltas>, ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostics)>)>? EmitSolutionUpdateAsyncImpl;
        public Func<ActiveInstructionId, bool?>? IsActiveStatementInExceptionRegionAsyncImpl;

        public bool IsDebuggingSessionInProgress => throw new NotImplementedException();

        public void CommitSolutionUpdate() { }

        public void DiscardSolutionUpdate() { }

        public Task<(SolutionUpdateStatus Summary, ImmutableArray<Deltas> Deltas, ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostics)> Diagnostics)> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken)
            => Task.FromResult((EmitSolutionUpdateAsyncImpl ?? throw new NotImplementedException()).Invoke(solution));

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
            => Task.FromResult((GetBaseActiveStatementSpansAsyncImpl ?? throw new NotImplementedException()).Invoke(documentIds));

        public Task<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(Solution solution, ActiveInstructionId instructionId, CancellationToken cancellationToken)
            => Task.FromResult((GetCurrentActiveStatementPositionAsyncImpl ?? throw new NotImplementedException()).Invoke(solution, instructionId));

        public Task<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>> GetDocumentActiveStatementSpansAsync(Document document, CancellationToken cancellationToken)
            => Task.FromResult((GetDocumentActiveStatementSpansAsyncImpl ?? throw new NotImplementedException()).Invoke(document));

        public Task<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<bool> HasChangesAsync(Solution solution, string? sourceFilePath, CancellationToken cancellationToken)
            => Task.FromResult((HasChangesAsyncImpl ?? throw new NotImplementedException()).Invoke(solution, sourceFilePath));

        public Task<bool?> IsActiveStatementInExceptionRegionAsync(ActiveInstructionId instructionId, CancellationToken cancellationToken)
            => Task.FromResult((IsActiveStatementInExceptionRegionAsyncImpl ?? throw new NotImplementedException()).Invoke(instructionId));

        public void OnSourceFileUpdated(DocumentId documentId) { }

        public void StartDebuggingSession(Solution solution)
            => StartDebuggingSessionImpl?.Invoke(solution);

        public void StartEditSession(ActiveStatementProvider activeStatementsProvider, IDebuggeeModuleMetadataProvider debuggeeModuleMetadataProvider, out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            documentsToReanalyze = ImmutableArray<DocumentId>.Empty;
            StartEditSessionImpl?.Invoke(activeStatementsProvider, debuggeeModuleMetadataProvider, out documentsToReanalyze);
        }
    }
}
