// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview;

internal sealed partial class DifferenceViewerPreview
{
    private sealed class NavigationalCommandTarget(ITextView textView, IEditorOperations editorOperations) : IOleCommandTarget
    {
        private readonly ITextView _textView = textView;
        private readonly IEditorOperations _editorOperations = editorOperations;

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (IsCommandAllowed(pguidCmdGroup, prgCmds[0].cmdID))
            {
                prgCmds[0].cmdf |= (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                return VSConstants.S_OK;
            }

            return (int)Constants.OLECMDERR_E_UNKNOWNGROUP;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.UP:
                        this._editorOperations.MoveLineUp(false);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.UP_EXT:
                        if (this._textView.Selection.IsEmpty)
                        {
                            this._textView.Caret.MoveTo(this._textView.Caret.Position.VirtualBufferPosition);
                        }
                        this._editorOperations.MoveLineUp(true);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.DOWN:
                        this._editorOperations.MoveLineDown(false);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.DOWN_EXT:
                        if (this._textView.Selection.IsEmpty)
                        {
                            this._textView.Caret.MoveTo(this._textView.Caret.Position.VirtualBufferPosition);
                        }
                        this._editorOperations.MoveLineDown(true);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.LEFT:
                        this._editorOperations.MoveToPreviousCharacter(false);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.LEFT_EXT:
                        this._editorOperations.MoveToPreviousCharacter(true);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.RIGHT:
                        this._editorOperations.MoveToNextCharacter(false);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.RIGHT_EXT:
                        this._editorOperations.MoveToNextCharacter(true);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.EditorLineFirstColumn:
                        this._editorOperations.MoveToStartOfLine(false);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.EditorLineFirstColumnExtend:
                        this._editorOperations.MoveToStartOfLine(true);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.PAGEUP:
                        this._editorOperations.PageUp(false);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.PAGEUP_EXT:
                        this._editorOperations.PageUp(true);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.PAGEDN:
                        this._editorOperations.PageDown(false);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.PAGEDN_EXT:
                        this._editorOperations.PageDown(true);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.HOME:
                        this._editorOperations.MoveToStartOfDocument(false);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.HOME_EXT:
                        this._editorOperations.MoveToStartOfDocument(true);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.END:
                        this._editorOperations.MoveToEndOfDocument(false);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.END_EXT:
                        this._editorOperations.MoveToEndOfDocument(true);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.SELECTALL:
                        this._editorOperations.SelectAll();
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.COPY:
                        this._editorOperations.CopySelection();
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.BOL:
                        this._editorOperations.MoveToHome(false);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.BOL_EXT:
                        this._editorOperations.MoveToHome(true);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.FIRSTCHAR:
                        this._editorOperations.MoveToStartOfLineAfterWhiteSpace(false);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.FIRSTCHAR_EXT:
                        this._editorOperations.MoveToStartOfLineAfterWhiteSpace(true);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.EOL:
                        this._editorOperations.MoveToEndOfLine(false);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.EOL_EXT:
                        this._editorOperations.MoveToEndOfLine(true);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.LASTCHAR:
                        this._editorOperations.MoveToLastNonWhiteSpaceCharacter(false);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.LASTCHAR_EXT:
                        this._editorOperations.MoveToLastNonWhiteSpaceCharacter(true);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.TOPLINE:
                        this._editorOperations.MoveToTopOfView(false);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.TOPLINE_EXT:
                        this._editorOperations.MoveToTopOfView(true);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.BOTTOMLINE:
                        this._editorOperations.MoveToBottomOfView(false);
                        return VSConstants.S_OK;
                    case VSConstants.VSStd2KCmdID.BOTTOMLINE_EXT:
                        this._editorOperations.MoveToBottomOfView(true);
                        return VSConstants.S_OK;
                }
            }

            return (int)Constants.OLECMDERR_E_UNKNOWNGROUP;
        }
    }
}
