// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal delegate void ActionOut<TArg1>(out TArg1 arg);
    internal delegate void ActionOut<TArg1, TArg2>(TArg1 arg1, out TArg2 arg2);

    [Export(typeof(IEditAndContinueService)), Shared]
    internal class MockEditAndContinueWorkspaceService : IEditAndContinueService
    {
        public Func<Solution, ImmutableArray<DocumentId>, ImmutableArray<ImmutableArray<ActiveStatementSpan>>>? GetBaseActiveStatementSpansImpl;

        public Func<TextDocument, ActiveStatementSpanProvider, ImmutableArray<ActiveStatementSpan>>? GetAdjustedActiveStatementSpansImpl;
        public Func<Solution, IManagedHotReloadService, IPdbMatchingSourceTextProvider, ImmutableArray<DocumentId>, bool, bool, DebuggingSessionId>? StartDebuggingSessionImpl;

        public ActionOut<ImmutableArray<DocumentId>>? EndDebuggingSessionImpl;
        public Func<Solution, ActiveStatementSpanProvider, EmitSolutionUpdateResults>? EmitSolutionUpdateImpl;
        public Action<Document>? OnSourceFileUpdatedImpl;
        public ActionOut<ImmutableArray<DocumentId>>? CommitSolutionUpdateImpl;
        public ActionOut<bool?, ImmutableArray<DocumentId>>? BreakStateOrCapabilitiesChangedImpl;
        public Action? DiscardSolutionUpdateImpl;
        public Func<Document, ActiveStatementSpanProvider, ImmutableArray<Diagnostic>>? GetDocumentDiagnosticsImpl;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MockEditAndContinueWorkspaceService()
        {
        }

        public void BreakStateOrCapabilitiesChanged(DebuggingSessionId sessionId, bool? inBreakState, out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            documentsToReanalyze = ImmutableArray<DocumentId>.Empty;
            BreakStateOrCapabilitiesChangedImpl?.Invoke(inBreakState, out documentsToReanalyze);
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

        public ValueTask<ImmutableArray<ActiveStatementSpan>> GetAdjustedActiveStatementSpansAsync(DebuggingSessionId sessionId, TextDocument document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
            => new((GetAdjustedActiveStatementSpansImpl ?? throw new NotImplementedException()).Invoke(document, activeStatementSpanProvider));

        public ValueTask<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
            => new((GetDocumentDiagnosticsImpl ?? throw new NotImplementedException()).Invoke(document, activeStatementSpanProvider));

        public void OnSourceFileUpdated(Document document)
            => OnSourceFileUpdatedImpl?.Invoke(document);

        public ValueTask<DebuggingSessionId> StartDebuggingSessionAsync(Solution solution, IManagedHotReloadService debuggerService, IPdbMatchingSourceTextProvider sourceTextProvider, ImmutableArray<DocumentId> captureMatchingDocuments, bool captureAllMatchingDocuments, bool reportDiagnostics, CancellationToken cancellationToken)
            => new((StartDebuggingSessionImpl ?? throw new NotImplementedException()).Invoke(solution, debuggerService, sourceTextProvider, captureMatchingDocuments, captureAllMatchingDocuments, reportDiagnostics));

        public void SetFileLoggingDirectory(string? logDirectory)
            => throw new NotImplementedException();
    }
}
