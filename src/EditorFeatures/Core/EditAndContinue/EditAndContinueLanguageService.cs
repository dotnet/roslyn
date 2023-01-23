﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [Shared]
    [Export(typeof(IManagedHotReloadLanguageService))]
    [Export(typeof(IEditAndContinueSolutionProvider))]
    [Export(typeof(EditAndContinueLanguageService))]
    [ExportMetadata("UIContext", EditAndContinueUIContext.EncCapableProjectExistsInWorkspaceUIContextString)]
    internal sealed class EditAndContinueLanguageService : IManagedHotReloadLanguageService, IEditAndContinueSolutionProvider
    {
        private readonly PdbMatchingSourceTextProvider _sourceTextProvider;
        private readonly Lazy<IManagedHotReloadService> _debuggerService;
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly EditAndContinueDiagnosticUpdateSource _diagnosticUpdateSource;

        public readonly Lazy<IHostWorkspaceProvider> WorkspaceProvider;

        public bool IsSessionActive { get; private set; }

        private bool _disabled;
        private RemoteDebuggingSessionProxy? _debuggingSession;

        private Solution? _pendingUpdatedDesignTimeSolution;
        private Solution? _committedDesignTimeSolution;

        public event Action<Solution>? SolutionCommitted;

        /// <summary>
        /// Import <see cref="IHostWorkspaceProvider"/> lazily so that the host does not need to implement it 
        /// unless the host implements debugger components.
        /// </summary>
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditAndContinueLanguageService(
            Lazy<IHostWorkspaceProvider> workspaceProvider,
            Lazy<IManagedHotReloadService> debuggerService,
            IDiagnosticAnalyzerService diagnosticService,
            EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource,
            PdbMatchingSourceTextProvider sourceTextProvider)
        {
            WorkspaceProvider = workspaceProvider;
            _debuggerService = debuggerService;
            _diagnosticService = diagnosticService;
            _diagnosticUpdateSource = diagnosticUpdateSource;
            _sourceTextProvider = sourceTextProvider;
        }

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
        {
            var debuggingSession = _debuggingSession;
            Contract.ThrowIfNull(debuggingSession);
            return debuggingSession;
        }

        private IActiveStatementTrackingService GetActiveStatementTrackingService()
            => WorkspaceProvider.Value.Workspace.Services.GetRequiredService<IActiveStatementTrackingService>();

        internal void Disable()
            => _disabled = true;

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
                    new ManagedHotReloadServiceImpl(_debuggerService.Value),
                    _sourceTextProvider,
                    captureMatchingDocuments: ImmutableArray<DocumentId>.Empty,
                    captureAllMatchingDocuments: false,
                    reportDiagnostics: true,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
            }

            // the service failed, error has been reported - disable further operations
            _disabled = _debuggingSession == null;
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
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                _disabled = true;
                return;
            }

            // Start tracking after we entered break state so that break-state session is active.
            // This is potentially costly operation but entering break state is non-blocking so it should be ok to await.
            await GetActiveStatementTrackingService().StartTrackingAsync(solution, session, cancellationToken).ConfigureAwait(false);
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
                GetActiveStatementTrackingService().EndTracking();
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                _disabled = true;
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
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                _disabled = true;
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
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                {
                    _disabled = true;
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
                return new ManagedHotReloadUpdates(ImmutableArray<ManagedHotReloadUpdate>.Empty, ImmutableArray<ManagedHotReloadDiagnostic>.Empty);
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

            var diagnostics = await EmitSolutionUpdateResults.GetHotReloadDiagnosticsAsync(solution, diagnosticData, rudeEdits, syntaxError, moduleUpdates.Status, cancellationToken).ConfigureAwait(false);
            return new ManagedHotReloadUpdates(moduleUpdates.Updates.FromContract(), diagnostics.FromContract());
        }

        public async ValueTask<SourceSpan?> GetCurrentActiveStatementPositionAsync(ManagedInstructionId instruction, CancellationToken cancellationToken)
        {
            try
            {
                var solution = GetCurrentCompileTimeSolution();
                var activeStatementTrackingService = GetActiveStatementTrackingService();

                var activeStatementSpanProvider = new ActiveStatementSpanProvider((documentId, filePath, cancellationToken) =>
                    activeStatementTrackingService.GetSpansAsync(solution, documentId, filePath, cancellationToken));

                var span = await GetDebuggingSession().GetCurrentActiveStatementPositionAsync(solution, activeStatementSpanProvider, instruction.ToContract(), cancellationToken).ConfigureAwait(false);
                return span?.ToSourceSpan().FromContract();
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return null;
            }
        }

        public async ValueTask<bool?> IsActiveStatementInExceptionRegionAsync(ManagedInstructionId instruction, CancellationToken cancellationToken)
        {
            try
            {
                var solution = GetCurrentCompileTimeSolution();
                return await GetDebuggingSession().IsActiveStatementInExceptionRegionAsync(solution, instruction.ToContract(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return null;
            }
        }
    }
}
