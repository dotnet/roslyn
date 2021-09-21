// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    internal sealed class EditAndContinueLanguageService : IEditAndContinueSolutionProvider
    {
        private static readonly ActiveStatementSpanProvider s_noActiveStatementSpanProvider =
            (_, _, _) => ValueTaskFactory.FromResult(ImmutableArray<ActiveStatementSpan>.Empty);

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
        public EditAndContinueLanguageService(
            Lazy<IHostWorkspaceProvider> workspaceProvider,
            IDiagnosticAnalyzerService diagnosticService,
            EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource)
        {
            WorkspaceProvider = workspaceProvider;
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
        public async ValueTask StartSessionAsync(IManagedEditAndContinueDebuggerService debugger, CancellationToken cancellationToken)
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
                    debugger,
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
                await session.BreakStateChangedAsync(_diagnosticService, inBreakState: true, cancellationToken).ConfigureAwait(false);
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
                await session.BreakStateChangedAsync(_diagnosticService, inBreakState: false, cancellationToken).ConfigureAwait(false);
                GetActiveStatementTrackingService().EndTracking();
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                _disabled = true;
                return;
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
            catch (Exception e) when (FatalError.ReportAndCatch(e))
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
            catch (Exception e) when (FatalError.ReportAndCatch(e))
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

        public async ValueTask<(ManagedModuleUpdates updates, ImmutableArray<DiagnosticData> diagnostics, ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> rudeEdits, DiagnosticData? syntaxError, Solution? solution)>
            GetUpdatesAsync(bool trackActiveStatements, CancellationToken cancellationToken)
        {
            if (_disabled)
            {
                return (new ManagedModuleUpdates(ManagedModuleUpdateStatus.None, ImmutableArray<ManagedModuleUpdate>.Empty),
                        ImmutableArray<DiagnosticData>.Empty,
                        ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)>.Empty,
                        syntaxError: null,
                        solution: null);
            }

            var solution = GetCurrentCompileTimeSolution();
            var activeStatementSpanProvider = trackActiveStatements ? GetActiveStatementSpanProvider(solution) : s_noActiveStatementSpanProvider;
            var (updates, diagnostics, rudeEdits, syntaxError) = await GetDebuggingSession().EmitSolutionUpdateAsync(solution, activeStatementSpanProvider, _diagnosticService, _diagnosticUpdateSource, cancellationToken).ConfigureAwait(false);
            _pendingUpdatedSolution = solution;
            return (updates, diagnostics, rudeEdits, syntaxError, solution);
        }

        public async Task<SourceSpan?> GetCurrentActiveStatementPositionAsync(ManagedInstructionId instruction, CancellationToken cancellationToken)
        {
            try
            {
                var solution = GetCurrentCompileTimeSolution();
                var activeStatementTrackingService = GetActiveStatementTrackingService();

                var activeStatementSpanProvider = new ActiveStatementSpanProvider((documentId, filePath, cancellationToken) =>
                    activeStatementTrackingService.GetSpansAsync(solution, documentId, filePath, cancellationToken));

                var span = await GetDebuggingSession().GetCurrentActiveStatementPositionAsync(solution, activeStatementSpanProvider, instruction, cancellationToken).ConfigureAwait(false);
                return span?.ToSourceSpan();
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return null;
            }
        }

        public async Task<bool?> IsActiveStatementInExceptionRegionAsync(ManagedInstructionId instruction, CancellationToken cancellationToken)
        {
            try
            {
                var solution = GetCurrentCompileTimeSolution();
                return await GetDebuggingSession().IsActiveStatementInExceptionRegionAsync(solution, instruction, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return null;
            }
        }
    }
}
