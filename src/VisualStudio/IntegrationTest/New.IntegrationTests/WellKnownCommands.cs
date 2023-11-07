// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Design;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices;
using InteractiveShell = Microsoft.VisualStudio.InteractiveWindow.Shell;

namespace Roslyn.VisualStudio.IntegrationTests
{
    internal static class WellKnownCommands
    {
        public static class Debug
        {
            public const VSConstants.VSStd97CmdID Immediate = VSConstants.VSStd97CmdID.ImmediateWindow;
        }

        public static class Edit
        {
            public const VSConstants.VSStd97CmdID ClearAll = VSConstants.VSStd97CmdID.ClearPane;
            public const VSConstants.VSStd97CmdID Copy = VSConstants.VSStd97CmdID.Copy;
            public const VSConstants.VSStd97CmdID Cut = VSConstants.VSStd97CmdID.Cut;
            public const VSConstants.VSStd97CmdID Delete = VSConstants.VSStd97CmdID.Delete;
            public const VSConstants.VSStd2KCmdID LineDown = VSConstants.VSStd2KCmdID.DOWN;
            public const VSConstants.VSStd2KCmdID LineEnd = VSConstants.VSStd2KCmdID.EOL;
            public const VSConstants.VSStd2KCmdID LineEndExtend = VSConstants.VSStd2KCmdID.EOL_EXT;
            public const VSConstants.VSStd2KCmdID LineStart = VSConstants.VSStd2KCmdID.BOL;
            public const VSConstants.VSStd2KCmdID LineStartExtend = VSConstants.VSStd2KCmdID.BOL_EXT;
            public const VSConstants.VSStd2KCmdID LineUp = VSConstants.VSStd2KCmdID.UP;
            public const VSConstants.VSStd2KCmdID ListMembers = VSConstants.VSStd2KCmdID.SHOWMEMBERLIST;
            public const VSConstants.VSStd2KCmdID ParameterInfo = VSConstants.VSStd2KCmdID.PARAMINFO;
            public const VSConstants.VSStd97CmdID Paste = VSConstants.VSStd97CmdID.Paste;
            public const VSConstants.VSStd97CmdID Redo = VSConstants.VSStd97CmdID.Redo;
            public const VSConstants.VSStd97CmdID SelectAll = VSConstants.VSStd97CmdID.SelectAll;
            public const VSConstants.VSStd2KCmdID SelectionCancel = VSConstants.VSStd2KCmdID.CANCEL;
            public const VSConstants.VSStd2KCmdID ToggleCompletionMode = VSConstants.VSStd2KCmdID.ToggleConsumeFirstCompletionMode;
            public const VSConstants.VSStd97CmdID Undo = VSConstants.VSStd97CmdID.Undo;

            public static readonly CommandID GoToImplementation = new(Guids.RoslynGroupId, ID.RoslynCommands.GoToImplementation);
            public static readonly CommandID RemoveAndSort = new(VSConstants.CMDSETID.CSharpGroup_guid, 6419);

            // These were never added to VSConstants, but are defined in CommandHandlerServiceAdapter
            public const VSConstants.VSStd2KCmdID NextHighlightedReference = (VSConstants.VSStd2KCmdID)2400;
            public const VSConstants.VSStd2KCmdID PreviousHighlightedReference = (VSConstants.VSStd2KCmdID)2401;
        }

        public static class InteractiveConsole
        {
            /// <seealso cref="InteractiveShell.CommandIds.ClearScreen"/>
            public static readonly CommandID ClearScreen = new(InteractiveShell::Guids.InteractiveCommandSetId, 264);

            /// <seealso cref="InteractiveShell.CommandIds.ExecuteInInteractiveWindow"/>
            public static readonly CommandID ExecuteInInteractive = new(InteractiveShell::Guids.InteractiveCommandSetId, 268);
        }

        public static class ProjectandSolutionContextMenus
        {
            public static class Project
            {
                public static readonly CommandID ResetCSharpInteractiveFromProject = new(ID.InteractiveCommands.CSharpInteractiveCommandSetId, ID.InteractiveCommands.ResetInteractiveFromProject);
            }
        }

        public static class Refactor
        {
            public const VSConstants.VSStd2KCmdID EncapsulateField = VSConstants.VSStd2KCmdID.ENCAPSULATEFIELD;
            public const VSConstants.VSStd2KCmdID ExtractInterface = VSConstants.VSStd2KCmdID.EXTRACTINTERFACE;
            public const VSConstants.VSStd2KCmdID ExtractMethod = VSConstants.VSStd2KCmdID.EXTRACTMETHOD;
            public const VSConstants.VSStd2KCmdID RemoveParameters = VSConstants.VSStd2KCmdID.REMOVEPARAMETERS;
            public const VSConstants.VSStd2KCmdID Rename = VSConstants.VSStd2KCmdID.RENAME;
            public const VSConstants.VSStd2KCmdID ReorderParameters = VSConstants.VSStd2KCmdID.REORDERPARAMETERS;
        }
    }
}
