// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Debugger
{
    internal sealed class GlassTestsHotReloadService
    {
        private static readonly ActiveStatementSpanProvider s_noActiveStatementSpanProvider =
           (_, _, _) => ValueTaskFactory.FromResult(ImmutableArray<ActiveStatementSpan>.Empty);

        private readonly IManagedHotReloadService _debuggerService;

        private readonly IEditAndContinueService _encService;
        private DebuggingSessionId _sessionId;

        public GlassTestsHotReloadService(HostWorkspaceServices services, IManagedHotReloadService debuggerService)
        {
            _encService = services.GetRequiredService<IEditAndContinueWorkspaceService>().Service;
            _debuggerService = debuggerService;
        }

        public async Task StartSessionAsync(Solution solution, CancellationToken cancellationToken)
        {
            var newSessionId = await _encService.StartDebuggingSessionAsync(
                solution,
                new ManagedHotReloadServiceBridge(_debuggerService),
                NullPdbMatchingSourceTextProvider.Instance,
                captureMatchingDocuments: ImmutableArray<DocumentId>.Empty,
                captureAllMatchingDocuments: true,
                reportDiagnostics: false,
                cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfFalse(_sessionId == default, "Session already started");
            _sessionId = newSessionId;
        }

        private DebuggingSessionId GetSessionId()
        {
            var sessionId = _sessionId;
            Contract.ThrowIfFalse(sessionId != default, "Session has not started");

            return sessionId;
        }

        public void EnterBreakState()
        {
            _encService.BreakStateOrCapabilitiesChanged(GetSessionId(), inBreakState: true, out _);
        }

        public void ExitBreakState()
        {
            _encService.BreakStateOrCapabilitiesChanged(GetSessionId(), inBreakState: false, out _);
        }

        public void OnCapabilitiesChanged()
        {
            _encService.BreakStateOrCapabilitiesChanged(GetSessionId(), inBreakState: null, out _);
        }

        public void CommitSolutionUpdate()
        {
            _encService.CommitSolutionUpdate(GetSessionId(), out _);
        }

        public void DiscardSolutionUpdate()
        {
            _encService.DiscardSolutionUpdate(GetSessionId());
        }

        public void EndDebuggingSession()
        {
            _encService.EndDebuggingSession(GetSessionId(), out _);
            _sessionId = default;
        }

        public async ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(Solution solution, CancellationToken cancellationToken)
        {
            var result = await _encService.EmitSolutionUpdateAsync(GetSessionId(), solution, s_noActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false);
            var diagnostics = await EmitSolutionUpdateResults.GetHotReloadDiagnosticsAsync(solution, result.Diagnostics.ToDiagnosticData(solution), result.RudeEdits, result.GetSyntaxErrorData(solution), result.ModuleUpdates.Status, cancellationToken).ConfigureAwait(false);
            return new ManagedHotReloadUpdates(result.ModuleUpdates.Updates.FromContract(), diagnostics.FromContract());
        }
    }
}
