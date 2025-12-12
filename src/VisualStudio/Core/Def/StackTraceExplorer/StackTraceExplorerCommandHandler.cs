// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Design;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.StackTraceExplorer;

internal sealed class StackTraceExplorerCommandHandler : IVsBroadcastMessageEvents, IDisposable
{
    private readonly RoslynPackage _package;
    private readonly IThreadingContext _threadingContext;
    private readonly IGlobalOptionService _globalOptions;
    private static StackTraceExplorerCommandHandler? _instance;
    private uint _vsShellBroadcastCookie;

    private StackTraceExplorerCommandHandler(RoslynPackage package)
    {
        _package = package;
        _threadingContext = package.ComponentModel.GetService<IThreadingContext>();
        _globalOptions = package.ComponentModel.GetService<IGlobalOptionService>();

        _globalOptions.AddOptionChangedHandler(this, GlobalOptionChanged);

        var enabled = _globalOptions.GetOption(StackTraceExplorerOptionsStorage.OpenOnFocus);
        if (enabled)
        {
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

        var window = GetOrInitializeWindow();
        _threadingContext.JoinableTaskFactory.RunAsync(async () =>
        {
            var shouldActivate = await window.ShouldShowOnActivatedAsync(default).ConfigureAwait(false);

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

        _globalOptions.RemoveOptionChangedHandler(this, GlobalOptionChanged);
    }

    private void AdviseBroadcastMessages()
    {
        if (_vsShellBroadcastCookie != 0)
        {
            return;
        }

        var serviceProvider = (IServiceProvider)_package;
        var vsShell = serviceProvider.GetService(typeof(SVsShell)) as IVsShell;

        vsShell?.AdviseBroadcastMessages(this, out _vsShellBroadcastCookie);
    }

    private void UnadviseBroadcastMessages()
    {
        if (_vsShellBroadcastCookie != 0)
        {
            var serviceProvider = (IServiceProvider)_package;
            var vsShell = serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
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

    private void Execute(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var window = GetOrInitializeWindow();

        var windowFrame = (IVsWindowFrame)window.Frame;
        ErrorHandler.ThrowOnFailure(windowFrame.Show());

        // Paste current clipboard contents on showing
        // the window
        window.Root?.ViewModel.DoPasteSynchronously(default);
    }

    private StackTraceExplorerToolWindow GetOrInitializeWindow()
    {
        // Get the instance number 0 of this tool window. This window is single instance so this instance
        // is actually the only one.
        // The last flag is set to true so that if the tool window does not exists it will be created.
        var window = _package.FindToolWindow(typeof(StackTraceExplorerToolWindow), 0, true) as StackTraceExplorerToolWindow;
        if (window is not { Frame: not null })
        {
            throw new NotSupportedException("Cannot create tool window");
        }

        window.InitializeIfNeeded(_package);

        return window;
    }

    private void Paste(object sender, EventArgs e)
    {
        RoslynDebug.AssertNotNull(_instance);

        var window = _instance.GetOrInitializeWindow();
        window.Root?.ViewModel?.DoPasteSynchronously(default);
    }

    private void Clear(object sender, EventArgs e)
    {
        RoslynDebug.AssertNotNull(_instance);

        var window = _instance.GetOrInitializeWindow();
        window.Root?.OnClear();
    }

    internal static void Initialize(OleMenuCommandService menuCommandService, RoslynPackage package)
    {
        if (_instance is not null)
        {
            return;
        }

        _instance = new(package);

        var menuCommandId = new CommandID(Guids.StackTraceExplorerCommandId, 0x0100);
        var menuItem = new MenuCommand(_instance.Execute, menuCommandId);
        menuCommandService.AddCommand(menuItem);

        var pasteCommandId = new CommandID(Guids.StackTraceExplorerCommandId, 0x0101);
        var clearCommandId = new CommandID(Guids.StackTraceExplorerCommandId, 0x0102);

        var pasteMenuItem = new MenuCommand(_instance.Paste, pasteCommandId);
        var clearMenuItem = new MenuCommand(_instance.Clear, clearCommandId);

        menuCommandService.AddCommand(pasteMenuItem);
        menuCommandService.AddCommand(clearMenuItem);
    }
}
