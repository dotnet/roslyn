// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

[Shared]
[Export(typeof(IManagedHotReloadLanguageService))]
[Export(typeof(IEditAndContinueSolutionProvider))]
[Export(typeof(EditAndContinueLanguageService))]
[ExportMetadata("UIContext", EditAndContinueUIContext.EncCapableProjectExistsInWorkspaceUIContextString)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class EditAndContinueLanguageService(
    IServiceBrokerProvider serviceBrokerProvider,
    Lazy<IHostWorkspaceProvider> workspaceProvider,
    Lazy<IManagedHotReloadService> debuggerService,
    IDiagnosticAnalyzerService diagnosticService,
    EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource,
    PdbMatchingSourceTextProvider sourceTextProvider,
    IDiagnosticsRefresher diagnosticRefresher,
    IAsynchronousOperationListenerProvider listenerProvider) : IManagedHotReloadLanguageService, IEditAndContinueSolutionProvider
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

    private readonly PdbMatchingSourceTextProvider _sourceTextProvider = sourceTextProvider;
    private readonly IDiagnosticsRefresher _diagnosticRefresher = diagnosticRefresher;
    private readonly IAsynchronousOperationListener _asyncListener = listenerProvider.GetListener(FeatureAttribute.EditAndContinue);
    private readonly Lazy<IManagedHotReloadService> _debuggerService = debuggerService;
    private readonly IDiagnosticAnalyzerService _diagnosticService = diagnosticService;
    private readonly EditAndContinueDiagnosticUpdateSource _diagnosticUpdateSource = diagnosticUpdateSource;
    private readonly HotReloadLoggerProxy _logger = new(serviceBrokerProvider.ServiceBroker);

    public readonly Lazy<IHostWorkspaceProvider> WorkspaceProvider = workspaceProvider;

    public bool IsSessionActive { get; private set; }

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
                var proxy = new RemoteEditAndContinueServiceProxy(WorkspaceProvider.Value.Workspace);
                await proxy.SetFileLoggingDirectoryAsync(logDirectory, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        });
    }

    private Solution GetCurrentCompileTimeSolution(Solution? currentDesignTimeSolution = null)
    {
        var workspace = WorkspaceProvider.Value.Workspace;
        return workspace.Services.GetRequiredService<ICompileTimeSolutionProvider>().GetCompileTimeSolution(currentDesignTimeSolution ?? workspace.CurrentSolution);
    }

    private RemoteDebuggingSessionProxy GetDebuggingSession()
        => _debuggingSession ?? throw new NoSessionException();

    private IActiveStatementTrackingService GetActiveStatementTrackingService()
        => WorkspaceProvider.Value.Workspace.Services.GetRequiredService<IActiveStatementTrackingService>();

    internal void Disable(Exception e)
    {
        _disabled = true;

        var token = _asyncListener.BeginAsyncOperation(nameof(EditAndContinueLanguageService) + ".LogToOutput");

        _ = _logger.LogAsync(new HotReloadLogMessage(HotReloadVerbosity.Diagnostic, e.ToString(), errorLevel: HotReloadDiagnosticErrorLevel.Error), CancellationToken.None).AsTask()
            .ReportNonFatalErrorAsync().CompletesAsyncOperation(token);
    }

    /// <summary>
    /// Called by the debugger when a debugging session starts and managed debugging is being used.
    /// </summary>
    public async ValueTask StartSessionAsync(CancellationToken cancellationToken)
    {
        IsSessionActive = true;

        if (_disabled)
        {
            return;
        }

        try
        {
            // Activate listener before capturing the current solution snapshot,
            // so that we don't miss any pertinent workspace update events.
            _sourceTextProvider.Activate();

            var workspace = WorkspaceProvider.Value.Workspace;
            var currentSolution = workspace.CurrentSolution;
            _committedDesignTimeSolution = currentSolution;
            var solution = GetCurrentCompileTimeSolution(currentSolution);

            _sourceTextProvider.SetBaseline(currentSolution);

            var proxy = new RemoteEditAndContinueServiceProxy(workspace);

            _debuggingSession = await proxy.StartDebuggingSessionAsync(
                solution,
                new ManagedHotReloadServiceBridge(_debuggerService.Value),
                _sourceTextProvider,
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

    public async ValueTask EnterBreakStateAsync(CancellationToken cancellationToken)
    {
        if (_disabled)
        {
            return;
        }

        var solution = GetCurrentCompileTimeSolution();
        var session = GetDebuggingSession();

        try
        {
            await session.BreakStateOrCapabilitiesChangedAsync(_diagnosticService, _diagnosticUpdateSource, inBreakState: true, cancellationToken).ConfigureAwait(false);

            _diagnosticRefresher.RequestWorkspaceRefresh();
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            Disable(e);
            return;
        }

        // Start tracking after we entered break state so that break-state session is active.
        // This is potentially costly operation as source generators might get invoked in OOP
        // to determine the spans of all active statements.
        // We start the operation but do not wait for it to complete.
        // The tracking session is cancelled when we exit the break state.

        GetActiveStatementTrackingService().StartTracking(solution, session);
    }

    public async ValueTask ExitBreakStateAsync(CancellationToken cancellationToken)
    {
        if (_disabled)
        {
            return;
        }

        var session = GetDebuggingSession();

        try
        {
            await session.BreakStateOrCapabilitiesChangedAsync(_diagnosticService, _diagnosticUpdateSource, inBreakState: false, cancellationToken).ConfigureAwait(false);

            _diagnosticRefresher.RequestWorkspaceRefresh();
            GetActiveStatementTrackingService().EndTracking();
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            Disable(e);
            return;
        }
    }

    public async ValueTask OnCapabilitiesChangedAsync(CancellationToken cancellationToken)
    {
        if (_disabled)
        {
            return;
        }

        try
        {
            await GetDebuggingSession().BreakStateOrCapabilitiesChangedAsync(_diagnosticService, _diagnosticUpdateSource, inBreakState: null, cancellationToken).ConfigureAwait(false);

            _diagnosticRefresher.RequestWorkspaceRefresh();
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            Disable(e);
        }
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
            await GetDebuggingSession().CommitSolutionUpdateAsync(_diagnosticService, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
        }
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

    public async ValueTask EndSessionAsync(CancellationToken cancellationToken)
    {
        IsSessionActive = false;

        if (!_disabled)
        {
            try
            {
                var solution = GetCurrentCompileTimeSolution();
                await GetDebuggingSession().EndDebuggingSessionAsync(solution, _diagnosticUpdateSource, _diagnosticService, cancellationToken).ConfigureAwait(false);

                _diagnosticRefresher.RequestWorkspaceRefresh();
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                Disable(e);
            }
        }

        _sourceTextProvider.Deactivate();
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
            var newSolution = WorkspaceProvider.Value.Workspace.CurrentSolution;

            return (sourceFilePath != null)
                ? await EditSession.HasChangesAsync(oldSolution, newSolution, sourceFilePath, cancellationToken).ConfigureAwait(false)
                : await EditSession.HasChangesAsync(oldSolution, newSolution, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            return true;
        }
    }

    public async ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(CancellationToken cancellationToken)
    {
        if (_disabled)
        {
            return new ManagedHotReloadUpdates([], []);
        }

        var workspace = WorkspaceProvider.Value.Workspace;
        var designTimeSolution = workspace.CurrentSolution;
        var solution = GetCurrentCompileTimeSolution(designTimeSolution);
        var activeStatementSpanProvider = GetActiveStatementSpanProvider(solution);
        var (moduleUpdates, diagnosticData, rudeEdits, syntaxError) = await GetDebuggingSession().EmitSolutionUpdateAsync(solution, activeStatementSpanProvider, _diagnosticService, _diagnosticUpdateSource, cancellationToken).ConfigureAwait(false);

        // Only store the solution if we have any changes to apply, otherwise CommitUpdatesAsync/DiscardUpdatesAsync won't be called.
        if (moduleUpdates.Status == ModuleUpdateStatus.Ready)
        {
            _pendingUpdatedDesignTimeSolution = designTimeSolution;
        }

        _diagnosticRefresher.RequestWorkspaceRefresh();

        var diagnostics = await EmitSolutionUpdateResults.GetHotReloadDiagnosticsAsync(solution, diagnosticData, rudeEdits, syntaxError, moduleUpdates.Status, cancellationToken).ConfigureAwait(false);
        return new ManagedHotReloadUpdates(moduleUpdates.Updates.FromContract(), diagnostics.FromContract());
    }
}
