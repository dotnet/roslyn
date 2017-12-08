// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Experimentation;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.VisualStudio.LanguageServices.Experimentation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.PlatformUI.OleComponentSupport;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

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
    [Export(typeof(IExperiment))]
    internal sealed class KeybindingResetDetector : ForegroundThreadAffinitizedObject, IExperiment, IOleCommandTarget
    {
        // Flight info
        private const string InternalFlightName = "keybindgoldbarint";
        private const string ExternalFlightName = "keybindgoldbarext";
        private const string KeybindingsFwLink = "https://go.microsoft.com/fwlink/?linkid=864209";

        // Resharper commands and package
        private const uint ResumeId = 707;
        private const uint SuspendId = 708;
        private const uint ToggleSuspendId = 709;
        private static readonly Guid ReSharperPackageGuid = new Guid("0C6E6407-13FC-4878-869A-C8B4016C57FE");
        private static readonly Guid ReSharperCommandGroup = new Guid("{47F03277-5055-4922-899C-0F7F30D26BF1}");

        private readonly VisualStudioWorkspace _workspace;
        private readonly SVsServiceProvider _serviceProvider;

        // All mutable fields are UI-thread affinitized

        private IExperimentationService _experimentationService;
        private IVsUIShell _uiShell;
        private IOleCommandTarget _oleCommandTarget;
        private OleComponent _oleComponent;
        private uint _priorityCommandTargetCookie = VSConstants.VSCOOKIE_NIL;

        /// <summary>
        /// If false, ReSharper is either not installed, or has been disabled in the extension manager.
        /// If true, the ReSharper extension is enabled. ReSharper's internal status could be either suspended or enabled.
        /// </summary>
        private bool _resharperExtensionEnabled = false;

        private bool _infoBarOpen = false;

        [ImportingConstructor]
        public KeybindingResetDetector(VisualStudioWorkspace workspace, SVsServiceProvider serviceProvider)
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

            return InvokeBelowInputPriority(InitializeCore);
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
            var hr = vsShell.IsPackageInstalled(ReSharperPackageGuid, out int extensionEnabled);
            if (ErrorHandler.Failed(hr))
            {
                FatalError.ReportWithoutCrash(Marshal.GetExceptionForHR(hr));
                return;
            }

            _resharperExtensionEnabled = extensionEnabled != 0;

            if (_resharperExtensionEnabled)
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

            UpdateStateMachine();
        }

        private void UpdateStateMachine()
        {
            AssertIsForeground();

            var isEnabled = IsReSharperEnabled();
            ReSharperStatus lastStatus = _workspace.Options.GetOption(KeybindingResetOptions.ReSharperStatus);

            switch (lastStatus)
            {
                case ReSharperStatus.NotInstalledOrDisabled:
                    if (!_resharperExtensionEnabled || !isEnabled)
                    {
                        // N->N or N->S. N->S can occur if the user suspends ReSharper, then disables
                        // the extension, then reenables the extension. Pop the gold bar if the user still has a pending
                        // reset. Otherwise, do nothing. Set status if we moved from N->S.
                        if (_resharperExtensionEnabled)
                        {
                            _workspace.Options = _workspace.Options.WithChangedOption(KeybindingResetOptions.ReSharperStatus, ReSharperStatus.Suspended);
                        }

                        if (_workspace.Options.GetOption(KeybindingResetOptions.NeedsReset))
                        {
                            ShowGoldBar();
                        }
                    }
                    else
                    {
                        // N->E. If ReSharper was just installed and is enabled, reset NeedsReset and set the current status
                        _workspace.Options = _workspace.Options.WithChangedOption(KeybindingResetOptions.ReSharperStatus, ReSharperStatus.Enabled)
                                                               .WithChangedOption(KeybindingResetOptions.NeedsReset, false);
                    }

                    break;
                case ReSharperStatus.Suspended:
                    if (!_resharperExtensionEnabled || !isEnabled)
                    {
                        // S->S or S->N. Pop the gold bar if the user has a pending reset, otherwise do nothing. Update status if required.
                        if (!_resharperExtensionEnabled)
                        {
                            _workspace.Options = _workspace.Options.WithChangedOption(KeybindingResetOptions.ReSharperStatus, ReSharperStatus.NotInstalledOrDisabled);
                        }

                        if (_workspace.Options.GetOption(KeybindingResetOptions.NeedsReset))
                        {
                            ShowGoldBar();
                        }
                    }
                    else
                    {
                        // S->E. Reset NeedsReset and update the status.
                        _workspace.Options = _workspace.Options.WithChangedOption(KeybindingResetOptions.ReSharperStatus, ReSharperStatus.Enabled)
                                                               .WithChangedOption(KeybindingResetOptions.NeedsReset, false);
                    }
                    break;
                case ReSharperStatus.Enabled:
                    if (!isEnabled)
                    {
                        // E->N or E->S. Update the status, and set NeedsReset. Pop the gold bar to the user.
                        _workspace.Options = _workspace.Options.WithChangedOption(KeybindingResetOptions.ReSharperStatus, _resharperExtensionEnabled ?
                                                                                                                          ReSharperStatus.Suspended :
                                                                                                                          ReSharperStatus.NotInstalledOrDisabled)
                                                               .WithChangedOption(KeybindingResetOptions.NeedsReset, true);
                        ShowGoldBar();
                    }

                    // Else is E->E. No actions to take
                    break;
            }
        }

        private void ShowGoldBar()
        {
            AssertIsForeground();

            // If the gold bar is already open, do not show
            if (_infoBarOpen)
            {
                return;
            }

            _infoBarOpen = true;

            string message;
            if (_experimentationService.IsExperimentEnabled(InternalFlightName))
            {
                message = ServicesVSResources.We_noticed_you_suspended_ReSharper_Ultimate_Restore_Visual_Studio_keybindings_to_continue_to_navigate_and_refactor;
            }
            else if (_experimentationService.IsExperimentEnabled(ExternalFlightName))
            {
                message = ServicesVSResources.We_noticed_your_keybindings_are_broken;
            }
            else
            {
                // Should never have gotten to checking this if one of the flights isn't enabled.
                throw ExceptionUtilities.Unreachable;
            }

            KeybindingsResetLogger.Log("InfoBarShown");
            var infoBarService = _workspace.Services.GetRequiredService<IInfoBarService>();
            infoBarService.ShowInfoBarInGlobalView(
                message,
                new InfoBarUI(title: ServicesVSResources.Restore_Visual_Studio_keybindings,
                              kind: InfoBarUI.UIKind.Button,
                              action: RestoreVsKeybindings),
                new InfoBarUI(title: ServicesVSResources.Use_Keybindings_for_extensions,
                              kind: InfoBarUI.UIKind.Button,
                              action: OpenExtensionsHyperlink),
                new InfoBarUI(title: ServicesVSResources.Never_show_this_again,
                              kind: InfoBarUI.UIKind.HyperLink,
                              action: NeverShowAgain),
                new InfoBarUI(title: "", kind: InfoBarUI.UIKind.Close,
                              action: InfoBarClose));
        }

        private bool IsReSharperEnabled()
        {
            AssertIsForeground();

            // Quick exit if resharper is either uninstalled or not enabled
            if (!_resharperExtensionEnabled)
            {
                return false;
            }

            if (_oleCommandTarget == null)
            {
                var oleServiceProvider = _serviceProvider.GetService<IOleServiceProvider>();
                _oleCommandTarget = (IOleCommandTarget)oleServiceProvider.QueryService(VSConstants.SID_SUIHostCommandDispatcher);
            }

            var cmds = new OLECMD[1];
            cmds[0].cmdID = SuspendId;
            cmds[0].cmdf = 0;

            ErrorHandler.ThrowOnFailure(_oleCommandTarget.QueryStatus(ReSharperCommandGroup, (uint)cmds.Length, cmds, IntPtr.Zero));

            // When ReSharper is enabled, the ReSharper_Suspend command has the Enabled | Supported flags. When disabled, it has Invisible | Supported.
            return ((OLECMDF)cmds[0].cmdf).HasFlag(OLECMDF.OLECMDF_ENABLED);
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
            if (!BrowserHelper.TryGetUri(KeybindingsFwLink, out Uri fwLink))
            {
                // We're providing a constant, known-good link. This should be impossible.
                throw ExceptionUtilities.Unreachable;
            }

            BrowserHelper.StartBrowser(fwLink);

            KeybindingsResetLogger.Log("ExtensionsLink");
            _workspace.Options = _workspace.Options.WithChangedOption(KeybindingResetOptions.NeedsReset, false);
        }

        private void NeverShowAgain()
        {
            AssertIsForeground();

            _workspace.Options = _workspace.Options.WithChangedOption(KeybindingResetOptions.NeverShowAgain, true)
                                                   .WithChangedOption(KeybindingResetOptions.NeedsReset, false);
            KeybindingsResetLogger.Log("NeverShowAgain");

            // The only external references to this object are as callbacks, which are removed by the Shutdown method.
            Shutdown();
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
                Task.Run(() => InvokeBelowInputPriority(UpdateStateMachine));
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
                InvokeBelowInputPriority(UpdateStateMachine);
            }
        }

        public void Shutdown()
        {
            AssertIsForeground();
            if (_priorityCommandTargetCookie != VSConstants.VSCOOKIE_NIL)
            {
                _oleComponent.Dispose();
                _oleComponent = null;

                var priorityCommandTargetRegistrar = _serviceProvider.GetService<IVsRegisterPriorityCommandTarget, SVsRegisterPriorityCommandTarget>();
                var cookie = _priorityCommandTargetCookie;
                _priorityCommandTargetCookie = VSConstants.VSCOOKIE_NIL;
                var hr = priorityCommandTargetRegistrar.UnregisterPriorityCommandTarget(cookie);
                if (ErrorHandler.Failed(hr))
                {
                    FatalError.ReportWithoutCrash(Marshal.GetExceptionForHR(hr));
                }
            }
        }
    }
}
