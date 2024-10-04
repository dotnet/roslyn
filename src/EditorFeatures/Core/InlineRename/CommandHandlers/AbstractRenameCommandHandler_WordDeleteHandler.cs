// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractRenameCommandHandler :
    ICommandHandler<WordDeleteToStartCommandArgs>,
    ICommandHandler<WordDeleteToEndCommandArgs>
{
    public CommandState GetCommandState(WordDeleteToStartCommandArgs args)
        => GetCommandState();

    public CommandState GetCommandState(WordDeleteToEndCommandArgs args)
        => GetCommandState();

    public bool ExecuteCommand(WordDeleteToStartCommandArgs args, CommandExecutionContext context)
        => HandleWordDeleteCommand(args.SubjectBuffer, args.TextView, deleteToStart: true);

    public bool ExecuteCommand(WordDeleteToEndCommandArgs args, CommandExecutionContext context)
        => HandleWordDeleteCommand(args.SubjectBuffer, args.TextView, deleteToStart: false);

    private bool HandleWordDeleteCommand(ITextBuffer subjectBuffer, ITextView view, bool deleteToStart)
    {
        if (renameService.ActiveSession == null)
        {
            return false;
        }

        var caretPoint = view.GetCaretPoint(subjectBuffer);
        if (caretPoint.HasValue)
        {
            if (renameService.ActiveSession.TryGetContainingEditableSpan(caretPoint.Value, out var span))
            {
                int start = caretPoint.Value;
                int end = caretPoint.Value;
                if (!view.Selection.IsEmpty)
                {
                    var selectedSpans = view.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);
                    if (selectedSpans.Count == 1 && span.Contains(selectedSpans.Single().Span))
                    {
                        // We might want to delete past the caret's active position if there's a selection
                        start = selectedSpans.Single().Start;
                        end = selectedSpans.Single().End;
                    }
                    else
                    {
                        // we're outside of an editable span, so let this command go to the next handler
                        return false;
                    }
                }

                subjectBuffer.Delete(deleteToStart
                    ? Span.FromBounds(span.Start, end)
                    : Span.FromBounds(start, span.End));

                return true;
            }
        }

        return false;
    }
}
