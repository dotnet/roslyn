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
        private readonly Dbg.IManagedModuleInfoProvider _managedModuleInfoProvider;

        private RemoteServiceConnection? _editSessionConnection;

        private bool _disabled;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioDebugStateChangeListener(VisualStudioWorkspace workspace, Dbg.IManagedModuleInfoProvider managedModuleInfoProvider)
        {
            _proxy = new RemoteEditAndContinueServiceProxy(workspace);
            _debuggingService = workspace.Services.GetRequiredService<IDebuggingWorkspaceService>();
            _activeStatementTrackingService = workspace.Services.GetRequiredService<IActiveStatementTrackingService>();
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
                    new StartEditSessionCallback(activeStatementProvider, _managedModuleInfoProvider),
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
                await _proxy.EndEditSessionAsync(cancellationToken).ConfigureAwait(false);
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
                await _proxy.EndDebuggingSessionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                _disabled = true;
            }
        }

        private sealed class StartEditSessionCallback : IRemoteEditAndContinueService.IStartEditSessionCallback
        {
            private readonly Dbg.IManagedActiveStatementProvider _activeStatementProvider;
            private readonly Dbg.IManagedModuleInfoProvider _managedModuleInfoProvider;

            public StartEditSessionCallback(Dbg.IManagedActiveStatementProvider activeStatementProvider, Dbg.IManagedModuleInfoProvider managedModuleInfoProvider)
            {
                _activeStatementProvider = activeStatementProvider;
                _managedModuleInfoProvider = managedModuleInfoProvider;
            }

            /// <summary>
            /// Remote API.
            /// </summary>
            public async Task<ImmutableArray<ActiveStatementDebugInfo.Data>> GetActiveStatementsAsync(CancellationToken cancellationToken)
            {
                try
                {
                    var infos = await _activeStatementProvider.GetActiveStatementsAsync(cancellationToken).ConfigureAwait(false);
                    return infos.SelectAsArray(ModuleUtilities.ToActiveStatementDebugInfoData);
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                    return ImmutableArray<ActiveStatementDebugInfo.Data>.Empty;
                }
            }

            /// <summary>
            /// Remote API.
            /// </summary>
            public async Task<(int errorCode, string? errorMessage)?> GetEncAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
            {
                try
                {
                    var availability = await _managedModuleInfoProvider.GetEncAvailability(mvid, cancellationToken).ConfigureAwait(false);
                    return availability.Status switch
                    {
                        DkmEncAvailableStatus.Available => (0, null),
                        DkmEncAvailableStatus.ModuleNotLoaded => null,
                        _ => ((int)availability.Status, availability.LocalizedMessage)
                    };
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                    // TODO: better error code
                    return ((int)DkmEncAvailableStatus.EngineMetricFalse, e.Message);
                }
            }

            /// <summary>
            /// Remote API.
            /// </summary>
            public Task PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken)
            {
                try
                {
                    return _managedModuleInfoProvider.PrepareModuleForUpdate(mvid, cancellationToken);
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                    return Task.CompletedTask;
                }
            }
        }
    }
}
