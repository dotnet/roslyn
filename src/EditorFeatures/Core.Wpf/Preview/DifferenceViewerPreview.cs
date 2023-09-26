// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
    internal sealed partial class DifferenceViewerPreview : IDifferenceViewerPreview<IWpfDifferenceViewer>
    {
        private const int WM_KEYFIRST = 0x0100;
        private const int WM_KEYLAST = 0x0108;

        private readonly IVsFilterKeys2? _filterKeys;

        private IWpfDifferenceViewer? _viewer;
        private bool _hasFocus;
        private NavigationalCommandTarget? _editorCommandTarget;

        public DifferenceViewerPreview(IWpfDifferenceViewer viewer, IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            Contract.ThrowIfNull(viewer);
            _viewer = viewer;

            _viewer.VisualElement.IsKeyboardFocusWithinChanged += OnDifferenceViewerKeyboardFocusWithinChanged;
            _hasFocus = _viewer.VisualElement.IsKeyboardFocusWithin;

            var host = _viewer.ViewMode switch
            {
                DifferenceViewMode.Inline => _viewer.InlineHost,
                DifferenceViewMode.LeftViewOnly => _viewer.LeftHost,
                DifferenceViewMode.RightViewOnly => _viewer.RightHost,
                _ => throw ExceptionUtilities.UnexpectedValue(_viewer.ViewMode),
            };

            _editorCommandTarget = new NavigationalCommandTarget(host.TextView,
                    editorOperationsFactoryService.GetEditorOperations(host.TextView));

            _filterKeys = Package.GetGlobalService(typeof(SVsFilterKeys)) as IVsFilterKeys2;
        }

        public IWpfDifferenceViewer Viewer
        {
            get
            {
                Contract.ThrowIfNull(_viewer);
                return _viewer;
            }
        }

        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            GC.SuppressFinalize(this);

            if (_viewer != null)
            {
                ComponentDispatcher.ThreadFilterMessage -= FilterThreadMessage;
                _viewer.VisualElement.IsKeyboardFocusWithinChanged -= OnDifferenceViewerKeyboardFocusWithinChanged;

                if (!_viewer.IsClosed)
                    _viewer.Close();
            }

            _viewer = null;
            _editorCommandTarget = null;
        }

        ~DifferenceViewerPreview()
        {
            // make sure we are not leaking diff viewer
            // we can't close the view from finalizer thread since it must be same
            // thread (owner thread) this UI is created.
            if (Environment.HasShutdownStarted)
            {
                return;
            }

            FatalError.ReportAndCatch(new Exception($"Dispose is not called how? viewer state : {_viewer?.IsClosed}"));
        }

        private void OnDifferenceViewerKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _hasFocus = (bool)e.NewValue;
            if (_hasFocus)
            {
                // Hook into WPFs thread message handling so we can handle WM_KEYDOWN messages
                ComponentDispatcher.ThreadFilterMessage += FilterThreadMessage;
            }
            else
            {
                // Unhook from WPF's thread message handling.
                ComponentDispatcher.ThreadFilterMessage -= FilterThreadMessage;
            }
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_hasFocus && _editorCommandTarget != null)
            {
                return _editorCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
            }

            return (int)VisualStudio.OLE.Interop.Constants.OLECMDERR_E_UNKNOWNGROUP;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_hasFocus && _editorCommandTarget != null)
            {
                return _editorCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            return (int)VisualStudio.OLE.Interop.Constants.OLECMDERR_E_UNKNOWNGROUP;
        }

        /// <summary>
        /// Preprocess input (keyboard) messages in order to translate them to editor commands if they map. Since we are in a modal dialog
        /// we need to tell the shell to allow pre-translate during a modal loop as well as instructing it to use the editor keyboard scope
        /// even though, as far as the shell knows, there is no editor active.
        /// </summary>
        private void FilterThreadMessage(ref System.Windows.Interop.MSG msg, ref bool handled)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_filterKeys != null
                && msg.message >= WM_KEYFIRST
                && msg.message <= WM_KEYLAST)
            {
                var oleMSG = new VisualStudio.OLE.Interop.MSG()
                {
                    hwnd = msg.hwnd,
                    lParam = msg.lParam,
                    wParam = msg.wParam,
                    message = (uint)msg.message
                };

                // Ask the shell to do the command mapping for us and without firing off the command. We need to check if this command is one of the
                // supported commands first before actually firing the command.
                if (ErrorHandler.Succeeded(
                    _filterKeys.TranslateAcceleratorEx(
                        new VisualStudio.OLE.Interop.MSG[] { oleMSG },
                        (uint)(__VSTRANSACCELEXFLAGS.VSTAEXF_NoFireCommand | __VSTRANSACCELEXFLAGS.VSTAEXF_UseTextEditorKBScope | __VSTRANSACCELEXFLAGS.VSTAEXF_AllowModalState),
                        0 /*scope count*/,
                        Array.Empty<Guid>() /*scopes*/,
                        out var cmdGuid,
                        out var cmdId,
                        out _,
                        out _)))
                {
                    // If the command is an allowed command then we fire it.
                    if (IsCommandAllowed(cmdGuid, cmdId))
                    {
                        var res = _filterKeys.TranslateAcceleratorEx(
                            new VisualStudio.OLE.Interop.MSG[] { oleMSG },
                            (uint)(__VSTRANSACCELEXFLAGS.VSTAEXF_UseTextEditorKBScope | __VSTRANSACCELEXFLAGS.VSTAEXF_AllowModalState),
                            0 /*scope count*/,
                            Array.Empty<Guid>() /*scopes*/,
                            out _,
                            out _,
                            out _,
                            out _);

                        // We set handled to true if the command was executed, otherwise handled will be false
                        handled = ErrorHandler.Succeeded(res);
                    }
                }
            }
        }

        /// <summary>
        /// Determines if the command specified is a valid command in Diff preview.
        /// </summary>
        /// <param name="cmdGuid">The command set guid for the command</param>
        /// <param name="cmdId">The command id</param>
        /// <returns>true for the supported commands that are copy, selection, navigation. False otherwise</returns>
        private static bool IsCommandAllowed(Guid cmdGuid, uint cmdId)
        {
            if (cmdGuid == VsMenus.guidStandardCommandSet2K)
            {
                return cmdId == (uint)VSConstants.VSStd2KCmdID.COPY ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.SELECTALL ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.UP ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.UP_EXT ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.PAGEUP ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.PAGEUP_EXT ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.DOWN ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.DOWN_EXT ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.PAGEDN ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.PAGEDN_EXT ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.LEFT ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.LEFT_EXT ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.RIGHT ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.RIGHT_EXT ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.BOL ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.BOL_EXT ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.FIRSTCHAR ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.FIRSTCHAR_EXT ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.EOL ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.EOL_EXT ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.LASTCHAR ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.LASTCHAR_EXT ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.TOPLINE ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.TOPLINE_EXT ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.BOTTOMLINE ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.BOTTOMLINE_EXT ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.HOME ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.HOME_EXT ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.END ||
                       cmdId == (uint)VSConstants.VSStd2KCmdID.END_EXT;
            }

            return false;
        }
    }
}
