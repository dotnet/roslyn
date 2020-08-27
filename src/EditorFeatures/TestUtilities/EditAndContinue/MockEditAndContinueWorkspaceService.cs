// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal class MockEditAndContinueWorkspaceService : IEditAndContinueWorkspaceService
    {
        public Func<ImmutableArray<DocumentId>, ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>>? GetBaseActiveStatementSpansAsyncImpl;
        public Func<Solution, ActiveInstructionId, LinePositionSpan?>? GetCurrentActiveStatementPositionAsyncImpl;
        public Func<Document, ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>? GetAdjustedDocumentActiveStatementSpansAsyncImpl;

        public bool IsDebuggingSessionInProgress => throw new NotImplementedException();

        public void CommitSolutionUpdate() => throw new NotImplementedException();

        public void DiscardSolutionUpdate() => throw new NotImplementedException();

        public Task<(SolutionUpdateStatus Summary, ImmutableArray<Deltas> Deltas)> EmitSolutionUpdateAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken) => throw new NotImplementedException();

        public void EndDebuggingSession() => throw new NotImplementedException();

        public void EndEditSession() => throw new NotImplementedException();

        public Task<ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>> GetBaseActiveStatementSpansAsync(ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
            => Task.FromResult((GetBaseActiveStatementSpansAsyncImpl ?? throw new NotImplementedException()).Invoke(documentIds));

        public Task<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, ActiveInstructionId instructionId, CancellationToken cancellationToken)
            => Task.FromResult((GetCurrentActiveStatementPositionAsyncImpl ?? throw new NotImplementedException()).Invoke(solution, instructionId));

        public Task<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>> GetAdjustedActiveStatementSpansAsync(Document document, DocumentActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
            => Task.FromResult((GetAdjustedDocumentActiveStatementSpansAsyncImpl ?? throw new NotImplementedException()).Invoke(document));

        public Task<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, DocumentActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<bool> HasChangesAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, string? sourceFilePath, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<bool?> IsActiveStatementInExceptionRegionAsync(ActiveInstructionId instructionId, CancellationToken cancellationToken) => throw new NotImplementedException();

        public void OnSourceFileUpdated(DocumentId documentId) => throw new NotImplementedException();

        public void ReportApplyChangesException(Solution solution, string message) => throw new NotImplementedException();

        public void StartDebuggingSession(Solution solution) => throw new NotImplementedException();

        public void StartEditSession(ActiveStatementProvider activeStatementsProvider) => throw new NotImplementedException();
    }
}
