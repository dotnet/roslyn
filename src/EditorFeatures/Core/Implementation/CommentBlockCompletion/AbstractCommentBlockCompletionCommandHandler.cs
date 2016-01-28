// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CommentBlockCompletion
{
    internal abstract class AbstractCommentBlockCompletionCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
    {
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        protected AbstractCommentBlockCompletionCommandHandler(
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
            var subjectBuffer = args.SubjectBuffer;
            var textView = args.TextView;

            var caretPosition = textView.GetCaretPoint(subjectBuffer);
            if (caretPosition == null)
            {
                nextHandler();
                return;
            }

            var snapshot = subjectBuffer.CurrentSnapshot;

            var exteriorText = GetExteriorTextForNextLine(caretPosition.Value);
            if (exteriorText == null)
            {
                nextHandler();
                return;
            }

            using (var transaction = _undoHistoryRegistry.GetHistory(args.TextView.TextBuffer).CreateTransaction(EditorFeaturesResources.InsertNewLine))
            {
                var editorOperations = _editorOperationsFactoryService.GetEditorOperations(args.TextView);

                editorOperations.InsertNewLine();
                editorOperations.InsertText(exteriorText);

                transaction.Complete();
            }
        }

        protected abstract string GetExteriorTextForNextLine(SnapshotPoint caretPosition);
    }
}
