// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

[Export(typeof(IManagedHotReloadLanguageService))]
[Export(typeof(IManagedHotReloadLanguageService2))]
[Export(typeof(IManagedHotReloadLanguageService3))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class ManagedHotReloadLanguageService(
    IServiceBrokerProvider serviceBrokerProvider,
    IEditAndContinueService encService,
    SolutionSnapshotRegistry solutionSnapshotRegistry) : IManagedHotReloadLanguageService3
{
    private sealed class PdbMatchingSourceTextProvider : IPdbMatchingSourceTextProvider
    {
        public static readonly PdbMatchingSourceTextProvider Instance = new();

        // Returning null will check the file on disk:
        public async ValueTask<string?> TryGetMatchingSourceTextAsync(string filePath, ImmutableArray<byte> requiredChecksum, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken)
            => null;
    }

    private static readonly ActiveStatementSpanProvider s_emptyActiveStatementProvider =
        async (_, _, _) => ImmutableArray<ActiveStatementSpan>.Empty;

    private readonly ManagedHotReloadServiceProxy _debuggerService = new(serviceBrokerProvider.ServiceBroker);
    private readonly SolutionSnapshotProviderProxy _solutionSnapshotProvider = new(serviceBrokerProvider.ServiceBroker);

    private bool _disabled;
    private DebuggingSessionId? _debuggingSession;
    private Solution? _committedSolution;
    private Solution? _pendingUpdatedSolution;

    private void Disable()
    {
        _disabled = true;
        _debuggingSession = null;
        _committedSolution = null;
        _pendingUpdatedSolution = null;
        solutionSnapshotRegistry.Clear();
    }

    private async ValueTask<Solution> GetCurrentSolutionAsync(CancellationToken cancellationToken)
    {
        // First, calls to the client to get the current snapshot id.
        // The client service calls the LSP client, which sends message to the LSP server, which in turn calls back to RegisterSolutionSnapshot.
        // Once complete the snapshot should be registered.
        var id = await _solutionSnapshotProvider.RegisterSolutionSnapshotAsync(cancellationToken).ConfigureAwait(false);

        return solutionSnapshotRegistry.GetRegisteredSolutionSnapshot(id);
    }

    public async ValueTask StartSessionAsync(CancellationToken cancellationToken)
    {
        if (_disabled)
        {
            return;
        }

        try
        {
            var currentSolution = await GetCurrentSolutionAsync(cancellationToken).ConfigureAwait(false);
            _committedSolution = currentSolution;

            // TODO: use remote proxy once we transition to pull diagnostics
            _debuggingSession = encService.StartDebuggingSession(
                currentSolution,
                _debuggerService,
                PdbMatchingSourceTextProvider.Instance,
                reportDiagnostics: true);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            // the service failed, error has been reported - disable further operations
            Disable();
        }
    }

    private async ValueTask BreakStateOrCapabilitiesChangedAsync(bool? inBreakState, CancellationToken cancellationToken)
    {
        if (_disabled)
        {
            return;
        }

        try
        {
            Contract.ThrowIfNull(_debuggingSession);
            encService.BreakStateOrCapabilitiesChanged(_debuggingSession.Value, inBreakState);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            Disable();
        }
    }

    public ValueTask EnterBreakStateAsync(CancellationToken cancellationToken)
        => BreakStateOrCapabilitiesChangedAsync(inBreakState: true, cancellationToken);

    public ValueTask ExitBreakStateAsync(CancellationToken cancellationToken)
        => BreakStateOrCapabilitiesChangedAsync(inBreakState: false, cancellationToken);

    public ValueTask OnCapabilitiesChangedAsync(CancellationToken cancellationToken)
        => BreakStateOrCapabilitiesChangedAsync(inBreakState: null, cancellationToken);

    public async ValueTask CommitUpdatesAsync(CancellationToken cancellationToken)
    {
        if (_disabled)
        {
            return;
        }

        try
        {
            Contract.ThrowIfNull(_debuggingSession);
            var committedSolution = Interlocked.Exchange(ref _pendingUpdatedSolution, null);
            Contract.ThrowIfNull(committedSolution);

            _committedSolution = committedSolution;

            encService.CommitSolutionUpdate(_debuggingSession.Value);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            Disable();
        }
    }

    [Obsolete]
    public ValueTask UpdateBaselinesAsync(ImmutableArray<string> projectPaths, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public async ValueTask DiscardUpdatesAsync(CancellationToken cancellationToken)
    {
        if (_disabled)
        {
            return;
        }

        try
        {
            Contract.ThrowIfNull(_debuggingSession);
            Contract.ThrowIfNull(Interlocked.Exchange(ref _pendingUpdatedSolution, null));

            encService.DiscardSolutionUpdate(_debuggingSession.Value);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            Disable();
        }
    }

    public async ValueTask EndSessionAsync(CancellationToken cancellationToken)
    {
        if (_disabled)
        {
            return;
        }

        try
        {
            Contract.ThrowIfNull(_debuggingSession);

            encService.EndDebuggingSession(_debuggingSession.Value);

            _debuggingSession = null;
            _committedSolution = null;
            _pendingUpdatedSolution = null;
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            Disable();
        }
    }

    /// <summary>
    /// Returns true if any changes have been made to the source since the last changes had been applied.
    /// For performance reasons it only implements a heuristic and may return both false positives and false negatives.
    /// If the result is a false negative the debugger will not apply the changes unless the user explicitly triggers apply change command.
    /// The background diagnostic analysis will still report rude edits for these ignored changes. It may also happen that these rude edits 
    /// will disappear once the debuggee is resumed - if they are caused by presence of active statements around the change.
    /// If the result is a false positive the debugger attempts to apply the changes, which will result in a delay but will correctly end up
    /// with no actual deltas to be applied.
    /// 
    /// If <paramref name="sourceFilePath"/> is specified checks for changes only in a document of the given path.
    /// This is not supported (returns false) for source-generated documents.
    /// </summary>
    public async ValueTask<bool> HasChangesAsync(string? sourceFilePath, CancellationToken cancellationToken)
    {
        try
        {
            var debuggingSession = _debuggingSession;
            if (debuggingSession == null)
            {
                return false;
            }

            Contract.ThrowIfNull(_committedSolution);
            var oldSolution = _committedSolution;

            var newSolution = await GetCurrentSolutionAsync(cancellationToken).ConfigureAwait(false);

            return (sourceFilePath != null)
                ? await EditSession.HasChangesAsync(oldSolution, newSolution, sourceFilePath, cancellationToken).ConfigureAwait(false)
                : await EditSession.HasChangesAsync(oldSolution, newSolution, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            return true;
        }
    }

    [Obsolete]
    public ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException();

    [Obsolete]
    public ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(ImmutableArray<string> runningProjects, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public async ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(ImmutableArray<RunningProjectInfo> runningProjects, CancellationToken cancellationToken)
    {
        if (_disabled)
        {
            return new ManagedHotReloadUpdates([], [], [], []);
        }

        try
        {
            Contract.ThrowIfNull(_debuggingSession);

            var solution = await GetCurrentSolutionAsync(cancellationToken).ConfigureAwait(false);
            var runningProjectOptions = runningProjects.ToRunningProjectOptions(solution, static info => (info.ProjectInstanceId.ProjectFilePath, info.ProjectInstanceId.TargetFramework, info.RestartAutomatically));

            EmitSolutionUpdateResults.Data results;

            try
            {
                results = (await encService.EmitSolutionUpdateAsync(_debuggingSession.Value, solution, runningProjectOptions, s_emptyActiveStatementProvider, cancellationToken).ConfigureAwait(false)).Dehydrate();
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                results = EmitSolutionUpdateResults.Data.CreateFromInternalError(solution, e.Message, runningProjectOptions);
            }

            // Only store the solution if we have any changes to apply, otherwise CommitUpdatesAsync/DiscardUpdatesAsync won't be called.
            if (results.ModuleUpdates.Status == ModuleUpdateStatus.Ready)
            {
                _pendingUpdatedSolution = solution;
            }

            return new ManagedHotReloadUpdates(
                results.ModuleUpdates.Updates,
                results.GetAllDiagnostics(),
                ToProjectIntanceIds(results.ProjectsToRebuild),
                ToProjectIntanceIds(results.ProjectsToRestart.Keys));

            ImmutableArray<ProjectInstanceId> ToProjectIntanceIds(IEnumerable<ProjectId> ids)
                => ids.SelectAsArray(id =>
                {
                    var project = solution.GetRequiredProject(id);
                    return new ProjectInstanceId(project.FilePath!, project.State.NameAndFlavor.flavor ?? "");
                });
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            Disable();
            return new ManagedHotReloadUpdates([], [], [], []);
        }
    }
}
