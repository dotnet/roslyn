// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

[Export(typeof(IEditAndContinueService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class MockEditAndContinueService() : IEditAndContinueService
{
    public Func<Solution, ImmutableArray<DocumentId>, ImmutableArray<ImmutableArray<ActiveStatementSpan>>>? GetBaseActiveStatementSpansImpl;

    public Func<TextDocument, ActiveStatementSpanProvider, ImmutableArray<ActiveStatementSpan>>? GetAdjustedActiveStatementSpansImpl;
    public Func<Solution, IManagedHotReloadService, IPdbMatchingSourceTextProvider, bool, DebuggingSessionId>? StartDebuggingSessionImpl;

    public Action? EndDebuggingSessionImpl;
    public Func<Solution, ImmutableDictionary<ProjectId, RunningProjectOptions>, ActiveStatementSpanProvider, EmitSolutionUpdateResults>? EmitSolutionUpdateImpl;
    public Action<Document>? OnSourceFileUpdatedImpl;
    public Action? CommitSolutionUpdateImpl;
    public Action<bool?>? BreakStateOrCapabilitiesChangedImpl;
    public Action? DiscardSolutionUpdateImpl;
    public Func<Document, ActiveStatementSpanProvider, ImmutableArray<Diagnostic>>? GetDocumentDiagnosticsImpl;

    public void BreakStateOrCapabilitiesChanged(DebuggingSessionId sessionId, bool? inBreakState)
        => BreakStateOrCapabilitiesChangedImpl?.Invoke(inBreakState);

    public void CommitSolutionUpdate(DebuggingSessionId sessionId)
        => CommitSolutionUpdateImpl?.Invoke();

    public void DiscardSolutionUpdate(DebuggingSessionId sessionId)
        => DiscardSolutionUpdateImpl?.Invoke();

    public ValueTask<EmitSolutionUpdateResults> EmitSolutionUpdateAsync(DebuggingSessionId sessionId, Solution solution, ImmutableDictionary<ProjectId, RunningProjectOptions> runningProjects, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        => new((EmitSolutionUpdateImpl ?? throw new NotImplementedException()).Invoke(solution, runningProjects, activeStatementSpanProvider));

    public void EndDebuggingSession(DebuggingSessionId sessionId)
        => EndDebuggingSessionImpl?.Invoke();

    public ValueTask<ImmutableArray<ImmutableArray<ActiveStatementSpan>>> GetBaseActiveStatementSpansAsync(DebuggingSessionId sessionId, Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        => new((GetBaseActiveStatementSpansImpl ?? throw new NotImplementedException()).Invoke(solution, documentIds));

    public ValueTask<ImmutableArray<ActiveStatementSpan>> GetAdjustedActiveStatementSpansAsync(DebuggingSessionId sessionId, TextDocument document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        => new((GetAdjustedActiveStatementSpansImpl ?? throw new NotImplementedException()).Invoke(document, activeStatementSpanProvider));

    public ValueTask<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        => new((GetDocumentDiagnosticsImpl ?? throw new NotImplementedException()).Invoke(document, activeStatementSpanProvider));

    public void OnSourceFileUpdated(Document document)
        => OnSourceFileUpdatedImpl?.Invoke(document);

    public DebuggingSessionId StartDebuggingSession(Solution solution, IManagedHotReloadService debuggerService, IPdbMatchingSourceTextProvider sourceTextProvider, bool reportDiagnostics)
        => (StartDebuggingSessionImpl ?? throw new NotImplementedException()).Invoke(solution, debuggerService, sourceTextProvider, reportDiagnostics);

    public void SetFileLoggingDirectory(string? logDirectory)
        => throw new NotImplementedException();
}
