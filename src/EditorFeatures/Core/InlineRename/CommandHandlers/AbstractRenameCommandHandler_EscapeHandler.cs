// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractRenameCommandHandler : ICommandHandler<EscapeKeyCommandArgs>
{
    public CommandState GetCommandState(EscapeKeyCommandArgs args)
        => GetCommandState();

    public bool ExecuteCommand(EscapeKeyCommandArgs args, CommandExecutionContext context)
    {
        if (_renameService.ActiveSession != null)
        {
            _ = CancelAsync(args).ReportNonFatalErrorAsync();
            return true;
        }

        return false;
    }

    private async Task CancelAsync(EscapeKeyCommandArgs args)
    {
        RoslynDebug.AssertNotNull(_renameService.ActiveSession);
        // ConfigureAwait(true) since we need to set focus back to UI later.
        await _renameService.ActiveSession.CancelAsync().ConfigureAwait(true);
        SetFocusToTextView(args.TextView);
    }
}
