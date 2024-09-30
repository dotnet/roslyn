// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractRenameCommandHandler : ICommandHandler<EscapeKeyCommandArgs>
{
    public CommandState GetCommandState(EscapeKeyCommandArgs args)
        => GetCommandState();

    public bool ExecuteCommand(EscapeKeyCommandArgs args, CommandExecutionContext context)
    {
        if (_renameService.ActiveSession != null)
        {
            _renameService.ActiveSession.Cancel();
            SetFocusToTextView(args.TextView);
            // When ESC is pressed, don't handle the command here because rename might rely on
            // BackgroundWorkIndicator to show the progress. Let platform propagates ESC to let indicator also get cancelled.
        }

        return false;
    }
}
