// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Experimentation;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Experimentation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.PlatformUI.OleComponentSupport;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Experimentation
{
    /// <summary>
    /// Detects if keybindings have been messed up by ReSharper disable, and offers the user the ability
    /// to reset if so.
    /// </summary>
    /// <remarks>
    /// The only objects to hold permanent references to this object should be callbacks that are registered for in
    /// <see cref="InitializeCore"/>. No other external objects should hold a reference to this. Unless the user clicks
    /// 'Never show this again', this will persist for the life of the VS instance, and does not need to be manually disposed
    /// in that case.
    /// </remarks>
    /// <para>
    /// We've written this in a generic mechanism we can extend to any extension as we know of them,
    /// but at this time ReSharper is the only one we know of that has this behavior.
    /// If we find other extensions that do this in the future, we'll re-use this same mechanism
    /// </para>
    [Export(typeof(IExperiment))]
    internal sealed class KeybindingResetDetector : ForegroundThreadAffinitizedObject, IExperiment, IOleCommandTarget
    {
        // Flight info
        private const string InternalFlightName = "keybindgoldbarint";
        private const string ExternalFlightName = "keybindgoldbarext";
        private const string KeybindingsFwLink = "https://go.microsoft.com/fwlink/?linkid=864209";
        private const string ReSharperExtensionName = "ReSharper Ultimate";
        private const string ReSharperKeyboardMappingName = "ReSharper (Visual Studio)";
        private const string VSCodeKeyboardMappingName = "Visual Studio Code";

        // Resharper commands and package
        private const uint ResumeId = 707;
        private const uint SuspendId = 708;
        private const uint ToggleSuspendId = 709;
        private static readonly Guid ReSharperPackageGuid = new Guid("0C6E6407-13FC-4878-869A-C8B4016C57FE");
        private static readonly Guid ReSharperCommandGroup = new Guid("{47F03277-5055-4922-899C-0F7F30D26BF1}");

        private readonly VisualStudioWorkspace _workspace;
        private readonly System.IServiceProvider _serviceProvider;

        // All mutable fields are UI-thread affinitized

        private IExperimentationService _experimentationService;
        private IVsUIShell _uiShell;
        private IOleCommandTarget _oleCommandTarget;
        private OleComponent _oleComponent;
        private uint _priorityCommandTargetCookie = VSConstants.VSCOOKIE_NIL;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        /// <summary>
        /// If false, ReSharper is either not installed, or has been disabled in the extension manager.
        /// If true, the ReSharper extension is enabled. ReSharper's internal status could be either suspended or enabled.
        /// </summary>
        private bool _resharperExtensionInstalledAndEnabled = false;
        private bool _infoBarOpen = false;

        /// <summary>
        /// Chain all update tasks so that task runs serially
        /// </summary>
        private Task _lastTask = Task.CompletedTask;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public KeybindingResetDetector(IThreadingContext threadingContext, VisualStudioWorkspace workspace, SVsServiceProvider serviceProvider)
            : base(threadingContext)
        {
            _workspace = workspace;
            _serviceProvider = serviceProvider;
        }

        public Task InitializeAsync()
        {
            // Immediately bail if the user has asked to never see this bar again.
            if (_workspace.Options.GetOption(KeybindingResetOptions.NeverShowAgain))
            {
                return Task.CompletedTask;
            }

            return InvokeBelowInputPriorityAsync(InitializeCore);
        }

        private void InitializeCore()
        {
            AssertIsForeground();

            // Ensure one of the flights is enabled, otherwise bail
            _experimentationService = _workspace.Services.GetRequiredService<IExperimentationService>();
            if (!_experimentationService.IsExperimentEnabled(ExternalFlightName) && !_experimentationService.IsExperimentEnabled(InternalFlightName))
            {
                return;
            }

            var vsShell = _serviceProvider.GetService<IVsShell, SVsShell>();
            var hr = vsShell.IsPackageInstalled(ReSharperPackageGuid, out var extensionEnabled);
            if (ErrorHandler.Failed(hr))
            {
                FatalError.ReportWithoutCrash(Marshal.GetExceptionForHR(hr));
                return;
            }

            _resharperExtensionInstalledAndEnabled = extensionEnabled != 0;

            if (_resharperExtensionInstalledAndEnabled)
            {
                // We need to monitor for suspend/resume commands, so create and install the command target and the modal callback.
                var priorityCommandTargetRegistrar = _serviceProvider.GetService<IVsRegisterPriorityCommandTarget, SVsRegisterPriorityCommandTarget>();
                hr = priorityCommandTargetRegistrar.RegisterPriorityCommandTarget(
                    dwReserved: 0 /* from docs must be 0 */,
                    pCmdTrgt: this,
                    pdwCookie: out _priorityCommandTargetCookie);

                if (ErrorHandler.Failed(hr))
                {
                    FatalError.ReportWithoutCrash(Marshal.GetExceptionForHR(hr));
                    return;
                }

                // Initialize the OleComponent to listen for modal changes (which will tell us when Tools->Options is closed)
                _oleComponent = OleComponent.CreateHostedComponent("Keybinding Reset Detector");
                _oleComponent.ModalStateChanged += OnModalStateChanged;
            }

            // run it from background and fire and forget
            StartUpdateStateMachine();
        }

        private void StartUpdateStateMachine()
        {
            // cancel previous state machine update request
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            // make sure all state machine change work is serialized so that cancellation
            // doesn't mess the state up.   
            _lastTask = _lastTask.SafeContinueWithFromAsync(_ =>
            {
                return UpdateStateMachineWorkerAsync(cancellationToken);
            }, cancellationToken, TaskScheduler.Default);
        }

        private async Task UpdateStateMachineWorkerAsync(CancellationToken cancellationToken)
        {
            var options = _workspace.Options;
            var lastStatus = options.GetOption(KeybindingResetOptions.ReSharperStatus);

            ReSharperStatus currentStatus;
            try
            {
                currentStatus = await IsReSharperRunningAsync(lastStatus, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (currentStatus == lastStatus)
            {
                return;
            }

            options = options.WithChangedOption(KeybindingResetOptions.ReSharperStatus, currentStatus);

            switch (lastStatus)
            {
                case ReSharperStatus.NotInstalledOrDisabled:
                case ReSharperStatus.Suspended:
                    if (currentStatus == ReSharperStatus.Enabled)
                    {
                        // N->E or S->E. If ReSharper was just installed and is enabled, reset NeedsReset.
                        options = options.WithChangedOption(KeybindingResetOptions.NeedsReset, false);
                    }

                    // Else is N->N, N->S, S->N, S->S. N->S can occur if the user suspends ReSharper, then disables
                    // the extension, then reenables the extension. We will show the gold bar after the switch
                    // if there is still a pending show.

                    break;
                case ReSharperStatus.Enabled:
                    if (currentStatus != ReSharperStatus.Enabled)
                    {
                        // E->N or E->S. Set NeedsReset. Pop the gold bar to the user.
                        options = options.WithChangedOption(KeybindingResetOptions.NeedsReset, true);
                    }

                    // Else is E->E. No actions to take
                    break;
            }

            _workspace.Options = options;
            if (options.GetOption(KeybindingResetOptions.NeedsReset))
            {
                ShowGoldBar();
            }
        }

        private void ShowGoldBar()
        {
            // If the gold bar is already open, do not show
            if (_infoBarOpen)
            {
                return;
            }

            _infoBarOpen = true;

            Debug.Assert(_experimentationService.IsExperimentEnabled(InternalFlightName) ||
                         _experimentationService.IsExperimentEnabled(ExternalFlightName));

            var message = ServicesVSResources.We_notice_you_suspended_0_Reset_keymappings_to_continue_to_navigate_and_refactor;
            KeybindingsResetLogger.Log("InfoBarShown");
            var infoBarService = _workspace.Services.GetRequiredService<IInfoBarService>();
            infoBarService.ShowInfoBarInGlobalView(
                string.Format(message, ReSharperExtensionName),
                new InfoBarUI(title: ServicesVSResources.Reset_Visual_Studio_default_keymapping,
                              kind: InfoBarUI.UIKind.Button,
                              action: RestoreVsKeybindings),
                new InfoBarUI(title: string.Format(ServicesVSResources.Apply_0_keymapping_scheme, ReSharperKeyboardMappingName),
                              kind: InfoBarUI.UIKind.Button,
                              action: OpenExtensionsHyperlink),
                new InfoBarUI(title: string.Format(ServicesVSResources.Apply_0_keymapping_scheme, VSCodeKeyboardMappingName),
                              kind: InfoBarUI.UIKind.Button,
                              action: OpenExtensionsHyperlink),
                new InfoBarUI(title: ServicesVSResources.Never_show_this_again,
                              kind: InfoBarUI.UIKind.HyperLink,
                              action: NeverShowAgain),
                new InfoBarUI(title: "", kind: InfoBarUI.UIKind.Close,
                              action: InfoBarClose));
        }

        /// <summary>
        /// Returns true if ReSharper is installed, enabled, and not suspended.  
        /// </summary>
        private async ValueTask<ReSharperStatus> IsReSharperRunningAsync(ReSharperStatus lastStatus, CancellationToken cancellationToken)
        {
            // Quick exit if resharper is either uninstalled or not enabled
            if (!_resharperExtensionInstalledAndEnabled)
            {
                return ReSharperStatus.NotInstalledOrDisabled;
            }

            await EnsureOleCommandTargetAsync().ConfigureAwait(false);

            // poll until either suspend or resume botton is available, or until operation is canceled
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var suspendFlag = await QueryStatusAsync(SuspendId).ConfigureAwait(false);

                // In the case of an error when attempting to get the status, pretend that ReSharper isn't enabled. We also
                // shut down monitoring so we don't keep hitting this.
                if (suspendFlag == 0)
                {
                    return ReSharperStatus.NotInstalledOrDisabled;
                }

                var resumeFlag = await QueryStatusAsync(ResumeId).ConfigureAwait(false);
                if (resumeFlag == 0)
                {
                    return ReSharperStatus.NotInstalledOrDisabled;
                }

                // When ReSharper is running, the ReSharper_Suspend command is Enabled and not Invisible
                if (suspendFlag.HasFlag(OLECMDF.OLECMDF_ENABLED) && !suspendFlag.HasFlag(OLECMDF.OLECMDF_INVISIBLE))
                {
                    return ReSharperStatus.Enabled;
                }

                // When ReSharper is suspended, the ReSharper_Resume command is Enabled and not Invisible
                if (resumeFlag.HasFlag(OLECMDF.OLECMDF_ENABLED) && !resumeFlag.HasFlag(OLECMDF.OLECMDF_INVISIBLE))
                {
                    return ReSharperStatus.Suspended;
                }

                // ReSharper has not finished initializing, so try again later
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
            }

            async Task<OLECMDF> QueryStatusAsync(uint cmdId)
            {
                var cmds = new OLECMD[1];
                cmds[0].cmdID = cmdId;
                cmds[0].cmdf = 0;

                await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                var hr = _oleCommandTarget.QueryStatus(ReSharperCommandGroup, (uint)cmds.Length, cmds, IntPtr.Zero);
                if (ErrorHandler.Failed(hr))
                {
                    FatalError.ReportWithoutCrash(Marshal.GetExceptionForHR(hr));
                    await ShutdownAsync().ConfigureAwait(false);

                    return 0;
                }

                return (OLECMDF)cmds[0].cmdf;
            }

            async Task EnsureOleCommandTargetAsync()
            {
                if (_oleCommandTarget != null)
                {
                    return;
                }

                await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                _oleCommandTarget = _serviceProvider.GetService<IOleCommandTarget, SUIHostCommandDispatcher>();
            }
        }

        private void RestoreVsKeybindings()
        {
            AssertIsForeground();

            if (_uiShell == null)
            {
                _uiShell = _serviceProvider.GetService<IVsUIShell, SVsUIShell>();
            }

            ErrorHandler.ThrowOnFailure(_uiShell.PostExecCommand(
                    VSConstants.GUID_VSStandardCommandSet97,
                    (uint)VSConstants.VSStd97CmdID.CustomizeKeyboard,
                    (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT,
                    null));

            KeybindingsResetLogger.Log("KeybindingsReset");

            _workspace.Options = _workspace.Options.WithChangedOption(KeybindingResetOptions.NeedsReset, false);
        }

        private void OpenExtensionsHyperlink()
        {
            ThisCanBeCalledOnAnyThread();

            BrowserHelper.StartBrowser(KeybindingsFwLink);

            KeybindingsResetLogger.Log("ExtensionsLink");
            _workspace.Options = _workspace.Options.WithChangedOption(KeybindingResetOptions.NeedsReset, false);
        }

        private void NeverShowAgain()
        {
            _workspace.Options = _workspace.Options.WithChangedOption(KeybindingResetOptions.NeverShowAgain, true)
                                                   .WithChangedOption(KeybindingResetOptions.NeedsReset, false);
            KeybindingsResetLogger.Log("NeverShowAgain");

            // The only external references to this object are as callbacks, which are removed by the Shutdown method.
            ThreadingContext.JoinableTaskFactory.Run(ShutdownAsync);
        }

        private void InfoBarClose()
        {
            AssertIsForeground();
            _infoBarOpen = false;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            // Technically can be called on any thread, though VS will only ever call it on the UI thread.
            ThisCanBeCalledOnAnyThread();
            // We don't care about query status, only when the command is actually executed
            return (int)OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            // Technically can be called on any thread, though VS will only ever call it on the UI thread.
            ThisCanBeCalledOnAnyThread();
            if (pguidCmdGroup == ReSharperCommandGroup && nCmdID >= ResumeId && nCmdID <= ToggleSuspendId)
            {
                // Don't delay command processing to update resharper status
                StartUpdateStateMachine();
            }

            // No matter the command, we never actually want to respond to it, so always return not supported. We're just monitoring.
            return (int)OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        private void OnModalStateChanged(object sender, StateChangedEventArgs args)
        {
            ThisCanBeCalledOnAnyThread();

            // Only monitor for StateTransitionType.Exit. This will be fired when the shell is leaving a modal state, including
            // Tools->Options being exited. This will fire more than just on Options close, but there's no harm from running an
            // extra QueryStatus.
            if (args.TransitionType == StateTransitionType.Exit)
            {
                StartUpdateStateMachine();
            }
        }

        private async Task ShutdownAsync()
        {
            // we are shutting down, cancel any pending work.
            _cancellationTokenSource.Cancel();

            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (_priorityCommandTargetCookie != VSConstants.VSCOOKIE_NIL)
            {
                var priorityCommandTargetRegistrar = _serviceProvider.GetService<IVsRegisterPriorityCommandTarget, SVsRegisterPriorityCommandTarget>();
                var cookie = _priorityCommandTargetCookie;
                _priorityCommandTargetCookie = VSConstants.VSCOOKIE_NIL;
                var hr = priorityCommandTargetRegistrar.UnregisterPriorityCommandTarget(cookie);

                if (ErrorHandler.Failed(hr))
                {
                    FatalError.ReportWithoutCrash(Marshal.GetExceptionForHR(hr));
                }
            }

            if (_oleComponent != null)
            {
                _oleComponent.Dispose();
                _oleComponent = null;
            }
        }
    }
}
