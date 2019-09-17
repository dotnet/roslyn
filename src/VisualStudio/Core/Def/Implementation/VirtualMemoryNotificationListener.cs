// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Runtime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using RemoteHostClientService = Microsoft.VisualStudio.LanguageServices.Remote.RemoteHostClientServiceFactory.RemoteHostClientService;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// Listens to broadcast notifications from the Visual Studio Shell indicating that the application is running
    /// low on available virtual memory.
    /// </summary>
    [Export, Shared]
    internal sealed class VirtualMemoryNotificationListener : ForegroundThreadAffinitizedObject, IVsBroadcastMessageEvents
    {
        // memory threshold to turn off full solution analysis - 200MB
        private const long MemoryThreshold = 200 * 1024 * 1024;

        // low vm more info page link
        private const string LowVMMoreInfoLink = "https://docs.microsoft.com/visualstudio/code-quality/automatic-feature-suspension";

        private readonly VisualStudioWorkspace _workspace;
        private readonly WorkspaceCacheService _workspaceCacheService;
        private readonly RemoteHostClientService _remoteHostService;

        private bool _alreadyLogged;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VirtualMemoryNotificationListener(
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspace workspace)
            : base(threadingContext, assertIsForeground: true)
        {
            _workspace = workspace;
            _workspaceCacheService = workspace.Services.GetService<IWorkspaceCacheService>() as WorkspaceCacheService;
            _remoteHostService = workspace.Services.GetService<IRemoteHostClientService>() as RemoteHostClientService;

            if (GCSettings.IsServerGC)
            {
                // Server GC has been explicitly enabled, which tends to run with higher memory pressure than the
                // default workstation GC. Allow this case without triggering frequent feature shutdown.
                return;
            }

            _workspace.WorkspaceChanged += OnWorkspaceChanged;

            var shell = (IVsShell)serviceProvider.GetService(typeof(SVsShell));
            // Note: We never unhook this event sink. It lives for the lifetime of the host.
            ErrorHandler.ThrowOnFailure(shell.AdviseBroadcastMessages(this, out var cookie));
        }

        /// <summary>
        /// Called by the Visual Studio Shell to notify components of a broadcast message.
        /// </summary>
        /// <param name="msg">The message identifier.</param>
        /// <param name="wParam">First parameter associated with the message.</param>
        /// <param name="lParam">Second parameter associated with the message.</param>
        /// <returns>S_OK always.</returns>
        public int OnBroadcastMessage(uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case VSConstants.VSM_VIRTUALMEMORYLOW:
                case VSConstants.VSM_VIRTUALMEMORYCRITICAL:
                    {
                        if (!_alreadyLogged)
                        {
                            // record that we had hit critical memory barrier
                            Logger.Log(FunctionId.VirtualMemory_MemoryLow, KeyValueLogMessage.Create(m =>
                            {
                                // which message we are logging and memory left in bytes when this is called.
                                m["MSG"] = msg;
                                m["MemoryLeft"] = (long)wParam;
                            }));

                            _alreadyLogged = true;
                        }

                        _workspaceCacheService?.FlushCaches();

                        if (ShouldDisableBackgroundAnalysis((long)wParam))
                        {
                            DisableBackgroundAnalysis();
                            ShowInfoBarIfRequired();
                        }

                        // turn off low latency GC mode.
                        // once we hit this, not hitting "Out of memory" exception is more important than typing being smooth all the time.
                        // once it is turned off, user will hit time to time keystroke which responsive time is more than 50ms. in our own perf lab,
                        // about 1-2% was over 50ms with this off when we first introduced this GC mode.
                        GCManager.TurnOffLowLatencyMode();

                        break;
                    }
            }

            return VSConstants.S_OK;
        }

        private bool ShouldDisableBackgroundAnalysis(long availableMemory)
        {
            // conditions
            // 1. Available memory is less than the threshold and 
            // 2. Power save mode is not already enabled and
            // 3. Background analysis memory monitor is on (user can set it off using registry to prevent turning off background analysis)

            return availableMemory < MemoryThreshold &&
                !ServiceFeatureOnOffOptions.IsPowerSaveModeEnabled(_workspace.Options) &&
                _workspace.Options.GetOption(InternalFeatureOnOffOptions.BackgroundAnalysisMemoryMonitor);
        }

        private void DisableBackgroundAnalysis()
        {
            // Turn on forced power save mode, which disables all background analysis for the current VS session.
            // Also disable the remote host service to reduce memory consumption.
            ServiceFeatureOnOffOptions.LowMemoryForcedPowerSaveMode = true;
            _remoteHostService?.Disable();
        }

        private void RenableBackgroundAnalysis()
        {
            // Turn off forced power save mode, which re-enables all background analysis for the current VS session.
            // Also re-enable the remote host service which was disabled earlier.
            ServiceFeatureOnOffOptions.LowMemoryForcedPowerSaveMode = false;
            _remoteHostService?.Enable();
        }

        private void ShowInfoBarIfRequired()
        {
            if (_workspace.Options.GetOption(RuntimeOptions.BackgroundAnalysisSuspendedInfoBarShown))
            {
                // Info bar already shown.
                return;
            }

            // Show info bar.
            _workspace.Services.GetService<IErrorReportingService>()
                .ShowGlobalErrorInfo(ServicesVSResources.Visual_Studio_has_suspended_some_advanced_features_to_improve_performance,
                    new InfoBarUI(ServicesVSResources.Re_enable, InfoBarUI.UIKind.Button, RenableBackgroundAnalysis),
                    new InfoBarUI(ServicesVSResources.Learn_more, InfoBarUI.UIKind.HyperLink,
                        () => BrowserHelper.StartBrowser(new Uri(LowVMMoreInfoLink)), closeAfterAction: false));

            // Update info bar shown state.
            _workspace.Options = _workspace.Options.WithChangedOption(RuntimeOptions.BackgroundAnalysisSuspendedInfoBarShown, true);
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind != WorkspaceChangeKind.SolutionAdded)
            {
                return;
            }

            // For newly opened solution, reset the info bar state.
            _workspace.Options = _workspace.Options.WithChangedOption(RuntimeOptions.BackgroundAnalysisSuspendedInfoBarShown, false);
        }
    }
}
