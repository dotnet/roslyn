// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [Name(PredefinedCommandHandlerNames.Rename)]
    // Line commit and rename are both executed on Save. Ensure any rename session is committed
    // before line commit runs to ensure changes from both are correctly applied.
    [Order(Before = PredefinedCommandHandlerNames.Commit)]
    // Commit rename before invoking command-based refactorings
    [Order(Before = PredefinedCommandHandlerNames.ChangeSignature)]
    [Order(Before = PredefinedCommandHandlerNames.ExtractInterface)]
    [Order(Before = PredefinedCommandHandlerNames.EncapsulateField)]
    internal partial class RenameCommandHandler
    {
        private readonly InlineRenameService _renameService;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        [ImportingConstructor]
        public RenameCommandHandler(
            InlineRenameService renameService,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            _renameService = renameService;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        public string DisplayName => EditorFeaturesResources.Rename;

        private VSCommanding.CommandState GetCommandState(Func<VSCommanding.CommandState> nextHandler)
        {
            if (_renameService.ActiveSession != null)
            {
                return VSCommanding.CommandState.Available;
            }

            return nextHandler();
        }

        private VSCommanding.CommandState GetCommandState()
        {
            return _renameService.ActiveSession != null ? VSCommanding.CommandState.Available : VSCommanding.CommandState.Unspecified;
        }

        private void HandlePossibleTypingCommand(EditorCommandArgs args, Action nextHandler, Action<SnapshotSpan> actionIfInsideActiveSpan)
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
                actionIfInsideActiveSpan(containingSpan);
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
