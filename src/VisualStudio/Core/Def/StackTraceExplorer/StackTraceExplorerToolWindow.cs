// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.StackTraceExplorer
{
    [Guid(Guids.StackTraceExplorerToolWindowIdString)]
    internal class StackTraceExplorerToolWindow : ToolWindowPane, IOleCommandTarget
    {
        private readonly StackTraceExplorerRoot _root = new();

        private StackTraceExplorerViewModel? _viewModel;
        public StackTraceExplorerViewModel? ViewModel
        {
            get => _viewModel;
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(ViewModel));
                }

                _viewModel = value;
                _root.SetChild(new StackTraceExplorer(_viewModel));
            }
        }
        public StackTraceExplorerToolWindow() : base(null)
        {
            Caption = ServicesVSResources.Stack_Trace_Explorer;
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

        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if ((nCmdID & (uint)OLECMDID.OLECMDID_PASTE) != 0 ||
                (nCmdID & (uint)OLECMDID.OLECMDID_PASTESPECIAL) != 0)
            {
                ViewModel?.OnPaste();
            }

            return VSConstants.S_OK;
        }

        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return VSConstants.S_OK;
        }
    }
}
