// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
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
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Microsoft.VisualStudio.Debugger.Symbols;
using Roslyn.Utilities;

using Dbg = Microsoft.VisualStudio.Debugger.UI.Interfaces;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    [Shared]
    [Export(typeof(Dbg.IEditAndContinueManagedModuleUpdateProvider))]
    [Export(typeof(Dbg.IManagedActiveStatementTracker))]
    [Export(typeof(Dbg.IDebugStateChangeListener))]
    [ExportMetadata("UIContext", Guids.EncCapableProjectExistsInWorkspaceUIContextString)]
    internal sealed class ManagedEditAndContinueLanguageService : Dbg.IEditAndContinueManagedModuleUpdateProvider, Dbg.IManagedActiveStatementTracker, Dbg.IDebugStateChangeListener
    {
        private sealed class DebuggerService : IManagedEditAndContinueDebuggerService
        {
            private readonly Dbg.IManagedModuleInfoProvider _managedModuleInfoProvider;
            private readonly Dbg.IManagedActiveStatementProvider _activeStatementProvider;

            public DebuggerService(Dbg.IManagedModuleInfoProvider managedModuleInfoProvider, Dbg.IManagedActiveStatementProvider activeStatementProvider)
            {
                _managedModuleInfoProvider = managedModuleInfoProvider;
                _activeStatementProvider = activeStatementProvider;
            }

            public async Task<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
            {
                var infos = await _activeStatementProvider.GetActiveStatementsAsync(cancellationToken).ConfigureAwait(false);
                return infos.SelectAsArray(ModuleUtilities.ToActiveStatementDebugInfo);
            }

            public async Task<ManagedEditAndContinueAvailability> GetAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
            {
                var availability = await _managedModuleInfoProvider.GetEncAvailability(mvid, cancellationToken).ConfigureAwait(false);
                return new ManagedEditAndContinueAvailability((ManagedEditAndContinueAvailabilityStatus)availability.Status, availability.LocalizedMessage);
            }

            public Task PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken)
            {
                _managedModuleInfoProvider.PrepareModuleForUpdate(mvid, cancellationToken);
                return Task.CompletedTask;
            }
        }

        private readonly RemoteEditAndContinueServiceProxy _proxy;
        private readonly IDebuggingWorkspaceService _debuggingService;
        private readonly IActiveStatementTrackingService _activeStatementTrackingService;
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly EditAndContinueDiagnosticUpdateSource _diagnosticUpdateSource;
        private readonly Dbg.IManagedModuleInfoProvider _managedModuleInfoProvider;

        private IDisposable? _editSessionConnection;

        private bool _disabled;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ManagedEditAndContinueLanguageService(
            VisualStudioWorkspace workspace,
            Dbg.IManagedModuleInfoProvider managedModuleInfoProvider,
            IDiagnosticAnalyzerService diagnosticService,
            EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource)
        {
            _proxy = new RemoteEditAndContinueServiceProxy(workspace);
            _debuggingService = workspace.Services.GetRequiredService<IDebuggingWorkspaceService>();
            _activeStatementTrackingService = workspace.Services.GetRequiredService<IActiveStatementTrackingService>();
            _diagnosticService = diagnosticService;
            _diagnosticUpdateSource = diagnosticUpdateSource;
            _managedModuleInfoProvider = managedModuleInfoProvider;
        }

#pragma warning disable VSTHRD102 // TODO: Implement internal logic asynchronously
        public void StartDebugging(Dbg.DebugSessionOptions options)
            => Shell.ThreadHelper.JoinableTaskFactory.Run(() => StartDebuggingAsync(options, CancellationToken.None));

        public void EnterBreakState(Dbg.IManagedActiveStatementProvider activeStatementProvider)
            => Shell.ThreadHelper.JoinableTaskFactory.Run(() => EnterBreakStateAsync(activeStatementProvider, CancellationToken.None));

        public void ExitBreakState()
            => Shell.ThreadHelper.JoinableTaskFactory.Run(() => ExitBreakStateAsync(CancellationToken.None));

        public void StopDebugging()
            => Shell.ThreadHelper.JoinableTaskFactory.Run(() => StopDebuggingAsync(CancellationToken.None));

        public void CommitUpdates()
            => Shell.ThreadHelper.JoinableTaskFactory.Run(() => CommitUpdatesAsync(CancellationToken.None));

        public void DiscardUpdates()
            => Shell.ThreadHelper.JoinableTaskFactory.Run(() => DiscardUpdatesAsync(CancellationToken.None));
#pragma warning restore

        /// <summary>
        /// Called by the debugger when a debugging session starts and managed debugging is being used.
        /// </summary>
        public async Task StartDebuggingAsync(Dbg.DebugSessionOptions options, CancellationToken cancellationToken)
        {
            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Design, DebuggingState.Run);
            _disabled = (options & Dbg.DebugSessionOptions.EditAndContinueDisabled) != 0;

            if (_disabled)
            {
                return;
            }

            try
            {
                var solution = _proxy.Workspace.CurrentSolution;
                await _proxy.StartDebuggingSessionAsync(solution, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
            {
                _disabled = true;
            }
        }

        public async Task EnterBreakStateAsync(Dbg.IManagedActiveStatementProvider activeStatementProvider, CancellationToken cancellationToken)
        {
            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Run, DebuggingState.Break);

            if (_disabled)
            {
                return;
            }

            try
            {
                var debuggerService = new DebuggerService(_managedModuleInfoProvider, activeStatementProvider);
                _editSessionConnection = await _proxy.StartEditSessionAsync(_diagnosticService, debuggerService, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
            {
                _disabled = true;
            }

            _activeStatementTrackingService.StartTracking();
        }

        public async Task ExitBreakStateAsync(CancellationToken cancellationToken)
        {
            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Break, DebuggingState.Run);

            if (_disabled)
            {
                return;
            }

            Contract.ThrowIfNull(_editSessionConnection);
            _editSessionConnection.Dispose();
            _editSessionConnection = null;

            _activeStatementTrackingService.EndTracking();

            try
            {
                await _proxy.EndEditSessionAsync(_diagnosticService, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
            {
                _disabled = true;
            }
        }

        public async Task CommitUpdatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                Contract.ThrowIfTrue(_disabled);
                await _proxy.CommitSolutionUpdateAsync(cancellationToken).ConfigureAwait(false);
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
                await _proxy.DiscardSolutionUpdateAsync(cancellationToken).ConfigureAwait(false);
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
                await _proxy.EndDebuggingSessionAsync(_diagnosticUpdateSource, _diagnosticService, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
            {
                _disabled = true;
            }
        }

        private SolutionActiveStatementSpanProvider GetActiveStatementSpanProvider(Solution solution)
           => new SolutionActiveStatementSpanProvider((documentId, cancellationToken) =>
               _activeStatementTrackingService.GetSpansAsync(solution.GetRequiredDocument(documentId), cancellationToken));

        /// <summary>
        /// Returns true if any changes have been made to the source since the last changes had been applied.
        /// </summary>
        public async Task<bool> HasChangesAsync(string sourceFilePath, CancellationToken cancellationToken)
        {
            try
            {
                var solution = _proxy.Workspace.CurrentSolution;
                var activeStatementSpanProvider = GetActiveStatementSpanProvider(solution);
                return await _proxy.HasChangesAsync(solution, activeStatementSpanProvider, sourceFilePath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
            {
                return true;
            }
        }

        public async Task<Dbg.ManagedModuleUpdates> GetManagedModuleUpdatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                var solution = _proxy.Workspace.CurrentSolution;
                var activeStatementSpanProvider = GetActiveStatementSpanProvider(solution);
                var updates = await _proxy.EmitSolutionUpdateAsync(solution, activeStatementSpanProvider, _diagnosticUpdateSource, cancellationToken).ConfigureAwait(false);
                return new Dbg.ManagedModuleUpdates(updates.Status.ToModuleUpdateStatus(), updates.Updates.SelectAsArray(ModuleUtilities.ToModuleUpdate).ToReadOnlyCollection());
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
            {
                return new Dbg.ManagedModuleUpdates(Dbg.ManagedModuleUpdateStatus.Blocked, new ReadOnlyCollection<DkmManagedModuleUpdate>(Array.Empty<DkmManagedModuleUpdate>()));
            }
        }

        public async Task<DkmTextSpan?> GetCurrentActiveStatementPositionAsync(Guid moduleId, int methodToken, int methodVersion, int ilOffset, CancellationToken cancellationToken)
        {
            try
            {
                var solution = _proxy.Workspace.CurrentSolution;

                var activeStatementSpanProvider = new SolutionActiveStatementSpanProvider(async (documentId, cancellationToken) =>
                {
                    var document = solution.GetRequiredDocument(documentId);
                    return await _activeStatementTrackingService.GetSpansAsync(document, cancellationToken).ConfigureAwait(false);
                });

                var instructionId = new ManagedInstructionId(new ManagedMethodId(moduleId, new ManagedModuleMethodId(methodToken, methodVersion)), ilOffset);
                var span = await _proxy.GetCurrentActiveStatementPositionAsync(solution, activeStatementSpanProvider, instructionId, cancellationToken).ConfigureAwait(false);
                return span?.ToSourceSpan().ToDebuggerSpan();
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
            {
                return null;
            }
        }

        public async Task<bool?> IsActiveStatementInExceptionRegionAsync(Guid moduleId, int methodToken, int methodVersion, int ilOffset, CancellationToken cancellationToken)
        {
            try
            {
                var instructionId = new ManagedInstructionId(new ManagedMethodId(moduleId, methodToken, methodVersion), ilOffset);
                return await _proxy.IsActiveStatementInExceptionRegionAsync(instructionId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
            {
                return null;
            }
        }
    }
}
