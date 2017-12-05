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
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using System.Threading;
using Roslyn.Utilities;

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
    internal sealed class KeybindingResetDetector : ForegroundThreadAffinitizedObject, IExperiment, IOleCommandTarget, IDisposable
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

        private IExperimentationService _experimentationService;
        private IVsUIShell _uiShell;
        private IOleCommandTarget _oleCommandTarget;

        private bool _disposedValue = false;
        private uint _priorityCommandTargetCookie = VSConstants.VSCOOKIE_NIL;

        /// <summary>
        /// Must compare/write with Interlocked.CompareExchange, as <see cref="ShowGoldBar"/> can be called on any thread.
        /// </summary>
        const int InfoBarOpen = 1;
        const int InfoBarClosed = 0;
        private int _infoBarOpen = InfoBarClosed;

        [ImportingConstructor]
        public KeybindingResetDetector(VisualStudioWorkspace workspace, SVsServiceProvider serviceProvider)
        {
            _workspace = workspace;
            _serviceProvider = serviceProvider;
        }

        public Task Initialize()
        {
            // Immediately bail if the user has asked to never see this bar again.
            if (_workspace.Options.GetOption(KeybindingResetOptions.NeverShowAgain))
            {
                return Task.CompletedTask;
            }

            return InvokeBelowInputPriority(() => InitializeCore());
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

            var vsShell = _serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
            var hr = vsShell.IsPackageInstalled(ReSharperPackageGuid, out int extensionEnabled);
            if (ErrorHandler.Failed(hr))
            {
                FatalError.ReportWithoutCrash(Marshal.GetExceptionForHR(hr));
                return;
            }

            var currentStatus = _workspace.Options.GetOption(KeybindingResetOptions.ReSharperStatus);

            if (extensionEnabled == 0)
            {
                // If 0, the extension is either disabled in the extension manager, or not installed at all.
                // If this is a change, update the status.
                bool needsReset = _workspace.Options.GetOption(KeybindingResetOptions.NeedsReset);
                if (currentStatus != ReSharperStatus.NotInstalledOrDisabled)
                {
                    // If we're going directly from Enabled->NotInstalled, we need to reset keybindings. Otherwise, the previous value of NeedsReset
                    // is correct.
                    var changedOptions = _workspace.Options;
                    if (currentStatus == ReSharperStatus.Enabled)
                    {
                        needsReset = true;
                        changedOptions = changedOptions.WithChangedOption(KeybindingResetOptions.NeedsReset, true);
                    }

                    changedOptions = _workspace.Options.WithChangedOption(KeybindingResetOptions.ReSharperStatus, ReSharperStatus.NotInstalledOrDisabled);

                    _workspace.Options = changedOptions;
                }

                if (needsReset)
                {
                    ShowGoldBar();
                }
            }
            else
            {
                UpdateReSharperEnableStatus();

                // We need to monitor for suspend/resume commands, so create and install the command target.
                var priorityCommandTargetRegistrar = _serviceProvider.GetService(typeof(SVsRegisterPriorityCommandTarget)) as IVsRegisterPriorityCommandTarget;
                hr = priorityCommandTargetRegistrar.RegisterPriorityCommandTarget(
                    dwReserved: 0 /* from docs must be 0 */,
                    pCmdTrgt: this,
                    pdwCookie: out _priorityCommandTargetCookie);

                if (ErrorHandler.Failed(hr))
                {
                    FatalError.ReportWithoutCrash(Marshal.GetExceptionForHR(hr));
                }
            }
        }

        private void UpdateReSharperEnableStatus()
        {
            AssertIsForeground();

            var isEnabled = IsReSharperEnabled();
            var options = _workspace.Options;

            if (isEnabled)
            {
                // Update the option if we weren't already enabled. There's no other actions to take, since
                // ReSharper is enabled and we don't want to pop a gold bar
                if (options.GetOption(KeybindingResetOptions.ReSharperStatus) != ReSharperStatus.Enabled)
                {
                    options = options.WithChangedOption(KeybindingResetOptions.ReSharperStatus, ReSharperStatus.Enabled)
                                     .WithChangedOption(KeybindingResetOptions.NeedsReset, false);
                    _workspace.Options = options;
                }
            }
            else
            {
                bool needsReset;
                if (options.GetOption(KeybindingResetOptions.ReSharperStatus) != ReSharperStatus.Suspended)
                {
                    options = options.WithChangedOption(KeybindingResetOptions.ReSharperStatus, ReSharperStatus.Suspended)
                                     .WithChangedOption(KeybindingResetOptions.NeedsReset, true);
                    needsReset = true;
                    _workspace.Options = options;
                }
                else
                {
                    needsReset = options.GetOption(KeybindingResetOptions.NeedsReset);
                }

                if (needsReset)
                {
                    ShowGoldBar();
                }
            }
        }

        private bool IsReSharperEnabled()
        {
            AssertIsForeground();

            if (_oleCommandTarget == null)
            {
                var oleServiceProvider = (IOleServiceProvider)_serviceProvider.GetService(typeof(IOleServiceProvider));
                _oleCommandTarget = (IOleCommandTarget)oleServiceProvider.QueryService(VSConstants.SID_SUIHostCommandDispatcher);
            }

            var cmds = new OLECMD[1];
            cmds[0].cmdID = SuspendId;
            cmds[0].cmdf = 0;

            ErrorHandler.ThrowOnFailure(_oleCommandTarget.QueryStatus(ReSharperCommandGroup, (uint)cmds.Length, cmds, IntPtr.Zero));

            // When ReSharper is enabled, the ReSharper_Suspend command has the Enabled | Supported flags. When disabled, it has Invisible | Supported.
            return ((OLECMDF)cmds[0].cmdf).HasFlag(OLECMDF.OLECMDF_ENABLED);
        }

        private void ShowGoldBar()
        {
            ThisCanBeCalledOnAnyThread();

            // If the gold bar is already open, do not show
            if (Interlocked.CompareExchange(ref _infoBarOpen, InfoBarOpen, InfoBarClosed) == InfoBarOpen)
            {
                return;
            }

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

            var infoBarService = _workspace.Services.GetRequiredService<IInfoBarService>();
            infoBarService.ShowInfoBarInGlobalView(
                message,
                new InfoBarUI(title: ServicesVSResources.Restore_Visual_Studio_keybindings,
                              kind: InfoBarUI.UIKind.HyperLink,
                              action: RestoreVsKeybindings),
                new InfoBarUI(title: ServicesVSResources.Use_Visual_Studio_Keybindings_for_extensions,
                              kind: InfoBarUI.UIKind.HyperLink,
                              action: OpenExtensionsHyperlink),
                new InfoBarUI(title: ServicesVSResources.Never_show_this_again,
                              kind: InfoBarUI.UIKind.Button,
                              action: NeverShowAgain),
                new InfoBarUI(title: "", kind: InfoBarUI.UIKind.Close,
                              action: InfoBarClose));
        }

        private void RestoreVsKeybindings()
        {
            AssertIsForeground();

            if (_uiShell == null)
            {
                _uiShell = _serviceProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;
            }

            ErrorHandler.ThrowOnFailure(_uiShell.PostExecCommand(
                    VSConstants.GUID_VSStandardCommandSet97,
                    (uint)VSConstants.VSStd97CmdID.CustomizeKeyboard,
                    (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT,
                    null));

            _workspace.Options = _workspace.Options.WithChangedOption(KeybindingResetOptions.NeedsReset, false);
        }

        private void OpenExtensionsHyperlink()
        {
            ThisCanBeCalledOnAnyThread();
            Process.Start(KeybindingsFwLink);

            _workspace.Options = _workspace.Options.WithChangedOption(KeybindingResetOptions.NeedsReset, false);
        }

        private void NeverShowAgain()
        {
            AssertIsForeground();

            _workspace.Options = _workspace.Options.WithChangedOption(KeybindingResetOptions.NeverShowAgain, true);

            // The only external references to this object are as callbacks, which are removed by the dispose method.
            Dispose();
        }

        private void InfoBarClose()
        {
            _infoBarOpen = InfoBarClosed;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            // We don't care about query status, only when the command is actually executed
            return (int)OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == ReSharperCommandGroup && nCmdID >= ResumeId && nCmdID <= ToggleSuspendId)
            {
                // Don't delay command processing to update resharper status
                Task.Run(() => InvokeBelowInputPriority(UpdateReSharperEnableStatus));
            }

            // No matter the command, we never actually want to respond to it, so always return not supported. We're just monitoring.
            return (int)OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
        }


        void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_priorityCommandTargetCookie != VSConstants.VSCOOKIE_NIL)
                    {
                        AssertIsForeground();
                        var priorityCommandTargetRegistrar = _serviceProvider.GetService(typeof(SVsRegisterPriorityCommandTarget)) as IVsRegisterPriorityCommandTarget;
                        var hr = priorityCommandTargetRegistrar.UnregisterPriorityCommandTarget(_priorityCommandTargetCookie);
                        if (ErrorHandler.Failed(hr))
                        {
                            FatalError.ReportWithoutCrash(Marshal.GetExceptionForHR(hr));
                        }
                    }
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
