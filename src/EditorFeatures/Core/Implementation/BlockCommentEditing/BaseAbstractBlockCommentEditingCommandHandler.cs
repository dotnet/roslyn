// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.BlockCommentEditing
{
    /// <summary>
    /// Command system independent abstract Block Comment Editing Command Handler.
    /// </summary>
    internal abstract class BaseAbstractBlockCommentEditingCommandHandler
    {
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        protected BaseAbstractBlockCommentEditingCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            Contract.ThrowIfNull(undoHistoryRegistry);
            Contract.ThrowIfNull(editorOperationsFactoryService);

            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        protected bool TryHandleReturnKey(ITextBuffer subjectBuffer, ITextView textView)
        {
            if (!subjectBuffer.GetFeatureOnOffOption(FeatureOnOffOptions.AutoInsertBlockCommentStartString))
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

            using var transaction = _undoHistoryRegistry.GetHistory(textView.TextBuffer).CreateTransaction(EditorFeaturesResources.Insert_new_line);

            var editorOperations = _editorOperationsFactoryService.GetEditorOperations(textView);

            editorOperations.InsertNewLine();
            editorOperations.InsertText(exteriorText);

            transaction.Complete();
            return true;
        }

        protected abstract string GetExteriorTextForNextLine(SnapshotPoint caretPosition);
    }
}
