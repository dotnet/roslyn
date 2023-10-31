// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Design;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices;

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
            public const VSConstants.VSStd2KCmdID ListMembers = VSConstants.VSStd2KCmdID.SHOWMEMBERLIST;
            public const VSConstants.VSStd2KCmdID ParameterInfo = VSConstants.VSStd2KCmdID.PARAMINFO;
            public const VSConstants.VSStd2KCmdID ToggleCompletionMode = VSConstants.VSStd2KCmdID.ToggleConsumeFirstCompletionMode;
            public const VSConstants.VSStd97CmdID Undo = VSConstants.VSStd97CmdID.Undo;

            public static readonly CommandID GoToImplementation = new(Guids.RoslynGroupId, ID.RoslynCommands.GoToImplementation);
            public static readonly CommandID RemoveAndSort = new(VSConstants.CMDSETID.CSharpGroup_guid, 6419);

            // These were never added to VSConstants, but are defined in CommandHandlerServiceAdapter
            public const VSConstants.VSStd2KCmdID NextHighlightedReference = (VSConstants.VSStd2KCmdID)2400;
            public const VSConstants.VSStd2KCmdID PreviousHighlightedReference = (VSConstants.VSStd2KCmdID)2401;
        }
    }
}
