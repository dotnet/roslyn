// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.InlineRename;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractRenameCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
{
    public CommandState GetCommandState(ReturnKeyCommandArgs args)
        => GetCommandState();

    public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
    {
        if (renameService.ActiveSession != null)
        {
            CommitAndSetFocus(renameService.ActiveSession, args.TextView, context.OperationContext);
            return true;
        }

        return false;
    }

    protected virtual void CommitAndSetFocus(InlineRenameSession activeSession, ITextView textView, IUIThreadOperationContext operationContext)
    {
        _ = activeSession.CommitAsync(previewChanges: false, operationContext).ReportNonFatalErrorAsync();
        SetFocusToTextView(textView);
    }
}
