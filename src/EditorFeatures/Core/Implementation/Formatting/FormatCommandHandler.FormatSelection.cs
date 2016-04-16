// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Formatting
{
    internal partial class FormatCommandHandler
    {
        public CommandState GetCommandState(FormatSelectionCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(args.SubjectBuffer, nextHandler);
        }

        public void ExecuteCommand(FormatSelectionCommandArgs args, Action nextHandler)
        {
            if (!TryExecuteCommand(args))
            {
                nextHandler();
            }
        }

        private bool TryExecuteCommand(FormatSelectionCommandArgs args)
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

            var formattingService = document.GetLanguageService<IEditorFormattingService>();
            if (formattingService == null || !formattingService.SupportsFormatSelection)
            {
                return false;
            }

            var result = false;
            _waitIndicator.Wait(
                title: EditorFeaturesResources.FormatSelection,
                message: EditorFeaturesResources.FormattingCurrentlySelected,
                allowCancel: true,
                action: waitContext =>
            {
                var buffer = args.SubjectBuffer;

                // we only support single selection for now
                var selection = args.TextView.Selection.GetSnapshotSpansOnBuffer(buffer);
                if (selection.Count != 1)
                {
                    return;
                }

                var formattingSpan = selection[0].Span.ToTextSpan();

                Format(args.TextView, document, formattingSpan, waitContext.CancellationToken);

                // make behavior same as dev12. 
                // make sure we set selection back and set caret position at the end of selection
                // we can delete this code once razor side fixes a bug where it depends on this behavior (dev12) on formatting.
                var currentSelection = selection[0].TranslateTo(args.SubjectBuffer.CurrentSnapshot, SpanTrackingMode.EdgeExclusive);
                args.TextView.SetSelection(currentSelection);
                args.TextView.TryMoveCaretToAndEnsureVisible(currentSelection.End, ensureSpanVisibleOptions: EnsureSpanVisibleOptions.MinimumScroll);

                // We don't call nextHandler, since we have handled this command.
                result = true;
            });

            return result;
        }
    }
}
