// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Design;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.StackTraceExplorer
{
    internal class StackTraceExplorerCommandHandler
    {
        private readonly RoslynPackage _package;
        private static StackTraceExplorerCommandHandler? _instance;

        private StackTraceExplorerCommandHandler(RoslynPackage package)
        {
            _package = package;
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var window = GetOrInitializeWindow();

            var windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());

            // Paste current clipboard contents on showing
            // the window
            window.Root?.OnPaste();
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

            // Initialize the window on startup
            _instance.GetOrInitializeWindow();

            var menuCommandId = new CommandID(Guids.StackTraceExplorerCommandId, 0x0100);
            var menuItem = new MenuCommand(_instance.Execute, menuCommandId);
            menuCommandService.AddCommand(menuItem);
        }
    }
}
