﻿// Licensed to the .NET Foundation under one or more agreements.
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

    public readonly struct RunningProjectInfo
    {
        public required bool RestartWhenChangesHaveNoEffect { get; init; }
    }

    [Obsolete("Use Updates2")]
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
        [Obsolete("Use ProjectIdsToRestart")]
        public IReadOnlySet<Project> ProjectsToRestart { get; } = projectsToRestart;

        /// <summary>
        /// Projects with changes that need to be rebuilt in order to apply changes.
        /// </summary>
        [Obsolete("Use ProjectIdsToRebuild")]
        public IReadOnlySet<Project> ProjectsToRebuild { get; } = projectsToRebuild;

        /// <summary>
        /// Running projects that need to be restarted due to rude edits in order to apply changes.
        /// </summary>
        public ImmutableArray<ProjectId> ProjectIdsToRestart { get; } = projectsToRestart.SelectAsArray(p => p.Id);

        /// <summary>
        /// Projects with changes that need to be rebuilt in order to apply changes.
        /// </summary>
        public ImmutableArray<ProjectId> ProjectIdsToRebuild { get; } = projectsToRebuild.SelectAsArray(p => p.Id);
    }

    public enum Status
    {
        /// <summary>
        /// No significant changes made that need to be applied.
        /// </summary>
        NoChangesToApply,

        /// <summary>
        /// Changes can be applied either via updates or restart.
        /// </summary>
        ReadyToApply,

        /// <summary>
        /// Some changes are errors that block rebuild of the module.
        /// This means that the code is in a broken state that cannot be resolved by restarting the application.
        /// </summary>
        Blocked,
    }

    public readonly struct Updates2
    {
        /// <summary>
        /// Status of the updates.
        /// </summary>
        public readonly Status Status { get; init; }

        /// <summary>
        /// Syntactic, semantic and emit diagnostics.
        /// </summary>
        /// <remarks>
        /// <see cref="Status"/> is <see cref="Status.Blocked"/> if these diagnostics contain any errors.
        /// </remarks>
        public required ImmutableArray<Diagnostic> CompilationDiagnostics { get; init; }

        /// <summary>
        /// Rude edits per project.
        /// </summary>
        public required ImmutableArray<(ProjectId project, ImmutableArray<Diagnostic> diagnostics)> RudeEdits { get; init; }

        /// <summary>
        /// Updates to be applied to modules. Empty if there are blocking rude edits.
        /// Only updates to projects that are not included in <see cref="ProjectsToRebuild"/> are listed.
        /// </summary>
        public ImmutableArray<Update> ProjectUpdates { get; init; }

        /// <summary>
        /// Running projects that need to be restarted due to rude edits in order to apply changes.
        /// </summary>
        public ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>> ProjectsToRestart { get; init; }

        /// <summary>
        /// Projects with changes that need to be rebuilt in order to apply changes.
        /// </summary>
        public ImmutableArray<ProjectId> ProjectsToRebuild { get; init; }
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

    /// <summary>
    /// Returns TFM of a given project.
    /// </summary>
    public static string? GetTargetFramework(Project project)
        => project.State.NameAndFlavor.flavor;

    [Obsolete]
    public async Task<Updates> GetUpdatesAsync(Solution solution, IImmutableSet<ProjectId> runningProjects, CancellationToken cancellationToken)
    {
        var sessionId = GetDebuggingSession();

        var runningProjectsImpl = runningProjects.ToImmutableDictionary(keySelector: p => p, elementSelector: _ => new EditAndContinue.RunningProjectInfo()
        {
            RestartWhenChangesHaveNoEffect = false,
            AllowPartialUpdate = false
        });

        var results = await _encService.EmitSolutionUpdateAsync(sessionId, solution, runningProjectsImpl, s_solutionActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false);

        // If the changes fail to apply dotnet-watch fails.
        // We don't support discarding the changes and letting the user retry.
        if (!results.ModuleUpdates.Updates.IsEmpty)
        {
            _encService.CommitSolutionUpdate(sessionId);
        }

        var diagnostics = results.GetAllDiagnostics();

        var projectUpdates =
            from update in results.ModuleUpdates.Updates
            let project = solution.GetRequiredProject(update.ProjectId)
            where !results.ProjectsToRestart.ContainsKey(project.Id)
            select new Update(
                update.Module,
                project.Id,
                update.ILDelta,
                update.MetadataDelta,
                update.PdbDelta,
                update.UpdatedTypes,
                update.RequiredCapabilities);

        return new Updates(
            results.ModuleUpdates.Status,
            diagnostics,
            [.. projectUpdates],
            results.ProjectsToRestart.Keys.Select(solution.GetRequiredProject).ToImmutableHashSet(),
            results.ProjectsToRebuild.Select(solution.GetRequiredProject).ToImmutableHashSet());
    }

    /// <summary>
    /// Emits updates for all projects that differ between the given <paramref name="solution"/> snapshot and the one given to the previous successful call or
    /// the one passed to <see cref="StartSessionAsync(Solution, CancellationToken)"/> for the first invocation.
    /// </summary>
    /// <param name="solution">Solution snapshot.</param>
    /// <param name="runningProjects">Identifies projects that launched a process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Updates (one for each changed project) and Rude Edit diagnostics. Does not include syntax or semantic diagnostics.
    /// May include both updates and Rude Edits for different projects.
    /// </returns>
    public async Task<Updates2> GetUpdatesAsync(Solution solution, ImmutableDictionary<ProjectId, RunningProjectInfo> runningProjects, CancellationToken cancellationToken)
    {
        var sessionId = GetDebuggingSession();

        var runningProjectsImpl = runningProjects.ToImmutableDictionary(
            static e => e.Key,
            static e => new EditAndContinue.RunningProjectInfo()
            {
                RestartWhenChangesHaveNoEffect = e.Value.RestartWhenChangesHaveNoEffect,
                AllowPartialUpdate = true
            });

        var results = await _encService.EmitSolutionUpdateAsync(sessionId, solution, runningProjectsImpl, s_solutionActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false);

        // If the changes fail to apply dotnet-watch fails.
        // We don't support discarding the changes and letting the user retry.
        if (!results.ModuleUpdates.Updates.IsEmpty)
        {
            _encService.CommitSolutionUpdate(sessionId);
        }

        return new Updates2
        {
            Status = results.ModuleUpdates.Status switch
            {
                ModuleUpdateStatus.None => Status.NoChangesToApply,
                ModuleUpdateStatus.Ready or ModuleUpdateStatus.RestartRequired => Status.ReadyToApply,
                ModuleUpdateStatus.Blocked => Status.Blocked,
                _ => throw ExceptionUtilities.UnexpectedValue(results.ModuleUpdates.Status)
            },
            CompilationDiagnostics = results.GetAllCompilationDiagnostics(),
            RudeEdits = results.RudeEdits.SelectAsArray(static re => (re.ProjectId, re.Diagnostics)),
            ProjectUpdates = results.ModuleUpdates.Updates.SelectAsArray(static update => new Update(
                update.Module,
                update.ProjectId,
                update.ILDelta,
                update.MetadataDelta,
                update.PdbDelta,
                update.UpdatedTypes,
                update.RequiredCapabilities)),
            ProjectsToRestart = results.ProjectsToRestart,
            ProjectsToRebuild = results.ProjectsToRebuild
        };
    }

    public void UpdateBaselines(Solution solution, ImmutableArray<ProjectId> projectIds)
    {
        var sessionId = GetDebuggingSession();
        _encService.UpdateBaselines(sessionId, solution, projectIds);
    }

    public void EndSession()
    {
        _encService.EndDebuggingSession(GetDebuggingSession());
        _sessionId = default;
    }

    // access to internal API:
    public static Solution WithProjectInfo(Solution solution, ProjectInfo info)
        => solution.WithProjectInfo(info);

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor(WatchHotReloadService instance)
    {
        public DebuggingSessionId SessionId
            => instance._sessionId;

        public IEditAndContinueService EncService
            => instance._encService;
    }
}
#endif
