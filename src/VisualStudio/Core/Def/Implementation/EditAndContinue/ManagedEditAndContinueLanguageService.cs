// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    [Shared]
    [Export(typeof(IManagedEditAndContinueLanguageService))]
    [ExportMetadata("UIContext", Guids.EncCapableProjectExistsInWorkspaceUIContextString)]
    internal sealed class ManagedEditAndContinueLanguageService : IManagedEditAndContinueLanguageService
    {
        private readonly RemoteEditAndContinueServiceProxy _proxy;
        private readonly IDebuggingWorkspaceService _debuggingService;
        private readonly IActiveStatementTrackingService _activeStatementTrackingService;
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly EditAndContinueDiagnosticUpdateSource _diagnosticUpdateSource;
        private readonly IManagedEditAndContinueDebuggerService _debuggerService;

        private RemoteDebuggingSessionProxy? _debuggingSession;

        private bool _disabled;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ManagedEditAndContinueLanguageService(
            VisualStudioWorkspace workspace,
            IManagedEditAndContinueDebuggerService debuggerService,
            IDiagnosticAnalyzerService diagnosticService,
            EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource)
        {
            _proxy = new RemoteEditAndContinueServiceProxy(workspace);
            _debuggingService = workspace.Services.GetRequiredService<IDebuggingWorkspaceService>();
            _activeStatementTrackingService = workspace.Services.GetRequiredService<IActiveStatementTrackingService>();
            _debuggerService = debuggerService;
            _diagnosticService = diagnosticService;
            _diagnosticUpdateSource = diagnosticUpdateSource;
        }

        private Solution GetCurrentCompileTimeSolution()
            => _proxy.Workspace.Services.GetRequiredService<ICompileTimeSolutionProvider>().GetCompileTimeSolution(_proxy.Workspace.CurrentSolution);

        private RemoteDebuggingSessionProxy GetDebuggingSession()
        {
            var debuggingSession = _debuggingSession;
            Contract.ThrowIfNull(debuggingSession);
            return debuggingSession;
        }

        /// <summary>
        /// Called by the debugger when a debugging session starts and managed debugging is being used.
        /// </summary>
        public async Task StartDebuggingAsync(DebugSessionFlags flags, CancellationToken cancellationToken)
        {
            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Design, DebuggingState.Run);
            _disabled = (flags & DebugSessionFlags.EditAndContinueDisabled) != 0;

            if (_disabled)
            {
                return;
            }

            try
            {
                var solution = GetCurrentCompileTimeSolution();
                var openedDocumentIds = _proxy.Workspace.GetOpenDocumentIds().ToImmutableArray();

                _debuggingSession = await _proxy.StartDebuggingSessionAsync(
                    solution,
                    _debuggerService,
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

        public async Task EnterBreakStateAsync(CancellationToken cancellationToken)
        {
            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Run, DebuggingState.Break);

            if (_disabled)
            {
                return;
            }

            var solution = GetCurrentCompileTimeSolution();
            var session = GetDebuggingSession();

            try
            {
                await session.BreakStateEnteredAsync(_diagnosticService, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                _disabled = true;
                return;
            }

            // Start tracking after we entered break state so that break-state session is active.
            // This is potentially costly operation but entering break state is non-blocking so it should be ok to await.
            await _activeStatementTrackingService.StartTrackingAsync(solution, session, cancellationToken).ConfigureAwait(false);
        }

        public Task ExitBreakStateAsync(CancellationToken cancellationToken)
        {
            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Break, DebuggingState.Run);

            if (!_disabled)
            {
                _activeStatementTrackingService.EndTracking();
            }

            return Task.CompletedTask;
        }

        public async Task CommitUpdatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                Contract.ThrowIfTrue(_disabled);
                await GetDebuggingSession().CommitSolutionUpdateAsync(_diagnosticService, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
            }
        }

        public async Task DiscardUpdatesAsync(CancellationToken cancellationToken)
        {
            if (_disabled)
            {
                return;
            }

            try
            {
                await GetDebuggingSession().DiscardSolutionUpdateAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
            }
        }

        public async Task StopDebuggingAsync(CancellationToken cancellationToken)
        {
            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Run, DebuggingState.Design);

            if (_disabled)
            {
                return;
            }

            try
            {
                await GetDebuggingSession().EndDebuggingSessionAsync(_diagnosticUpdateSource, _diagnosticService, cancellationToken).ConfigureAwait(false);
                _debuggingSession = null;
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                _disabled = true;
            }
        }

        private ActiveStatementSpanProvider GetActiveStatementSpanProvider(Solution solution)
           => new((documentId, filePath, cancellationToken) => _activeStatementTrackingService.GetSpansAsync(solution, documentId, filePath, cancellationToken));

        /// <summary>
        /// Returns true if any changes have been made to the source since the last changes had been applied.
        /// </summary>
        public async Task<bool> HasChangesAsync(string? sourceFilePath, CancellationToken cancellationToken)
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

        public async Task<ManagedModuleUpdates> GetManagedModuleUpdatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                var solution = GetCurrentCompileTimeSolution();
                var activeStatementSpanProvider = GetActiveStatementSpanProvider(solution);
                var (updates, _, _) = await GetDebuggingSession().EmitSolutionUpdateAsync(solution, activeStatementSpanProvider, _diagnosticService, _diagnosticUpdateSource, cancellationToken).ConfigureAwait(false);
                return updates;
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return new ManagedModuleUpdates(ManagedModuleUpdateStatus.Blocked, ImmutableArray<ManagedModuleUpdate>.Empty);
            }
        }

        public async Task<SourceSpan?> GetCurrentActiveStatementPositionAsync(ManagedInstructionId instruction, CancellationToken cancellationToken)
        {
            try
            {
                var solution = GetCurrentCompileTimeSolution();

                var activeStatementSpanProvider = new ActiveStatementSpanProvider((documentId, filePath, cancellationToken) =>
                    _activeStatementTrackingService.GetSpansAsync(solution, documentId, filePath, cancellationToken));

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
