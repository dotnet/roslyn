// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// Listens to broadcast notifications from the Visual Studio Shell indicating that the application is running
    /// low on available virtual memory.
    /// </summary>
    internal sealed class VirtualMemoryNotificationListener : IVsBroadcastMessageEvents
    {
        // memory threshold to turn off full solution analysis - 200MB
        private const long MemoryThreshold = 200 * 1024 * 1024;

        // low vm more info page link
        private const string LowVMMoreInfoLink = "https://go.microsoft.com/fwlink/?LinkID=799402&clcid=0x409";
        private readonly IGlobalOptionService _globalOptions;
        private readonly VisualStudioWorkspace _workspace;
        private readonly WorkspaceCacheService? _workspaceCacheService;

        private bool _alreadyLogged;
        private bool _infoBarShown;

        private VirtualMemoryNotificationListener(
            IVsShell shell,
            IGlobalOptionService globalOptions,
            VisualStudioWorkspace workspace)
        {
            _globalOptions = globalOptions;
            _workspace = workspace;
            _workspaceCacheService = workspace.Services.GetService<IWorkspaceCacheService>() as WorkspaceCacheService;

            if (GCSettings.IsServerGC)
            {
                // Server GC has been explicitly enabled, which tends to run with higher memory pressure than the
                // default workstation GC. Allow this case without triggering frequent feature shutdown.
                return;
            }

            _workspace.WorkspaceChanged += OnWorkspaceChanged;

            // Note: We never unhook this event sink. It lives for the lifetime of the host.
            ErrorHandler.ThrowOnFailure(shell.AdviseBroadcastMessages(this, out var cookie));
        }

        public static async Task<VirtualMemoryNotificationListener> CreateAsync(
            VisualStudioWorkspace workspace,
            IThreadingContext threadingContext,
            IAsyncServiceProvider serviceProvider,
            IGlobalOptionService globalOptions,
            CancellationToken cancellationToken)
        {
            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var shell = (IVsShell?)await serviceProvider.GetServiceAsync(typeof(SVsShell)).ConfigureAwait(true);
            Assumes.Present(shell);

            return new VirtualMemoryNotificationListener(shell, globalOptions, workspace);
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
                case VSConstants.VSM_MEMORYHIGH:
                case VSConstants.VSM_MEMORYEXCESSIVE:
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
            // 2. Background analysis is not already minimal and
            // 3. Background analysis memory monitor is on (user can set it off using registry to prevent turning off background analysis)

            return availableMemory < MemoryThreshold &&
                !SolutionCrawlerOptionsStorage.LowMemoryForcedMinimalBackgroundAnalysis &&
                _globalOptions.GetOption(InternalFeatureOnOffOptions.BackgroundAnalysisMemoryMonitor);
        }

        private static void DisableBackgroundAnalysis()
        {
            // Force low VM minimal background analysis for the current VS session.
            SolutionCrawlerOptionsStorage.LowMemoryForcedMinimalBackgroundAnalysis = true;
        }

        private void RenableBackgroundAnalysis()
        {
            // Revert forced low VM minimal background analysis for the current VS session.
            SolutionCrawlerOptionsStorage.LowMemoryForcedMinimalBackgroundAnalysis = false;
        }

        private void ShowInfoBarIfRequired()
        {
            if (_infoBarShown)
            {
                return;
            }

            // Show info bar.
            _workspace.Services.GetRequiredService<IErrorReportingService>()
                .ShowGlobalErrorInfo(
                    message: ServicesVSResources.Visual_Studio_has_suspended_some_advanced_features_to_improve_performance,
                    TelemetryFeatureName.VirtualMemoryNotification,
                    exception: null,
                    new InfoBarUI(ServicesVSResources.Re_enable, InfoBarUI.UIKind.Button, RenableBackgroundAnalysis),
                    new InfoBarUI(ServicesVSResources.Learn_more, InfoBarUI.UIKind.HyperLink,
                        () => VisualStudioNavigateToLinkService.StartBrowser(new Uri(LowVMMoreInfoLink)), closeAfterAction: false));

            _infoBarShown = true;
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind != WorkspaceChangeKind.SolutionAdded)
            {
                return;
            }

            // For newly opened solution, reset the info bar state.
            _infoBarShown = false;
        }
    }
}
