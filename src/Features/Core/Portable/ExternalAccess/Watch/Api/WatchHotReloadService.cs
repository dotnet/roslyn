﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Watch.Api;

internal sealed class WatchHotReloadService(SolutionServices services, Func<ValueTask<ImmutableArray<string>>> capabilitiesProvider)
{
    private sealed class DebuggerService(Func<ValueTask<ImmutableArray<string>>> capabilitiesProvider) : IManagedHotReloadService
    {
        public ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(ImmutableArray<ManagedActiveStatementDebugInfo>.Empty);

        public ValueTask<ManagedHotReloadAvailability> GetAvailabilityAsync(Guid module, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(new ManagedHotReloadAvailability(ManagedHotReloadAvailabilityStatus.Available));

        public ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
            => capabilitiesProvider();

        public ValueTask PrepareModuleForUpdateAsync(Guid module, CancellationToken cancellationToken)
            => ValueTaskFactory.CompletedTask;
    }

    public readonly struct Update(
        Guid moduleId,
        ImmutableArray<byte> ilDelta,
        ImmutableArray<byte> metadataDelta,
        ImmutableArray<byte> pdbDelta,
        ImmutableArray<int> updatedTypes,
        ImmutableArray<string> requiredCapabilities)
    {
        public readonly Guid ModuleId = moduleId;
        public readonly ImmutableArray<byte> ILDelta = ilDelta;
        public readonly ImmutableArray<byte> MetadataDelta = metadataDelta;
        public readonly ImmutableArray<byte> PdbDelta = pdbDelta;
        public readonly ImmutableArray<int> UpdatedTypes = updatedTypes;
        public readonly ImmutableArray<string> RequiredCapabilities = requiredCapabilities;
    }

    private static readonly ActiveStatementSpanProvider s_solutionActiveStatementSpanProvider =
        (_, _, _) => ValueTaskFactory.FromResult(ImmutableArray<ActiveStatementSpan>.Empty);

    private readonly IEditAndContinueService _encService = services.GetRequiredService<IEditAndContinueWorkspaceService>().Service;

    private DebuggingSessionId _sessionId;

    public WatchHotReloadService(HostWorkspaceServices services, ImmutableArray<string> capabilities)
        : this(services.SolutionServices, () => ValueTaskFactory.FromResult(AddImplicitDotNetCapabilities(capabilities)))
    {
    }

    /// <summary>
    /// Adds capabilities that are available by default on runtimes supported by dotnet-watch: .NET and Mono
    /// and not on .NET Framework (they are not in <see cref="EditAndContinueCapabilities.Baseline"/>.
    /// </summary>
    private static ImmutableArray<string> AddImplicitDotNetCapabilities(ImmutableArray<string> capabilities)
        => capabilities.Add(nameof(EditAndContinueCapabilities.AddExplicitInterfaceImplementation));

    /// <summary>
    /// Starts the watcher.
    /// </summary>
    /// <param name="solution">Solution that represents sources that match the built binaries on disk.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task StartSessionAsync(Solution solution, CancellationToken cancellationToken)
    {
        var newSessionId = await _encService.StartDebuggingSessionAsync(
            solution,
            new DebuggerService(capabilitiesProvider),
            NullPdbMatchingSourceTextProvider.Instance,
            captureMatchingDocuments: [],
            captureAllMatchingDocuments: true,
            reportDiagnostics: false,
            cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfFalse(_sessionId == default, "Session already started");
        _sessionId = newSessionId;
    }

    /// <summary>
    /// Invoke when capabilities have changed.
    /// </summary>
    public void CapabilitiesChanged()
    {
        var sessionId = _sessionId;
        Contract.ThrowIfFalse(sessionId != default, "Session has not started");

        _encService.BreakStateOrCapabilitiesChanged(sessionId, inBreakState: null);
    }

    /// <summary>
    /// Emits updates for all projects that differ between the given <paramref name="solution"/> snapshot and the one given to the previous successful call or
    /// the one passed to <see cref="StartSessionAsync(Solution, CancellationToken)"/> for the first invocation.
    /// </summary>
    /// <param name="solution">Solution snapshot.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Updates (one for each changed project) and Rude Edit diagnostics. Does not include syntax or semantic diagnostics.
    /// </returns>
    public async Task<(ImmutableArray<Update> updates, ImmutableArray<Diagnostic> diagnostics)> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken)
    {
        var sessionId = _sessionId;
        Contract.ThrowIfFalse(sessionId != default, "Session has not started");

        var results = await _encService.EmitSolutionUpdateAsync(sessionId, solution, s_solutionActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false);

        if (results.ModuleUpdates.Status == ModuleUpdateStatus.Ready)
        {
            _encService.CommitSolutionUpdate(sessionId);
        }

        var updates = results.ModuleUpdates.Updates.SelectAsArray(
            update => new Update(update.Module, update.ILDelta, update.MetadataDelta, update.PdbDelta, update.UpdatedTypes, update.RequiredCapabilities));

        var diagnostics = await results.GetAllDiagnosticsAsync(solution, cancellationToken).ConfigureAwait(false);

        return (updates, diagnostics);
    }

    public void EndSession()
    {
        Contract.ThrowIfFalse(_sessionId != default, "Session has not started");
        _encService.EndDebuggingSession(_sessionId);
        _sessionId = default;
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor
    {
        private readonly WatchHotReloadService _instance;

        internal TestAccessor(WatchHotReloadService instance)
            => _instance = instance;

        public DebuggingSessionId SessionId
            => _instance._sessionId;
    }
}
