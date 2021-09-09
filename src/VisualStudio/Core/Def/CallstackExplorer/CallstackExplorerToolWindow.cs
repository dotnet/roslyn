// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CallstackExplorer
{
    [Guid(Guids.CallstackExplorerToolWindowIdString)]
    internal class CallstackExplorerToolWindow : ToolWindowPane
    {
        private readonly CallstackExplorerRoot _root = new();

        private CallstackExplorerViewModel? _viewModel;
        public CallstackExplorerViewModel? ViewModel
        {
            get => _viewModel;
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(ViewModel));
                }

                _viewModel = value;
                _root.SetChild(new CallstackExplorer(_viewModel));
            }
        }
        public CallstackExplorerToolWindow() : base(null)
        {
            Caption = "Callstack Explorer";
            Content = _root;
        }

        public override void OnToolWindowCreated()
        {
            if (ViewModel is not null)
            {
                // Paste from the clipboard on toolwindow creation
                ViewModel.OnPaste();
            }
            else if (Frame is IVsWindowFrame windowFrame)
            {
                // If we're not initialized don't show the frame
                windowFrame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
            }
        }
    }
}
