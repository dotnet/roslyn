// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Design;
using System.IO.Packaging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.StackTraceExplorer
{
    internal class StackTraceExplorerCommandHandler : IVsBroadcastMessageEvents, IDisposable
    {
        private readonly RoslynPackage _package;
        private readonly IThreadingContext _threadingContext;
        private static StackTraceExplorerCommandHandler? _instance;
        private uint _vsShellBroadcastCookie;

        private StackTraceExplorerCommandHandler(RoslynPackage package)
        {
            _package = package;
            _threadingContext = package.ComponentModel.GetService<IThreadingContext>();
            var globalOptions = package.ComponentModel.GetService<IGlobalOptionService>();

            globalOptions.OptionChanged += OptionService_OptionChanged;

            var enabled = globalOptions.GetOption(StackTraceExplorerOptionsMetadata.OpenOnFocus);
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

            var workspace = _package.ComponentModel.GetService<VisualStudioWorkspace>();
            var optionService = workspace.Services.GetRequiredService<IOptionService>();
            optionService.OptionChanged -= OptionService_OptionChanged;
        }

        private void AdviseBroadcastMessages()
        {
            if (_vsShellBroadcastCookie != 0)
            {
                return;
            }

            var serviceProvider = (IServiceProvider)_package;
            var vsShell = serviceProvider.GetService(typeof(SVsShell)) as IVsShell;

            if (vsShell is not null)
            {
                vsShell.AdviseBroadcastMessages(this, out _vsShellBroadcastCookie);
            }
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

        private void OptionService_OptionChanged(object sender, OptionChangedEventArgs e)
        {
            if (e.Option == StackTraceExplorerOptionsMetadata.OpenOnFocus && e.Value is not null)
            {
                var enabled = (bool)e.Value;
                if (enabled)
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
        }
    }
}
