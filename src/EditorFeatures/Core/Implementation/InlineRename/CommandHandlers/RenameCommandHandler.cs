// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    // Line commit and rename are both executed on Save. Ensure any rename session is committed
    // before line commit runs to ensure changes from both are correctly applied.
    [Order(Before = PredefinedCommandHandlerNames.Commit)]
    // Commit rename before invoking command-based refactorings
    [Order(Before = PredefinedCommandHandlerNames.ChangeSignature)]
    [Order(Before = PredefinedCommandHandlerNames.ExtractInterface)]
    [Order(Before = PredefinedCommandHandlerNames.EncapsulateField)]
    [VSC.ExportCommandHandler(PredefinedCommandHandlerNames.Rename, ContentTypeNames.RoslynContentType, ContentTypeNames.XamlContentType)]
    internal partial class RenameCommandHandler
    {
        private readonly InlineRenameService _renameService;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;
        private readonly IWaitIndicator _waitIndicator;

        [ImportingConstructor]
        public RenameCommandHandler(
            InlineRenameService renameService,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            IWaitIndicator waitIndicator)
        {
            _renameService = renameService;
            _editorOperationsFactoryService = editorOperationsFactoryService;
            _waitIndicator = waitIndicator;
        }

        private CommandState2 GetCommandState(Func<CommandState2> nextHandler)
        {
            if (_renameService.ActiveSession != null)
            {
                return CommandState2.Available;
            }

            return nextHandler();
        }

        private Microsoft.VisualStudio.Text.UI.Commanding.CommandState GetCommandState()
        {
            if (_renameService.ActiveSession != null)
            {
                return Microsoft.VisualStudio.Text.UI.Commanding.CommandState.CommandIsAvailable;
            }

            return Microsoft.VisualStudio.Text.UI.Commanding.CommandState.CommandIsUnavailable;
        }

        private bool HandlePossibleTypingCommand(VSC.Commands.CommandArgs args, Func<SnapshotSpan, bool> actionIfInsideActiveSpan)
        {
            if (_renameService.ActiveSession == null)
            {
                return false;
            }

            var selectedSpans = args.TextView.Selection.GetSnapshotSpansOnBuffer(args.SubjectBuffer);

            if (selectedSpans.Count > 1)
            {
                // If we have multiple spans active, then that means we have something like box
                // selection going on. In this case, we'll just forward along.
                return false;
            }

            var singleSpan = selectedSpans.Single();
            if (_renameService.ActiveSession.TryGetContainingEditableSpan(singleSpan.Start, out var containingSpan) &&
                containingSpan.Contains(singleSpan))
            {
                return actionIfInsideActiveSpan(containingSpan);
            }
            else
            {
                // It's in a read-only area, so let's commit the rename and then let the character go
                // through

                return CommitIfActive(args.TextView);
            }
        }

        private bool HandlePossibleTypingCommand(ITextView textView, ITextBuffer subjectBuffer, Action<SnapshotSpan> actionIfInsideActiveSpan)
        {
            if (_renameService.ActiveSession == null)
            {
                return false;
            }

            var selectedSpans = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);

            if (selectedSpans.Count > 1)
            {
                // If we have multiple spans active, then that means we have something like box
                // selection going on. In this case, we'll just forward along.
                return false;
            }

            var singleSpan = selectedSpans.Single();
            if (_renameService.ActiveSession.TryGetContainingEditableSpan(singleSpan.Start, out var containingSpan) &&
                containingSpan.Contains(singleSpan))
            {
                actionIfInsideActiveSpan(containingSpan);
                return true;
            }
            else
            {
                // It's in a read-only area, so let's commit the rename and then let the character go
                // through

                CommitIfActive(textView);
                return false;
            }
        }

        private void CommitIfActiveAndCallNextHandler(CommandArgs args, Action nextHandler)
        {
            CommitIfActive(args.TextView);

            nextHandler();
        }

        private bool CommitIfActive(ITextView textView)
        {
            if (_renameService.ActiveSession != null)
            {
                var selection = textView.Selection.VirtualSelectedSpans.First();

                _renameService.ActiveSession.Commit();

                var translatedSelection = selection.TranslateTo(textView.TextBuffer.CurrentSnapshot);
                textView.Selection.Select(translatedSelection.Start, translatedSelection.End);
                textView.Caret.MoveTo(translatedSelection.End);
            }

            return true;
        }
    }
}
