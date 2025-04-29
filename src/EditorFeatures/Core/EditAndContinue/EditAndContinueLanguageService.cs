// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

[Shared]
[Export(typeof(IManagedHotReloadLanguageService))]
[Export(typeof(IManagedHotReloadLanguageService2))]
[Export(typeof(IEditAndContinueSolutionProvider))]
[Export(typeof(EditAndContinueLanguageService))]
[ExportMetadata("UIContext", EditAndContinueUIContext.EncCapableProjectExistsInWorkspaceUIContextString)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class EditAndContinueLanguageService(
    EditAndContinueSessionState sessionState,
    Lazy<IHostWorkspaceProvider> workspaceProvider,
    Lazy<IManagedHotReloadService> debuggerService,
    PdbMatchingSourceTextProvider sourceTextProvider,
    IEditAndContinueLogReporter logReporter,
    IDiagnosticsRefresher diagnosticRefresher) : IManagedHotReloadLanguageService2, IEditAndContinueSolutionProvider
{
    private sealed class NoSessionException : InvalidOperationException
    {
        public NoSessionException()
            : base("Internal error: no session.")
        {
            // unique enough HResult to distinguish from other exceptions
            HResult = unchecked((int)0x801315087);
        }
    }

    private bool _disabled;
    private RemoteDebuggingSessionProxy? _debuggingSession;

    private Solution? _pendingUpdatedDesignTimeSolution;
    private Solution? _committedDesignTimeSolution;

    public event Action<Solution>? SolutionCommitted;

    public void SetFileLoggingDirectory(string? logDirectory)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var proxy = new RemoteEditAndContinueServiceProxy(Services);
                await proxy.SetFileLoggingDirectoryAsync(logDirectory, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        });
    }

    private SolutionServices Services
        => workspaceProvider.Value.Workspace.Services.SolutionServices;

    private Solution GetCurrentDesignTimeSolution()
        => workspaceProvider.Value.Workspace.CurrentSolution;

    private Solution GetCurrentCompileTimeSolution(Solution currentDesignTimeSolution)
        => Services.GetRequiredService<ICompileTimeSolutionProvider>().GetCompileTimeSolution(currentDesignTimeSolution);

    private RemoteDebuggingSessionProxy GetDebuggingSession()
        => _debuggingSession ?? throw new NoSessionException();

    private IActiveStatementTrackingService GetActiveStatementTrackingService()
        => Services.GetRequiredService<IActiveStatementTrackingService>();

    internal void Disable(Exception e)
    {
        _disabled = true;
        logReporter.Report(e.ToString(), LogMessageSeverity.Error);
    }

    private void UpdateApplyChangesDiagnostics(ImmutableArray<DiagnosticData> diagnostics)
    {
        sessionState.ApplyChangesDiagnostics = diagnostics;
        diagnosticRefresher.RequestWorkspaceRefresh();
    }

    /// <summary>
    /// Called by the debugger when a debugging session starts and managed debugging is being used.
    /// </summary>
    public async ValueTask StartSessionAsync(CancellationToken cancellationToken)
    {
        sessionState.IsSessionActive = true;

        if (_disabled)
        {
            return;
        }

        try
        {
            // Activate listener before capturing the current solution snapshot,
            // so that we don't miss any pertinent workspace update events.
            sourceTextProvider.Activate();

            var currentSolution = GetCurrentDesignTimeSolution();
            _committedDesignTimeSolution = currentSolution;
            var solution = GetCurrentCompileTimeSolution(currentSolution);

            sourceTextProvider.SetBaseline(currentSolution);

            var proxy = new RemoteEditAndContinueServiceProxy(Services);

            _debuggingSession = await proxy.StartDebuggingSessionAsync(
                solution,
                new ManagedHotReloadServiceBridge(debuggerService.Value),
                sourceTextProvider,
                captureMatchingDocuments: [],
                captureAllMatchingDocuments: false,
                reportDiagnostics: true,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            // the service failed, error has been reported - disable further operations
            Disable(e);
        }
    }

    public ValueTask EnterBreakStateAsync(CancellationToken cancellationToken)
        => BreakStateOrCapabilitiesChangedAsync(inBreakState: true, cancellationToken);

    public ValueTask ExitBreakStateAsync(CancellationToken cancellationToken)
        => BreakStateOrCapabilitiesChangedAsync(inBreakState: false, cancellationToken);

    public ValueTask OnCapabilitiesChangedAsync(CancellationToken cancellationToken)
        => BreakStateOrCapabilitiesChangedAsync(inBreakState: null, cancellationToken);

    private async ValueTask BreakStateOrCapabilitiesChangedAsync(bool? inBreakState, CancellationToken cancellationToken)
    {
        if (_disabled)
        {
            return;
        }

        try
        {
            var session = GetDebuggingSession();
            var solution = (inBreakState == true) ? GetCurrentCompileTimeSolution(GetCurrentDesignTimeSolution()) : null;

            await session.BreakStateOrCapabilitiesChangedAsync(inBreakState, cancellationToken).ConfigureAwait(false);

            if (inBreakState == false)
            {
                GetActiveStatementTrackingService().EndTracking();
            }
            else if (inBreakState == true)
            {
                // Start tracking after we entered break state so that break-state session is active.
                // This is potentially costly operation as source generators might get invoked in OOP
                // to determine the spans of all active statements.
                // We start the operation but do not wait for it to complete.
                // The tracking session is cancelled when we exit the break state.

                Debug.Assert(solution != null);
                GetActiveStatementTrackingService().StartTracking(solution, session);
            }
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            Disable(e);
        }

        // clear diagnostics reported previously:
        UpdateApplyChangesDiagnostics([]);
    }

    public async ValueTask CommitUpdatesAsync(CancellationToken cancellationToken)
    {
        if (_disabled)
        {
            return;
        }

        var committedDesignTimeSolution = Interlocked.Exchange(ref _pendingUpdatedDesignTimeSolution, null);
        Contract.ThrowIfNull(committedDesignTimeSolution);

        try
        {
            SolutionCommitted?.Invoke(committedDesignTimeSolution);
        }
        catch (Exception e) when (FatalError.ReportAndCatch(e))
        {
        }

        _committedDesignTimeSolution = committedDesignTimeSolution;

        try
        {
            await GetDebuggingSession().CommitSolutionUpdateAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
        }

        workspaceProvider.Value.Workspace.EnqueueUpdateSourceGeneratorVersion(projectId: null, forceRegeneration: false);
    }

    public async ValueTask DiscardUpdatesAsync(CancellationToken cancellationToken)
    {
        if (_disabled)
        {
            return;
        }

        Contract.ThrowIfNull(Interlocked.Exchange(ref _pendingUpdatedDesignTimeSolution, null));

        try
        {
            await GetDebuggingSession().DiscardSolutionUpdateAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
        }
    }

    public async ValueTask UpdateBaselinesAsync(ImmutableArray<string> projectPaths, CancellationToken cancellationToken)
    {
        if (_disabled)
        {
            return;
        }

        var currentDesignTimeSolution = GetCurrentDesignTimeSolution();
        var currentCompileTimeSolution = GetCurrentCompileTimeSolution(currentDesignTimeSolution);

        try
        {
            SolutionCommitted?.Invoke(currentDesignTimeSolution);
        }
        catch (Exception e) when (FatalError.ReportAndCatch(e))
        {
        }

        _committedDesignTimeSolution = currentDesignTimeSolution;
        var projectIds = GetProjectIds(projectPaths, currentCompileTimeSolution);

        try
        {
            await GetDebuggingSession().UpdateBaselinesAsync(currentCompileTimeSolution, projectIds, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
        }

        foreach (var projectId in projectIds)
        {
            workspaceProvider.Value.Workspace.EnqueueUpdateSourceGeneratorVersion(projectId, forceRegeneration: false);
        }
    }

    private ImmutableArray<ProjectId> GetProjectIds(ImmutableArray<string> projectPaths, Solution solution)
    {
        using var _ = ArrayBuilder<ProjectId>.GetInstance(out var projectIds);
        foreach (var path in projectPaths)
        {
            var projectId = solution.Projects.FirstOrDefault(project => project.FilePath == path)?.Id;
            if (projectId != null)
            {
                projectIds.Add(projectId);
            }
            else
            {
                logReporter.Report($"Project with path '{path}' not found in the current solution.", LogMessageSeverity.Info);
            }
        }

        return projectIds.ToImmutable();
    }

    public async ValueTask EndSessionAsync(CancellationToken cancellationToken)
    {
        sessionState.IsSessionActive = false;

        if (!_disabled)
        {
            try
            {
                await GetDebuggingSession().EndDebuggingSessionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                Disable(e);
            }
        }

        // clear diagnostics reported previously:
        UpdateApplyChangesDiagnostics([]);

        sourceTextProvider.Deactivate();
        _debuggingSession = null;
        _committedDesignTimeSolution = null;
        _pendingUpdatedDesignTimeSolution = null;
    }

    private ActiveStatementSpanProvider GetActiveStatementSpanProvider(Solution solution)
    {
        var service = GetActiveStatementTrackingService();
        return new((documentId, filePath, cancellationToken) => service.GetSpansAsync(solution, documentId, filePath, cancellationToken));
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

            Contract.ThrowIfNull(_committedDesignTimeSolution);
            var oldSolution = _committedDesignTimeSolution;
            var newSolution = GetCurrentDesignTimeSolution();

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
        => GetUpdatesAsync(runningProjects: [], cancellationToken);

    public async ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(ImmutableArray<string> runningProjects, CancellationToken cancellationToken)
    {
        if (_disabled)
        {
            return new ManagedHotReloadUpdates([], []);
        }

        var designTimeSolution = GetCurrentDesignTimeSolution();
        var solution = GetCurrentCompileTimeSolution(designTimeSolution);
        var activeStatementSpanProvider = GetActiveStatementSpanProvider(solution);

        using var _ = PooledHashSet<string>.GetInstance(out var runningProjectPaths);
        runningProjectPaths.AddAll(runningProjects);

        // TODO: Update once implemented: https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2449700
        var runningProjectInfos = solution.Projects.Where(p => p.FilePath != null && runningProjectPaths.Contains(p.FilePath)).ToImmutableDictionary(
            keySelector: static p => p.Id,
            elementSelector: static p => new RunningProjectInfo { RestartWhenChangesHaveNoEffect = false, AllowPartialUpdate = false });

        var result = await GetDebuggingSession().EmitSolutionUpdateAsync(solution, runningProjectInfos, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);

        switch (result.ModuleUpdates.Status)
        {
            case ModuleUpdateStatus.Ready:
                // We have updates to be applied. The debugger will call Commit/Discard on the solution
                // based on whether the updates will be applied successfully or not.
                _pendingUpdatedDesignTimeSolution = designTimeSolution;
                break;

            case ModuleUpdateStatus.None:
                // No significant changes have been made.
                // Commit the solution to apply any changes in comments that do not generate updates.
                _committedDesignTimeSolution = designTimeSolution;
                break;
        }

        UpdateApplyChangesDiagnostics(result.Diagnostics);

        return new ManagedHotReloadUpdates(
            result.ModuleUpdates.Updates.FromContract(),
            result.GetAllDiagnostics().FromContract(),
            GetProjectPaths(result.ProjectsToRebuild),
            GetProjectPaths(result.ProjectsToRestart.Keys));

        ImmutableArray<string> GetProjectPaths(IEnumerable<ProjectId> ids)
            => ids.SelectAsArray(id => solution.GetRequiredProject(id).FilePath!);
    }
}
