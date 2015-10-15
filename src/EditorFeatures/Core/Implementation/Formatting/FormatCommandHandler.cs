// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Formatting
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.FormatDocument, ContentTypeNames.RoslynContentType)]
    [Order(After = PredefinedCommandHandlerNames.Rename)]
    [Order(Before = PredefinedCommandHandlerNames.Completion)]
    internal partial class FormatCommandHandler :
        ICommandHandler<FormatDocumentCommandArgs>,
        ICommandHandler<FormatSelectionCommandArgs>,
        ICommandHandler<PasteCommandArgs>,
        ICommandHandler<TypeCharCommandArgs>,
        ICommandHandler<ReturnKeyCommandArgs>
    {
        private readonly IWaitIndicator _waitIndicator;
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        [ImportingConstructor]
        public FormatCommandHandler(
            IWaitIndicator waitIndicator,
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            _waitIndicator = waitIndicator;
            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        private void Format(ITextView textView, Document document, TextSpan? selectionOpt, CancellationToken cancellationToken)
        {
            var formattingService = document.GetLanguageService<IEditorFormattingService>();

            using (var transaction = new CaretPreservingEditTransaction(EditorFeaturesResources.Formatting, textView, _undoHistoryRegistry, _editorOperationsFactoryService))
            {
                var changes = formattingService.GetFormattingChangesAsync(document, selectionOpt, cancellationToken).WaitAndGetResult(cancellationToken);
                if (changes.Count == 0)
                {
                    return;
                }

                if (selectionOpt.HasValue)
                {
                    var ruleFactory = document.Project.Solution.Workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();

                    changes = ruleFactory.FilterFormattedChanges(document, selectionOpt.Value, changes).ToList();
                    if (changes.Count == 0)
                    {
                        return;
                    }
                }

                using (Logger.LogBlock(FunctionId.Formatting_ApplyResultToBuffer, cancellationToken))
                {
                    document.Project.Solution.Workspace.ApplyTextChanges(document.Id, changes, cancellationToken);
                }

                transaction.Complete();
            }
        }

        private static CommandState GetCommandState(ITextBuffer buffer, Func<CommandState> nextHandler)
        {
            if (!buffer.CanApplyChangeDocumentToWorkspace())
            {
                return nextHandler();
            }

            return CommandState.Available;
        }

        public void ExecuteReturnOrTypeCommand(CommandArgs args, Action nextHandler, CancellationToken cancellationToken)
        {
            // This method handles only return / type char
            if (!(args is ReturnKeyCommandArgs || args is TypeCharCommandArgs))
            {
                return;
            }

            // run next handler first so that editor has chance to put the return into the buffer first.
            nextHandler();

            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;
            if (!subjectBuffer.CanApplyChangeDocumentToWorkspace())
            {
                return;
            }

            var caretPosition = textView.GetCaretPoint(args.SubjectBuffer);
            if (!caretPosition.HasValue)
            {
                return;
            }

            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            var service = document.GetLanguageService<IEditorFormattingService>();
            if (service == null)
            {
                return;
            }

            // save current caret position
            var caretPositionMarker = new SnapshotPoint(args.SubjectBuffer.CurrentSnapshot, caretPosition.Value);
            if (args is ReturnKeyCommandArgs)
            {
                if (!service.SupportsFormatOnReturn ||
                    !TryFormat(textView, document, service, ' ', caretPositionMarker, formatOnReturn: true, cancellationToken: cancellationToken))
                {
                    return;
                }
            }
            else if (args is TypeCharCommandArgs)
            {
                var typedChar = ((TypeCharCommandArgs)args).TypedChar;
                if (!service.SupportsFormattingOnTypedCharacter(document, typedChar) ||
                    !TryFormat(textView, document, service, typedChar, caretPositionMarker, formatOnReturn: false, cancellationToken: cancellationToken))
                {
                    return;
                }
            }

            // get new caret position after formatting
            var newCaretPositionMarker = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!newCaretPositionMarker.HasValue)
            {
                return;
            }

            var snapshotAfterFormatting = args.SubjectBuffer.CurrentSnapshot;

            var oldCaretPosition = caretPositionMarker.TranslateTo(snapshotAfterFormatting, PointTrackingMode.Negative);
            var newCaretPosition = newCaretPositionMarker.Value.TranslateTo(snapshotAfterFormatting, PointTrackingMode.Negative);
            if (oldCaretPosition.Position == newCaretPosition.Position)
            {
                return;
            }

            // caret has moved to wrong position, move it back to correct position
            args.TextView.TryMoveCaretToAndEnsureVisible(oldCaretPosition);
        }
    }
}
