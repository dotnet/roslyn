// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.BlockCommentEditing
{
    internal abstract class AbstractBlockCommentEditingCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
    {
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        protected AbstractBlockCommentEditingCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            Contract.ThrowIfNull(undoHistoryRegistry);
            Contract.ThrowIfNull(editorOperationsFactoryService);

            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        public CommandState GetCommandState(ReturnKeyCommandArgs args, Func<CommandState> nextHandler) => nextHandler();

        public void ExecuteCommand(ReturnKeyCommandArgs args, Action nextHandler)
        {
            if (TryHandleReturnKey(args))
            {
                return;
            }

            nextHandler();
        }

        private bool TryHandleReturnKey(ReturnKeyCommandArgs args)
        {
            var subjectBuffer = args.SubjectBuffer;
            var textView = args.TextView;

            if (!subjectBuffer.GetOption(FeatureOnOffOptions.AutoInsertBlockCommentStartString))
            {
                return false;
            }

            var caretPosition = textView.GetCaretPoint(subjectBuffer);
            if (caretPosition == null)
            {
                return false;
            }

            var exteriorText = GetExteriorTextForNextLine(caretPosition.Value);
            if (exteriorText == null)
            {
                return false;
            }

            using (var transaction = _undoHistoryRegistry.GetHistory(args.TextView.TextBuffer).CreateTransaction(EditorFeaturesResources.InsertNewLine))
            {
                var editorOperations = _editorOperationsFactoryService.GetEditorOperations(args.TextView);

                editorOperations.InsertNewLine();
                editorOperations.InsertText(exteriorText);

                transaction.Complete();
                return true;
            }
        }

        protected abstract string GetExteriorTextForNextLine(SnapshotPoint caretPosition);
    }
}
