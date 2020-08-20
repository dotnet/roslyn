// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
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
using Microsoft.VisualStudio.Debugger.Clr;
using Roslyn.Utilities;

using Dbg = Microsoft.VisualStudio.Debugger.UI.Interfaces;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    [Export(typeof(Dbg.IDebugStateChangeListener))]
    [ExportMetadata("UIContext", Guids.EncCapableProjectExistsInWorkspaceUIContextString)]
    internal sealed class VisualStudioDebugStateChangeListener : Dbg.IDebugStateChangeListener
    {
        private readonly RemoteEditAndContinueServiceProxy _proxy;
        private readonly IDebuggingWorkspaceService _debuggingService;
        private readonly IActiveStatementTrackingService _activeStatementTrackingService;
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly EditAndContinueDiagnosticUpdateSource _diagnosticUpdateSource;
        private readonly DebuggeeModuleMetadataProvider _managedModuleInfoProvider;

        private IDisposable? _editSessionConnection;

        private bool _disabled;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioDebugStateChangeListener(
            VisualStudioWorkspace workspace,
            Dbg.IManagedModuleInfoProvider managedModuleInfoProvider,
            IDiagnosticAnalyzerService diagnosticService,
            EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource)
        {
            _proxy = new RemoteEditAndContinueServiceProxy(workspace);
            _debuggingService = workspace.Services.GetRequiredService<IDebuggingWorkspaceService>();
            _activeStatementTrackingService = workspace.Services.GetRequiredService<IActiveStatementTrackingService>();
            _managedModuleInfoProvider = new DebuggeeModuleMetadataProvider(managedModuleInfoProvider);
            _diagnosticService = diagnosticService;
            _diagnosticUpdateSource = diagnosticUpdateSource;
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
                await _proxy.StartDebuggingSessionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
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
                _editSessionConnection = await _proxy.StartEditSessionAsync(
                    _diagnosticService,
                    activeStatementProvider: async cancellationToken =>
                    {
                        var infos = await activeStatementProvider.GetActiveStatementsAsync(cancellationToken).ConfigureAwait(false);
                        return infos.SelectAsArray(ModuleUtilities.ToActiveStatementDebugInfo);
                    },
                    debuggeeModuleMetadataProvider: _managedModuleInfoProvider,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
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
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                _disabled = true;
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
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                _disabled = true;
            }
        }

        private sealed class DebuggeeModuleMetadataProvider : IDebuggeeModuleMetadataProvider
        {
            private readonly Dbg.IManagedModuleInfoProvider _managedModuleInfoProvider;

            public DebuggeeModuleMetadataProvider(Dbg.IManagedModuleInfoProvider managedModuleInfoProvider)
            {
                _managedModuleInfoProvider = managedModuleInfoProvider;
            }

            public async Task<(int errorCode, string? errorMessage)?> GetEncAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
            {
                var availability = await _managedModuleInfoProvider.GetEncAvailability(mvid, cancellationToken).ConfigureAwait(false);
                return availability.Status switch
                {
                    DkmEncAvailableStatus.Available => (0, null),
                    DkmEncAvailableStatus.ModuleNotLoaded => null,
                    _ => ((int)availability.Status, availability.LocalizedMessage)
                };
            }

            public Task PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken)
            {
                _managedModuleInfoProvider.PrepareModuleForUpdate(mvid, cancellationToken);
                return Task.CompletedTask;
            }
        }
    }
}
