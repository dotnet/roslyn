// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;

using InternalContracts = Microsoft.CodeAnalysis.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.ExternalAccess.Debugger;

internal sealed class GlassTestsHotReloadService(HostWorkspaceServices services, IManagedHotReloadService debuggerService)
{
    internal sealed class ServiceWrapper(IManagedHotReloadService service) : InternalContracts.IManagedHotReloadService
    {
        public async ValueTask<ImmutableArray<InternalContracts.ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellation)
            => (await service.GetActiveStatementsAsync(cancellation).ConfigureAwait(false)).SelectAsArray(a => a.ToContract());

        public async ValueTask<InternalContracts.ManagedHotReloadAvailability> GetAvailabilityAsync(Guid module, CancellationToken cancellation)
            => (await service.GetAvailabilityAsync(module, cancellation).ConfigureAwait(false)).ToContract();

        public ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellation)
            => service.GetCapabilitiesAsync(cancellation);

        public ValueTask PrepareModuleForUpdateAsync(Guid module, CancellationToken cancellation)
            => service.PrepareModuleForUpdateAsync(module, cancellation);
    }

    private static readonly ActiveStatementSpanProvider s_noActiveStatementSpanProvider = async (_, _, _) => [];
    private readonly IEditAndContinueService _encService = services.GetRequiredService<IEditAndContinueWorkspaceService>().Service;
    private DebuggingSessionId _sessionId;

#pragma warning disable IDE0060 // Remove unused parameter
    public async Task StartSessionAsync(Solution solution, CancellationToken cancellationToken)
#pragma warning restore IDE0060
    {
        var newSessionId = _encService.StartDebuggingSession(
            solution,
            new ServiceWrapper(debuggerService),
            NullPdbMatchingSourceTextProvider.Instance,
            reportDiagnostics: false);

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
        _encService.BreakStateOrCapabilitiesChanged(GetSessionId(), inBreakState: true);
    }

    public void ExitBreakState()
    {
        _encService.BreakStateOrCapabilitiesChanged(GetSessionId(), inBreakState: false);
    }

    public void OnCapabilitiesChanged()
    {
        _encService.BreakStateOrCapabilitiesChanged(GetSessionId(), inBreakState: null);
    }

    public void CommitSolutionUpdate()
    {
        _encService.CommitSolutionUpdate(GetSessionId());
    }

    public void DiscardSolutionUpdate()
    {
        _encService.DiscardSolutionUpdate(GetSessionId());
    }

    public void EndDebuggingSession()
    {
        _encService.EndDebuggingSession(GetSessionId());
        _sessionId = default;
    }

    public async ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(Solution solution, CancellationToken cancellationToken)
    {
        var results = (await _encService.EmitSolutionUpdateAsync(GetSessionId(), solution, runningProjects: ImmutableDictionary<ProjectId, RunningProjectOptions>.Empty, s_noActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false)).Dehydrate();
        return new ManagedHotReloadUpdates(results.ModuleUpdates.Updates.FromContract(), results.GetAllDiagnostics().FromContract(), projectInstancesToRebuild: [], projectInstancesToRestart: []);
    }
}
