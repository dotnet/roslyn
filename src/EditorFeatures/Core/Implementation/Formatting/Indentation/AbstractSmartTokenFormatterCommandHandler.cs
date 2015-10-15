// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Formatting.Indentation
{
    internal abstract class AbstractSmartTokenFormatterCommandHandler :
        ICommandHandler<ReturnKeyCommandArgs>
    {
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        public AbstractSmartTokenFormatterCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        protected abstract ISmartTokenFormatter CreateSmartTokenFormatter(OptionSet optionSet, IEnumerable<IFormattingRule> formattingRules, SyntaxNode root);

        protected abstract bool UseSmartTokenFormatter(SyntaxNode root, ITextSnapshotLine line, IEnumerable<IFormattingRule> formattingRules, OptionSet options, CancellationToken cancellationToken);
        protected abstract bool IsInvalidToken(SyntaxToken token);

        protected abstract IEnumerable<IFormattingRule> GetFormattingRules(Document document, int position);

        /// <returns>True if any change is made.</returns>
        protected bool FormatToken(ITextView view, Document document, SyntaxToken token, IEnumerable<IFormattingRule> formattingRules, CancellationToken cancellationToken)
        {
            var root = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var formatter = CreateSmartTokenFormatter(document.Project.Solution.Workspace.Options, formattingRules, root);
            var changes = formatter.FormatToken(document.Project.Solution.Workspace, token, cancellationToken);
            if (changes.Count == 0)
            {
                return false;
            }

            using (var transaction = CreateEditTransaction(view, EditorFeaturesResources.FormatToken))
            {
                transaction.MergePolicy = AutomaticCodeChangeMergePolicy.Instance;

                document.Project.Solution.Workspace.ApplyTextChanges(document.Id, changes, cancellationToken);
                transaction.Complete();
            }

            return true;
        }

        public CommandState GetCommandState(ReturnKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            return nextHandler();
        }

        public void ExecuteCommand(ReturnKeyCommandArgs args, Action nextHandler)
        {
            var textView = args.TextView;
            var oldCaretPoint = textView.GetCaretPoint(args.SubjectBuffer);

            nextHandler();

            // A feature like completion handled the return key but did not pass it on to the editor.
            var newCaretPoint = textView.GetCaretPoint(args.SubjectBuffer);
            if (textView.Selection.IsEmpty && oldCaretPoint.HasValue && newCaretPoint.HasValue &&
                oldCaretPoint.Value.GetContainingLine().LineNumber == newCaretPoint.Value.GetContainingLine().LineNumber)
            {
                return;
            }

            if (args.SubjectBuffer.CanApplyChangeDocumentToWorkspace())
            {
                ExecuteCommandWorker(args, CancellationToken.None);
            }
        }

        internal void ExecuteCommandWorker(ReturnKeyCommandArgs args, CancellationToken cancellationToken)
        {
            var textView = args.TextView;
            var caretPoint = textView.GetCaretPoint(args.SubjectBuffer);

            if (args.SubjectBuffer.GetOption(FormattingOptions.SmartIndent) != FormattingOptions.IndentStyle.Smart ||
                !caretPoint.HasValue)
            {
                return;
            }

            var currentPosition = caretPoint.Value;
            var document = currentPosition.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            var formattingRules = this.GetFormattingRules(document, currentPosition.Position);

            // see whether we should use token formatter
            if (TryFormatUsingTokenFormatter(textView, args.SubjectBuffer, document, formattingRules, cancellationToken))
            {
                return;
            }

            // check whether editor would have used smart indenter
            if (EditorHandled(textView) && !RequireReadjustment(textView, args.SubjectBuffer))
            {
                // editor already took care of smart indentation.
                return;
            }

            // check whether we can do it ourselves
            if (!CanHandleOurselves(textView, args.SubjectBuffer))
            {
                return;
            }

            var indentationService = document.GetLanguageService<IIndentationService>();
            var indentation = indentationService.GetDesiredIndentationAsync(document,
                currentPosition.GetContainingLine().LineNumber, cancellationToken).WaitAndGetResult(cancellationToken);

            // looks like we can't.
            if (!indentation.HasValue)
            {
                return;
            }

            HandleOurselves(textView, args.SubjectBuffer, indentation.Value.GetIndentation(textView, currentPosition.GetContainingLine()));
        }

        private bool RequireReadjustment(ITextView view, ITextBuffer subjectBuffer)
        {
            var caretInSubjectBuffer = view.GetCaretPoint(subjectBuffer).Value;
            var lineInSubjectBuffer = caretInSubjectBuffer.GetContainingLine();

            var currentOffset = caretInSubjectBuffer - lineInSubjectBuffer.Start;
            var firstNonWhitespaceIndex = lineInSubjectBuffer.GetFirstNonWhitespaceOffset();
            if (!firstNonWhitespaceIndex.HasValue || currentOffset >= firstNonWhitespaceIndex.Value)
            {
                return false;
            }

            // there are whitespace after caret position (which smart indentation has put),
            // we might need to re-adjust indentation
            return true;
        }

        private void HandleOurselves(ITextView view, ITextBuffer subjectBuffer, int indentation)
        {
            var lineInSubjectBuffer = view.GetCaretPoint(subjectBuffer).Value.GetContainingLine();
            var lengthOfLine = lineInSubjectBuffer.GetColumnFromLineOffset(lineInSubjectBuffer.Length, view.Options);

            // check whether we are dealing with virtual space or real space
            if (indentation > lengthOfLine)
            {
                // we are dealing with virtual space
                var caretPosition = view.GetVirtualCaretPoint(subjectBuffer);
                var positionInVirtualSpace = new VirtualSnapshotPoint(caretPosition.Value.Position, indentation - lineInSubjectBuffer.Length);
                view.TryMoveCaretToAndEnsureVisible(positionInVirtualSpace);
                return;
            }

            // we are dealing with real space. check whether we need to just set caret position or move text
            var firstNonWhitespaceIndex = lineInSubjectBuffer.GetFirstNonWhitespaceOffset();

            if (firstNonWhitespaceIndex.HasValue)
            {
                // if leading whitespace is not what we expect, re-adjust indentation
                var columnOfFirstNonWhitespace = lineInSubjectBuffer.GetColumnFromLineOffset(firstNonWhitespaceIndex.Value, view.Options);
                if (columnOfFirstNonWhitespace != indentation)
                {
                    ReadjustIndentation(view, subjectBuffer, firstNonWhitespaceIndex.Value, indentation);
                    return;
                }
            }

            // okay, it is an empty line with some whitespaces 
            Contract.Requires(indentation <= lineInSubjectBuffer.Length);

            var offset = GetOffsetFromIndentation(indentation, view.Options);
            view.TryMoveCaretToAndEnsureVisible(lineInSubjectBuffer.Start.Add(offset));
        }

        private int GetOffsetFromIndentation(int indentation, IEditorOptions option)
        {
            int numberOfTabs = 0;
            int numberOfSpaces = Math.Max(0, indentation);

            if (!option.IsConvertTabsToSpacesEnabled())
            {
                var tabSize = option.GetTabSize();

                numberOfTabs = indentation / tabSize;
                numberOfSpaces -= numberOfTabs * tabSize;
            }

            return numberOfTabs + numberOfSpaces;
        }

        /// <summary>
        /// re-adjust caret position to be the beginning of first text on the line. and make sure the text start at the given indentation
        /// </summary>
        private static void ReadjustIndentation(ITextView view, ITextBuffer subjectBuffer, int firstNonWhitespaceIndex, int indentation)
        {
            var lineInSubjectBuffer = view.GetCaretPoint(subjectBuffer).Value.GetContainingLine();

            // first set the caret at the beginning of the text on the line
            view.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(lineInSubjectBuffer.Snapshot, lineInSubjectBuffer.Start + firstNonWhitespaceIndex));

            var document = lineInSubjectBuffer.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            var options = document.Project.Solution.Workspace.Options;

            // and then, insert the text
            document.Project.Solution.Workspace.ApplyTextChanges(document.Id,
                new TextChange(
                    new TextSpan(
                        lineInSubjectBuffer.Start.Position, firstNonWhitespaceIndex),
                        indentation.CreateIndentationString(options.GetOption(FormattingOptions.UseTabs, document.Project.Language), options.GetOption(FormattingOptions.TabSize, document.Project.Language))),
                        CancellationToken.None);
        }

        /// <summary>
        /// check whether we can smart indent ourselves. we only attempt to smart indent ourselves
        /// if the line in subject buffer we do smart indenting maps back to the view as it is and
        /// if it starts from the beginning of the line
        /// </summary>
        private bool CanHandleOurselves(ITextView view, ITextBuffer subjectBuffer)
        {
            var lineInSubjectBuffer = view.GetCaretPoint(subjectBuffer).Value.GetContainingLine();

            // first, make sure whole line is map back to the view
            if (view.GetSpanInView(lineInSubjectBuffer.ExtentIncludingLineBreak).Count != 1)
            {
                return false;
            }

            // now check, start position of the line is start position in the view
            var caretPosition = view.GetPositionInView(view.GetVirtualCaretPoint(subjectBuffer).Value.Position);
            var containingLineCaret = caretPosition.Value.GetContainingLine();

            var startPositionSubjectBuffer = view.GetPositionInView(lineInSubjectBuffer.Start);
            var startPositionCaret = view.GetPositionInView(containingLineCaret.Start);
            if (!startPositionSubjectBuffer.HasValue ||
                !startPositionCaret.HasValue ||
                startPositionCaret.Value != startPositionSubjectBuffer.Value)
            {
                // if start position of subject buffer is not equal to start position of view line,
                // we can't use the indenter
                return false;
            }

            // get containing line in view from start position of the caret in view
            var containingLineView = startPositionCaret.Value.GetContainingLine();

            // make sure line start at the beginning of the line
            if (containingLineView.Start != startPositionCaret.Value)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// check whether editor smart indenter mechanism handled this case already
        /// </summary>
        private bool EditorHandled(ITextView view)
        {
            var caretPosition = view.Caret.Position.VirtualBufferPosition;
            return caretPosition.IsInVirtualSpace || (caretPosition.Position != view.Caret.Position.BufferPosition.GetContainingLine().Start);
        }

        /// <summary>
        /// check whether we can do automatic formatting using token formatter instead of smart indenter for the "enter" key
        /// </summary>
        private bool TryFormatUsingTokenFormatter(ITextView view, ITextBuffer subjectBuffer, Document document, IEnumerable<IFormattingRule> formattingRules, CancellationToken cancellationToken)
        {
            var position = view.GetCaretPoint(subjectBuffer).Value;
            var line = position.GetContainingLine();
            var root = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var options = document.Project.Solution.Workspace.Options;
            if (!UseSmartTokenFormatter(root, line, formattingRules, options, cancellationToken))
            {
                return false;
            }

            var firstNonWhitespacePosition = line.GetFirstNonWhitespacePosition();
            var token = root.FindToken(firstNonWhitespacePosition.Value);
            if (IsInvalidToken(token))
            {
                return false;
            }

            // when undo, make sure it undo the caret movement I did below
            using (var transaction = CreateEditTransaction(view, EditorFeaturesResources.SmartIndenting))
            {
                // if caret position is before the token, make sure we put caret at the beginning of the token so that caret
                // is at the right position after formatting
                var currentSnapshot = line.Snapshot;
                if (position.Position < token.SpanStart)
                {
                    view.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(currentSnapshot, token.SpanStart));
                }

                if (FormatToken(view, document, token, formattingRules, cancellationToken))
                {
                    transaction.Complete();
                }
                else
                {
                    transaction.Cancel();
                }
            }

            return true;
        }

        /// <summary>
        /// create caret preserving edit transaction
        /// </summary>
        protected CaretPreservingEditTransaction CreateEditTransaction(ITextView view, string description)
        {
            return new CaretPreservingEditTransaction(description, view, _undoHistoryRegistry, _editorOperationsFactoryService);
        }
    }
}
