// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Design;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CallstackExplorer
{
    internal class CallstackExplorerMenuCommand
    {
        private readonly RoslynPackage _package;
        private readonly IThreadingContext _threadingContext;
        private static CallstackExplorerMenuCommand? _instance;

        private CallstackExplorerMenuCommand(RoslynPackage package, IThreadingContext threadingContext)
        {
            _package = package;
            _threadingContext = threadingContext;
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            var window = _package.FindToolWindow(typeof(CallstackExplorerToolWindow), 0, true);
            if (window is not { Frame: not null })
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            if (window is CallstackExplorerToolWindow toolWindow && toolWindow.ViewModel is null)
            {
                var workspace = _package.ComponentModel.GetService<VisualStudioWorkspace>();
                toolWindow.ViewModel = new(_threadingContext, workspace);
            }

            var windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        internal static void Initialize(OleMenuCommandService menuCommandService, RoslynPackage package)
        {
            if (_instance is not null)
            {
                return;
            }

            var threadingContext = package.ComponentModel.GetService<IThreadingContext>();
            _instance = new(package, threadingContext);
            var menuCommandId = new CommandID(Guids.CallstackExplorerCommandId, 0x0100);
            var menuItem = new MenuCommand(_instance.Execute, menuCommandId);
            menuCommandService.AddCommand(menuItem);
        }
    }
}
