// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        VSC.ICommandHandler<CutCommandArgs>, VSC.ICommandHandler<PasteCommandArgs>
    {
        public VSC.CommandState GetCommandState(CutCommandArgs args)
        {
            return VSC.CommandState.CommandIsUnavailable;
        }

        public bool ExecuteCommand(CutCommandArgs args)
        {
            return HandlePossibleTypingCommand(args, span => false);
        }

        public VSC.CommandState GetCommandState(PasteCommandArgs args)
        {
            return VSC.CommandState.CommandIsUnavailable;
        }

        public bool ExecuteCommand(PasteCommandArgs args)
        {
            return HandlePossibleTypingCommand(args, span => false);
        }
    }
}
