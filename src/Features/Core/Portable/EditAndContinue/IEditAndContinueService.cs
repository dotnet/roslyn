// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal interface IEditAndContinueWorkspaceService : IWorkspaceService
{
    IEditAndContinueService Service { get; }
    IEditAndContinueSessionTracker SessionTracker { get; }
}

internal interface IEditAndContinueService
{
    ValueTask<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken);
    ValueTask<EmitSolutionUpdateResults> EmitSolutionUpdateAsync(DebuggingSessionId sessionId, Solution solution, ImmutableDictionary<ProjectId, RunningProjectOptions> runningProjects, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken);

    void CommitSolutionUpdate(DebuggingSessionId sessionId);
    void DiscardSolutionUpdate(DebuggingSessionId sessionId);

    DebuggingSessionId StartDebuggingSession(Solution solution, IManagedHotReloadService debuggerService, IPdbMatchingSourceTextProvider sourceTextProvider, bool reportDiagnostics);
    void BreakStateOrCapabilitiesChanged(DebuggingSessionId sessionId, bool? inBreakState);
    void EndDebuggingSession(DebuggingSessionId sessionId);

    ValueTask<ImmutableArray<ImmutableArray<ActiveStatementSpan>>> GetBaseActiveStatementSpansAsync(DebuggingSessionId sessionId, Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken);
    ValueTask<ImmutableArray<ActiveStatementSpan>> GetAdjustedActiveStatementSpansAsync(DebuggingSessionId sessionId, TextDocument document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken);

    void SetFileLoggingDirectory(string? logDirectory);
}
