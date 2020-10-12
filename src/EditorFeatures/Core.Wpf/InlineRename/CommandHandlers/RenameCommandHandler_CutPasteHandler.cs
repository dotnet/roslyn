﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        IChainedCommandHandler<CutCommandArgs>, IChainedCommandHandler<PasteCommandArgs>
    {
        public CommandState GetCommandState(CutCommandArgs args, Func<CommandState> nextHandler)
            => nextHandler();

        public void ExecuteCommand(CutCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            HandlePossibleTypingCommand(args, nextHandler, span =>
            {
                nextHandler();
            });
        }

        public CommandState GetCommandState(PasteCommandArgs args, Func<CommandState> nextHandler)
            => nextHandler();

        public void ExecuteCommand(PasteCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            HandlePossibleTypingCommand(args, nextHandler, span =>
            {
                nextHandler();
            });
        }
    }
}
