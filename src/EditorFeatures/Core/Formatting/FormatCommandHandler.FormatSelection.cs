// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Formatting;

internal partial class FormatCommandHandler
{
    public CommandState GetCommandState(FormatSelectionCommandArgs args)
        => GetCommandState(args.SubjectBuffer);

    public bool ExecuteCommand(FormatSelectionCommandArgs args, CommandExecutionContext context)
        => TryExecuteCommand(args, context);

    private bool TryExecuteCommand(FormatSelectionCommandArgs args, CommandExecutionContext context)
    {
        if (!args.SubjectBuffer.CanApplyChangeDocumentToWorkspace())
        {
            return false;
        }

        var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
        {
            return false;
        }

        var formattingService = document.GetLanguageService<IFormattingInteractionService>();
        if (formattingService == null || !formattingService.SupportsFormatSelection)
        {
            return false;
        }

        using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Formatting_currently_selected_text))
        {
            var buffer = args.SubjectBuffer;

            // we only support single selection for now
            var selection = args.TextView.Selection.GetSnapshotSpansOnBuffer(buffer);
            if (selection.Count != 1)
            {
                return false;
            }

            var formattingSpan = selection[0].Span.ToTextSpan();

            Format(args.TextView, buffer, document, formattingSpan, context.OperationContext.UserCancellationToken);

            // make behavior same as dev12.
            // make sure we set selection back and set caret position at the end of selection
            // we can delete this code once razor side fixes a bug where it depends on this behavior (dev12) on formatting.
            var currentSelection = selection[0].TranslateTo(args.SubjectBuffer.CurrentSnapshot, SpanTrackingMode.EdgeExclusive);
            args.TextView.SetSelection(currentSelection);
            args.TextView.TryMoveCaretToAndEnsureVisible(currentSelection.End, ensureSpanVisibleOptions: EnsureSpanVisibleOptions.MinimumScroll);

            // We have handled this command
            return true;
        }
    }
}
