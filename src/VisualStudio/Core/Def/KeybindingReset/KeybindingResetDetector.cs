// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.PlatformUI.OleComponentSupport;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.KeybindingReset;

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
[Export(typeof(KeybindingResetDetector))]
internal sealed class KeybindingResetDetector : IOleCommandTarget
{
    private const string KeybindingsFwLink = "https://go.microsoft.com/fwlink/?linkid=864209";
    private const string ReSharperExtensionName = "ReSharper Ultimate";
    private const string ReSharperKeyboardMappingName = "ReSharper (Visual Studio)";
    private const string VSCodeKeyboardMappingName = "Visual Studio Code";

    // Resharper commands and package
    private const uint ResumeId = 707;
    private const uint SuspendId = 708;
    private const uint ToggleSuspendId = 709;

    private static readonly Guid s_resharperPackageGuid = new("0C6E6407-13FC-4878-869A-C8B4016C57FE");
    private static readonly Guid s_resharperCommandGroup = new("47F03277-5055-4922-899C-0F7F30D26BF1");

    private static readonly ImmutableArray<OptionKey2> s_statusOptions = [new OptionKey2(KeybindingResetOptionsStorage.ReSharperStatus), new OptionKey2(KeybindingResetOptionsStorage.NeedsReset)];
    private readonly IThreadingContext _threadingContext;
    private readonly IGlobalOptionService _globalOptions;
    private readonly System.IServiceProvider _serviceProvider;
    private readonly VisualStudioInfoBar _infoBar;

    // All mutable fields are UI-thread affinitized

    private IVsUIShell _uiShell;
    private IOleCommandTarget _oleCommandTarget;
    private OleComponent _oleComponent;
    private uint _priorityCommandTargetCookie = VSConstants.VSCOOKIE_NIL;

    private CancellationTokenSource _cancellationTokenSource = new();
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
    public KeybindingResetDetector(
        IThreadingContext threadingContext,
        IGlobalOptionService globalOptions,
        IVsService<SVsInfoBarUIFactory, IVsInfoBarUIFactory> vsInfoBarUIFactory,
        IVsService<SVsShell, IVsShell> vsShell,
        SVsServiceProvider serviceProvider,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _threadingContext = threadingContext;
        _globalOptions = globalOptions;
        _serviceProvider = serviceProvider;

        // Attach this info bar to the global shell location for info-bars (independent of any particular window).
        _infoBar = new VisualStudioInfoBar(threadingContext, vsInfoBarUIFactory, vsShell, listenerProvider, windowFrame: null);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Immediately bail if the user has asked to never see this bar again.
        if (_globalOptions.GetOption(KeybindingResetOptionsStorage.NeverShowAgain))
            return;

        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
        InitializeCore();
    }

    private void InitializeCore()
    {
        _threadingContext.ThrowIfNotOnUIThread();

        if (!_globalOptions.GetOption(KeybindingResetOptionsStorage.EnabledFeatureFlag))
        {
            return;
        }

        var vsShell = _serviceProvider.GetServiceOnMainThread<SVsShell, IVsShell>();
        var hr = vsShell.IsPackageInstalled(s_resharperPackageGuid, out var extensionEnabled);
        if (ErrorHandler.Failed(hr))
        {
            FatalError.ReportAndCatch(Marshal.GetExceptionForHR(hr));
            return;
        }

        _resharperExtensionInstalledAndEnabled = extensionEnabled != 0;

        if (_resharperExtensionInstalledAndEnabled)
        {
            // We need to monitor for suspend/resume commands, so create and install the command target and the modal callback.
            var priorityCommandTargetRegistrar = _serviceProvider.GetServiceOnMainThread<SVsRegisterPriorityCommandTarget, IVsRegisterPriorityCommandTarget>();
            hr = priorityCommandTargetRegistrar.RegisterPriorityCommandTarget(
                dwReserved: 0 /* from docs must be 0 */,
                pCmdTrgt: this,
                pdwCookie: out _priorityCommandTargetCookie);

            if (ErrorHandler.Failed(hr))
            {
                FatalError.ReportAndCatch(Marshal.GetExceptionForHR(hr));
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
        var options = _globalOptions.GetOptions(s_statusOptions);
        var lastStatus = (ReSharperStatus)options[0];
        var needsReset = (bool)options[1];

        ReSharperStatus currentStatus;
        try
        {
            currentStatus = await IsReSharperRunningAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (currentStatus == lastStatus)
        {
            return;
        }

        switch (lastStatus)
        {
            case ReSharperStatus.NotInstalledOrDisabled:
            case ReSharperStatus.Suspended:
                if (currentStatus == ReSharperStatus.Enabled)
                {
                    // N->E or S->E. If ReSharper was just installed and is enabled, reset NeedsReset.
                    needsReset = false;
                }

                // Else is N->N, N->S, S->N, S->S. N->S can occur if the user suspends ReSharper, then disables
                // the extension, then reenables the extension. We will show the gold bar after the switch
                // if there is still a pending show.

                break;
            case ReSharperStatus.Enabled:
                if (currentStatus != ReSharperStatus.Enabled)
                {
                    // E->N or E->S. Set NeedsReset. Pop the gold bar to the user.
                    needsReset = true;
                }

                // Else is E->E. No actions to take
                break;
        }

        _globalOptions.SetGlobalOptions(
        [
            KeyValuePairUtil.Create(new OptionKey2(KeybindingResetOptionsStorage.ReSharperStatus), (object)currentStatus),
            KeyValuePairUtil.Create(new OptionKey2(KeybindingResetOptionsStorage.NeedsReset), (object)needsReset),
        ]);

        if (needsReset)
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

        var message = ServicesVSResources.We_notice_you_suspended_0_Reset_keymappings_to_continue_to_navigate_and_refactor;
        KeybindingsResetLogger.Log("InfoBarShown");
        _infoBar.ShowInfoBarMessageFromAnyThread(
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
    private async ValueTask<ReSharperStatus> IsReSharperRunningAsync(CancellationToken cancellationToken)
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

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var hr = _oleCommandTarget.QueryStatus(s_resharperCommandGroup, (uint)cmds.Length, cmds, IntPtr.Zero);
            if (ErrorHandler.Failed(hr))
            {
                FatalError.ReportAndCatch(Marshal.GetExceptionForHR(hr));
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

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _oleCommandTarget = _serviceProvider.GetServiceOnMainThread<SUIHostCommandDispatcher, IOleCommandTarget>();
        }
    }

    private void RestoreVsKeybindings()
    {
        _threadingContext.ThrowIfNotOnUIThread();

        _uiShell ??= _serviceProvider.GetServiceOnMainThread<SVsUIShell, IVsUIShell>();

        ErrorHandler.ThrowOnFailure(_uiShell.PostExecCommand(
                VSConstants.GUID_VSStandardCommandSet97,
                (uint)VSConstants.VSStd97CmdID.CustomizeKeyboard,
                (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT,
                null));

        KeybindingsResetLogger.Log("KeybindingsReset");

        _globalOptions.SetGlobalOption(KeybindingResetOptionsStorage.NeedsReset, false);
    }

    private void OpenExtensionsHyperlink()
    {
        // ThisCanBeCalledOnAnyThread();

        VisualStudioNavigateToLinkService.StartBrowser(KeybindingsFwLink);

        KeybindingsResetLogger.Log("ExtensionsLink");
        _globalOptions.SetGlobalOption(KeybindingResetOptionsStorage.NeedsReset, false);
    }

    private void NeverShowAgain()
    {
        _globalOptions.SetGlobalOption(KeybindingResetOptionsStorage.NeverShowAgain, true);
        _globalOptions.SetGlobalOption(KeybindingResetOptionsStorage.NeedsReset, false);
        KeybindingsResetLogger.Log("NeverShowAgain");

        // The only external references to this object are as callbacks, which are removed by the Shutdown method.
        _threadingContext.JoinableTaskFactory.Run(ShutdownAsync);
    }

    private void InfoBarClose()
    {
        _threadingContext.ThrowIfNotOnUIThread();
        _infoBarOpen = false;
    }

    public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
    {
        // Technically can be called on any thread, though VS will only ever call it on the UI thread.
        // ThisCanBeCalledOnAnyThread();

        // We don't care about query status, only when the command is actually executed
        return (int)OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
    }

    public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
    {
        // Technically can be called on any thread, though VS will only ever call it on the UI thread.
        // ThisCanBeCalledOnAnyThread();

        if (pguidCmdGroup == s_resharperCommandGroup && nCmdID >= ResumeId && nCmdID <= ToggleSuspendId)
        {
            // Don't delay command processing to update resharper status
            StartUpdateStateMachine();
        }

        // No matter the command, we never actually want to respond to it, so always return not supported. We're just monitoring.
        return (int)OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
    }

    private void OnModalStateChanged(object sender, StateChangedEventArgs args)
    {
        // ThisCanBeCalledOnAnyThread();

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

        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (_priorityCommandTargetCookie != VSConstants.VSCOOKIE_NIL)
        {
            var priorityCommandTargetRegistrar = _serviceProvider.GetServiceOnMainThread<SVsRegisterPriorityCommandTarget, IVsRegisterPriorityCommandTarget>();
            var cookie = _priorityCommandTargetCookie;
            _priorityCommandTargetCookie = VSConstants.VSCOOKIE_NIL;
            var hr = priorityCommandTargetRegistrar.UnregisterPriorityCommandTarget(cookie);

            if (ErrorHandler.Failed(hr))
            {
                FatalError.ReportAndCatch(Marshal.GetExceptionForHR(hr));
            }
        }

        if (_oleComponent != null)
        {
            _oleComponent.Dispose();
            _oleComponent = null;
        }
    }
}
