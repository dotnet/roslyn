// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.OLE.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal abstract partial class AbstractOleCommandTarget
    {
        public int QueryStatus(ref Guid pguidCmdGroup, uint commandCount, OLECMD[] prgCmds, IntPtr commandText)
        {
            Contract.ThrowIfFalse(commandCount == 1);
            Contract.ThrowIfFalse(prgCmds.Length == 1);

            // TODO: We'll need to extend the command handler interfaces at some point when we have commands that
            // require enabling/disabling at some point.  For now, we just enable the few that we care about.
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                return QueryVisualStudio2000Status(ref pguidCmdGroup, commandCount, prgCmds, commandText);
            }
            else if (pguidCmdGroup == Guids.CSharpGroupId)
            {
                return QueryCSharpGroupStatus(ref pguidCmdGroup, commandCount, prgCmds, commandText);
            }
            else if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                return QueryVisualStudio97Status(ref pguidCmdGroup, commandCount, prgCmds, commandText);
            }
            else if (pguidCmdGroup == VSConstants.VsStd14)
            {
                return QueryVisualStudio2014Status(ref pguidCmdGroup, commandCount, prgCmds, commandText);
            }
            else if (pguidCmdGroup == VSConstants.GUID_AppCommand)
            {
                return QueryAppCommandStatus(ref pguidCmdGroup, commandCount, prgCmds, commandText);
            }
            else if (pguidCmdGroup == Guids.RoslynGroupId)
            {
                return QueryRoslynGroupStatus(ref pguidCmdGroup, commandCount, prgCmds, commandText);
            }
            else
            {
                return NextCommandTarget.QueryStatus(ref pguidCmdGroup, commandCount, prgCmds, commandText);
            }
        }

        private int QueryAppCommandStatus(ref Guid pguidCmdGroup, uint commandCount, OLECMD[] prgCmds, IntPtr commandText)
        {
            switch ((VSConstants.AppCommandCmdID)prgCmds[0].cmdID)
            {
                case VSConstants.AppCommandCmdID.BrowserBackward:
                case VSConstants.AppCommandCmdID.BrowserForward:
                    prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                    return VSConstants.S_OK;

                default:
                    return NextCommandTarget.QueryStatus(ref pguidCmdGroup, commandCount, prgCmds, commandText);
            }
        }

        private int QueryVisualStudio2014Status(ref Guid pguidCmdGroup, uint commandCount, OLECMD[] prgCmds, IntPtr commandText)
        {
            switch ((VSConstants.VSStd14CmdID)prgCmds[0].cmdID)
            {
                case VSConstants.VSStd14CmdID.SmartBreakLine:
                    prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE);
                    return VSConstants.S_OK;

                default:
                    return NextCommandTarget.QueryStatus(ref pguidCmdGroup, commandCount, prgCmds, commandText);
            }
        }

        private int QueryVisualStudio97Status(ref Guid pguidCmdGroup, uint commandCount, OLECMD[] prgCmds, IntPtr commandText)
        {
            switch ((VSConstants.VSStd97CmdID)prgCmds[0].cmdID)
            {
                case VSConstants.VSStd97CmdID.GotoDefn:
                case VSConstants.VSStd97CmdID.FindReferences:
                case VSConstants.VSStd97CmdID.SyncClassView:
                    prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                    return VSConstants.S_OK;

                default:
                    return NextCommandTarget.QueryStatus(ref pguidCmdGroup, commandCount, prgCmds, commandText);
            }
        }

        private int QueryCSharpGroupStatus(ref Guid pguidCmdGroup, uint commandCount, OLECMD[] prgCmds, IntPtr commandText)
        {
            switch (prgCmds[0].cmdID)
            {
                case ID.CSharpCommands.OrganizeRemoveAndSort:
                case ID.CSharpCommands.ContextOrganizeRemoveAndSort:
                    prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                    return VSConstants.S_OK;

                default:
                    return NextCommandTarget.QueryStatus(ref pguidCmdGroup, commandCount, prgCmds, commandText);
            }
        }

        private int QueryRoslynGroupStatus(ref Guid pguidCmdGroup, uint commandCount, OLECMD[] prgCmds, IntPtr commandText)
        {
            switch (prgCmds[0].cmdID)
            {
                case ID.RoslynCommands.GoToImplementation:
                    prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                    return VSConstants.S_OK;

                default:
                    return NextCommandTarget.QueryStatus(ref pguidCmdGroup, commandCount, prgCmds, commandText);
            }
        }

        private int QueryVisualStudio2000Status(ref Guid pguidCmdGroup, uint commandCount, OLECMD[] prgCmds, IntPtr commandText)
        {
            switch ((VSConstants.VSStd2KCmdID)prgCmds[0].cmdID)
            {
                case VSConstants.VSStd2KCmdID.FORMATDOCUMENT:
                    prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                    return VSConstants.S_OK;

                default:
                    return NextCommandTarget.QueryStatus(ref pguidCmdGroup, commandCount, prgCmds, commandText);
            }
        }

        private int QuerySortAndRemoveUnusedUsingsStatus(ref Guid pguidCmdGroup, uint commandCount, OLECMD[] prgCmds, IntPtr commandText)
        {
            var textBuffer = GetSubjectBufferContainingCaret();

            if (textBuffer != null)
            {
                if (CodeAnalysis.Workspace.TryGetWorkspace(textBuffer.AsTextContainer(), out var workspace))
                {
                    var organizeImportsService = workspace.Services.GetLanguageServices(textBuffer).GetService<IOrganizeImportsService>();
                    if (organizeImportsService != null)
                    {
                        SetText(commandText, organizeImportsService.SortAndRemoveUnusedImportsDisplayStringWithAccelerator);
                    }
                }
            }

            return NextCommandTarget.QueryStatus(ref pguidCmdGroup, commandCount, prgCmds, commandText);
        }

        private static unsafe void SetText(IntPtr pCmdTextInt, string text)
        {
            var pText = (OLECMDTEXT*)pCmdTextInt;

            // If, for some reason, we don't get passed an array, we should just bail
            if (pText->cwBuf == 0)
            {
                return;
            }

            fixed (char* pinnedText = text)
            {
                var src = pinnedText;
                var dest = (char*)(&pText->rgwz);

                // Don't copy too much, and make sure to reserve space for the terminator
                var length = Math.Min(text.Length, (int)pText->cwBuf - 1);

                for (var i = 0; i < length; i++)
                {
                    *dest++ = *src++;
                }

                // Add terminating NUL
                *dest = '\0';

                pText->cwActual = (uint)length;
            }
        }
    }
}
