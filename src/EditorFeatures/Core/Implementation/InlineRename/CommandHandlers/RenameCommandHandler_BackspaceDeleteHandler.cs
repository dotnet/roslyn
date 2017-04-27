// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler : VSC.ICommandHandler<BackspaceKeyCommandArgs>, VSC.ICommandHandler<DeleteKeyCommandArgs>
    {
        public VSC.CommandState GetCommandState(BackspaceKeyCommandArgs args)
        {
            return GetCommandState();
        }

        public VSC.CommandState GetCommandState(DeleteKeyCommandArgs args)
        {
            return GetCommandState();
        }

        public bool ExecuteCommand(BackspaceKeyCommandArgs args)
        {
            return HandlePossibleTypingCommand(args, span =>
                {
                    var caretPoint = args.TextView.GetCaretPoint(args.SubjectBuffer);
                    if (!args.TextView.Selection.IsEmpty || caretPoint.Value != span.Start)
                    {
                        return false;   
                    }

                    return true;
                });
        }

        public bool ExecuteCommand(DeleteKeyCommandArgs args)
        {
            return HandlePossibleTypingCommand(args, span =>
                {
                    var caretPoint = args.TextView.GetCaretPoint(args.SubjectBuffer);
                    if (!args.TextView.Selection.IsEmpty || caretPoint.Value != span.End)
                    {
                        return false;
                    }

                    return true;
                });
        }
    }
}
