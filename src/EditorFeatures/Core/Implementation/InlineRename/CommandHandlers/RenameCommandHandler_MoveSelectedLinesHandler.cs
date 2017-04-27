// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        VSC.ICommandHandler<MoveSelectedLinesUpCommandArgs>, VSC.ICommandHandler<MoveSelectedLinesDownCommandArgs>
    {
        public VSC.CommandState GetCommandState(MoveSelectedLinesUpCommandArgs args)
        {
            return VSC.CommandState.CommandIsUnavailable;
        }

        public bool ExecuteCommand(MoveSelectedLinesUpCommandArgs args)
        {
            CommitIfActive(args.TextView);
            return false;
        }

        public VSC.CommandState GetCommandState(MoveSelectedLinesDownCommandArgs args)
        {
            return VSC.CommandState.CommandIsUnavailable;
        }

        public bool ExecuteCommand(MoveSelectedLinesDownCommandArgs args)
        {
            CommitIfActive(args.TextView);
            return false;
        }
    }
}
