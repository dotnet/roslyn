// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue;
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

        private readonly IEditAndContinueWorkspaceService _encService;
        private DebuggingSessionId _sessionId;

        public GlassTestsHotReloadService(HostWorkspaceServices services, IManagedHotReloadService debuggerService)
        {
            _encService = services.GetRequiredService<IEditAndContinueWorkspaceService>();
            _debuggerService = debuggerService;
        }

        public async Task StartSessionAsync(Solution solution, CancellationToken cancellationToken)
        {
            var newSessionId = await _encService.StartDebuggingSessionAsync(
                solution,
                new ManagedHotReloadServiceImpl(_debuggerService),
                captureMatchingDocuments: ImmutableArray<DocumentId>.Empty,
                captureAllMatchingDocuments: true,
                reportDiagnostics: false,
                cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfFalse(_sessionId == default, "Session already started");
            _sessionId = newSessionId;
        }

        public void EnterBreakState()
        {
            var sessionId = _sessionId;
            Contract.ThrowIfFalse(sessionId != default, "Session has not started");

            _encService.BreakStateOrCapabilitiesChanged(sessionId, inBreakState: true, out _);
        }

        public void ExitBreakState()
        {
            var sessionId = _sessionId;
            Contract.ThrowIfFalse(sessionId != default, "Session has not started");

            _encService.BreakStateOrCapabilitiesChanged(sessionId, inBreakState: false, out _);
        }

        public void OnCapabilitiesChanged()
        {
            var sessionId = _sessionId;
            Contract.ThrowIfFalse(sessionId != default, "Session has not started");

            _encService.BreakStateOrCapabilitiesChanged(sessionId, inBreakState: null, out _);
        }

        public void CommitSolutionUpdate()
        {
            var sessionId = _sessionId;
            Contract.ThrowIfFalse(sessionId != default, "Session has not started");

            _encService.CommitSolutionUpdate(sessionId, out _);
        }

        public void DiscardSolutionUpdate()
        {
            var sessionId = _sessionId;
            Contract.ThrowIfFalse(sessionId != default, "Session has not started");

            _encService.DiscardSolutionUpdate(sessionId);
        }

        public void EndDebuggingSession()
        {
            var sessionId = _sessionId;
            Contract.ThrowIfFalse(sessionId != default, "Session has not started");

            _encService.EndDebuggingSession(sessionId, out _);
            _sessionId = default;
        }

        public async ValueTask<bool> HasChangesAsync(Solution solution, string? sourceFilePath, CancellationToken cancellationToken)
        {
            var sessionId = _sessionId;
            if (sessionId == default)
            {
                return false;
            }

            return await _encService.HasChangesAsync(sessionId, solution, s_noActiveStatementSpanProvider, sourceFilePath, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<ManagedHotReloadUpdates> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken)
        {
            var sessionId = _sessionId;
            Contract.ThrowIfFalse(sessionId != default, "Session has not started");

            var result = await _encService.EmitSolutionUpdateAsync(sessionId, solution, s_noActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false);

            var updates = result.ModuleUpdates.Updates.SelectAsArray(
                update => new ManagedHotReloadUpdate(update.Module, update.ILDelta, update.MetadataDelta, update.UpdatedTypes));

            var diagnostics = await EmitSolutionUpdateResults.GetHotReloadDiagnosticsAsync(solution, result.GetDiagnosticData(solution), result.RudeEdits, result.GetSyntaxErrorData(solution), cancellationToken).ConfigureAwait(false);

            return new ManagedHotReloadUpdates(updates, diagnostics.FromContract());
        }
    }
}
