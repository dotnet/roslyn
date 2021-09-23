// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.StackTraceExplorer
{
    [Guid(Guids.StackTraceExplorerToolWindowIdString)]
    internal class StackTraceExplorerToolWindow : ToolWindowPane, IOleCommandTarget
    {
        private bool _initialized;
        public StackTraceExplorerRoot? Root { get; private set; }

        public StackTraceExplorerToolWindow() : base(null)
        {
            Caption = ServicesVSResources.Stack_Trace_Explorer;
            Content = new DockPanel
            {
                LastChildFill = true
            };
        }

        public void InitializeIfNeeded(RoslynPackage roslynPackage)
        {
            if (_initialized)
            {
                return;
            }

            var workspace = roslynPackage.ComponentModel.GetService<VisualStudioWorkspace>();
            var formatMapService = roslynPackage.ComponentModel.GetService<IClassificationFormatMapService>();
            var formatMap = formatMapService.GetClassificationFormatMap(StandardContentTypeNames.Text);
            var typeMap = roslynPackage.ComponentModel.GetService<ClassificationTypeMap>();
            var threadingContext = roslynPackage.ComponentModel.GetService<IThreadingContext>();
            var streamingFindUsagesPresenter = roslynPackage.ComponentModel.GetService<IStreamingFindUsagesPresenter>();

            Root = new StackTraceExplorerRoot(new StackTraceExplorerRootViewModel(threadingContext, workspace, formatMap, typeMap, streamingFindUsagesPresenter))
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var contentRoot = (DockPanel)Content;
            contentRoot.Children.Add(Root);

            var contextMenu = new ThemedContextMenu();
            contextMenu.Items.Add(new MenuItem()
            {
                Header = ServicesVSResources.Paste,
                Command = new DelegateCommand(_ => Root.OnPaste()),
                Icon = new CrispImage()
                {
                    Moniker = KnownMonikers.Paste
                }
            });

            contextMenu.Items.Add(new MenuItem()
            {
                Header = ServicesVSResources.Clear,
                Command = new DelegateCommand(_ => Root.OnClear()),
                Icon = new CrispImage()
                {
                    Moniker = KnownMonikers.ClearCollection
                }
            });

            contentRoot.ContextMenu = contextMenu;

            _initialized = true;
        }

        public override void OnToolWindowCreated()
        {
            // Hide the frame by default when VS starts
            if (Frame is IVsWindowFrame windowFrame)
            {
                windowFrame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
            }
        }

        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            //if ((nCmdID & (uint)OLECMDID.OLECMDID_PASTE) != 0 ||
            //    (nCmdID & (uint)OLECMDID.OLECMDID_PASTESPECIAL) != 0)
            //{
            //    ViewModel?.OnPaste();
            //}

            return VSConstants.S_OK;
        }

        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return VSConstants.S_OK;
        }
    }
}
