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
    internal class MockEditAndContinueWorkspaceService : IEditAndContinueWorkspaceService
    {
        public Func<ImmutableArray<DocumentId>, ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>>? GetBaseActiveStatementSpansAsyncImpl;
        public Func<Solution, ActiveInstructionId, LinePositionSpan?>? GetCurrentActiveStatementPositionAsyncImpl;
        public Func<Document, ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>? GetDocumentActiveStatementSpansAsyncImpl;
        public Action<Solution>? StartDebuggingSessionImpl;
        public Action<ActiveStatementProvider, IDebuggeeModuleMetadataProvider>? StartEditSessionImpl;
        public Func<Solution, string?, bool>? HasChangesAsyncImpl;
        public Func<Solution, (SolutionUpdateStatus, ImmutableArray<Deltas>)>? EmitSolutionUpdateAsyncImpl;
        public Func<ActiveInstructionId, bool?>? IsActiveStatementInExceptionRegionAsyncImpl;

        public bool IsDebuggingSessionInProgress => throw new NotImplementedException();

        public void CommitSolutionUpdate() { }

        public void DiscardSolutionUpdate() { }

        public Task<(SolutionUpdateStatus Summary, ImmutableArray<Deltas> Deltas)> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken)
            => Task.FromResult((EmitSolutionUpdateAsyncImpl ?? throw new NotImplementedException()).Invoke(solution));

        public void EndDebuggingSession() { }

        public void EndEditSession() { }

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

        public void ReportApplyChangesException(Solution solution, string message) { }

        public void StartDebuggingSession(Solution solution)
            => StartDebuggingSessionImpl?.Invoke(solution);

        public void StartEditSession(ActiveStatementProvider activeStatementsProvider, IDebuggeeModuleMetadataProvider debuggeeModuleMetadataProvider)
            => StartEditSessionImpl?.Invoke(activeStatementsProvider, debuggeeModuleMetadataProvider);
    }
}
