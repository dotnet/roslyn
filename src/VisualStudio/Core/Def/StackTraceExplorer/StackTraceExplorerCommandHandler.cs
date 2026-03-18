// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.StackTraceExplorer;

[Export(typeof(StackTraceExplorerCommandHandler))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class StackTraceExplorerCommandHandler(
    IThreadingContext threadingContext,
    IGlobalOptionService globalOptions,
    [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider) : IVsBroadcastMessageEvents, IDisposable
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IGlobalOptionService _globalOptions = globalOptions;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private uint _vsShellBroadcastCookie;
    private bool _initialized;

    /// <summary>
    /// Called during solution load to ensure the handler is created and subscribes to broadcast
    /// messages if the "open on focus" option is enabled.
    /// </summary>
    internal async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
            return;

        _initialized = true;

        _globalOptions.AddOptionChangedHandler(this, GlobalOptionChanged);

        var enabled = _globalOptions.GetOption(StackTraceExplorerOptionsStorage.OpenOnFocus);
        if (enabled)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            AdviseBroadcastMessages();
        }
    }

    public int OnBroadcastMessage(uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg != 0x001C) // WM_ACTIVATEAPP
        {
            return VSConstants.S_OK;
        }

        // wParam contains a value indicated if the window was activated.
        // See https://docs.microsoft.com/en-us/windows/win32/winmsg/wm-activateapp 
        // 1 = activate
        // 0 = deactivated
        if (wParam == IntPtr.Zero)
        {
            return VSConstants.S_OK;
        }

        _threadingContext.JoinableTaskFactory.RunAsync(async () =>
        {
            var window = await GetOrInitializeWindowAsync().ConfigureAwait(true);
            var shouldActivate = await window.ShouldShowOnActivatedAsync(default).ConfigureAwait(true);

            if (shouldActivate)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                var windowFrame = (IVsWindowFrame)window.Frame;
                ErrorHandler.ThrowOnFailure(windowFrame.Show());
                Logger.Log(FunctionId.StackTraceToolWindow_ShowOnActivated, logLevel: LogLevel.Information);
            }
        });

        return VSConstants.S_OK;
    }

    public void Dispose()
    {
        UnadviseBroadcastMessages();

        if (_initialized)
            _globalOptions.RemoveOptionChangedHandler(this, GlobalOptionChanged);
    }

    private void AdviseBroadcastMessages()
    {
        if (_vsShellBroadcastCookie != 0)
        {
            return;
        }

        var vsShell = _serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
        vsShell?.AdviseBroadcastMessages(this, out _vsShellBroadcastCookie);
    }

    private void UnadviseBroadcastMessages()
    {
        if (_vsShellBroadcastCookie != 0)
        {
            var vsShell = _serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
            if (vsShell is not null)
            {
                vsShell.UnadviseBroadcastMessages(_vsShellBroadcastCookie);
                _vsShellBroadcastCookie = 0;
            }
        }
    }

    private void GlobalOptionChanged(object sender, object target, OptionChangedEventArgs e)
    {
        bool? enabled = null;
        foreach (var (key, newValue) in e.ChangedOptions)
        {
            if (key.Option.Equals(StackTraceExplorerOptionsStorage.OpenOnFocus))
            {
                Contract.ThrowIfNull(newValue);
                enabled = (bool)newValue;
            }
        }

        if (enabled.HasValue)
        {
            if (enabled.Value)
            {
                AdviseBroadcastMessages();
            }
            else
            {
                UnadviseBroadcastMessages();
            }
        }
    }

    internal void OnExecute(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var window = _threadingContext.JoinableTaskFactory.Run(() => GetOrInitializeWindowAsync());

        var windowFrame = (IVsWindowFrame)window.Frame;
        ErrorHandler.ThrowOnFailure(windowFrame.Show());

        // Paste current clipboard contents on showing
        // the window
        window.Root?.ViewModel.DoPasteSynchronously(default);
    }

    private async Task<StackTraceExplorerToolWindow> GetOrInitializeWindowAsync()
    {
        var package = await RoslynPackage.GetOrLoadAsync(_threadingContext, (IAsyncServiceProvider)_serviceProvider, _threadingContext.DisposalToken).ConfigureAwait(true);
        Contract.ThrowIfNull(package);

        // Get the instance number 0 of this tool window. This window is single instance so this instance
        // is actually the only one.
        // The last flag is set to true so that if the tool window does not exists it will be created.
        var window = package.FindToolWindow(typeof(StackTraceExplorerToolWindow), 0, true) as StackTraceExplorerToolWindow;
        if (window is not { Frame: not null })
        {
            throw new NotSupportedException("Cannot create tool window");
        }

        window.InitializeIfNeeded(package);

        return window;
    }

    internal void OnPaste(object sender, EventArgs e)
    {
        var window = _threadingContext.JoinableTaskFactory.Run(() => GetOrInitializeWindowAsync());
        window.Root?.ViewModel?.DoPasteSynchronously(default);
    }

    internal void OnClear(object sender, EventArgs e)
    {
        var window = _threadingContext.JoinableTaskFactory.Run(() => GetOrInitializeWindowAsync());
        window.Root?.OnClear();
    }
}
