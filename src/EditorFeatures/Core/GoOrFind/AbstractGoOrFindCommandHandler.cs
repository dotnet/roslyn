// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding;

namespace Microsoft.CodeAnalysis.GoOrFind;

internal abstract class AbstractGoOrFindCommandHandler<TCommandArgs>(
    IGoOrFindNavigationService navigationService) : ICommandHandler<TCommandArgs>
    where TCommandArgs : EditorCommandArgs
{
    private readonly IGoOrFindNavigationService _navigationService = navigationService;

    public string DisplayName => _navigationService.DisplayName;

    public CommandState GetCommandState(TCommandArgs args)
    {
        var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        return _navigationService.IsAvailable(document)
            ? CommandState.Available
            : CommandState.Unspecified;
    }

    public bool ExecuteCommand(TCommandArgs args, CommandExecutionContext context)
    {
        var subjectBuffer = args.SubjectBuffer;
        var caret = args.TextView.GetCaretPoint(subjectBuffer);
        if (!caret.HasValue)
            return false;

        var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (!_navigationService.IsAvailable(document))
            return false;

        // We were called on the current snapshot of a buffer, and we have a position within that buffer.
        // We should never have an invalid position here, so pass "false" for allowInvalidPosition so we
        // blow up if some invariant was broken.
        return _navigationService.ExecuteCommand(document, caret.Value.Position, allowInvalidPosition: false);
    }
}
