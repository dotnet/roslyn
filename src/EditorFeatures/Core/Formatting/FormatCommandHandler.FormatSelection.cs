// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal partial class FormatCommandHandler
    {
        public CommandState GetCommandState(FormatSelectionCommandArgs args)
            => GetCommandState(args.SubjectBuffer);

        public bool ExecuteCommand(FormatSelectionCommandArgs args, CommandExecutionContext context)
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

            // we only support single selection for now
            var selection = args.TextView.Selection.GetSnapshotSpansOnBuffer(args.SubjectBuffer);
            if (selection.Count != 1)
            {
                return false;
            }

            var token = _listener.BeginAsyncOperation(nameof(ExecuteCommand));
            _ = ExecuteCommandAsync(args, document, selection.Single()).CompletesAsyncOperation(token);

            return true;
        }

        private async Task ExecuteCommandAsync(FormatSelectionCommandArgs args, Document document, SnapshotSpan selectionSpan)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var visibleSpanCollection = args.TextView.GetSpanInView(selectionSpan);
            RoslynDebug.AssertNotNull(visibleSpanCollection);
            RoslynDebug.Assert(visibleSpanCollection.Count == 1);

            var visibleSpan = visibleSpanCollection.Single();
            SnapshotSpan indicatorSpan;

            if (!visibleSpan.Contains(selectionSpan) && visibleSpan.Start > selectionSpan.Start)
            {
                // The selection goes above what is visible to a user.
                // Put the indicator at the top of the view
                indicatorSpan = new SnapshotSpan(visibleSpan.Start, 0);
            }
            else
            {
                // Selection is within view but may go below the visible
                // span. Put the indicator at the beginning of the selection
                // to keep behavior easy to understand
                indicatorSpan = new SnapshotSpan(selectionSpan.Start, 0);
            }

            var backgroundWorkFactory = document.Project.Solution.Workspace.Services.GetRequiredService<IBackgroundWorkIndicatorFactory>();
            using var context = backgroundWorkFactory.Create(
                args.TextView,
                indicatorSpan,
                EditorFeaturesResources.Formatting_currently_selected_text);

            var formattingSpan = selectionSpan.Span.ToTextSpan();

            await Task.Delay(5000).ConfigureAwait(false);

            await FormatAsync(args.TextView, document, formattingSpan, context.UserCancellationToken).ConfigureAwait(false);

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

            // make behavior same as dev12.
            // make sure we set selection back and set caret position at the end of selection
            // we can delete this code once razor side fixes a bug where it depends on this behavior (dev12) on formatting.
            var currentSelection = selectionSpan.TranslateTo(args.SubjectBuffer.CurrentSnapshot, SpanTrackingMode.EdgeExclusive);
            args.TextView.SetSelection(currentSelection);
            args.TextView.TryMoveCaretToAndEnsureVisible(currentSelection.End, ensureSpanVisibleOptions: EnsureSpanVisibleOptions.MinimumScroll);
        }
    }
}
