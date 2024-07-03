// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

    public readonly struct Update
    {
        public readonly Guid ModuleId;
        public readonly ProjectId ProjectId;
        public readonly ImmutableArray<byte> ILDelta;
        public readonly ImmutableArray<byte> MetadataDelta;
        public readonly ImmutableArray<byte> PdbDelta;
        public readonly ImmutableArray<int> UpdatedTypes;
        public readonly ImmutableArray<string> RequiredCapabilities;

        internal Update(
            Guid moduleId,
            ProjectId projectId,
            ImmutableArray<byte> ilDelta,
            ImmutableArray<byte> metadataDelta,
            ImmutableArray<byte> pdbDelta,
            ImmutableArray<int> updatedTypes,
            ImmutableArray<string> requiredCapabilities)
        {
            ModuleId = moduleId;
            ProjectId = projectId;
            ILDelta = ilDelta;
            MetadataDelta = metadataDelta;
            PdbDelta = pdbDelta;
            UpdatedTypes = updatedTypes;
            RequiredCapabilities = requiredCapabilities;
        }
    }

    public readonly struct Updates(
        ModuleUpdateStatus status,
        ImmutableArray<Diagnostic> diagnostics,
        ImmutableArray<Update> projectUpdates,
        IReadOnlySet<Project> projectsToRestart,
        IReadOnlySet<Project> projectsToRebuild)
    {
        /// <summary>
        /// Status of the updates.
        /// </summary>
        public readonly ModuleUpdateStatus Status { get; } = status;

        /// <summary>
        /// Hot Reload specific diagnostics to be reported (includes rude edits and emit errors).
        /// </summary>
        public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;

        /// <summary>
        /// Updates to be applied to modules. Empty if there are blocking rude edits.
        /// Only updates to projects that are not included in <see cref="ProjectsToRebuild"/> are listed.
        /// </summary>
        public ImmutableArray<Update> ProjectUpdates { get; } = projectUpdates;

        /// <summary>
        /// Running projects that need to be restarted due to rude edits in order to apply changes.
        /// </summary>
        public IReadOnlySet<Project> ProjectsToRestart { get; } = projectsToRestart;

        /// <summary>
        /// Projects with changes that need to be rebuilt in order to apply changes.
        /// </summary>
        public IReadOnlySet<Project> ProjectsToRebuild { get; } = projectsToRebuild;
    }

    private static readonly ActiveStatementSpanProvider s_solutionActiveStatementSpanProvider =
        (_, _, _) => ValueTaskFactory.FromResult(ImmutableArray<ActiveStatementSpan>.Empty);

    private readonly IEditAndContinueService _encService = services.GetRequiredService<IEditAndContinueWorkspaceService>().Service;

    private DebuggingSessionId _sessionId;

    public WatchHotReloadService(HostWorkspaceServices services, ImmutableArray<string> capabilities)
        : this(services.SolutionServices, () => ValueTaskFactory.FromResult(AddImplicitDotNetCapabilities(capabilities)))
    {
    }

    private DebuggingSessionId GetDebuggingSession()
    {
        var sessionId = _sessionId;
        Contract.ThrowIfFalse(sessionId != default, "Session has not started");
        return sessionId;
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
        _encService.BreakStateOrCapabilitiesChanged(GetDebuggingSession(), inBreakState: null);
    }

    public async Task<(ImmutableArray<Update> updates, ImmutableArray<Diagnostic> diagnostics)> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken)
    {
        var result = await GetUpdatesAsync(solution, isRunningProject: static _ => false, cancellationToken).ConfigureAwait(false);
        return (result.ProjectUpdates, result.Diagnostics);
    }

    /// <summary>
    /// Emits updates for all projects that differ between the given <paramref name="solution"/> snapshot and the one given to the previous successful call or
    /// the one passed to <see cref="StartSessionAsync(Solution, CancellationToken)"/> for the first invocation.
    /// </summary>
    /// <param name="solution">Solution snapshot.</param>
    /// <param name="isRunningProject">Identifies projects that launched a process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Updates (one for each changed project) and Rude Edit diagnostics. Does not include syntax or semantic diagnostics.
    /// </returns>
    public async Task<Updates> GetUpdatesAsync(Solution solution, Func<Project, bool> isRunningProject, CancellationToken cancellationToken)
    {
        var sessionId = GetDebuggingSession();

        var results = await _encService.EmitSolutionUpdateAsync(sessionId, solution, s_solutionActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false);

        if (results.ModuleUpdates.Status == ModuleUpdateStatus.Ready)
        {
            _encService.CommitSolutionUpdate(sessionId);
        }

        var diagnostics = await results.GetAllDiagnosticsAsync(solution, cancellationToken).ConfigureAwait(false);

        var projectsToRestart = new HashSet<Project>();
        var projectsToRebuild = new HashSet<Project>();
        results.GetProjectsToRebuildAndRestart(solution, isRunningProject, projectsToRestart, projectsToRebuild);

        var projectUpdates =
            from update in results.ModuleUpdates.Updates
            let project = solution.GetRequiredProject(update.ProjectId)
            where !projectsToRestart.Contains(project)
            select new Update(
                update.Module,
                project.Id,
                update.ILDelta,
                update.MetadataDelta,
                update.PdbDelta,
                update.UpdatedTypes,
                update.RequiredCapabilities);

        return new Updates(results.ModuleUpdates.Status, diagnostics, projectUpdates.ToImmutableArray(), projectsToRestart, projectsToRebuild);
    }

    public void EndSession()
    {
        _encService.EndDebuggingSession(GetDebuggingSession());
        _sessionId = default;
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor(WatchHotReloadService instance)
    {
        public DebuggingSessionId SessionId
            => instance._sessionId;
    }
}
#endif
