// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Dumps commands in QueryStatus and Exec.
// #define DUMP_COMMANDS

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    internal sealed class VsInteractiveWindowCommandFilter : IOleCommandTarget
    {
        //
        // Command filter chain: 
        // *window* -> VsTextView -> ... -> *pre-language + the current language service's filter* -> editor services -> *preEditor* -> editor
        //
        private IOleCommandTarget _preLanguageCommandFilter;
        private IOleCommandTarget _editorServicesCommandFilter;
        private IOleCommandTarget _preEditorCommandFilter;
        private IOleCommandTarget _editorCommandFilter;
        // we route undo/redo commands to this target:
        internal IOleCommandTarget currentBufferCommandHandler;
        internal IOleCommandTarget firstLanguageServiceCommandFilter;
        private readonly IInteractiveWindow _window;
        internal readonly IVsTextView textViewAdapter;
        private readonly IWpfTextViewHost _textViewHost;
        internal readonly IEnumerable<Lazy<IVsInteractiveWindowOleCommandTargetProvider, ContentTypeMetadata>> _oleCommandTargetProviders;
        internal readonly IContentTypeRegistryService _contentTypeRegistry;

        public VsInteractiveWindowCommandFilter(IVsEditorAdaptersFactoryService adapterFactory, IInteractiveWindow window, IVsTextView textViewAdapter, IVsTextBuffer bufferAdapter, IEnumerable<Lazy<IVsInteractiveWindowOleCommandTargetProvider, ContentTypeMetadata>> oleCommandTargetProviders, IContentTypeRegistryService contentTypeRegistry)
        {
            _window = window;
            _oleCommandTargetProviders = oleCommandTargetProviders;
            _contentTypeRegistry = contentTypeRegistry;

            this.textViewAdapter = textViewAdapter;

            // make us a code window so we'll have the same colors as a normal code window.
            IVsTextEditorPropertyContainer propContainer;
            ErrorHandler.ThrowOnFailure(((IVsTextEditorPropertyCategoryContainer)textViewAdapter).GetPropertyCategory(Microsoft.VisualStudio.Editor.DefGuidList.guidEditPropCategoryViewMasterSettings, out propContainer));
            propContainer.SetProperty(VSEDITPROPID.VSEDITPROPID_ViewComposite_AllCodeWindowDefaults, true);
            propContainer.SetProperty(VSEDITPROPID.VSEDITPROPID_ViewGlobalOpt_AutoScrollCaretOnTextEntry, true);

            // editor services are initialized in textViewAdapter.Initialize - hook underneath them:
            _preEditorCommandFilter = new CommandFilter(this, CommandFilterLayer.PreEditor);
            ErrorHandler.ThrowOnFailure(textViewAdapter.AddCommandFilter(_preEditorCommandFilter, out _editorCommandFilter));

            textViewAdapter.Initialize(
                (IVsTextLines)bufferAdapter,
                IntPtr.Zero,
                (uint)TextViewInitFlags.VIF_HSCROLL | (uint)TextViewInitFlags.VIF_VSCROLL | (uint)TextViewInitFlags3.VIF_NO_HWND_SUPPORT,
                new[] { new INITVIEW { fSelectionMargin = 0, fWidgetMargin = 0, fVirtualSpace = 0, fDragDropMove = 1 } });

            // disable change tracking because everything will be changed
            var textViewHost = adapterFactory.GetWpfTextViewHost(textViewAdapter);

            _preLanguageCommandFilter = new CommandFilter(this, CommandFilterLayer.PreLanguage);
            ErrorHandler.ThrowOnFailure(textViewAdapter.AddCommandFilter(_preLanguageCommandFilter, out _editorServicesCommandFilter));

            _textViewHost = textViewHost;
        }

        private IOleCommandTarget TextViewCommandFilterChain
        {
            get
            {
                // Non-character command processing starts with WindowFrame which calls ReplWindow.Exec.
                // We need to invoke the view's Exec method in order to invoke its full command chain 
                // (features add their filters to the view).
                return (IOleCommandTarget)textViewAdapter;
            }
        }

        #region IOleCommandTarget

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            var nextTarget = TextViewCommandFilterChain;

            return nextTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var nextTarget = TextViewCommandFilterChain;

            return nextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        #endregion

        public IWpfTextViewHost TextViewHost
        {
            get
            {
                return _textViewHost;
            }
        }

        private enum CommandFilterLayer
        {
            PreLanguage,
            PreEditor
        }

        private sealed class CommandFilter : IOleCommandTarget
        {
            private readonly VsInteractiveWindowCommandFilter _window;
            private readonly CommandFilterLayer _layer;

            public CommandFilter(VsInteractiveWindowCommandFilter window, CommandFilterLayer layer)
            {
                _window = window;
                _layer = layer;
            }

            public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
            {
                try
                {
                    switch (_layer)
                    {
                        case CommandFilterLayer.PreLanguage:
                            return _window.PreLanguageCommandFilterQueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);

                        case CommandFilterLayer.PreEditor:
                            return _window.PreEditorCommandFilterQueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);

                        default:
                            throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(_layer);
                    }
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                    // Exceptions should not escape from command filters.
                    return _window._editorCommandFilter.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
                }
            }

            public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
            {
                try
                {
                    switch (_layer)
                    {
                        case CommandFilterLayer.PreLanguage:
                            return _window.PreLanguageCommandFilterExec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                        case CommandFilterLayer.PreEditor:
                            return _window.PreEditorCommandFilterExec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                        default:
                            throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(_layer);
                    }
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                    // Exceptions should not escape from command filters.
                    return _window._editorCommandFilter.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }
            }
        }

        private int PreEditorCommandFilterQueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == Guids.InteractiveCommandSetId)
            {
                switch ((CommandIds)prgCmds[0].cmdID)
                {
                    case CommandIds.BreakLine:
                        prgCmds[0].cmdf = _window.CurrentLanguageBuffer != null ? CommandEnabled : CommandDisabled;
                        prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_DEFHIDEONCTXTMENU;
                        return VSConstants.S_OK;
                }
            }
            else if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                switch ((VSConstants.VSStd97CmdID)prgCmds[0].cmdID)
                {
                    // TODO: Add support of rotating clipboard ring 
                    // https://github.com/dotnet/roslyn/issues/5651
                    case VSConstants.VSStd97CmdID.PasteNextTBXCBItem:
                        prgCmds[0].cmdf = CommandDisabled;
                        return VSConstants.S_OK;
                }
            }

            return _editorCommandFilter.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        private int PreEditorCommandFilterExec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var nextTarget = _editorCommandFilter;

            if (pguidCmdGroup == Guids.InteractiveCommandSetId)
            {
                switch ((CommandIds)nCmdID)
                {
                    case CommandIds.BreakLine:
                        if (_window.Operations.BreakLine())
                        {
                            return VSConstants.S_OK;
                        }
                        break;
                }
            }
            else if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.TYPECHAR:
                        // No-op since character was inserted in pre-language service filter below
                        return VSConstants.S_OK;

                    case VSConstants.VSStd2KCmdID.RETURN:
                        if (_window.Operations.Return())
                        {
                            return VSConstants.S_OK;
                        }
                        break;

                    // TODO: 
                    //case VSConstants.VSStd2KCmdID.DELETEWORDLEFT:
                    //case VSConstants.VSStd2KCmdID.DELETEWORDRIGHT:
                    //    break;

                    case VSConstants.VSStd2KCmdID.BACKSPACE:
                        _window.Operations.Backspace();
                        return VSConstants.S_OK;

                    case VSConstants.VSStd2KCmdID.UP:

                        if (_window.CurrentLanguageBuffer != null && !_window.IsRunning && CaretAtEnd && UseSmartUpDown)
                        {
                            _window.Operations.HistoryPrevious();
                            return VSConstants.S_OK;
                        }
                        break;

                    case VSConstants.VSStd2KCmdID.DOWN:
                        if (_window.CurrentLanguageBuffer != null && !_window.IsRunning && CaretAtEnd && UseSmartUpDown)
                        {
                            _window.Operations.HistoryNext();
                            return VSConstants.S_OK;
                        }
                        break;

                    case VSConstants.VSStd2KCmdID.CANCEL:
                        if (_window.TextView.Selection.IsEmpty)
                        {
                            _window.Operations.Cancel();
                        }
                        break;

                    case VSConstants.VSStd2KCmdID.BOL:
                        _window.Operations.Home(false);
                        return VSConstants.S_OK;

                    case VSConstants.VSStd2KCmdID.BOL_EXT:
                        _window.Operations.Home(true);
                        return VSConstants.S_OK;

                    case VSConstants.VSStd2KCmdID.EOL:
                        _window.Operations.End(false);
                        return VSConstants.S_OK;

                    case VSConstants.VSStd2KCmdID.EOL_EXT:
                        _window.Operations.End(true);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.CUTLINE:
                        {
                            var operations = _window.Operations as IInteractiveWindowOperations2;
                            if (operations != null)
                            {
                                operations.CutLine();
                                return VSConstants.S_OK;
                            }
                        }
                        break;
                    case VSConstants.VSStd2KCmdID.DELETELINE:
                        {
                            var operations = _window.Operations as IInteractiveWindowOperations2;
                            if (operations != null)
                            {
                                operations.DeleteLine();
                                return VSConstants.S_OK;
                            }
                        }
                        break;
                }
            }
            else if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                switch ((VSConstants.VSStd97CmdID)nCmdID)
                {
                    // TODO: Add support of rotating clipboard ring 
                    // https://github.com/dotnet/roslyn/issues/5651
                    case VSConstants.VSStd97CmdID.PasteNextTBXCBItem:
                        return (int)OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;

                    case VSConstants.VSStd97CmdID.Paste:
                        _window.Operations.Paste();
                        return VSConstants.S_OK;

                    case VSConstants.VSStd97CmdID.Cut:
                        _window.Operations.Cut();
                        return VSConstants.S_OK;

                    case VSConstants.VSStd97CmdID.Copy:
                        {
                            var operations = _window.Operations as IInteractiveWindowOperations2;
                            if (operations != null)
                            {
                                operations.Copy();
                                return VSConstants.S_OK;
                            }
                        }
                        break;

                    case VSConstants.VSStd97CmdID.Delete:
                        _window.Operations.Delete();
                        return VSConstants.S_OK;

                    case VSConstants.VSStd97CmdID.SelectAll:
                        _window.Operations.SelectAll();
                        return VSConstants.S_OK;
                }
            }

            return nextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        private int PreLanguageCommandFilterQueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            var nextTarget = firstLanguageServiceCommandFilter ?? _editorServicesCommandFilter;

            if (pguidCmdGroup == Guids.InteractiveCommandSetId)
            {
                switch ((CommandIds)prgCmds[0].cmdID)
                {
                    case CommandIds.HistoryNext:
                    case CommandIds.HistoryPrevious:
                    case CommandIds.SearchHistoryNext:
                    case CommandIds.SearchHistoryPrevious:
                    case CommandIds.SmartExecute:
                        // TODO: Submit?
                        prgCmds[0].cmdf = _window.CurrentLanguageBuffer != null ? CommandEnabled : CommandDisabled;
                        prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_DEFHIDEONCTXTMENU;
                        return VSConstants.S_OK;
                    case CommandIds.AbortExecution:
                        prgCmds[0].cmdf = _window.IsRunning ? CommandEnabled : CommandDisabled;
                        prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_DEFHIDEONCTXTMENU;
                        return VSConstants.S_OK;
                    case CommandIds.Reset:
                        prgCmds[0].cmdf = !_window.IsResetting ? CommandEnabled : CommandDisabled;
                        prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_DEFHIDEONCTXTMENU;
                        return VSConstants.S_OK;
                    default:
                        prgCmds[0].cmdf = CommandEnabled;
                        break;
                }
                prgCmds[0].cmdf |= (uint)OLECMDF.OLECMDF_DEFHIDEONCTXTMENU;
            }
            else if (currentBufferCommandHandler != null && pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                // undo/redo support:
                switch ((VSConstants.VSStd97CmdID)prgCmds[0].cmdID)
                {
                    case VSConstants.VSStd97CmdID.Undo:
                    case VSConstants.VSStd97CmdID.MultiLevelUndo:
                    case VSConstants.VSStd97CmdID.MultiLevelUndoList:
                    case VSConstants.VSStd97CmdID.Redo:
                    case VSConstants.VSStd97CmdID.MultiLevelRedo:
                    case VSConstants.VSStd97CmdID.MultiLevelRedoList:
                        return currentBufferCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
                }
            }

            if (nextTarget != null)
            {
                var result = nextTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
#if DUMP_COMMANDS
            //DumpCmd("QS", result, ref pguidCmdGroup, prgCmds[0].cmdID, prgCmds[0].cmdf);
#endif
                return result;
            }
            return VSConstants.E_FAIL;
        }

        private int PreLanguageCommandFilterExec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var nextTarget = firstLanguageServiceCommandFilter ?? _editorServicesCommandFilter;

            if (pguidCmdGroup == Guids.InteractiveCommandSetId)
            {
                switch ((CommandIds)nCmdID)
                {
                    case CommandIds.AbortExecution: _window.Evaluator.AbortExecution(); return VSConstants.S_OK;
                    case CommandIds.Reset: _window.Operations.ResetAsync(); return VSConstants.S_OK;
                    case CommandIds.SmartExecute: _window.Operations.ExecuteInput(); return VSConstants.S_OK;
                    case CommandIds.HistoryNext: _window.Operations.HistoryNext(); return VSConstants.S_OK;
                    case CommandIds.HistoryPrevious: _window.Operations.HistoryPrevious(); return VSConstants.S_OK;
                    case CommandIds.ClearScreen: _window.Operations.ClearView(); return VSConstants.S_OK;
                    case CommandIds.SearchHistoryNext:
                        _window.Operations.HistorySearchNext();
                        return VSConstants.S_OK;
                    case CommandIds.SearchHistoryPrevious:
                        _window.Operations.HistorySearchPrevious();
                        return VSConstants.S_OK;
                }
            }
            else if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.TYPECHAR:
                        {
                            var operations = _window.Operations as IInteractiveWindowOperations2;
                            if (operations != null)
                            {
                                char typedChar = (char)(ushort)System.Runtime.InteropServices.Marshal.GetObjectForNativeVariant(pvaIn);
                                operations.TypeChar(typedChar);
                            }
                            else
                            {
                                _window.Operations.Delete();
                            }
                            break;
                        }

                    case VSConstants.VSStd2KCmdID.RETURN:
                        if (_window.Operations.TrySubmitStandardInput())
                        {
                            return VSConstants.S_OK;
                        }
                        break;

                    case VSConstants.VSStd2KCmdID.SHOWCONTEXTMENU:
                        ShowContextMenu();
                        return VSConstants.S_OK;
                }
            }
            else if (currentBufferCommandHandler != null && pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                // undo/redo support:
                switch ((VSConstants.VSStd97CmdID)nCmdID)
                {
                    // TODO: remove (https://github.com/dotnet/roslyn/issues/5642)
                    case VSConstants.VSStd97CmdID.FindReferences:
                        return VSConstants.S_OK;
                    case VSConstants.VSStd97CmdID.Undo:
                    case VSConstants.VSStd97CmdID.MultiLevelUndo:
                    case VSConstants.VSStd97CmdID.MultiLevelUndoList:
                    case VSConstants.VSStd97CmdID.Redo:
                    case VSConstants.VSStd97CmdID.MultiLevelRedo:
                    case VSConstants.VSStd97CmdID.MultiLevelRedoList:
                        return currentBufferCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }
            }

            int res = nextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
#if DUMP_COMMANDS
            DumpCmd("Exec", result, ref pguidCmdGroup, nCmdID, 0);
#endif
            return res;
        }

        private void ShowContextMenu()
        {
            var uishell = (IVsUIShell)InteractiveWindowPackage.GetGlobalService(typeof(SVsUIShell));
            if (uishell != null)
            {
                var pt = System.Windows.Forms.Cursor.Position;
                var position = new[] { new POINTS { x = (short)pt.X, y = (short)pt.Y } };
                var guid = Guids.InteractiveCommandSetId;
                ErrorHandler.ThrowOnFailure(uishell.ShowContextMenu(0, ref guid, (int)MenuIds.InteractiveWindowContextMenu, position, this));
            }
        }

        private const uint CommandEnabled = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
        private const uint CommandDisabled = (uint)(OLECMDF.OLECMDF_SUPPORTED);
        private const uint CommandDisabledAndHidden = (uint)(OLECMDF.OLECMDF_INVISIBLE | OLECMDF.OLECMDF_SUPPORTED);

        private bool CaretAtEnd
        {
            get
            {
                var caret = _window.TextView.Caret;
                return caret.Position.BufferPosition.Position == caret.Position.BufferPosition.Snapshot.Length;
            }
        }

        private bool UseSmartUpDown
        {
            get
            {
                return _window.TextView.Options.GetOptionValue(InteractiveWindowOptions.SmartUpDown);
            }
        }

        public IOleCommandTarget EditorServicesCommandFilter
        {
            get
            {
                return _editorServicesCommandFilter;
            }
        }
    }
}
