// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities.Internal;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls
{
    /// <summary>
    /// Enables in place editing of a text block by placing an editor control over it.
    /// </summary>
    internal class IntellisenseTextBox : FrameworkElement, IOleCommandTarget
    {
        /// <summary>
        /// IWpfTextView container
        /// </summary>
        private IWpfTextViewHost _textViewHost;

        /// <summary>
        /// The reference to the next command handler in the view filter chain
        /// </summary>
        private IOleCommandTarget _nextCommandTarget = null;

        /// <summary>
        /// Used for instructing the editor control to carry out certain operations
        /// </summary>
        private IEditorOperations _editorOperations;

        /// <summary>
        /// Appearance category of text view in the intellisense textbox
        /// </summary>
        private const string appearanceCategory = "IntellisenseTextblock";

        /// <summary>
        /// Initializes a new instance of the <see cref="IntellisenseTextBox"/> class.
        /// </summary>
        public IntellisenseTextBox(IntellisenseTextBoxViewModel viewModel, ContentControl container)
        {
            this.InitializeEditorControl(viewModel, container);
        }

        /// <summary>
        /// Gets a value indicating whether there is an active intellisense session.
        /// </summary>
        public bool HasActiveIntellisenseSession
        {
            get
            {
                if (this._textViewHost != null)
                {
                    if (this._textViewHost.TextView.Properties.TryGetProperty(typeof(ICompletionBroker), out ICompletionBroker completionBroker))
                    {
                        return completionBroker.IsCompletionActive(this._textViewHost.TextView);
                    }

                    if (this._textViewHost.TextView.Properties.TryGetProperty(typeof(IIntellisenseSessionStack), out IIntellisenseSessionStack intellisenseSessionStack))
                    {
                        return intellisenseSessionStack.Sessions.Count > 0;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the text in the control
        /// </summary>
        public string Text
        {
            get => this._textViewHost.TextView.TextSnapshot.GetText();
        }

        ///// <summary>
        ///// Return the count of visual children.
        ///// </summary>
        protected override int VisualChildrenCount
        {
            get => 1;
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
                return (int)OLE.Interop.Constants.OLECMDERR_E_UNKNOWNGROUP;
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
            else if ((pguidCmdGroup == VsMenus.guidStandardCommandSet97 && cmdID == StandardCommands.Cut.ID) ||
                    (pguidCmdGroup == VsMenus.guidStandardCommandSet2K && cmdID == (uint)VSConstants.VSStd2KCmdID.CUT))
            {
                _editorOperations.CutSelection();
                hr = VSConstants.S_OK;
            }
            else if ((pguidCmdGroup == VsMenus.guidStandardCommandSet97 && cmdID == StandardCommands.Copy.ID) ||
                (pguidCmdGroup == VsMenus.guidStandardCommandSet2K && cmdID == (uint)VSConstants.VSStd2KCmdID.COPY))
            {
                _editorOperations.CopySelection();
                hr = VSConstants.S_OK;
            }
            else if ((pguidCmdGroup == VsMenus.guidStandardCommandSet97 && cmdID == StandardCommands.Paste.ID) ||
                    (pguidCmdGroup == VsMenus.guidStandardCommandSet2K && cmdID == (uint)VSConstants.VSStd2KCmdID.PASTE))
            {
                _editorOperations.Paste();
                hr = VSConstants.S_OK;
            }
            else
            {
                hr = (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
            }

            return hr;
        }

        /// <summary>
        /// Queries command status
        /// Queries command status
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

                    if (this.CanCutCopy())
                    {
                        prgCmds[i].cmdf |= (uint)OLECMDF.OLECMDF_ENABLED;
                    }
                }
                else if ((pguidCmdGroup == VsMenus.guidStandardCommandSet97 && prgCmds[i].cmdID == StandardCommands.Paste.ID) ||
                    (pguidCmdGroup == VsMenus.guidStandardCommandSet2K && prgCmds[i].cmdID == (uint)VSConstants.VSStd2KCmdID.PASTE))
                {
                    prgCmds[i].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;

                    if (this.CanPaste())
                    {
                        prgCmds[i].cmdf |= (uint)OLECMDF.OLECMDF_ENABLED;
                    }
                }
                else
                {
                    prgCmds[i].cmdf = 0;
                }
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns visual child at given index
        /// </summary>
        /// <param name="index">child index</param>
        /// <returns>returns visual child</returns>
        protected override Visual GetVisualChild(int index)
        {
            if (this._textViewHost != null)
            {
                return this._textViewHost.HostControl;
            }

            return null;
        }

        /// <summary>
        /// Arrange visual children
        /// </summary>
        /// <param name="finalSize">final size to arrange children</param>
        /// <returns>return arrange size</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (this._textViewHost != null)
            {
                this._textViewHost.HostControl.Arrange(new Rect(new Point(0, 0), finalSize));
            }

            return finalSize;
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            e.Handled = true;
            this._textViewHost.TextView.VisualElement.Focus();
        }

        /// <summary>
        /// Initializes the editor control
        /// </summary>
        private void InitializeEditorControl(IntellisenseTextBoxViewModel viewModel, ContentControl container)
        {
            IComponentModel componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));

            // Sets editor options that control its final look
            IEditorOptions editorOptions = viewModel.WpfTextView.Properties.GetProperty(typeof(IEditorOptions)) as IEditorOptions;
            editorOptions.SetOptionValue("TextViewHost/ZoomControl", false);
            editorOptions.SetOptionValue(DefaultWpfViewOptions.AppearanceCategory, appearanceCategory);

            // Set the font used in the editor
            // The editor will automatically choose the font, color for the text
            // depending on the current language. Override the font information
            // to have uniform look and feel in the parallel watch window
            IClassificationFormatMapService classificationFormatMapService = componentModel.GetService<IClassificationFormatMapService>();
            IClassificationFormatMap classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(appearanceCategory);
            Typeface typeface = new Typeface(container.FontFamily, container.FontStyle, container.FontWeight, container.FontStretch);
            classificationFormatMap.DefaultTextProperties = classificationFormatMap.DefaultTextProperties.SetTypeface(typeface);
            classificationFormatMap.DefaultTextProperties = classificationFormatMap.DefaultTextProperties.SetFontRenderingEmSize(container.FontSize);

            // Install this object in the text view command filter chain
            IEditorOperationsFactoryService editorOperationsFactoryService = componentModel.GetService<IEditorOperationsFactoryService>();
            if (editorOperationsFactoryService != null)
            {
                this._editorOperations = editorOperationsFactoryService.GetEditorOperations(viewModel.WpfTextView);
            }

            ErrorHandler.ThrowOnFailure(viewModel.VsTextView.AddCommandFilter(this, out this._nextCommandTarget));

            // Get the host control to render the view
            IVsEditorAdaptersFactoryService editorAdapterFactory = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            this._textViewHost = editorAdapterFactory.GetWpfTextViewHost(viewModel.VsTextView);

            // For non-blurry text
            TextOptions.SetTextFormattingMode(this._textViewHost.HostControl, TextFormattingMode.Display);
            this._textViewHost.HostControl.Loaded += this.HostControl_Loaded;

            this.AddLogicalChild(this._textViewHost.HostControl);
            this.AddVisualChild(this._textViewHost.HostControl);
        }

        /// <summary>
        /// Event handler for editor control load
        /// </summary>
        /// <param name="sender">Editor control</param>
        /// <param name="e">Event args</param>
        private void HostControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (this._textViewHost != null)
            {
                // Set the focus
                this._textViewHost.TextView.VisualElement.Focus();

                if (this._textViewHost != null)
                {
                    ITextSnapshot textSnapshot = this._textViewHost.TextView.TextSnapshot;
                    if (this.Text.IsNullOrWhiteSpace())
                    {
                        // Select all text in the control if edit is not started by user keyboard input
                        ITextSelection textSelection = this._textViewHost.TextView.Selection;
                        textSelection.Select(new SnapshotSpan(textSnapshot, 0, textSnapshot.Length), false);
                    }
                    else
                    {
                        // Otherwise Move to caret to end
                        ITextCaret textCaret = this._textViewHost.TextView.Caret;
                        textCaret.MoveTo(new SnapshotPoint(textSnapshot, textSnapshot.Length));
                    }
                }
            }
        }

        /// <summary>
        /// Determines if we can cut or copy in the editor control
        /// </summary>
        /// <returns>True if cut and copy operation should be enabled</returns>
        private bool CanCutCopy()
        {
            if (this._textViewHost != null)
            {
                ITextSelection textSelection = this._textViewHost.TextView.Selection;

                if (textSelection.SelectedSpans != null && textSelection.SelectedSpans.Count > 0)
                {
                    foreach (SnapshotSpan snapshotSpan in textSelection.SelectedSpans)
                    {
                        if (snapshotSpan.Length > 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        [DllImport("user32.dll")]
        [PreserveSig]
        private static extern int GetPriorityClipboardFormat([In] uint[] formatPriorityList, [In] int formatCount);

        /// <summary>
        /// Determines if we can paste to the editor control
        /// </summary>
        /// <returns>True, if paste is supported</returns>
        private bool CanPaste()
        {
            const uint TextFormatId = 1;          // ID for clipboard text format (CF_TEXT)
            const uint UnicodeTextFormatId = 13;  // ID for clipboard unicode text format (CF_UNICODETEXT)

            // We should avoid calling Clipboard.GetDataObject() as it will hang if the clipboard owner is blocked while
            // putting data into the clipboard using delay rendering (Dev14 bug: 1142153, Dev10 bug: 788188)
            uint[] formats = new uint[] { TextFormatId, UnicodeTextFormatId };

            // GetPriorityClipboardFormat returns 0 if the clipboard is empty, -1 if there is data in the clipboard but it does not match
            // any of the input formats and otherwise returns the ID of the format available in the clipboard.
            return GetPriorityClipboardFormat(formats, formats.Length) > 0;
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
                    case VSConstants.VSStd2KCmdID.CUT:
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
                    case VSConstants.VSStd2KCmdID.SHOWMEMBERLIST:
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

        public bool HandleKeyDown()
        {
            System.Windows.Interop.MSG msg = ComponentDispatcher.CurrentKeyboardMessage;
            var oleInteropMsg = new OLE.Interop.MSG();

            oleInteropMsg.hwnd = msg.hwnd;
            oleInteropMsg.message = (uint)msg.message;
            oleInteropMsg.wParam = msg.wParam;
            oleInteropMsg.lParam = msg.lParam;
            oleInteropMsg.pt.x = msg.pt_x;
            oleInteropMsg.pt.y = msg.pt_y;

            return HandleKeyDown(oleInteropMsg);
        }

        /// <summary>Converts the given window message for keystroke into a shell command and executes it</summary>
        /// <param name="message">Message representing the keypress command to handle</param>
        /// <returns>True if the command is handled (converted and executed properly), false otherwise</returns>
        /// <remarks>This method is needed when the control is not able to receive VS shell commands directly.
        /// For instance, it might be hosted in a modal dialog. It generates the proper VS commands 
        /// and posts it to the internal editor control.</remarks>
        public bool HandleKeyDown(OLE.Interop.MSG message)
        {
            uint editCmdID;
            Guid editCmdGuid;
            int VariantSize = 16;

            IVsFilterKeys2 filterKeys = Package.GetGlobalService(typeof(SVsFilterKeys)) as IVsFilterKeys2;

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
                    IntPtr inArg = IntPtr.Zero;
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
