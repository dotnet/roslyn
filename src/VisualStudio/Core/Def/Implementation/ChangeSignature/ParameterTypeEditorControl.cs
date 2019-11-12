// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Constants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal sealed class ParameterTypeEditorControl : TextBox
    {
        private IVsTextView _vsTextView;
        private IWpfTextView _wpfTextView;
        private IWpfTextViewHost _host;
        private IEditorOperations _editorOperations;
        private IVsEditorAdaptersFactoryService _vsEditorAdaptersFactoryService;
        private IEditorOperationsFactoryService _editorOperationsFactoryService;
        private System.IServiceProvider _serviceProvider;
        private IOleCommandTarget _nextCommandTarget;

        public void Initialize(
            IVsTextView vsTextView,
            IWpfTextView wpfTextView,
            IWpfTextViewHost textViewHost,
            IVsEditorAdaptersFactoryService editorAdaptersFactory,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            ITextEditorFactoryService textEditorFactoryService,
            ITextBufferFactoryService textBufferFactoryService,
            System.IServiceProvider serviceProvider,
            IContentType contentType)
        {
            var buffer = textBufferFactoryService.CreateTextBuffer(contentType);
            var view = textEditorFactoryService.CreateTextView(buffer, textEditorFactoryService.AllPredefinedRoles); // DefaultRoles might be ok
            var viewHost = textEditorFactoryService.CreateTextViewHost((IWpfTextView)view, setFocus: true).HostControl;

            //Control viewHost = textViewHost.HostControl;


  
            _vsTextView = vsTextView;
            _wpfTextView = wpfTextView;
            _host = textViewHost;
            _vsEditorAdaptersFactoryService = editorAdaptersFactory;
            _editorOperationsFactoryService = editorOperationsFactoryService;
            _serviceProvider = serviceProvider;

            InstallCommandFilter();
            InitializeEditorControl();
        }

        private void InitializeEditorControl()
        {
            //AddLogicalChild(_host.HostControl);
            //AddVisualChild(_host.HostControl);
        }

        private void InstallCommandFilter()
        {
            //if (_vsEditorAdaptersFactoryService != null)
            //{
            //    _editorOperations = _editorOperationsFactoryService.GetEditorOperations(this._host.TextView);
            //}

            //ErrorHandler.ThrowOnFailure(this._vsTextView.AddCommandFilter(this, out this._nextCommandTarget));
        }

        public string GetText() => this._wpfTextView.TextSnapshot.GetText();

        /// <summary>
        /// Query command status
        /// </summary>
        /// <param name="pguidCmdGroup">Command group guid</param>
        /// <param name="cmdCount">The number of commands in the OLECMD array</param>
        /// <param name="prgCmds">The set of command ids</param>
        /// <param name="cmdText">Unuses pCmdText</param>
        /// <returns>A Microsoft.VisualStudio.OLE.Interop.Constants value</returns>
        public int QueryStatus(ref Guid pguidCmdGroup, uint cmdCount, OLECMD[] prgCmds, IntPtr cmdText)
        {

            // Return status UNKNOWNGROUP if the passed command group is different than the ones we know about
            if (pguidCmdGroup != VsMenus.guidStandardCommandSet2K &&
                pguidCmdGroup != VsMenus.guidStandardCommandSet97)
            {
                return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_UNKNOWNGROUP;
            }

            // 1. For the commands we support and don't need to have a custom implementation
            // simply ask the next command handler in the filter chain for the command status
            // 2. For the commands we have a custom implementation, calculate and return status value
            // 3. For other commands, set status to NOTSUPPORTED (0)
            for (int i = 0; i < cmdCount; i++)
            {
                if (this.IsPassThroughCommand(ref pguidCmdGroup, prgCmds[i].cmdID))
                {
                    OLECMD[] cmdArray = new OLECMD[] { new OLECMD() };
                    cmdArray[0].cmdID = prgCmds[i].cmdID;
                    int hr = this._nextCommandTarget.QueryStatus(ref pguidCmdGroup, 1, cmdArray, cmdText);

                    if (ErrorHandler.Failed(hr))
                    {
                        continue;
                    }

                    prgCmds[i].cmdf = cmdArray[0].cmdf;
                }
                else if ((pguidCmdGroup == VsMenus.guidStandardCommandSet97 && prgCmds[i].cmdID == StandardCommands.Cut.ID) ||
                            (pguidCmdGroup == VsMenus.guidStandardCommandSet2K && prgCmds[i].cmdID == (uint)VSConstants.VSStd2KCmdID.CUT) ||
                            (pguidCmdGroup == VsMenus.guidStandardCommandSet97 && prgCmds[i].cmdID == StandardCommands.Copy.ID) ||
                            (pguidCmdGroup == VsMenus.guidStandardCommandSet2K && prgCmds[i].cmdID == (uint)VSConstants.VSStd2KCmdID.COPY))
                {
                    prgCmds[i].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;

                    //if (this.CanCutCopy())
                    //{
                    //    prgCmds[i].cmdf |= (uint)OLECMDF.OLECMDF_ENABLED;
                    //}
                }
                else if ((pguidCmdGroup == VsMenus.guidStandardCommandSet97 && prgCmds[i].cmdID == StandardCommands.Paste.ID) ||
                            (pguidCmdGroup == VsMenus.guidStandardCommandSet2K && prgCmds[i].cmdID == (uint)VSConstants.VSStd2KCmdID.PASTE))
                {
                    prgCmds[i].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;

                    //if (this.CanPaste())
                    //{
                    //    prgCmds[i].cmdf |= (uint)OLECMDF.OLECMDF_ENABLED;
                    //}
                }
                else
                {
                    prgCmds[i].cmdf = 0;
                }
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Executes the given shell command
        /// </summary>
        /// <param name="pguidCmdGroup">Command group guid</param>
        /// <param name="cmdID">Command id</param>
        /// <param name="cmdExecOpt">Options for the executing command</param>
        /// <param name="pvaIn">The input arguments structure</param>
        /// <param name="pvaOut">The command output structure</param>
        /// <returns>Exec return value</returns>
        public int Exec(ref Guid pguidCmdGroup, uint cmdID, uint cmdExecOpt, IntPtr pvaIn, IntPtr pvaOut)
        {
            // Return status UNKNOWNGROUP if the passed command group is different than the ones we know about
            if (pguidCmdGroup != VsMenus.guidStandardCommandSet2K &&
                pguidCmdGroup != VsMenus.guidStandardCommandSet97)
            {
                return (int)Constants.OLECMDERR_E_UNKNOWNGROUP;
            }

            int hr = 0;

            // 1. For the commands we support and don't need to have a custom implementation
            // simply pass the command to the next command handler in the filter chain
            // 2. For the commands we have a custom implementation, carry out the command
            // don't pass it to the next command handler
            // 3. For other commands, simply return with NOTSUPPORTED
            if (this.IsPassThroughCommand(ref pguidCmdGroup, cmdID))
            {
                hr = this._nextCommandTarget.Exec(ref pguidCmdGroup, cmdID, cmdExecOpt, pvaIn, pvaOut);
            }

            else
            {
                hr = (int)Constants.OLECMDERR_E_NOTSUPPORTED;
            }

            return hr;
        }

        /// <summary>
        /// Determines whether the given command should be passed to the
        /// next command handler in the text view command filter chain.
        /// </summary>
        /// <param name="pguidCmdGroup">The command group guid</param>
        /// <param name="cmdID">The command id</param>
        /// <returns>True, if the command is supported and should be passed to the next command handler</returns>
        private bool IsPassThroughCommand(ref Guid pguidCmdGroup, uint cmdID)
        {
            if (pguidCmdGroup == VsMenus.guidStandardCommandSet2K)
            {
                switch ((VSConstants.VSStd2KCmdID)cmdID)
                {
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                    case VSConstants.VSStd2KCmdID.TYPECHAR:
                    case VSConstants.VSStd2KCmdID.BACKSPACE:
                    case VSConstants.VSStd2KCmdID.TAB:
                    case VSConstants.VSStd2KCmdID.BACKTAB:
                    case VSConstants.VSStd2KCmdID.DELETE:
                    case VSConstants.VSStd2KCmdID.DELETEWORDRIGHT:
                    case VSConstants.VSStd2KCmdID.DELETEWORDLEFT:
                    case VSConstants.VSStd2KCmdID.DELETETOBOL:
                    case VSConstants.VSStd2KCmdID.DELETETOEOL:
                    case VSConstants.VSStd2KCmdID.UP:
                    case VSConstants.VSStd2KCmdID.DOWN:
                    case VSConstants.VSStd2KCmdID.LEFT:
                    case VSConstants.VSStd2KCmdID.LEFT_EXT:
                    case VSConstants.VSStd2KCmdID.LEFT_EXT_COL:
                    case VSConstants.VSStd2KCmdID.RIGHT:
                    case VSConstants.VSStd2KCmdID.RIGHT_EXT:
                    case VSConstants.VSStd2KCmdID.RIGHT_EXT_COL:
                    case VSConstants.VSStd2KCmdID.EditorLineFirstColumn:
                    case VSConstants.VSStd2KCmdID.EditorLineFirstColumnExtend:
                    case VSConstants.VSStd2KCmdID.BOL:
                    case VSConstants.VSStd2KCmdID.BOL_EXT:
                    case VSConstants.VSStd2KCmdID.BOL_EXT_COL:
                    case VSConstants.VSStd2KCmdID.EOL:
                    case VSConstants.VSStd2KCmdID.EOL_EXT:
                    case VSConstants.VSStd2KCmdID.EOL_EXT_COL:
                    case VSConstants.VSStd2KCmdID.SELECTALL:
                    case VSConstants.VSStd2KCmdID.CANCEL:
                    case VSConstants.VSStd2KCmdID.WORDPREV:
                    case VSConstants.VSStd2KCmdID.WORDPREV_EXT:
                    case VSConstants.VSStd2KCmdID.WORDPREV_EXT_COL:
                    case VSConstants.VSStd2KCmdID.WORDNEXT:
                    case VSConstants.VSStd2KCmdID.WORDNEXT_EXT:
                    case VSConstants.VSStd2KCmdID.WORDNEXT_EXT_COL:
                    case VSConstants.VSStd2KCmdID.SELECTCURRENTWORD:
                    case VSConstants.VSStd2KCmdID.TOGGLE_OVERTYPE_MODE:
                        return true;
                }
            }
            else if (pguidCmdGroup == VsMenus.guidStandardCommandSet97)
            {
                switch ((VSConstants.VSStd97CmdID)cmdID)
                {
                    case VSConstants.VSStd97CmdID.Delete:
                    case VSConstants.VSStd97CmdID.SelectAll:
                    case VSConstants.VSStd97CmdID.Undo:
                    case VSConstants.VSStd97CmdID.Redo:
                        return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Return visual child at given index
        /// </summary>
        /// <param name="index">child index</param>
        /// <returns>returns visual child</returns>
      //  protected override Visual GetVisualChild(int index) => this._host?.HostControl;

      //  protected override Size ArrangeOverride(Size finalSize)
      //  {
      ////      _host.HostControl.Arrange(new Rect(new Point(0, 0), finalSize));
      //      return finalSize;
      //  }

      //  protected override void OnGotFocus(RoutedEventArgs e)
      //  {
      //      e.Handled = true;
      //      this._host.TextView.VisualElement.Focus();
      //  }

        private static DependencyObject TryGetParent(DependencyObject obj)
        {
            return (obj is Visual) ? VisualTreeHelper.GetParent(obj) : null;
        }

        private static T GetParentOfType<T>(DependencyObject element) where T : Visual
        {
            var parent = TryGetParent(element);
            if (parent is T)
            {
                return (T)parent;
            }

            if (parent == null)
            {
                return null;
            }

            return GetParentOfType<T>(parent);
        }

        internal static void HandleKeyDown(object sender, KeyEventArgs e)
        {
            var parameterTypeEditorControl = Keyboard.FocusedElement as ParameterTypeEditorControl;

            if (parameterTypeEditorControl != null && parameterTypeEditorControl._vsTextView != null)
            {
                switch (e.Key)
                {
                    case Key.Escape:
                    case Key.Tab:
                    case Key.Enter:
                        e.Handled = true;
                        break;

                    default:
                        // Let the editor control handle the keystrokes
                        var msg = ComponentDispatcher.CurrentKeyboardMessage;

                        var oleInteropMsg = new OLE.Interop.MSG();

                        oleInteropMsg.hwnd = msg.hwnd;
                        oleInteropMsg.message = (uint)msg.message;
                        oleInteropMsg.wParam = msg.wParam;
                        oleInteropMsg.lParam = msg.lParam;
                        oleInteropMsg.pt.x = msg.pt_x;
                        oleInteropMsg.pt.y = msg.pt_y;

                        e.Handled = parameterTypeEditorControl.HandleKeyDown(oleInteropMsg);
                        break;
                }
            }
            else
            {
                if (e.Key == Key.Escape)
                {
                    //OnCancel();
                }
            }
        }

        private bool HandleKeyDown(OLE.Interop.MSG message)
        {
            uint editCmdID = 0;
            Guid editCmdGuid = Guid.Empty;
            int VariantSize = 16;

            var filterKeys = Package.GetGlobalService(typeof(SVsFilterKeys)) as IVsFilterKeys2;

            if (filterKeys != null)
            {
                int translated;
                int firstKeyOfCombo;
                var pMsg = new OLE.Interop.MSG[1];
                pMsg[0] = message;
                ErrorHandler.ThrowOnFailure(filterKeys.TranslateAcceleratorEx(pMsg,
                    (uint)(__VSTRANSACCELEXFLAGS.VSTAEXF_NoFireCommand | __VSTRANSACCELEXFLAGS.VSTAEXF_UseTextEditorKBScope | __VSTRANSACCELEXFLAGS.VSTAEXF_AllowModalState),
                    0,
                    null,
                    out editCmdGuid,
                    out editCmdID,
                    out translated,
                    out firstKeyOfCombo));

                if (translated == 1)
                {
                    var inArg = IntPtr.Zero;
                    try
                    {
                        // if the command is undo (Ctrl + Z) or redo (Ctrl + Y) then leave it as IntPtr.Zero because of a bug in undomgr.cpp where 
                        // it does undo or redo only for null, VT_BSTR and VT_EMPTY 
                        if ((int)message.wParam != Convert.ToInt32('Z') && (int)message.wParam != Convert.ToInt32('Y'))
                        {
                            inArg = Marshal.AllocHGlobal(VariantSize);
                            Marshal.GetNativeVariantForObject(message.wParam, inArg);
                        }

                        return Exec(ref editCmdGuid, editCmdID, 0, inArg, IntPtr.Zero) == VSConstants.S_OK;
                    }
                    finally
                    {
                        if (inArg != IntPtr.Zero)
                            Marshal.FreeHGlobal(inArg);
                    }
                }
            }

            // no translation available for this message
            return false;
        }

    }
}
