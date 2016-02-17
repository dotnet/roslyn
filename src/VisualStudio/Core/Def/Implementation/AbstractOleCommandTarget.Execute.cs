// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal abstract partial class AbstractOleCommandTarget
    {
        public virtual int Exec(ref Guid pguidCmdGroup, uint commandId, uint executeInformation, IntPtr pvaIn, IntPtr pvaOut)
        {
            try
            {
                var subjectBuffer = GetSubjectBufferContainingCaret();
                this.CurrentlyExecutingCommand = commandId;

                // If we didn't get a subject buffer, then that means we're outside our code and we should ignore it
                // Also, ignore the command if executeInformation indicates isn't meant to be executed. From env\msenv\core\cmdwin.cpp:
                //      To query the parameter type list of a command, we call Exec with 
                //      the LOWORD of nCmdexecopt set to OLECMDEXECOPT_SHOWHELP (instead of
                //      the more usual OLECMDEXECOPT_DODEFAULT), the HIWORD of nCmdexecopt
                //      set to VSCmdOptQueryParameterList, pvaIn set to NULL, and pvaOut 
                //      pointing to a VARIANT ready to receive the result BSTR.
                var shouldSkipCommand = executeInformation == (((uint)VsMenus.VSCmdOptQueryParameterList << 16) | (uint)OLECMDEXECOPT.OLECMDEXECOPT_SHOWHELP);
                if (subjectBuffer == null || shouldSkipCommand)
                {
                    return NextCommandTarget.Exec(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
                }

                var contentType = subjectBuffer.ContentType;

                if (pguidCmdGroup == VSConstants.VSStd2K)
                {
                    return ExecuteVisualStudio2000(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut, subjectBuffer, contentType);
                }
                else if (pguidCmdGroup == Guids.CSharpGroupId)
                {
                    return ExecuteCSharpGroup(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut, subjectBuffer, contentType);
                }
                else if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
                {
                    return ExecuteVisualStudio97(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut, subjectBuffer, contentType);
                }
                else if (pguidCmdGroup == VSConstants.VsStd11)
                {
                    return ExecuteVisualStudio11(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut, subjectBuffer, contentType);
                }
                else if (pguidCmdGroup == VSConstants.VsStd14)
                {
                    return ExecuteVisualStudio2014(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut, subjectBuffer, contentType);
                }
                else if (pguidCmdGroup == VSConstants.GUID_AppCommand)
                {
                    return ExecuteAppCommand(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut, subjectBuffer, contentType);
                }
                else if (pguidCmdGroup == VSConstants.VsStd12)
                {
                    return ExecuteVisualStudio2013(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut, subjectBuffer, contentType);
                }
                else if (pguidCmdGroup == Guids.RoslynGroupId)
                {
                    return ExecuteRoslyn(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut, subjectBuffer, contentType);
                }
                else
                {
                    return NextCommandTarget.Exec(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
                }
            }
            finally
            {
                this.CurrentlyExecutingCommand = default(uint);
            }
        }

        private int ExecuteAppCommand(ref Guid pguidCmdGroup, uint commandId, uint executeInformation, IntPtr pvaIn, IntPtr pvaOut, ITextBuffer subjectBuffer, IContentType contentType)
        {
            int result = VSConstants.S_OK;
            var guidCmdGroup = pguidCmdGroup;
            Action executeNextCommandTarget = () =>
            {
                result = NextCommandTarget.Exec(ref guidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            };

            switch ((VSConstants.AppCommandCmdID)commandId)
            {
                case VSConstants.AppCommandCmdID.BrowserBackward:
                    ExecuteBrowserBackward(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.AppCommandCmdID.BrowserForward:
                    ExecuteBrowserForward(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                default:
                    return NextCommandTarget.Exec(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            }

            return result;
        }

        private int ExecuteVisualStudio2014(ref Guid pguidCmdGroup, uint commandId, uint executeInformation, IntPtr pvaIn, IntPtr pvaOut, ITextBuffer subjectBuffer, IContentType contentType)
        {
            int result = VSConstants.S_OK;
            var guidCmdGroup = pguidCmdGroup;
            Action executeNextCommandTarget = () =>
            {
                result = NextCommandTarget.Exec(ref guidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            };

            switch ((VSConstants.VSStd14CmdID)commandId)
            {
                case VSConstants.VSStd14CmdID.SmartBreakLine:
                    ExecuteAutomaticLineEnder(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                default:
                    return NextCommandTarget.Exec(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            }

            return result;
        }

        private int ExecuteVisualStudio2013(ref Guid pguidCmdGroup, uint commandId, uint executeInformation, IntPtr pvaIn, IntPtr pvaOut, ITextBuffer subjectBuffer, IContentType contentType)
        {
            int result = VSConstants.S_OK;
            var guidCmdGroup = pguidCmdGroup;
            Action executeNextCommandTarget = () =>
            {
                result = NextCommandTarget.Exec(ref guidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            };

            switch ((VSConstants.VSStd12CmdID)commandId)
            {
                case VSConstants.VSStd12CmdID.MoveSelLinesDown:
                    ExecuteMoveSelectedLinesDown(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd12CmdID.MoveSelLinesUp:
                    ExecuteMoveSelectedLinesUp(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                default:
                    return NextCommandTarget.Exec(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            }

            return result;
        }

        private int ExecuteVisualStudio97(ref Guid pguidCmdGroup, uint commandId, uint executeInformation, IntPtr pvaIn, IntPtr pvaOut, ITextBuffer subjectBuffer, IContentType contentType)
        {
            int result = VSConstants.S_OK;
            var guidCmdGroup = pguidCmdGroup;
            Action executeNextCommandTarget = () =>
            {
                result = NextCommandTarget.Exec(ref guidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            };

            switch ((VSConstants.VSStd97CmdID)commandId)
            {
                case VSConstants.VSStd97CmdID.GotoDefn:
                    ExecuteGoToDefinition(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd97CmdID.FindReferences:
                    ExecuteFindReferences(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd97CmdID.SyncClassView:
                    ExecuteSyncClassView(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd97CmdID.Paste:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecutePaste(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd97CmdID.Delete:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteDelete(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd97CmdID.SelectAll:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteSelectAll(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd97CmdID.Undo:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteUndo(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd97CmdID.Redo:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteRedo(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd97CmdID.MultiLevelUndo:
                case VSConstants.VSStd97CmdID.MultiLevelRedo:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    if (pvaOut == IntPtr.Zero)
                    {
                        // mirror logic in COleUndoManager::Exec
                        int count = 1;
                        if (pvaIn != IntPtr.Zero)
                        {
                            object o = Marshal.GetObjectForNativeVariant(pvaIn);
                            if (o == null || o is string)
                            {
                                count = 1;
                            }
                            else if (o is int)
                            {
                                count = (int)o;
                            }
                            else
                            {
                                count = -1; // we don't want to handle this case
                            }
                        }

                        if (count > 0)
                        {
                            if ((VSConstants.VSStd97CmdID)commandId == VSConstants.VSStd97CmdID.MultiLevelUndo)
                            {
                                ExecuteUndo(subjectBuffer, contentType, executeNextCommandTarget, count: count);
                            }
                            else
                            {
                                ExecuteRedo(subjectBuffer, contentType, executeNextCommandTarget, count: count);
                            }

                            break;
                        }
                    }

                    goto default;

                default:
                    return NextCommandTarget.Exec(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            }

            return result;
        }

        private int ExecuteCSharpGroup(ref Guid pguidCmdGroup, uint commandId, uint executeInformation, IntPtr pvaIn, IntPtr pvaOut, ITextBuffer subjectBuffer, IContentType contentType)
        {
            int result = VSConstants.S_OK;
            var guidCmdGroup = pguidCmdGroup;
            Action executeNextCommandTarget = () =>
            {
                result = NextCommandTarget.Exec(ref guidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            };

            switch (commandId)
            {
                case ID.CSharpCommands.OrganizeSortUsings:
                case ID.CSharpCommands.ContextOrganizeSortUsings:
                    ExecuteSortUsings(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case ID.CSharpCommands.OrganizeRemoveUnusedUsings:
                case ID.CSharpCommands.ContextOrganizeRemoveUnusedUsings:
                    ExecuteRemoveUnusedUsings(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case ID.CSharpCommands.OrganizeRemoveAndSort:
                case ID.CSharpCommands.ContextOrganizeRemoveAndSort:
                    ExecuteSortAndRemoveUnusedUsings(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                default:
                    return NextCommandTarget.Exec(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            }

            return result;
        }

        private int ExecuteRoslyn(ref Guid pguidCmdGroup, uint commandId, uint executeInformation, IntPtr pvaIn, IntPtr pvaOut, ITextBuffer subjectBuffer, IContentType contentType)
        {
            int result = VSConstants.S_OK;
            var guidCmdGroup = pguidCmdGroup;
            Action executeNextCommandTarget = () =>
            {
                result = NextCommandTarget.Exec(ref guidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            };

            switch (commandId)
            {
                case ID.RoslynCommands.GoToImplementation:
                    ExecuteGoToImplementation(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                default:
                    return NextCommandTarget.Exec(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            }

            return result;
        }

        protected virtual int ExecuteVisualStudio2000(ref Guid pguidCmdGroup, uint commandId, uint executeInformation, IntPtr pvaIn, IntPtr pvaOut, ITextBuffer subjectBuffer, IContentType contentType)
        {
            int result = VSConstants.S_OK;
            var guidCmdGroup = pguidCmdGroup;
            Action executeNextCommandTarget = () =>
            {
                result = NextCommandTarget.Exec(ref guidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            };

            switch ((VSConstants.VSStd2KCmdID)commandId)
            {
                case VSConstants.VSStd2KCmdID.TYPECHAR:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteTypeCharacter(pvaIn, subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.RETURN:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteReturn(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.TAB:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteTab(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.BACKTAB:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteBackTab(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.HOME:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteDocumentStart(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.END:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteDocumentEnd(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.BOL:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteLineStart(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.BOL_EXT:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteLineStartExtend(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.EOL:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteLineEnd(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.EOL_EXT:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteLineEndExtend(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.SELECTALL:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteSelectAll(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.OPENLINEABOVE:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteOpenLineAbove(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.OPENLINEBELOW:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteOpenLineBelow(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.UP:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteUp(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.DOWN:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteDown(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.PAGEDN:
                    ExecutePageDown(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.PAGEUP:
                    ExecutePageUp(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.CANCEL:
                    ExecuteCancel(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.BACKSPACE:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteBackspace(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.DELETE:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteDelete(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.DELETEWORDLEFT:
                    ExecuteWordDeleteToStart(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.DELETEWORDRIGHT:
                    ExecuteWordDeleteToEnd(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.FORMATDOCUMENT:
                    ExecuteFormatDocument(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.FORMATSELECTION:
                    ExecuteFormatSelection(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.ECMD_INSERTCOMMENT:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteInsertComment(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case CmdidToggleConsumeFirstMode:
                    ExecuteToggleConsumeFirstMode(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case CmdidNextHighlightedReference:
                    ExecuteNextHighlightedReference(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case CmdidPreviousHighlightedReference:
                    ExecutePreviousHighlightedReference(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.COMMENTBLOCK:
                case VSConstants.VSStd2KCmdID.COMMENT_BLOCK:
                    ExecuteCommentBlock(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK:
                case VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK:
                    ExecuteUncommentBlock(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteCommitUniqueCompletionItem(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.SHOWMEMBERLIST:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteInvokeCompletionList(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.PARAMINFO:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteParameterInfo(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.QUICKINFO:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteQuickInfo(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.RENAME:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteRename(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.EXTRACTINTERFACE:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteExtractInterface(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.EXTRACTMETHOD:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteExtractMethod(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.PASTE:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecutePaste(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.INSERTSNIPPET:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteInsertSnippet(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.SURROUNDWITH:
                    GCManager.UseLowLatencyModeForProcessingUserInput();
                    ExecuteSurroundWith(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.ViewCallHierarchy:
                    ExecuteViewCallHierarchy(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.ENCAPSULATEFIELD:
                    ExecuteEncapsulateField(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.REMOVEPARAMETERS:
                    ExecuteRemoveParameters(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.REORDERPARAMETERS:
                    ExecuteReorderParameters(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.ECMD_NEXTMETHOD:
                    ExecuteGoToNextMethod(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                case VSConstants.VSStd2KCmdID.ECMD_PREVMETHOD:
                    ExecuteGoToPreviousMethod(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                default:
                    return NextCommandTarget.Exec(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            }

            return result;
        }

        private void ExecuteEncapsulateField(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new EncapsulateFieldCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        private void ExecuteRemoveParameters(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new RemoveParametersCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        private void ExecuteReorderParameters(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new ReorderParametersCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        private void ExecuteGoToNextMethod(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new GoToAdjacentMemberCommandArgs(ConvertTextView(), subjectBuffer, NavigateDirection.Down),
                lastHandler: executeNextCommandTarget);
        }

        private void ExecuteGoToPreviousMethod(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new GoToAdjacentMemberCommandArgs(ConvertTextView(), subjectBuffer, NavigateDirection.Up),
                lastHandler: executeNextCommandTarget);
        }

        private int ExecuteVisualStudio11(ref Guid pguidCmdGroup, uint commandId, uint executeInformation, IntPtr pvaIn, IntPtr pvaOut, ITextBuffer subjectBuffer, IContentType contentType)
        {
            int result = VSConstants.S_OK;
            var guidCmdGroup = pguidCmdGroup;
            Action executeNextCommandTarget = () =>
            {
                result = NextCommandTarget.Exec(ref guidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            };

            switch ((VSConstants.VSStd11CmdID)commandId)
            {
                case VSConstants.VSStd11CmdID.ExecuteSelectionInInteractive:
                    ExecuteExecuteInInteractiveWindow(subjectBuffer, contentType, executeNextCommandTarget);
                    break;

                default:
                    return NextCommandTarget.Exec(ref pguidCmdGroup, commandId, executeInformation, pvaIn, pvaOut);
            }

            return result;
        }

        private void ExecuteMoveSelectedLinesUp(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new MoveSelectedLinesUpCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        private void ExecuteMoveSelectedLinesDown(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new MoveSelectedLinesDownCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        private void ExecuteAutomaticLineEnder(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new AutomaticLineEnderCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteExtractInterface(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new ExtractInterfaceCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteExtractMethod(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new ExtractMethodCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteViewCallHierarchy(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new ViewCallHierarchyCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteInsertSnippet(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new InsertSnippetCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteSurroundWith(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new SurroundWithCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteRename(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new RenameCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteQuickInfo(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new InvokeQuickInfoCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteParameterInfo(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new InvokeSignatureHelpCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteCommitUniqueCompletionItem(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new CommitUniqueCompletionListItemCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteInvokeCompletionList(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new InvokeCompletionListCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteUncommentBlock(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new UncommentSelectionCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteCommentBlock(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new CommentSelectionCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecutePreviousHighlightedReference(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new NavigateToHighlightedReferenceCommandArgs(ConvertTextView(), subjectBuffer, NavigateDirection.Up),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteNextHighlightedReference(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new NavigateToHighlightedReferenceCommandArgs(ConvertTextView(), subjectBuffer, NavigateDirection.Down),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteToggleConsumeFirstMode(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new ToggleCompletionModeCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteInsertComment(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new InsertCommentCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteFormatDocument(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new FormatDocumentCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteFormatSelection(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new FormatSelectionCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteBackspace(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new BackspaceKeyCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteDelete(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new DeleteKeyCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteWordDeleteToStart(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new WordDeleteToStartCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteWordDeleteToEnd(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new WordDeleteToEndCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteCancel(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new EscapeKeyCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecutePageUp(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new PageUpKeyCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecutePageDown(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new PageDownKeyCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteDown(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new DownKeyCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteUp(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new UpKeyCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteDocumentStart(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new DocumentStartCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteDocumentEnd(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new DocumentEndCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteLineStart(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new LineStartCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteLineStartExtend(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new LineStartExtendCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteLineEnd(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new LineEndCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteLineEndExtend(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new LineEndExtendCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteSelectAll(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new SelectAllCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteOpenLineAbove(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new OpenLineAboveCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteOpenLineBelow(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new OpenLineBelowCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteUndo(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget, int count = 1)
        {
            CurrentHandlers.Execute(contentType,
                args: new UndoCommandArgs(ConvertTextView(), subjectBuffer, count),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteRedo(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget, int count = 1)
        {
            CurrentHandlers.Execute(contentType,
                args: new RedoCommandArgs(ConvertTextView(), subjectBuffer, count),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteTab(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new TabKeyCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteBackTab(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new BackTabKeyCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteReturn(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new ReturnKeyCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecuteTypeCharacter(IntPtr pvaIn, ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            var typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            CurrentHandlers.Execute(contentType,
                args: new TypeCharCommandArgs(ConvertTextView(), subjectBuffer, typedChar),
                lastHandler: executeNextCommandTarget);
        }

        private void ExecuteGoToDefinition(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new GoToDefinitionCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        private void ExecuteGoToImplementation(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new GoToImplementationCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        private void ExecuteFindReferences(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new FindReferencesCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        private void ExecuteSyncClassView(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new SyncClassViewCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        protected void ExecutePaste(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new PasteCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        private void ExecuteSortUsings(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new SortImportsCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        private void ExecuteRemoveUnusedUsings(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new RemoveUnnecessaryImportsCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        private void ExecuteSortAndRemoveUnusedUsings(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new SortAndRemoveUnnecessaryImportsCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        private void ExecuteExecuteInInteractiveWindow(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            CurrentHandlers.Execute(contentType,
                args: new ExecuteInInteractiveCommandArgs(ConvertTextView(), subjectBuffer),
                lastHandler: executeNextCommandTarget);
        }

        private void ExecuteBrowserBackward(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            ExecuteBrowserNavigationCommand(navigateBackward: true, executeNextCommandTarget: executeNextCommandTarget);
        }

        private void ExecuteBrowserForward(ITextBuffer subjectBuffer, IContentType contentType, Action executeNextCommandTarget)
        {
            ExecuteBrowserNavigationCommand(navigateBackward: false, executeNextCommandTarget: executeNextCommandTarget);
        }

        private void ExecuteBrowserNavigationCommand(bool navigateBackward, Action executeNextCommandTarget)
        {
            // We just want to delegate to the shell's NavigateBackward/Forward commands
            var target = _serviceProvider.GetService(typeof(SUIHostCommandDispatcher)) as IOleCommandTarget;
            if (target != null)
            {
                var cmd = (uint)(navigateBackward ?
                     VSConstants.VSStd97CmdID.ShellNavBackward :
                     VSConstants.VSStd97CmdID.ShellNavForward);

                OLECMD[] cmds = new[] { new OLECMD() { cmdf = 0, cmdID = cmd } };
                var hr = target.QueryStatus(VSConstants.GUID_VSStandardCommandSet97, 1, cmds, IntPtr.Zero);
                if (hr == VSConstants.S_OK && (cmds[0].cmdf & (uint)OLECMDF.OLECMDF_ENABLED) != 0)
                {
                    // ignore failure
                    target.Exec(VSConstants.GUID_VSStandardCommandSet97, cmd, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, IntPtr.Zero, IntPtr.Zero);
                    return;
                }
            }

            executeNextCommandTarget();
        }
    }
}
