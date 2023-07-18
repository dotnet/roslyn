﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    [Export]
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.FormatDocument)]
    [Order(After = PredefinedCommandHandlerNames.Rename)]
    [Order(Before = PredefinedCommandHandlerNames.StringCopyPaste)]
    [Order(Before = PredefinedCompletionNames.CompletionCommandHandler)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal partial class FormatCommandHandler(
        ITextUndoHistoryRegistry undoHistoryRegistry,
        IEditorOperationsFactoryService editorOperationsFactoryService,
        IGlobalOptionService globalOptions) :
        ICommandHandler<FormatDocumentCommandArgs>,
        ICommandHandler<FormatSelectionCommandArgs>,
        IChainedCommandHandler<PasteCommandArgs>,
        IChainedCommandHandler<TypeCharCommandArgs>,
        IChainedCommandHandler<ReturnKeyCommandArgs>
    {
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry = undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService = editorOperationsFactoryService;
        private readonly IGlobalOptionService _globalOptions = globalOptions;

        public string DisplayName => EditorFeaturesResources.Automatic_Formatting;

        private void Format(ITextView textView, ITextBuffer textBuffer, Document document, TextSpan? selectionOpt, CancellationToken cancellationToken)
        {
            var formattingService = document.GetRequiredLanguageService<IFormattingInteractionService>();

            using (Logger.LogBlock(FunctionId.CommandHandler_FormatCommand, KeyValueLogMessage.Create(LogType.UserAction, m => m["Span"] = selectionOpt?.Length ?? -1), cancellationToken))
            using (var transaction = CreateEditTransaction(textView, EditorFeaturesResources.Formatting))
            {
                // Note: C# always completes synchronously, TypeScript is async
                var changes = formattingService.GetFormattingChangesAsync(document, textBuffer, selectionOpt, cancellationToken).WaitAndGetResult(cancellationToken);
                if (changes.IsEmpty)
                {
                    return;
                }

                if (selectionOpt.HasValue)
                {
                    var ruleFactory = document.Project.Solution.Services.GetRequiredService<IHostDependentFormattingRuleFactoryService>();
                    changes = ruleFactory.FilterFormattedChanges(document.Id, selectionOpt.Value, changes).ToImmutableArray();
                }

                if (!changes.IsEmpty)
                {
                    using (Logger.LogBlock(FunctionId.Formatting_ApplyResultToBuffer, cancellationToken))
                    {
                        textBuffer.ApplyChanges(changes);
                    }
                }

                transaction.Complete();
            }
        }

        private static bool CanExecuteCommand(ITextBuffer buffer)
            => buffer.CanApplyChangeDocumentToWorkspace();

        private static CommandState GetCommandState(ITextBuffer buffer)
            => CanExecuteCommand(buffer) ? CommandState.Available : CommandState.Unspecified;

        public void ExecuteReturnOrTypeCommand(EditorCommandArgs args, Action nextHandler, CancellationToken cancellationToken)
        {
            // run next handler first so that editor has chance to put the return into the buffer first.
            nextHandler();
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                ExecuteReturnOrTypeCommandWorker(args, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // According to Editor command handler API guidelines, it's best if we return early if cancellation
                // is requested instead of throwing. Otherwise, we could end up in an invalid state due to already
                // calling nextHandler().
            }
        }

        private void ExecuteReturnOrTypeCommandWorker(EditorCommandArgs args, CancellationToken cancellationToken)
        {
            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;
            if (!CanExecuteCommand(subjectBuffer))
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

            var service = document.GetLanguageService<IFormattingInteractionService>();
            if (service == null)
            {
                return;
            }

            IList<TextChange>? textChanges;

            // save current caret position
            if (args is ReturnKeyCommandArgs)
            {
                if (!service.SupportsFormatOnReturn)
                {
                    return;
                }

                // Note: C# always completes synchronously, TypeScript is async
                textChanges = service.GetFormattingChangesOnReturnAsync(document, caretPosition.Value, cancellationToken).WaitAndGetResult(cancellationToken);
            }
            else if (args is TypeCharCommandArgs typeCharArgs)
            {
                if (!service.SupportsFormattingOnTypedCharacter(document, typeCharArgs.TypedChar))
                {
                    return;
                }

                // Note: C# always completes synchronously, TypeScript is async
                textChanges = service.GetFormattingChangesAsync(
                    document, typeCharArgs.SubjectBuffer, typeCharArgs.TypedChar, caretPosition.Value, cancellationToken).WaitAndGetResult(cancellationToken);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(args);
            }

            if (textChanges == null || textChanges.Count == 0)
            {
                return;
            }

            using (var transaction = CreateEditTransaction(textView, EditorFeaturesResources.Automatic_Formatting))
            {
                transaction.MergePolicy = AutomaticCodeChangeMergePolicy.Instance;
                subjectBuffer.ApplyChanges(textChanges);
                transaction.Complete();
            }

            // get new caret position after formatting
            var newCaretPositionMarker = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!newCaretPositionMarker.HasValue)
            {
                return;
            }

            var snapshotAfterFormatting = subjectBuffer.CurrentSnapshot;

            var oldCaretPosition = caretPosition.Value.TranslateTo(snapshotAfterFormatting, PointTrackingMode.Negative);
            var newCaretPosition = newCaretPositionMarker.Value.TranslateTo(snapshotAfterFormatting, PointTrackingMode.Negative);
            if (oldCaretPosition.Position == newCaretPosition.Position)
            {
                return;
            }

            // caret has moved to wrong position, move it back to correct position
            args.TextView.TryMoveCaretToAndEnsureVisible(oldCaretPosition);
        }

        private CaretPreservingEditTransaction CreateEditTransaction(ITextView view, string description)
            => new(description, view, _undoHistoryRegistry, _editorOperationsFactoryService);
    }
}
