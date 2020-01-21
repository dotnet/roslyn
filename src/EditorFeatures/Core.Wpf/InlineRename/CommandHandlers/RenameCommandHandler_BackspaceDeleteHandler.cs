// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler : IChainedCommandHandler<BackspaceKeyCommandArgs>, IChainedCommandHandler<DeleteKeyCommandArgs>
    {
        public CommandState GetCommandState(BackspaceKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(nextHandler);
        }

        public CommandState GetCommandState(DeleteKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(nextHandler);
        }

        public void ExecuteCommand(BackspaceKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            HandlePossibleTypingCommand(args, nextHandler, span =>
                {
                    var caretPoint = args.TextView.GetCaretPoint(args.SubjectBuffer);
                    if (!args.TextView.Selection.IsEmpty || caretPoint.Value != span.Start)
                    {
                        nextHandler();
                    }
                });
        }

        public void ExecuteCommand(DeleteKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            HandlePossibleTypingCommand(args, nextHandler, span =>
                {
                    var caretPoint = args.TextView.GetCaretPoint(args.SubjectBuffer);
                    if (!args.TextView.Selection.IsEmpty || caretPoint.Value != span.End)
                    {
                        nextHandler();
                    }
                });
        }
    }
}
