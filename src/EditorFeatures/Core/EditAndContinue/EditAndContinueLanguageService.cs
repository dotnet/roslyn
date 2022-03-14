// Licensed to the .NET Foundation under one or more agreements.
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
        private static readonly ActiveStatementSpanProvider s_noActiveStatementSpanProvider =
            (_, _, _) => ValueTaskFactory.FromResult(ImmutableArray<ActiveStatementSpan>.Empty);

        private readonly Lazy<IManagedHotReloadService> _debuggerService;
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly EditAndContinueDiagnosticUpdateSource _diagnosticUpdateSource;

        public readonly Lazy<IHostWorkspaceProvider> WorkspaceProvider;

        public bool IsSessionActive { get; private set; }

        private bool _disabled;
        private RemoteDebuggingSessionProxy? _debuggingSession;

        private Solution? _pendingUpdatedSolution;
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
            EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource)
        {
            WorkspaceProvider = workspaceProvider;
            _debuggerService = debuggerService;
            _diagnosticService = diagnosticService;
            _diagnosticUpdateSource = diagnosticUpdateSource;
        }

        private Solution GetCurrentCompileTimeSolution()
        {
            var workspace = WorkspaceProvider.Value.Workspace;
            return workspace.Services.GetRequiredService<ICompileTimeSolutionProvider>().GetCompileTimeSolution(workspace.CurrentSolution);
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
                var workspace = WorkspaceProvider.Value.Workspace;
                var solution = GetCurrentCompileTimeSolution();
                var openedDocumentIds = workspace.GetOpenDocumentIds().ToImmutableArray();
                var proxy = new RemoteEditAndContinueServiceProxy(workspace);

                _debuggingSession = await proxy.StartDebuggingSessionAsync(
                    solution,
                    new ManagedHotReloadServiceImpl(_debuggerService.Value),
                    captureMatchingDocuments: openedDocumentIds,
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
            try
            {
                var committedSolution = Interlocked.Exchange(ref _pendingUpdatedSolution, null);
                Contract.ThrowIfNull(committedSolution);
                SolutionCommitted?.Invoke(committedSolution);
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
            }

            try
            {
                Contract.ThrowIfTrue(_disabled);
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

            _pendingUpdatedSolution = null;

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

            if (_disabled)
            {
                return;
            }

            try
            {
                var solution = GetCurrentCompileTimeSolution();
                await GetDebuggingSession().EndDebuggingSessionAsync(solution, _diagnosticUpdateSource, _diagnosticService, cancellationToken).ConfigureAwait(false);
                _debuggingSession = null;
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                _disabled = true;
            }
        }

        private ActiveStatementSpanProvider GetActiveStatementSpanProvider(Solution solution)
        {
            var service = GetActiveStatementTrackingService();
            return new((documentId, filePath, cancellationToken) => service.GetSpansAsync(solution, documentId, filePath, cancellationToken));
        }

        /// <summary>
        /// Returns true if any changes have been made to the source since the last changes had been applied.
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

                var solution = GetCurrentCompileTimeSolution();
                var activeStatementSpanProvider = GetActiveStatementSpanProvider(solution);
                return await debuggingSession.HasChangesAsync(solution, activeStatementSpanProvider, sourceFilePath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return true;
            }
        }

        public async ValueTask<ManagedModuleUpdates> GetEditAndContinueUpdatesAsync(CancellationToken cancellationToken)
        {
            if (_disabled)
            {
                return new ManagedModuleUpdates(ManagedModuleUpdateStatus.None, ImmutableArray<ManagedModuleUpdate>.Empty);
            }

            var solution = GetCurrentCompileTimeSolution();
            var activeStatementSpanProvider = GetActiveStatementSpanProvider(solution);
            var (updates, _, _, _) = await GetDebuggingSession().EmitSolutionUpdateAsync(solution, activeStatementSpanProvider, _diagnosticService, _diagnosticUpdateSource, cancellationToken).ConfigureAwait(false);
            _pendingUpdatedSolution = solution;
            return updates.FromContract();
        }

        public async ValueTask<ManagedHotReloadUpdates> GetHotReloadUpdatesAsync(CancellationToken cancellationToken)
        {
            if (_disabled)
            {
                return new ManagedHotReloadUpdates(ImmutableArray<ManagedHotReloadUpdate>.Empty, ImmutableArray<ManagedHotReloadDiagnostic>.Empty);
            }

            var solution = GetCurrentCompileTimeSolution();
            var (moduleUpdates, diagnosticData, rudeEdits, syntaxError) = await GetDebuggingSession().EmitSolutionUpdateAsync(solution, s_noActiveStatementSpanProvider, _diagnosticService, _diagnosticUpdateSource, cancellationToken).ConfigureAwait(false);
            _pendingUpdatedSolution = solution;

            var updates = moduleUpdates.Updates.SelectAsArray(
                update => new ManagedHotReloadUpdate(update.Module, update.ILDelta, update.MetadataDelta, update.PdbDelta, update.UpdatedTypes));

            var diagnostics = await EmitSolutionUpdateResults.GetHotReloadDiagnosticsAsync(solution, diagnosticData, rudeEdits, syntaxError, cancellationToken).ConfigureAwait(false);

            return new ManagedHotReloadUpdates(updates, diagnostics.FromContract());
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
