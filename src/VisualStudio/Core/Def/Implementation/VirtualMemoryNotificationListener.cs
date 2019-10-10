// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Linq;
using System.Runtime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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
        private const string LowVMMoreInfoLink = "http://go.microsoft.com/fwlink/?LinkID=799402&clcid=0x409";

        private readonly VisualStudioWorkspace _workspace;
        private readonly WorkspaceCacheService _workspaceCacheService;

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

                        // turn off full solution analysis only if user option is on.
                        if (ShouldTurnOffFullSolutionAnalysis((long)wParam))
                        {
                            // turn our full solution analysis option off.
                            // if user full solution analysis option is on, then we will show info bar to users to restore it.
                            // if user full solution analysis option is off, then setting this doesn't matter. full solution analysis is already off.
                            _workspace.Options = _workspace.Options.WithChangedOption(RuntimeOptions.FullSolutionAnalysis, false);

                            if (IsUserOptionOn())
                            {
                                // let user know full analysis is turned off due to memory concern.
                                // make sure we show info bar only once for the same solution.
                                _workspace.Options = _workspace.Options.WithChangedOption(RuntimeOptions.FullSolutionAnalysisInfoBarShown, true);

                                _workspace.Services.GetService<IErrorReportingService>().ShowGlobalErrorInfo(ServicesVSResources.Visual_Studio_has_suspended_some_advanced_features_to_improve_performance,
                                    new InfoBarUI(ServicesVSResources.Re_enable, InfoBarUI.UIKind.Button, () =>
                                        _workspace.Options = _workspace.Options.WithChangedOption(RuntimeOptions.FullSolutionAnalysis, true)),
                                    new InfoBarUI(ServicesVSResources.Learn_more, InfoBarUI.UIKind.HyperLink, () =>
                                        BrowserHelper.StartBrowser(new Uri(LowVMMoreInfoLink)), closeAfterAction: false));
                            }
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

        private bool ShouldTurnOffFullSolutionAnalysis(long availableMemory)
        {
            // conditions
            // 1. if available memory is less than the threshold and 
            // 2. if full solution analysis memory monitor is on (user can set it off using registry, when he does, we will never show info bar) and
            // 3. if our full solution analysis option is on (not user full solution analysis option, but our internal one) and
            // 4. if infobar is never shown to users for this solution
            return availableMemory < MemoryThreshold &&
                  _workspace.Options.GetOption(InternalFeatureOnOffOptions.FullSolutionAnalysisMemoryMonitor) &&
                  _workspace.Options.GetOption(RuntimeOptions.FullSolutionAnalysis) &&
                  !_workspace.Options.GetOption(RuntimeOptions.FullSolutionAnalysisInfoBarShown);
        }

        private bool IsUserOptionOn()
        {
            // check languages currently on solution. since we only show info bar once, we don't need to track solution changes.
            var languages = _workspace.CurrentSolution.Projects.Select(p => p.Language).Distinct();
            foreach (var language in languages)
            {
                if (ServiceFeatureOnOffOptions.IsClosedFileDiagnosticsEnabled(_workspace.Options, language))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind != WorkspaceChangeKind.SolutionAdded)
            {
                return;
            }

            // first make sure full solution analysis is on. (not user options but our internal options. even if our option is on, if user option is off
            // full solution analysis won't run. also, reset infobar state.
            _workspace.Options = _workspace.Options.WithChangedOption(RuntimeOptions.FullSolutionAnalysisInfoBarShown, false)
                                                   .WithChangedOption(RuntimeOptions.FullSolutionAnalysis, true);
        }
    }
}
