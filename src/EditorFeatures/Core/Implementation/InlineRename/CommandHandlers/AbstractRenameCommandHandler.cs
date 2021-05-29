﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal abstract partial class AbstractRenameCommandHandler
    {
        private readonly IThreadingContext _threadingContext;
        private readonly InlineRenameService _renameService;

        protected AbstractRenameCommandHandler(
            IThreadingContext threadingContext,
            InlineRenameService renameService)
        {
            _threadingContext = threadingContext;
            _renameService = renameService;
        }

        public string DisplayName => EditorFeaturesResources.Rename;

        protected abstract bool DashboardShouldReceiveKeyboardNavigation(ITextView textView);

        protected abstract void SetFocusToTextView(ITextView textView);

        protected abstract void SetFocusToDashboard(ITextView textView);

        protected abstract void SetDashboardFocusToPreviousElement(ITextView textView);

        protected abstract void SetDashboardFocusToNextElement(ITextView textView);

        private CommandState GetCommandState(Func<CommandState> nextHandler)
        {
            if (_renameService.ActiveSession != null)
            {
                return CommandState.Available;
            }

            return nextHandler();
        }

        private CommandState GetCommandState()
            => _renameService.ActiveSession != null ? CommandState.Available : CommandState.Unspecified;

        private void HandlePossibleTypingCommand<TArgs>(TArgs args, Action nextHandler, Action<InlineRenameSession, SnapshotSpan> actionIfInsideActiveSpan)
            where TArgs : EditorCommandArgs
        {
            if (_renameService.ActiveSession == null)
            {
                nextHandler();
                return;
            }

            var selectedSpans = args.TextView.Selection.GetSnapshotSpansOnBuffer(args.SubjectBuffer);

            if (selectedSpans.Count > 1)
            {
                // If we have multiple spans active, then that means we have something like box
                // selection going on. In this case, we'll just forward along.
                nextHandler();
                return;
            }

            var singleSpan = selectedSpans.Single();
            if (_renameService.ActiveSession.TryGetContainingEditableSpan(singleSpan.Start, out var containingSpan) &&
                containingSpan.Contains(singleSpan))
            {
                actionIfInsideActiveSpan(_renameService.ActiveSession, containingSpan);
            }
            else
            {
                // It's in a read-only area, so let's commit the rename and then let the character go
                // through

                CommitIfActiveAndCallNextHandler(args, nextHandler);
            }
        }

        private void CommitIfActive(EditorCommandArgs args)
        {
            if (_renameService.ActiveSession != null)
            {
                var selection = args.TextView.Selection.VirtualSelectedSpans.First();

                _renameService.ActiveSession.Commit();

                var translatedSelection = selection.TranslateTo(args.TextView.TextBuffer.CurrentSnapshot);
                args.TextView.Selection.Select(translatedSelection.Start, translatedSelection.End);
                args.TextView.Caret.MoveTo(translatedSelection.End);
            }
        }

        private void CommitIfActiveAndCallNextHandler(EditorCommandArgs args, Action nextHandler)
        {
            CommitIfActive(args);
            nextHandler();
        }
    }
}
