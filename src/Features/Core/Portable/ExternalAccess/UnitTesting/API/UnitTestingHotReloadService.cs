// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

internal sealed class UnitTestingHotReloadService(HostWorkspaceServices services)
{
    private sealed class DebuggerService(ImmutableArray<string> capabilities) : IManagedHotReloadService
    {
        private readonly ImmutableArray<string> _capabilities = capabilities;

        public async ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
            => ImmutableArray<ManagedActiveStatementDebugInfo>.Empty;

        public async ValueTask<ManagedHotReloadAvailability> GetAvailabilityAsync(Guid module, CancellationToken cancellationToken)
            => new ManagedHotReloadAvailability(ManagedHotReloadAvailabilityStatus.Available);

        public async ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
            => _capabilities;

        public ValueTask PrepareModuleForUpdateAsync(Guid module, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    public readonly struct Update(
        Guid moduleId,
        ImmutableArray<byte> ilDelta,
        ImmutableArray<byte> metadataDelta,
        ImmutableArray<byte> pdbDelta,
        ImmutableArray<int> updatedMethods,
        ImmutableArray<int> updatedTypes)
    {
        public readonly Guid ModuleId = moduleId;
        public readonly ImmutableArray<byte> ILDelta = ilDelta;
        public readonly ImmutableArray<byte> MetadataDelta = metadataDelta;
        public readonly ImmutableArray<byte> PdbDelta = pdbDelta;
        public readonly ImmutableArray<int> UpdatedMethods = updatedMethods;
        public readonly ImmutableArray<int> UpdatedTypes = updatedTypes;
    }

    private static readonly ActiveStatementSpanProvider s_solutionActiveStatementSpanProvider =
        async (_, _, _) => ImmutableArray<ActiveStatementSpan>.Empty;

    private readonly IEditAndContinueService _encService = services.GetRequiredService<IEditAndContinueWorkspaceService>().Service;
    private DebuggingSessionId _sessionId;

    /// <summary>
    /// Starts the watcher.
    /// </summary>
    /// <param name="solution">Solution that represents sources that match the built binaries on disk.</param>
    /// <param name="capabilities">Array of capabilities retrieved from the runtime to dictate supported rude edits.</param>
    public async Task StartSessionAsync(Solution solution, ImmutableArray<string> capabilities, CancellationToken cancellationToken)
    {
        // Hydrate the solution snapshot with file content.
        // It's important to do this before we start watching for changes so that we have a baseline we can compare future snapshots to.
        await EditAndContinueService.HydrateDocumentsAsync(solution, cancellationToken).ConfigureAwait(false);

        var newSessionId = _encService.StartDebuggingSession(
            solution,
            new DebuggerService(capabilities),
            NullPdbMatchingSourceTextProvider.Instance,
            reportDiagnostics: false);

        Contract.ThrowIfFalse(_sessionId == default, "Session already started");
        _sessionId = newSessionId;
    }

    /// <summary>
    /// Emits updates for all projects that differ between the given <paramref name="solution"/> snapshot and the one given to the previous successful call 
    /// where <paramref name="commitUpdates"/> was `true` or the one passed to <see cref="StartSessionAsync(Solution, ImmutableArray{string}, CancellationToken)"/>
    /// for the first invocation.
    /// </summary>
    /// <param name="solution">Solution snapshot.</param>
    /// <param name="commitUpdates">commits changes if true, discards if false</param>
    /// <returns>
    /// Updates (one for each changed project) and Rude Edit diagnostics. Does not include syntax or semantic diagnostics.
    /// </returns>
    public async Task<(ImmutableArray<Update> updates, ImmutableArray<Diagnostic> diagnostics)> EmitSolutionUpdateAsync(Solution solution, bool commitUpdates, CancellationToken cancellationToken)
    {
        var sessionId = _sessionId;
        Contract.ThrowIfFalse(sessionId != default, "Session has not started");

        var results = await _encService
            .EmitSolutionUpdateAsync(sessionId, solution, runningProjects: ImmutableDictionary<ProjectId, RunningProjectOptions>.Empty, s_solutionActiveStatementSpanProvider, cancellationToken)
            .ConfigureAwait(false);

        if (results.ModuleUpdates.Status == ModuleUpdateStatus.Ready)
        {
            if (commitUpdates)
            {
                _encService.CommitSolutionUpdate(sessionId);
            }
            else
            {
                _encService.DiscardSolutionUpdate(sessionId);
            }
        }

        var diagnostics = results.GetAllDiagnostics();
        if (diagnostics.HasAnyErrors())
        {
            return ([], diagnostics);
        }

        var updates = results.ModuleUpdates.Updates.SelectAsArray(
            update => new Update(
                update.Module,
                update.ILDelta,
                update.MetadataDelta,
                update.PdbDelta,
                update.UpdatedMethods,
                update.UpdatedTypes));

        return (updates, diagnostics);
    }

    public void EndSession()
    {
        Contract.ThrowIfFalse(_sessionId != default, "Session has not started");
        _encService.EndDebuggingSession(_sessionId);
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor
    {
        private readonly UnitTestingHotReloadService _instance;

        internal TestAccessor(UnitTestingHotReloadService instance)
            => _instance = instance;

        public DebuggingSessionId SessionId
            => _instance._sessionId;
    }
}
