// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        ICommandHandler<CutCommandArgs>, ICommandHandler<PasteCommandArgs>
    {
        public CommandState GetCommandState(CutCommandArgs args, Func<CommandState> nextHandler)
        {
            return nextHandler();
        }

        public void ExecuteCommand(CutCommandArgs args, Action nextHandler)
        {
            HandlePossibleTypingCommand(args, nextHandler, span =>
            {
                nextHandler();
            });
        }

        public CommandState GetCommandState(PasteCommandArgs args, Func<CommandState> nextHandler)
        {
            return nextHandler();
        }

        public void ExecuteCommand(PasteCommandArgs args, Action nextHandler)
        {
            HandlePossibleTypingCommand(args, nextHandler, span =>
            {
                nextHandler();
            });
        }
    }
}
