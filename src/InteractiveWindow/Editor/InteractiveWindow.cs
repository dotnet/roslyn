// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Dumps commands in QueryStatus and Exec.
// #define DUMP_COMMANDS

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    // TODO: We should condense committed language buffers into a single language buffer and save the
    // classifications from the previous language buffer if the perf of having individual buffers
    // starts having problems w/ a large number of inputs.

    /// <summary>
    /// Provides implementation of a Repl Window built on top of the VS editor using projection buffers.
    /// </summary>
    internal partial class InteractiveWindow : IInteractiveWindow,  IInteractiveWindowOperations2
    {
        private bool _adornmentToMinimize;

        private readonly IWpfTextView _textView;
        private readonly History _history;

        public event EventHandler<SubmissionBufferAddedEventArgs> SubmissionBufferAdded;

        ////
        //// Services
        //// 

        private readonly IIntellisenseSessionStackMapService _intellisenseSessionStackMap;
        private readonly ISmartIndentationService _smartIndenterService;

        // the language engine and content type of the active submission:
        private readonly IInteractiveEvaluator _evaluator;

        private IIntellisenseSessionStack _sessionStack; // TODO: remove

        public PropertyCollection Properties { get; }

        ////
        //// Buffer composition.
        //// 
        private readonly ITextBuffer _outputBuffer;
        private readonly IProjectionBuffer _projectionBuffer;
        private readonly ITextBuffer _standardInputBuffer;

        private ITextBuffer _currentLanguageBuffer;
        private string _historySearch;

        // List of projection buffer spans - the projection buffer doesn't allow us to enumerate spans so we need to track them manually:
        private readonly List<ReplSpan> _projectionSpans = new List<ReplSpan>();

        ////
        //// Standard input.
        ////

        // non-null if reading from stdin - position in the _inputBuffer where we map stdin
        private int? _stdInputStart; // TODO (tomat): this variable is not used in thread-safe manner
        private SnapshotSpan? _inputValue;
        private readonly AutoResetEvent _inputEvent = new AutoResetEvent(false);

        //// 
        //// Output.
        //// 

        private readonly OutputBuffer _buffer;
        private readonly TextWriter _outputWriter;
        private readonly InteractiveWindowWriter _errorOutputWriter;

        private readonly string _lineBreakString;

        private const string BoxSelectionCutCopyTag = "MSDEVColumnSelect";

        void IInteractiveWindow.Close()
        {
            Caret.PositionChanged -= CaretPositionChanged;

            UIThread(uiOnly => _textView.Close());
        }

        #region Misc Helpers

        private IIntellisenseSessionStack SessionStack
        {
            get
            {
                if (_sessionStack == null)
                {
                    _sessionStack = _intellisenseSessionStackMap.GetStackForTextView(_textView);
                }

                return _sessionStack;
            }
        }

        private ITextBuffer TextBuffer => _textView.TextBuffer;

        public ITextBuffer CurrentLanguageBuffer => _currentLanguageBuffer;

        void IDisposable.Dispose()
        {
            if (_buffer != null)
            {
                _buffer.Dispose();
            }
        }

        public static InteractiveWindow FromBuffer(ITextBuffer buffer)
        {
            object result;
            buffer.Properties.TryGetProperty(typeof(InteractiveWindow), out result);
            return result as InteractiveWindow;
        }

        #endregion

        #region IInteractiveWindow

        public event Action ReadyForInput;

        IWpfTextView IInteractiveWindow.TextView => _textView;

        ITextBuffer IInteractiveWindow.OutputBuffer => _outputBuffer;

        TextWriter IInteractiveWindow.OutputWriter => _outputWriter;

        TextWriter IInteractiveWindow.ErrorOutputWriter => _errorOutputWriter;

        IInteractiveEvaluator IInteractiveWindow.Evaluator => _evaluator;

        Task IInteractiveWindow.SubmitAsync(IEnumerable<string> inputs)
        {
            var completion = new TaskCompletionSource<object>();
            var submissions = inputs.ToArray();
            var numSubmissions = submissions.Length;
            PendingSubmission[] pendingSubmissions = new PendingSubmission[numSubmissions];
            if (numSubmissions == 0)
            {
                completion.SetResult(null);
            }
            else
            {
                for (int i = 0; i < numSubmissions; i++)
                {
                    pendingSubmissions[i] = new PendingSubmission(submissions[i], i == numSubmissions - 1 ? completion : null);
                }
            }

            UIThread(uiOnly => uiOnly.Submit(pendingSubmissions));
            return completion.Task;
        }

        private class PendingSubmission
        {
            public readonly string Input;
            public readonly TaskCompletionSource<object> Completion; // only set on the last submission to 
                                                                     // inform caller about completion of batch

            public PendingSubmission(string input, TaskCompletionSource<object> completion)
            {
                Input = input;
                Completion = completion;
            }
        }

        void IInteractiveWindow.AddInput(string command)
        {
            UIThread(uiOnly => uiOnly.AddInput(command));
        }

        void IInteractiveWindow.FlushOutput()
        {
            // Flush can only be called on the UI thread.
            UIThread(uiOnly => _buffer.Flush());
        }

        void IInteractiveWindow.InsertCode(string text)
        {
            UIThread(uiOnly => uiOnly.InsertCode(text));
        }

        #endregion

        #region Commands

        Task<ExecutionResult> IInteractiveWindowOperations.ResetAsync(bool initialize)
        {
            // This is an implementation of ResetAsync on IInteractiveWindowOperations interface.
            // C# and VB Interactive window should not be calling this method at all
            // as they now call reset through IInteractiveWindowOperations2
            return UIThread(uiOnly => uiOnly.ResetAsync(initialize, isFromSubmit: true));
        }

        Task<ExecutionResult> IInteractiveWindowOperations2.ResetAsync(bool initialize, bool isFromSubmit)
        {
            return UIThread(uiOnly => uiOnly.ResetAsync(initialize, isFromSubmit));
        }

        void IInteractiveWindowOperations.ClearHistory()
        {
            UIThread(uiOnly => _history.Clear());
        }

        void IInteractiveWindowOperations.ClearView()
        {
            UIThread(uiOnly => uiOnly.ClearView());
        }

        /// <summary>
        /// Pastes from the clipboard into the text view
        /// </summary>
        bool IInteractiveWindowOperations.Paste()
        {
            return UIThread(uiOnly => uiOnly.Paste());
        }

        void IInteractiveWindowOperations.ExecuteInput()
        {
            UIThread(uiOnly => uiOnly.ExecuteInput());
        }

        /// <summary>
        /// Appends text to the output buffer and updates projection buffer to include it.
        /// WARNING: this has to be the only method that writes to the output buffer so that 
        /// the output buffering counters are kept in sync.
        /// </summary>
        internal void AppendOutput(IEnumerable<string> output, int outputLength)
        {
            RequiresUIThread();
            _dangerous_uiOnly.AppendOutput(output, outputLength);
        }

        /// <summary>
        /// Clears the current input
        /// </summary>
        void IInteractiveWindowOperations.Cancel()
        {
            ClearInput();
            UIThread(uiOnly =>
            {
                uiOnly.EditorOperations.MoveToEndOfDocument(false);
                uiOnly.UncommittedInput = null;
            });
            _historySearch = null;
        }

        public void HistoryPrevious(string search = null)
        {
            if (_currentLanguageBuffer == null)
            {
                return;
            }

            var previous = _history.GetPrevious(search);
            if (previous != null)
            {
                if (string.IsNullOrWhiteSpace(search))
                {
                    // don't store search as an uncommitted history item
                    StoreUncommittedInputForHistory();
                }

                SetActiveCodeToHistory(previous);
                UIThread(uiOnly => uiOnly.EditorOperations.MoveToEndOfDocument(false));
            }
        }

        public void HistoryNext(string search = null)
        {
            if (_currentLanguageBuffer == null)
            {
                return;
            }

            var next = _history.GetNext(search);
            if (next != null)
            {
                if (string.IsNullOrWhiteSpace(search))
                {
                    // don't store search as an uncommitted history item
                    StoreUncommittedInputForHistory();
                }

                SetActiveCodeToHistory(next);
                UIThread(uiOnly => uiOnly.EditorOperations.MoveToEndOfDocument(false));
            }
            else
            {
                string code = _history.UncommittedInput;
                _history.UncommittedInput = null;
                if (!string.IsNullOrEmpty(code))
                {
                    SetActiveCode(code);
                    UIThread(uiOnly => uiOnly.EditorOperations.MoveToEndOfDocument(false));
                }
            }
        }

        void IInteractiveWindowOperations.HistorySearchNext()
        {
            EnsureHistorySearch();
            HistoryNext(_historySearch);
        }

        void IInteractiveWindowOperations.HistorySearchPrevious()
        {
            EnsureHistorySearch();
            HistoryPrevious(_historySearch);
        }

        private void EnsureHistorySearch()
        {
            if (_historySearch == null)
            {
                _historySearch = _currentLanguageBuffer.CurrentSnapshot.GetText();
            }
        }

        private void StoreUncommittedInputForHistory()
        {
            if (_history.UncommittedInput == null)
            {
                string activeCode = GetActiveCode();
                if (activeCode.Length > 0)
                {
                    _history.UncommittedInput = activeCode;
                }
            }
        }

        /// <summary>
        /// Moves to the beginning of the line.
        /// </summary>
        void IInteractiveWindowOperations.Home(bool extendSelection)
        {
            var caret = Caret;

            // map the end of subject buffer line:
            var subjectLineEnd = _textView.BufferGraph.MapDownToFirstMatch(
                caret.Position.BufferPosition.GetContainingLine().End,
                PointTrackingMode.Positive,
                snapshot => snapshot.TextBuffer != _projectionBuffer,
                PositionAffinity.Successor).Value;

            ITextSnapshotLine subjectLine = subjectLineEnd.GetContainingLine();

            var projectedSubjectLineStart = _textView.BufferGraph.MapUpToBuffer(
                subjectLine.Start,
                PointTrackingMode.Positive,
                PositionAffinity.Successor,
                _projectionBuffer).Value;

            // If the caret is already at the first non-whitespace character or the line is
            // entirely whitespace, move to the start of the view line. See
            // (EditorOperations.MoveToHome).
            //
            // If the caret is in the prompt move the caret to the beginning of the language
            // line.

            int firstNonWhiteSpace = IndexOfNonWhiteSpaceCharacter(subjectLine);
            SnapshotPoint moveTo;
            if (firstNonWhiteSpace == -1 ||
                projectedSubjectLineStart.Position + firstNonWhiteSpace == caret.Position.BufferPosition ||
                caret.Position.BufferPosition < projectedSubjectLineStart.Position)
            {
                moveTo = projectedSubjectLineStart;
            }
            else
            {
                moveTo = projectedSubjectLineStart + firstNonWhiteSpace;
            }

            if (extendSelection)
            {
                VirtualSnapshotPoint anchor = _textView.Selection.AnchorPoint;
                caret.MoveTo(moveTo);
                _textView.Selection.Select(anchor.TranslateTo(_textView.TextSnapshot), _textView.Caret.Position.VirtualBufferPosition);
            }
            else
            {
                _textView.Selection.Clear();
                caret.MoveTo(moveTo);
            }
        }

        /// <summary>
        /// Moves to the end of the line.
        /// </summary>
        void IInteractiveWindowOperations.End(bool extendSelection)
        {
            var caret = Caret;

            // map the end of the subject buffer line:
            var subjectLineEnd = _textView.BufferGraph.MapDownToFirstMatch(
                caret.Position.BufferPosition.GetContainingLine().End,
                PointTrackingMode.Positive,
                snapshot => snapshot.TextBuffer != _projectionBuffer,
                PositionAffinity.Successor).Value;

            ITextSnapshotLine subjectLine = subjectLineEnd.GetContainingLine();

            var moveTo = _textView.BufferGraph.MapUpToBuffer(
                subjectLine.End,
                PointTrackingMode.Positive,
                PositionAffinity.Successor,
                _projectionBuffer).Value;

            if (extendSelection)
            {
                VirtualSnapshotPoint anchor = _textView.Selection.AnchorPoint;
                caret.MoveTo(moveTo);
                _textView.Selection.Select(anchor.TranslateTo(_textView.TextSnapshot), _textView.Caret.Position.VirtualBufferPosition);
            }
            else
            {
                _textView.Selection.Clear();
                caret.MoveTo(moveTo);
            }
        }

        void IInteractiveWindowOperations.SelectAll()
        {
            SnapshotSpan? span = GetContainingRegion(_textView.Caret.Position.BufferPosition);

            var selection = _textView.Selection;

            // if the span is already selected select all text in the projection buffer:
            if (span == null || selection.SelectedSpans.Count == 1 && selection.SelectedSpans[0] == span.Value)
            {
                var currentSnapshot = TextBuffer.CurrentSnapshot;
                span = new SnapshotSpan(currentSnapshot, new Span(0, currentSnapshot.Length));
            }

            UIThread(uiOnly => _textView.Selection.Select(span.Value, isReversed: false));
        }

        /// <summary>
        /// Given a point in projection buffer calculate a span that includes the point and comprises of 
        /// subsequent projection spans forming a region, i.e. a sequence of output spans in between two subsequent submissions,
        /// a language input block, or standard input block.
        /// </summary>
        private SnapshotSpan? GetContainingRegion(SnapshotPoint point)
        {
            int promptIndex = GetPromptIndexForPoint(point);
            if (promptIndex < 0)
            {
                return null;
            }

            // Grab the span following the prompt (either language or standard input).
            ReplSpan projectionSpan = _projectionSpans[promptIndex + 1];

            Debug.Assert(projectionSpan.Kind == ReplSpanKind.Language || projectionSpan.Kind == ReplSpanKind.StandardInput);

            var inputSnapshot = projectionSpan.TrackingSpan.TextBuffer.CurrentSnapshot;

            // Language input block is a projection of the entire snapshot;
            // std input block is a projection of a single span:
            SnapshotPoint inputBufferEnd = (projectionSpan.Kind == ReplSpanKind.Language) ?
                new SnapshotPoint(inputSnapshot, inputSnapshot.Length) :
                projectionSpan.TrackingSpan.GetEndPoint(inputSnapshot);

            var bufferGraph = _textView.BufferGraph;
            var textBuffer = TextBuffer;

            SnapshotPoint projectedInputBufferEnd = bufferGraph.MapUpToBuffer(
                inputBufferEnd,
                PointTrackingMode.Positive,
                PositionAffinity.Predecessor,
                textBuffer).Value;

            // point is between the primary prompt (including) and the last character of the corresponding language/stdin buffer:
            if (point <= projectedInputBufferEnd)
            {
                var projectedLanguageBufferStart = bufferGraph.MapUpToBuffer(
                    new SnapshotPoint(inputSnapshot, 0),
                    PointTrackingMode.Positive,
                    PositionAffinity.Successor,
                    textBuffer).Value;

                var promptProjectionSpan = _projectionSpans[promptIndex];
                if (point < projectedLanguageBufferStart - promptProjectionSpan.Length)
                {
                    // cursor is before the first language buffer:
                    return new SnapshotSpan(new SnapshotPoint(textBuffer.CurrentSnapshot, 0), projectedLanguageBufferStart - promptProjectionSpan.Length);
                }

                // cursor is within the language buffer:
                return new SnapshotSpan(projectedLanguageBufferStart, projectedInputBufferEnd);
            }

            int nextPromptIndex = -1;
            for (int i = promptIndex + 1; i < _projectionSpans.Count; i++)
            {
                if (_projectionSpans[i].Kind.IsPrompt())
                {
                    nextPromptIndex = i;
                    break;
                }
            }

            // this was the last primary/stdin prompt - select the part of the projection buffer behind the end of the language/stdin buffer:
            if (nextPromptIndex < 0)
            {
                var currentSnapshot = textBuffer.CurrentSnapshot;
                return new SnapshotSpan(
                    projectedInputBufferEnd,
                    new SnapshotPoint(currentSnapshot, currentSnapshot.Length));
            }

            ReplSpan lastSpanBeforeNextPrompt = _projectionSpans[nextPromptIndex - 1];
            Debug.Assert(lastSpanBeforeNextPrompt.Kind == ReplSpanKind.Output);

            // select all text in between the language buffer and the next prompt:
            var trackingSpan = lastSpanBeforeNextPrompt.TrackingSpan;
            return new SnapshotSpan(
                projectedInputBufferEnd,
                bufferGraph.MapUpToBuffer(
                    trackingSpan.GetEndPoint(trackingSpan.TextBuffer.CurrentSnapshot),
                    PointTrackingMode.Positive,
                    PositionAffinity.Predecessor,
                    textBuffer).Value);
        }

        /// <summary>
        /// Indents the line where the caret is currently located.
        /// </summary>
        /// <remarks>
        /// We don't send this command to the editor since smart indentation doesn't work along with
        /// BufferChanged event. Instead, we need to implement indentation ourselves. We still use
        /// ISmartIndentProvider provided by the language.
        /// </remarks>
        private void IndentCurrentLine(SnapshotPoint caretPosition)
        {
            Debug.Assert(_currentLanguageBuffer != null);

            var caretLine = caretPosition.GetContainingLine();
            var indentation = _smartIndenterService.GetDesiredIndentation(_textView, caretLine);

            // When the user submits via ctrl-enter, the indenter service sometimes
            // gets confused and maps the subject position after the last newline in
            // a language buffer to the location *before* the next prompt in the
            // surface buffer.  When this happens, indentation will be 0.  Fortunately,
            // no indentation is required in such cases, so we can just do nothing.
            if (indentation != null && indentation != 0)
            {
                var promptIndex = GetPromptIndexForPoint(caretPosition);
                var promptSpan = _projectionSpans[promptIndex];
                Debug.Assert(promptSpan.Kind.IsPrompt());
                int promptLength = promptSpan.Length;
                Debug.Assert(promptLength == 2 || promptLength == 0); // Not required, just expected.
                var adjustedIndentationValue = indentation.GetValueOrDefault() - promptLength;

                if (caretPosition == caretLine.End)
                {
                    // create virtual space:
                    _textView.Caret.MoveTo(new VirtualSnapshotPoint(caretPosition, adjustedIndentationValue));
                }
                else
                {
                    var langCaret = GetPositionInLanguageBuffer(caretPosition);
                    if (langCaret == null)
                    {
                        return;
                    }

                    // insert whitespace indentation:
                    var options = _textView.Options;
                    string whitespace = GetWhiteSpaceForVirtualSpace(adjustedIndentationValue, options.IsConvertTabsToSpacesEnabled() ? default(int?) : options.GetTabSize());
                    _currentLanguageBuffer.Insert(langCaret.Value, whitespace);
                }
            }
        }

        private SnapshotPoint? GetPositionInLanguageBuffer(SnapshotPoint point)
        {
            Debug.Assert(_currentLanguageBuffer != null);
            return GetPositionInBuffer(point, _currentLanguageBuffer);
        }

        private SnapshotPoint? GetPositionInStdInputBuffer(SnapshotPoint point)
        {
            Debug.Assert(_standardInputBuffer != null);
            return GetPositionInBuffer(point, _standardInputBuffer);
        }

        private SnapshotPoint? GetPositionInBuffer(SnapshotPoint point, ITextBuffer buffer)
        {
            return _textView.BufferGraph.MapDownToBuffer(
                        point,
                        PointTrackingMode.Positive,
                        buffer,
                        PositionAffinity.Successor);
        }

        // Mimics EditorOperations.GetWhiteSpaceForPositionAndVirtualSpace.
        private static string GetWhiteSpaceForVirtualSpace(int virtualSpaces, int? tabSize)
        {
            string textToInsert;
            if (tabSize.HasValue)
            {
                int tabSizeInt = tabSize.GetValueOrDefault();

                int spacesAfterPreviousTabStop = virtualSpaces % tabSizeInt;
                int columnOfPreviousTabStop = virtualSpaces - spacesAfterPreviousTabStop;

                int requiredTabs = (columnOfPreviousTabStop + tabSizeInt - 1) / tabSizeInt;

                if (requiredTabs > 0)
                {
                    textToInsert = new string('\t', requiredTabs) + new string(' ', spacesAfterPreviousTabStop);
                }
                else
                {
                    textToInsert = new string(' ', virtualSpaces);
                }
            }
            else
            {
                textToInsert = new string(' ', virtualSpaces);
            }

            return textToInsert;
        }

        /// <summary>
        /// Deletes characters preceding the current caret position in the current language buffer.
        /// </summary>
        private void DeletePreviousCharacter()
        {
            SnapshotPoint? point = MapToEditableBuffer(_textView.Caret.Position.BufferPosition);

            // We are not in an editable buffer, or we are at the start of the buffer, nothing to delete.
            if (point == null || point.Value == 0)
            {
                return;
            }

            var line = point.Value.GetContainingLine();
            int characterSize;
            if (line.Start.Position == point.Value.Position)
            {
                Debug.Assert(line.LineNumber != 0);
                characterSize = line.Snapshot.GetLineFromLineNumber(line.LineNumber - 1).LineBreakLength;
            }
            else
            {
                characterSize = 1;
            }

            point.Value.Snapshot.TextBuffer.Delete(new Span(point.Value.Position - characterSize, characterSize));

            UIThread(uiOnly =>
            {
                // scroll to the line being edited:
                SnapshotPoint caretPosition = _textView.Caret.Position.BufferPosition;
                _textView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(caretPosition.Snapshot, caretPosition, 0));
            });
        }

        private void CutOrDeleteCurrentLine(bool isCut)
        {
            ITextSnapshotLine line;
            int column;
            _textView.Caret.Position.VirtualBufferPosition.GetLineAndColumn(out line, out column);

            CutOrDelete(new[] { line.ExtentIncludingLineBreak }, isCut);

            _textView.Caret.MoveTo(new VirtualSnapshotPoint(_textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(line.LineNumber), column));
        }

        /// <summary>
        /// Deletes currently selected text from the language buffer and optionally saves it to the clipboard.
        /// </summary>
        private void CutOrDeleteSelection(bool isCut)
        {
            CutOrDelete(_textView.Selection.SelectedSpans, isCut);

            // if the selection spans over prompts the prompts remain selected, so clear manually:
            _textView.Selection.Clear();
        }

        private void CutOrDelete(IEnumerable<SnapshotSpan> projectionSpans, bool isCut)
        {
            Debug.Assert(_currentLanguageBuffer != null);

            StringBuilder deletedText = null;

            // split into multiple deletes that only affect the language/input buffer:
            ITextBuffer affectedBuffer = (_stdInputStart != null) ? _standardInputBuffer : _currentLanguageBuffer;
            using (var edit = affectedBuffer.CreateEdit())
            {
                foreach (var projectionSpan in projectionSpans)
                {
                    var spans = _textView.BufferGraph.MapDownToBuffer(projectionSpan, SpanTrackingMode.EdgeInclusive, affectedBuffer);
                    foreach (var span in spans)
                    {
                        if (isCut)
                        {
                            if (deletedText == null)
                            {
                                deletedText = new StringBuilder();
                            }

                            deletedText.Append(span.GetText());
                        }

                        edit.Delete(span);
                    }
                }

                edit.Apply();
            }

            // copy the deleted text to the clipboard:
            if (deletedText != null)
            {
                var data = new DataObject();
                if (_textView.Selection.Mode == TextSelectionMode.Box)
                {
                    data.SetData(BoxSelectionCutCopyTag, new object());
                }

                data.SetText(deletedText.ToString());
                Clipboard.SetDataObject(data, true);
            }
        }

        private bool ReduceBoxSelectionToEditableBox(bool isDelete = true)
        {
            Debug.Assert(_textView.Selection.Mode == TextSelectionMode.Box);

            VirtualSnapshotPoint anchor = _textView.Selection.AnchorPoint;
            VirtualSnapshotPoint active = _textView.Selection.ActivePoint;

            bool result;
            if (active < anchor)
            {
                result = ReduceBoxSelectionToEditableBox(ref active, ref anchor, isDelete);
            }
            else
            {
                result = ReduceBoxSelectionToEditableBox(ref anchor, ref active, isDelete);
            }

            _textView.Selection.Select(anchor, active);
            _textView.Caret.MoveTo(active);

            return result;
        }

        private bool ReduceBoxSelectionToEditableBox(ref VirtualSnapshotPoint selectionTop, ref VirtualSnapshotPoint selectionBottom, bool isDelete)
        {
            int selectionTopColumn, selectionBottomColumn;
            ITextSnapshotLine selectionTopLine, selectionBottomLine;
            selectionTop.GetLineAndColumn(out selectionTopLine, out selectionTopColumn);
            selectionBottom.GetLineAndColumn(out selectionBottomLine, out selectionBottomColumn);

            int selectionLeftColumn, selectionRightColumn;
            bool horizontallyReversed = selectionTopColumn > selectionBottomColumn;

            if (horizontallyReversed)
            {
                // bottom-left <-> top-right 
                selectionLeftColumn = selectionBottomColumn;
                selectionRightColumn = selectionTopColumn;
            }
            else
            {
                // top-left <-> bottom-right
                selectionLeftColumn = selectionTopColumn;
                selectionRightColumn = selectionBottomColumn;
            }

            var selectionTopLeft = new VirtualSnapshotPoint(selectionTopLine, selectionLeftColumn);
            var selectionBottomRight = new VirtualSnapshotPoint(selectionBottomLine, selectionRightColumn);

            SnapshotPoint editable = GetClosestEditablePoint(selectionTopLeft.Position);
            int editableColumn;
            ITextSnapshotLine editableLine;
            editable.GetLineAndColumn(out editableLine, out editableColumn);

            Debug.Assert(selectionLeftColumn <= selectionRightColumn);
            Debug.Assert(selectionTopLine.LineNumber <= selectionBottomLine.LineNumber);

            if (editable > selectionBottomRight.Position)
            {
                // entirely within readonly output region:
                return false;
            }

            int minPromptLength, maxPromptLength;
            MeasurePrompts(editableLine.LineNumber, selectionBottomLine.LineNumber + 1, out minPromptLength, out maxPromptLength);

            bool result = true;
            if (isDelete)
            {
                if (selectionLeftColumn > maxPromptLength || maxPromptLength == minPromptLength)
                {
                    selectionTopLine = editableLine;
                    selectionLeftColumn = Math.Max(selectionLeftColumn, maxPromptLength);
                    result = false;
                }
            }
            else
            {
                if (selectionRightColumn < minPromptLength)
                {
                    // entirely within readonly prompt region:
                    result = false;
                }
                else if (maxPromptLength > selectionRightColumn)
                {
                    selectionTopLine = editableLine;
                    selectionLeftColumn = maxPromptLength;
                    selectionRightColumn = maxPromptLength;
                }
                else
                {
                    selectionTopLine = editableLine;
                    selectionLeftColumn = Math.Max(maxPromptLength, selectionLeftColumn);
                }
            }

            if (horizontallyReversed)
            {
                // bottom-left <-> top-right 
                selectionTop = new VirtualSnapshotPoint(selectionTopLine, selectionRightColumn);
                selectionBottom = new VirtualSnapshotPoint(selectionBottomLine, selectionLeftColumn);
            }
            else
            {
                // top-left <-> bottom-right
                selectionTop = new VirtualSnapshotPoint(selectionTopLine, selectionLeftColumn);
                selectionBottom = new VirtualSnapshotPoint(selectionBottomLine, selectionRightColumn);
            }

            return result;
        }

        #endregion

        #region Keyboard Commands

        /// <remarks>Only consistent on the UI thread.</remarks>
        bool IInteractiveWindow.IsRunning => _dangerous_uiOnly.State != State.WaitingForInput;

        /// <remarks>Only consistent on the UI thread.</remarks>
        bool IInteractiveWindow.IsResetting => _dangerous_uiOnly.State == State.Resetting;

        /// <remarks>Only consistent on the UI thread.</remarks>
        bool IInteractiveWindow.IsInitializing => _dangerous_uiOnly.State == State.Starting || _dangerous_uiOnly.State == State.Initializing;

        IInteractiveWindowOperations IInteractiveWindow.Operations => this;

        bool IInteractiveWindowOperations.Delete()
        {
            _historySearch = null;
            bool handled = false;
            if (!_textView.Selection.IsEmpty)
            {
                if (_textView.Selection.Mode == TextSelectionMode.Stream || ReduceBoxSelectionToEditableBox())
                {
                    CutOrDeleteSelection(isCut: false);
                    MoveCaretToClosestEditableBuffer();
                    handled = true;
                }
            }

            return handled;
        }

        void IInteractiveWindowOperations.Cut()
        {
            if (_textView.Selection.IsEmpty)
            {
                CutOrDeleteCurrentLine(isCut: true);
            }
            else
            {
                CutOrDeleteSelection(isCut: true);
            }

            MoveCaretToClosestEditableBuffer();
        }

        bool IInteractiveWindowOperations.Backspace()
        {
            bool handled = false;
            if (!_textView.Selection.IsEmpty)
            {
                if (_textView.Selection.Mode == TextSelectionMode.Stream || ReduceBoxSelectionToEditableBox())
                {
                    CutOrDeleteSelection(isCut: false);
                    MoveCaretToClosestEditableBuffer();
                    handled = true;
                }
            }
            else if (_textView.Caret.Position.VirtualSpaces == 0)
            {
                DeletePreviousCharacter();
                handled = true;
            }

            return handled;
        }

        bool IInteractiveWindowOperations.TrySubmitStandardInput()
        {
            _historySearch = null;
            if (_stdInputStart != null)
            {
                if (InStandardInputRegion(_textView.Caret.Position.BufferPosition))
                {
                    SubmitStandardInput();
                }

                return true;
            }

            return false;
        }

        bool IInteractiveWindowOperations.BreakLine()
        {
            return HandlePostServicesReturn(false);
        }

        bool IInteractiveWindowOperations.Return()
        {
            _historySearch = null;
            return HandlePostServicesReturn(true);
        }

        private bool HandlePostServicesReturn(bool trySubmit)
        {
            if (_currentLanguageBuffer == null)
            {
                return false;
            }

            // handle "RETURN" command that is not handled by either editor or service
            var langCaret = GetPositionInLanguageBuffer(Caret.Position.BufferPosition);
            if (langCaret != null)
            {
                int caretPosition = langCaret.Value.Position;

                // note that caret might be located in virtual space behind the current buffer end:
                if (trySubmit && caretPosition >= _currentLanguageBuffer.CurrentSnapshot.Length && CanExecuteActiveCode())
                {
                    UIThread(uiOnly => uiOnly.SubmitAsync());
                    return true;
                }

                // insert new line (triggers secondary prompt injection in buffer changed event):
                _currentLanguageBuffer.Insert(caretPosition, _lineBreakString);
                IndentCurrentLine(_textView.Caret.Position.BufferPosition);

                return true;
            }
            else
            {
                MoveCaretToClosestEditableBuffer();
            }

            return false;
        }

        #endregion

        #region Command Debugging

#if DUMP_COMMANDS
        private static void DumpCmd(string prefix, int result, ref Guid pguidCmdGroup, uint cmd, uint cmdf)
        {
            string cmdName;
            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                cmdName = ((VSConstants.VSStd97CmdID)cmd).ToString();
            }
            else if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                cmdName = ((VSConstants.VSStd2KCmdID)cmd).ToString();
            }
            else if (pguidCmdGroup == VSConstants.VsStd2010)
            {
                cmdName = ((VSConstants.VSStd2010CmdID)cmd).ToString();
            }
            else if (pguidCmdGroup == GuidList.guidReplWindowCmdSet)
            {
                cmdName = ((ReplCommandId)cmd).ToString();
            }
            else
            {
                return;
            }

            Debug.WriteLine("{3}({0}) -> {1}  {2}", cmdName, Enum.Format(typeof(OLECMDF), (OLECMDF)cmdf, "F"), result, prefix);
        }
#endif

        #endregion

        #region Caret and Cursor

        private ITextCaret Caret => _textView.Caret;

        private void MoveCaretToClosestEditableBuffer()
        {
            SnapshotPoint currentPosition = _textView.Caret.Position.BufferPosition;
            SnapshotPoint newPosition = GetClosestEditablePoint(currentPosition);
            if (currentPosition != newPosition)
            {
                _textView.Caret.MoveTo(newPosition);
            }
        }

        /// <summary>
        /// Finds a point in an editable buffer that is the closest towards the end to the given projection point.
        /// </summary>
        private SnapshotPoint GetClosestEditablePoint(SnapshotPoint projectionPoint)
        {
            ITextBuffer editableBuffer = (_stdInputStart != null) ? _standardInputBuffer : _currentLanguageBuffer;

            if (editableBuffer == null)
            {
                return new SnapshotPoint(_projectionBuffer.CurrentSnapshot, _projectionBuffer.CurrentSnapshot.Length);
            }

            SnapshotPoint? point = GetPositionInBuffer(projectionPoint, editableBuffer);
            if (point != null)
            {
                return projectionPoint;
            }

            var projectionLine = projectionPoint.GetContainingLine();

            SnapshotPoint? lineEnd = _textView.BufferGraph.MapDownToBuffer(
                projectionLine.End,
                PointTrackingMode.Positive,
                editableBuffer,
                PositionAffinity.Successor);

            SnapshotPoint editablePoint;
            if (lineEnd == null)
            {
                editablePoint = new SnapshotPoint(editableBuffer.CurrentSnapshot, 0);
            }
            else
            {
                editablePoint = lineEnd.Value.GetContainingLine().Start;
            }

            return _textView.BufferGraph.MapUpToBuffer(
                editablePoint,
                PointTrackingMode.Positive,
                PositionAffinity.Successor,
                _projectionBuffer).Value;
        }

        /// <summary>
        /// Maps point to the current language buffer or standard input buffer.
        /// </summary>
        private SnapshotPoint? MapToEditableBuffer(SnapshotPoint projectionPoint)
        {
            SnapshotPoint? result = null;

            if (_currentLanguageBuffer != null)
            {
                result = GetPositionInLanguageBuffer(projectionPoint);
            }

            if (result != null)
            {
                return result;
            }

            if (_standardInputBuffer != null)
            {
                result = GetPositionInStdInputBuffer(projectionPoint);
            }

            return result;
        }

        /// <summary>
        /// Returns the insertion point relative to the current language buffer.
        /// </summary>
        private int GetActiveCodeInsertionPosition()
        {
            Debug.Assert(_currentLanguageBuffer != null);

            var langPoint = _textView.BufferGraph.MapDownToBuffer(
                new SnapshotPoint(
                    _projectionBuffer.CurrentSnapshot,
                    Caret.Position.BufferPosition.Position),
                PointTrackingMode.Positive,
                _currentLanguageBuffer,
                PositionAffinity.Predecessor);

            if (langPoint != null)
            {
                return langPoint.Value;
            }

            return _currentLanguageBuffer.CurrentSnapshot.Length;
        }

        private void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            // make sure language buffer exist
            if (_currentLanguageBuffer == null)
            {
                return;
            }

            var caret = e.NewPosition.BufferPosition;

            // make sure caret is on the right line
            // 1. changes are on virtual space
            if (e.NewPosition.BufferPosition == e.OldPosition.BufferPosition)
            {
                return;
            }

            // 2. caret is at the end of the surface line
            if (caret != caret.GetContainingLine().End)
            {
                return;
            }

            // 3. subject line has length == 0
            var point = e.NewPosition.Point.GetInsertionPoint(b => b == _currentLanguageBuffer);
            if (!point.HasValue)
            {
                return;
            }

            var line = point.Value.GetContainingLine();
            if (point.Value != line.End || line.Length != 0)
            {
                return;
            }

            try
            {
                // detach event handler
                Caret.PositionChanged -= CaretPositionChanged;

                IndentCurrentLine(caret);
            }
            finally
            {
                // attach event handler
                Caret.PositionChanged += CaretPositionChanged;
            }
        }

        #endregion

        #region Active Code and Standard Input

        /// <summary>
        /// Returns the full text of the current active input.
        /// </summary>
        private string GetActiveCode()
        {
            return _currentLanguageBuffer.CurrentSnapshot.GetText();
        }

        /// <summary>
        /// Sets the active code to the specified text w/o executing it.
        /// </summary>
        private void SetActiveCode(string text)
        {
            // TODO (tomat): this should be handled by the language intellisense provider, not here:
            var completionSession = this.SessionStack.TopSession;
            if (completionSession != null)
            {
                completionSession.Dismiss();
            }

            using (var edit = _currentLanguageBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, editTag: null))
            {
                edit.Replace(new Span(0, _currentLanguageBuffer.CurrentSnapshot.Length), text);
                edit.Apply();
            }
        }

        /// <summary>
        /// Sets the active code to the specified history entry.
        /// </summary>
        /// <param name="entry"></param>
        private void SetActiveCodeToHistory(History.Entry entry)
        {
            SetActiveCode(entry.Text);
        }

        private void ClearInput()
        {
            Debug.Assert(_projectionSpans.Count > 0);

            // Finds the last primary prompt (standard input or code input).
            // Removes all spans following the primary prompt from the projection buffer.
            int i = _projectionSpans.Count - 1;
            while (i >= 0)
            {
                if (_projectionSpans[i].Kind == ReplSpanKind.Prompt || _projectionSpans[i].Kind == ReplSpanKind.StandardInputPrompt)
                {
                    Debug.Assert(i != _projectionSpans.Count - 1);
                    break;
                }

                i--;
            }

            if (i >= 0)
            {
                if (_projectionSpans[i].Kind != ReplSpanKind.StandardInputPrompt)
                {
                    _currentLanguageBuffer.Delete(new Span(0, _currentLanguageBuffer.CurrentSnapshot.Length));
                }
                else
                {
                    Debug.Assert(_stdInputStart != null);
                    _standardInputBuffer.Delete(Span.FromBounds(_stdInputStart.Value, _standardInputBuffer.CurrentSnapshot.Length));
                }
            }
        }

        // TODO: What happens if multiple non-UI threads call this method? (https://github.com/dotnet/roslyn/issues/3984)
        TextReader IInteractiveWindow.ReadStandardInput()
        {
            // shouldn't be called on the UI thread because we'll hang
            RequiresNonUIThread();

            State previousState = default(State); // Compiler doesn't know these lambdas run sequentially.
            UIThread(uiOnly =>
            {
                previousState = uiOnly.State;

                uiOnly.State = State.ReadingStandardInput;

                _buffer.Flush();

                if (previousState == State.WaitingForInput && _projectionSpans.Count > 0 && _projectionSpans[_projectionSpans.Count - 1].Kind == ReplSpanKind.Language)
                {
                    // we need to remove our input prompt.
                    uiOnly.RemoveLastInputPrompt();
                }

                AddStandardInputSpan();

                Caret.EnsureVisible();
                uiOnly.ResetCursor();

                uiOnly.UncommittedInput = null;
                _stdInputStart = _standardInputBuffer.CurrentSnapshot.Length;
            });

            _inputEvent.WaitOne();
            _stdInputStart = null;

            UIThread(uiOnly =>
            {
                // if the user cleared the screen we cancelled the input, so we won't have our span here.
                // We can also have an interleaving output span, so we'll search back for the last input span.
                int i = uiOnly.IndexOfLastStandardInputSpan();
                if (i != -1)
                {
                    uiOnly.RemoveProtection(_standardInputBuffer, uiOnly.StandardInputProtection);

                    // replace previous span w/ a span that won't grow...
                    var oldSpan = _projectionSpans[i];
                    var newSpan = new ReplSpan(
                        oldSpan.TrackingSpan.WithEndTrackingMode(PointTrackingMode.Negative),
                        ReplSpanKind.StandardInput,
                        oldSpan.LineNumber);

                    uiOnly.ReplaceProjectionSpan(i, newSpan);
                    uiOnly.ApplyProtection(_standardInputBuffer, uiOnly.StandardInputProtection, allowAppend: true);

                    uiOnly.NewOutputBuffer();

                    // TODO: Do we need to restore the state if reading is cancelled? (https://github.com/dotnet/roslyn/issues/3984)
                    if (previousState == State.WaitingForInput)
                    {
                        uiOnly.PrepareForInput(); // Will update _uiOnly.State.
                    }
                    else
                    {
                        uiOnly.State = previousState;
                    }
                }
            });

            // input has been cancelled:
            if (_inputValue.HasValue)
            {
                _history.Add(_inputValue.Value);
                return new StringReader(_inputValue.Value.GetText());
            }
            else
            {
                return null;
            }
        }

        private bool InStandardInputRegion(SnapshotPoint point)
        {
            if (_stdInputStart == null)
            {
                return false;
            }

            var stdInputPoint = GetPositionInStdInputBuffer(point);
            return stdInputPoint != null && stdInputPoint.Value.Position >= _stdInputStart.Value;
        }

        private void SubmitStandardInput()
        {
            AppendLineNoPromptInjection(_standardInputBuffer);
            _inputValue = new SnapshotSpan(_standardInputBuffer.CurrentSnapshot, Span.FromBounds(_stdInputStart.Value, _standardInputBuffer.CurrentSnapshot.Length));
            _inputEvent.Set();
        }

        #endregion

        #region Output

        Span IInteractiveWindow.Write(string text)
        {
            int result = _buffer.Write(text);
            return new Span(result, (text != null ? text.Length : 0));
        }

        public Span WriteLine(string text)
        {
            int result = _buffer.Write(text);
            _buffer.Write(_lineBreakString);
            return new Span(result, (text != null ? text.Length : 0) + _lineBreakString.Length);
        }

        Span IInteractiveWindow.WriteError(string text)
        {
            int result = _buffer.Write(text);
            var res = new Span(result, (text != null ? text.Length : 0));
            _errorOutputWriter.Spans.Add(res);
            return res;
        }

        Span IInteractiveWindow.WriteErrorLine(string text)
        {
            int result = _buffer.Write(text);
            _buffer.Write(_lineBreakString);
            var res = new Span(result, (text != null ? text.Length : 0) + _lineBreakString.Length);
            _errorOutputWriter.Spans.Add(res);
            return res;
        }

        void IInteractiveWindow.Write(UIElement element)
        {
            if (element == null)
            {
                return;
            }

            _buffer.Flush();
            InlineAdornmentProvider.AddInlineAdornment(_textView, element, OnAdornmentLoaded);
            _adornmentToMinimize = true; // TODO (https://github.com/dotnet/roslyn/issues/4044): probably ui only
            WriteLine(string.Empty);
            WriteLine(string.Empty);
        }

        private void OnAdornmentLoaded(object source, EventArgs e)
        {
            // Make sure the caret line is rendered
            DoEvents();
            Caret.EnsureVisible();
        }

        #endregion

        #region Execution

        private bool CanExecuteActiveCode()
        {
            Debug.Assert(_currentLanguageBuffer != null);

            var input = GetActiveCode();
            if (string.IsNullOrWhiteSpace(input))
            {
                // Always allow "execution" of a blank line.
                // This will just close the current prompt and start a new one
                return true;
            }

            // Ignore any whitespace past the insertion point when determining
            // whether or not we're at the end of the input
            var pt = GetActiveCodeInsertionPosition();
            var isEnd = (pt == input.Length) || (pt >= 0 && input.Substring(pt).Trim().Length == 0);
            if (!isEnd)
            {
                return false;
            }

            return _evaluator.CanExecuteCode(input);
        }

        #endregion

        #region Buffers, Spans and Prompts
        private ReplSpan CreateStandardInputPrompt()
        {
            return CreatePrompt(string.Empty, ReplSpanKind.StandardInputPrompt, LastLineNumber);
        }

        private ReplSpan CreatePrimaryPrompt()
        {
            return CreatePrompt(_evaluator.GetPrompt(), ReplSpanKind.Prompt, LastLineNumber);
        }

        private ReplSpan CreatePrompt(string prompt, ReplSpanKind promptKind, int lineNumber)
        {
            Debug.Assert(promptKind.IsPrompt());
            return new ReplSpan(prompt, promptKind, lineNumber);
        }

        private ReplSpan CreateSecondaryPrompt(int lineNumber)
        {
            // TODO (crwilcox) format prompt used to get a blank here but now gets "> " from get prompt.
            return CreatePrompt(_evaluator.GetPrompt(), ReplSpanKind.SecondaryPrompt, lineNumber);
        }

        /// <summary>
        /// Returns the lengths of the longest and shortest prompts within the specified range of lines of the current submission buffer.
        /// </summary>
        private void MeasurePrompts(int startLine, int endLine, out int minPromptLength, out int maxPromptLength)
        {
            Debug.Assert(endLine > startLine);

            var promptSpanIndex = GetProjectionSpanIndexFromEditableBufferPosition(_projectionBuffer.CurrentSnapshot, _projectionSpans.Count, startLine) - 1;
            Debug.Assert(_projectionSpans[promptSpanIndex].Kind.IsPrompt());

            var promptSpan = _projectionSpans[promptSpanIndex];
            minPromptLength = maxPromptLength = promptSpan.Length;
        }

        private int GetPromptIndexForPoint(SnapshotPoint point)
        {
            int lineNumber = point.GetContainingLine().LineNumber;
            return _projectionSpans.BinarySearch(new ReplSpan("", ReplSpanKind.Prompt, lineNumber), ReplSpanComparer.Instance);
        }

        private class ReplSpanComparer : IComparer<ReplSpan>
        {
            public static readonly IComparer<ReplSpan> Instance = new ReplSpanComparer();

            private ReplSpanComparer()
            {
            }

            int IComparer<ReplSpan>.Compare(ReplSpan x, ReplSpan y)
            {
                int comp = x.LineNumber - y.LineNumber;
                return comp != 0
                    ? comp
                    : GetKindOrdinal(x) - GetKindOrdinal(y);
            }

            /// <summary>
            /// Within a given line, the order is Output &lt; Prompt &lt; Language/StdIn.
            /// </summary>
            /// <remarks>
            /// While, visually, it appears that every line begins with a prompt, a line
            /// may actually begin with an empty output prompt, because at any given time
            /// there must be a writable output buffer for <see cref="AppendOutput"/>.
            /// Generally, this occurs when a submission has no output.
            /// </remarks>
            private static int GetKindOrdinal(ReplSpan span)
            {
                switch (span.Kind)
                {
                    case ReplSpanKind.Output:
                        return 1;
                    case ReplSpanKind.Prompt:
                    case ReplSpanKind.SecondaryPrompt:
                    case ReplSpanKind.StandardInputPrompt:
                        return 2;
                    case ReplSpanKind.Language:
                    case ReplSpanKind.StandardInput:
                        return 3;
                    default:
                        throw new InvalidOperationException($"Unexpected {nameof(ReplSpanKind)} '{span.Kind}'");
                }
            }
        }

        /// <summary>
        /// Creates the language span for the last line of the active input.  This span
        /// is effectively edge inclusive so it will grow as the user types at the end.
        /// </summary>
        private CustomTrackingSpan CreateLanguageTrackingSpan(Span span)
        {
            return new CustomTrackingSpan(
                _currentLanguageBuffer.CurrentSnapshot,
                span,
                PointTrackingMode.Negative,
                PointTrackingMode.Positive);
        }

        /// <summary>
        /// Creates the tracking span for a line previous in the input.  This span
        /// is negative tracking on the end so when the user types at the beginning of
        /// the next line we don't grow with the change.
        /// </summary>
        private CustomTrackingSpan CreateNonGrowingLanguageTrackingSpan(Span span)
        {
            return new CustomTrackingSpan(
                _currentLanguageBuffer.CurrentSnapshot,
                span,
                PointTrackingMode.Negative,
                PointTrackingMode.Negative);
        }

        /// <summary>
        /// Add a zero-width tracking span at the end of the projection buffer mapping to the end of the standard input buffer.
        /// </summary>
        private void AddStandardInputSpan()
        {
            ReplSpan promptSpan = CreateStandardInputPrompt();

            var currentSnapshot = _standardInputBuffer.CurrentSnapshot;
            var stdInputSpan = new CustomTrackingSpan(
                currentSnapshot,
                new Span(currentSnapshot.Length, 0),
                PointTrackingMode.Negative,
                PointTrackingMode.Positive);

            ReplSpan inputSpan = new ReplSpan(stdInputSpan, ReplSpanKind.StandardInput, promptSpan.LineNumber);

            AppendProjectionSpans(promptSpan, inputSpan);
        }

        private const int SpansPerLineOfInput = 2;

        private static readonly object s_suppressPromptInjectionTag = new object();

        private struct SpanRangeEdit
        {
            public readonly int Start;
            public readonly int Count;
            public readonly ReplSpan[] Replacement;

            public SpanRangeEdit(int start, int count, ReplSpan[] replacement)
            {
                Start = start;
                Count = count;
                Replacement = replacement;
            }
        }

        private bool TryGetCurrentLanguageBufferExtent(IProjectionSnapshot projectionSnapshot, out Span result)
        {
            if (projectionSnapshot.SpanCount == 0)
            {
                result = default(Span);
                return false;
            }

            // the last source snapshot is always a projection of a language buffer:
            var snapshot = projectionSnapshot.GetSourceSpans(projectionSnapshot.SpanCount - 1, 1)[0].Snapshot;
            if (snapshot.TextBuffer != _currentLanguageBuffer)
            {
                result = default(Span);
                return false;
            }

            SnapshotPoint start = new SnapshotPoint(snapshot, 0);
            SnapshotPoint end = new SnapshotPoint(snapshot, snapshot.Length);

            // projection of the previous version of current language buffer snapshot:
            var surfaceSpans = projectionSnapshot.MapFromSourceSnapshot(new SnapshotSpan(start, end));

            // the language buffer might be projected to multiple surface lines:
            Debug.Assert(surfaceSpans.Count > 0);
            result = new Span(surfaceSpans[0].Start, surfaceSpans.Last().End);
            return true;
        }

        private void ProjectionBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // this is an edit performed in this event:
            if (e.EditTag == s_suppressPromptInjectionTag)
            {
                return;
            }

            // projection buffer is changed before language buffer is created (for example, output might be printed out during initialization):
            if (_currentLanguageBuffer == null)
            {
                return;
            }

            var oldSnapshot = (IProjectionSnapshot)e.Before;
            var newSnapshot = (IProjectionSnapshot)e.After;
            Span oldSurfaceLanguageBufferExtent;
            Span newSurfaceLanguageBufferExtent;

            if (!TryGetCurrentLanguageBufferExtent(oldSnapshot, out oldSurfaceLanguageBufferExtent) ||
                !TryGetCurrentLanguageBufferExtent(newSnapshot, out newSurfaceLanguageBufferExtent))
            {
                return;
            }

            List<SpanRangeEdit> spanEdits = null;
            int oldProjectionSpanCount = _projectionSpans.Count;

            // changes are sorted by position
            foreach (var change in e.Changes)
            {
                // Old/new span might be outside of the language buffer -- on the left of it, since
                // the language buffer always reaches to the end of the projection buffer.
                Span oldSurfaceIntersection = oldSurfaceLanguageBufferExtent.Intersection(change.OldSpan) ?? new Span(oldSurfaceLanguageBufferExtent.Start, 0);
                Span newSurfaceIntersection = newSurfaceLanguageBufferExtent.Intersection(change.NewSpan) ?? new Span(newSurfaceLanguageBufferExtent.Start, 0);

                // change doesn't affect the language buffer:
                if (oldSurfaceIntersection.IsEmpty && newSurfaceIntersection.IsEmpty)
                {
                    continue;
                }

                var oldSurfaceStartLineNumber = oldSnapshot.GetLineNumberFromPosition(oldSurfaceIntersection.Start);
                var oldSurfaceEndLineNumber = oldSnapshot.GetLineNumberFromPosition(oldSurfaceIntersection.End);

                // The change doesn't include line breaks and is entirely within the current
                // language buffer. Note that we might need to proceed with span replacement even if
                // line count delta is zero: the tracking spans of all but last language buffer
                // projections need to stop growing.
                if (oldSurfaceStartLineNumber == oldSurfaceEndLineNumber &&
                    change.LineCountDelta == 0 &&
                    oldSurfaceIntersection == change.OldSpan &&
                    newSurfaceIntersection == change.NewSpan)
                {
                    continue;
                }

                // calculate which prompts and language projection spans to remove and replace with new spans:
                int oldStartSpanIndex = GetProjectionSpanIndexFromEditableBufferPosition(oldSnapshot, oldProjectionSpanCount, oldSurfaceStartLineNumber);
                int oldEndSpanIndex = GetProjectionSpanIndexFromEditableBufferPosition(oldSnapshot, oldProjectionSpanCount, oldSurfaceEndLineNumber);

                int spansToReplace = oldEndSpanIndex - oldStartSpanIndex + 1;
                Debug.Assert(spansToReplace >= 1);

                var newSubjectStartLine = newSnapshot.MapToSourceSnapshot(newSurfaceIntersection.Start).GetContainingLine();
                var newSubjectEndLine = newSnapshot.MapToSourceSnapshot(newSurfaceIntersection.End).GetContainingLine();

                var newSubjectEndLineNumber = newSubjectEndLine.LineNumber;

                int i = 0;
                int lineBreakCount = newSubjectEndLineNumber - newSubjectStartLine.LineNumber;
                var newSpans = new ReplSpan[lineBreakCount * SpansPerLineOfInput + 1];

                var subjectLine = newSubjectStartLine;
                while (true)
                {
                    if (subjectLine.LineNumber != newSubjectStartLine.LineNumber)
                    {
                        // TODO (crwilcox): do we need two prompts?  Can I tell it to not do this?  Or perhaps we do want this since we want different markings?
                        newSpans[i++] = CreateSecondaryPrompt(lineNumber: -1); // Line number will be corrected later.
                    }

                    newSpans[i++] = CreateLanguageSpanForLine(subjectLine, lineNumber: -1); // Line number will be corrected later.

                    if (subjectLine.LineNumber == newSubjectEndLineNumber)
                    {
                        break;
                    }

                    subjectLine = subjectLine.Snapshot.GetLineFromLineNumber(subjectLine.LineNumber + 1);
                }

                Debug.Assert(i == newSpans.Length);

                if (spanEdits == null)
                {
                    spanEdits = new List<SpanRangeEdit>();
                }

                spanEdits.Add(new SpanRangeEdit(oldStartSpanIndex, spansToReplace, newSpans));
            }

            if (spanEdits != null)
            {
                ReplaceProjectionSpans(spanEdits);
            }
        }

        /// <remarks>
        /// This should only be called from within the current language buffer.  If there are
        /// any output or standard input buffers between the specified line and the end of the
        /// surface buffer, then the result will be incorrect.
        /// </remarks>
        private int GetProjectionSpanIndexFromEditableBufferPosition(ITextSnapshot surfaceSnapshot, int projectionSpansCount, int surfaceLineNumber)
        {
            // The current language buffer is projected to a set of projections interleaved regularly by prompt projections 
            // and ending at the end of the projection buffer, each language buffer projection is on a separate line:
            //   [prompt)[language)...[prompt)[language)<end of projection buffer>
            int result = projectionSpansCount - (surfaceSnapshot.LineCount - surfaceLineNumber) * SpansPerLineOfInput + 1;
            Debug.Assert(_projectionSpans[result].Kind == ReplSpanKind.Language);
            return result;
        }

        private void ReplaceProjectionSpans(List<SpanRangeEdit> spanEdits)
        {
            Debug.Assert(spanEdits.Count > 0);

            int start = spanEdits.First().Start;
            int end = spanEdits.Last().Start + spanEdits.Last().Count;

            var replacement = new List<ReplSpan>();
            replacement.AddRange(spanEdits[0].Replacement);
            int lastEnd = start + spanEdits[0].Count;

            for (int i = 1; i < spanEdits.Count; i++)
            {
                SpanRangeEdit edit = spanEdits[i];

                int gap = edit.Start - lastEnd;

                // there is always at least prompt span in between subsequent edits
                Debug.Assert(gap != 0);

                // spans can't share more then one span
                Debug.Assert(gap >= -1);

                if (gap == -1)
                {
                    replacement.AddRange(edit.Replacement.Skip(1));
                }
                else
                {
                    replacement.AddRange(_projectionSpans.Skip(lastEnd).Take(gap));
                    replacement.AddRange(edit.Replacement);
                }

                lastEnd = edit.Start + edit.Count;
            }

            ReplaceProjectionSpans(start, end - start, replacement);
        }

        private ReplSpan CreateLanguageSpanForLine(ITextSnapshotLine languageLine, int lineNumber)
        {
            CustomTrackingSpan languageSpan;
            if (languageLine.LineNumber == languageLine.Snapshot.LineCount - 1)
            {
                languageSpan = CreateLanguageTrackingSpan(languageLine.ExtentIncludingLineBreak);
            }
            else
            {
                languageSpan = CreateNonGrowingLanguageTrackingSpan(languageLine.ExtentIncludingLineBreak);
            }

            return new ReplSpan(languageSpan, ReplSpanKind.Language, lineNumber);
        }

        private void AppendLineNoPromptInjection(ITextBuffer buffer)
        {
            using (var edit = buffer.CreateEdit(EditOptions.None, null, s_suppressPromptInjectionTag))
            {
                edit.Insert(buffer.CurrentSnapshot.Length, _lineBreakString);
                edit.Apply();
            }
        }

        // WARNING: When updating projection spans we need to update _projectionSpans list first and 
        // then projection buffer, since the projection buffer update might trigger events that might 
        // access the projection spans.

        private void AppendProjectionSpans(ReplSpan span1, ReplSpan span2)
        {
            Debug.Assert(span1.Kind.IsPrompt());
            Debug.Assert(span2.Kind == ReplSpanKind.Language || span2.Kind == ReplSpanKind.StandardInput);
            Debug.Assert(span1.LineNumber == span2.LineNumber);

            int index = _projectionSpans.Count;
            _projectionSpans.Add(span1);
            _projectionSpans.Add(span2);
            _projectionBuffer.ReplaceSpans(index, 0, new[] { span1.Span, span2.Span }, EditOptions.None, editTag: s_suppressPromptInjectionTag);
            CheckProjectionSpanLineNumbers();
        }

        private void ReplaceProjectionSpans(int position, int count, IList<ReplSpan> newSpans)
        {
            int newSpansCount = newSpans.Count;
            int lineNumber = _projectionSpans[position].LineNumber;

            var oldProjectionSpanCount = _projectionSpans.Count;
            for (int i = position + count; i < oldProjectionSpanCount; i++)
            {
                newSpans.Add(_projectionSpans[i]);
            }

            _projectionSpans.RemoveRange(position, oldProjectionSpanCount - position);

            foreach (var newSpan in newSpans)
            {
                _projectionSpans.Add(newSpan.WithLineNumber(lineNumber));

                var kind = newSpan.Kind;
                Debug.Assert(kind == ReplSpanKind.Prompt || kind == ReplSpanKind.SecondaryPrompt || kind == ReplSpanKind.Language);
                if (kind == ReplSpanKind.Language)
                {
                    lineNumber++;
                }
            }

            // NB: Specifially didn't update newSpansCount when we added the existing spans.
            object[] trackingSpans = new object[newSpansCount];
            for (int i = 0; i < newSpansCount; i++)
            {
                trackingSpans[i] = newSpans[i].Span;
            }
            
            _projectionBuffer.ReplaceSpans(position, count, trackingSpans, EditOptions.None, s_suppressPromptInjectionTag);
            CheckProjectionSpanLineNumbers();
        }

        [Conditional("DEBUG")]
        private void CheckProjectionSpanLineNumbers()
        {
            if (_projectionSpans.Count == 0) return;

            ReplSpan prev = _projectionSpans[0];
            Debug.Assert(prev.LineNumber == 0);

            int prevPromptLineNumber = prev.Kind.IsPrompt() ? prev.LineNumber : -1;

            foreach (var curr in _projectionSpans.Skip(1))
            {
                if (curr.Kind.IsPrompt())
                {
                    int currPromptLineNumber = curr.LineNumber;
                    Debug.Assert(prevPromptLineNumber < currPromptLineNumber); // One prompt per line.
                    prevPromptLineNumber = currPromptLineNumber;
                }

                int comp = ReplSpanComparer.Instance.Compare(prev, curr);
                Debug.Assert(comp <= 0);
                Debug.Assert(comp < 0 || curr.Kind == ReplSpanKind.Output); // We might add a trailing newline as a separate span.
                prev = curr;
            }
        }

        #endregion

        #region Editor Helpers

        private static ITextSnapshotLine GetLastLine(ITextSnapshot snapshot)
        {
            return snapshot.GetLineFromLineNumber(snapshot.LineCount - 1);
        }

        private int LastLineNumber => TextBuffer.CurrentSnapshot.LineCount - 1;

        private static int IndexOfNonWhiteSpaceCharacter(ITextSnapshotLine line)
        {
            var snapshot = line.Snapshot;
            int start = line.Start.Position;
            int count = line.Length;
            for (int i = 0; i < count; i++)
            {
                if (!char.IsWhiteSpace(snapshot[start + i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private sealed class EditResolver : IProjectionEditResolver
        {
            private readonly InteractiveWindow _window;

            public EditResolver(InteractiveWindow window)
            {
                _window = window;
            }

            // We always favor the last buffer of our language type.  This handles cases where we're on a boundary between a prompt and a language 
            // buffer - we favor the language buffer because the prompts cannot be edited.  In the case of two language buffers this also works because
            // our spans are laid out like:
            // <lang span 1 including newline>
            // <prompt span><lang span 2>
            // 
            // In the case where the prompts are in the margin we have an insertion conflict between the two language spans.  But because
            // lang span 1 includes the new line in order to be oun the boundary we need to be on lang span 2's line.
            // 
            // This works the same way w/ our input buffer where the input buffer present instead of <lang span 2>.

            void IProjectionEditResolver.FillInInsertionSizes(SnapshotPoint projectionInsertionPoint, ReadOnlyCollection<SnapshotPoint> sourceInsertionPoints, string insertionText, IList<int> insertionSizes)
            {
                int index = IndexOfEditableBuffer(sourceInsertionPoints);
                if (index != -1)
                {
                    insertionSizes[index] = insertionText.Length;
                }
            }

            int IProjectionEditResolver.GetTypicalInsertionPosition(SnapshotPoint projectionInsertionPoint, ReadOnlyCollection<SnapshotPoint> sourceInsertionPoints)
            {
                int index = IndexOfEditableBuffer(sourceInsertionPoints);
                return index != -1 ? index : 0;
            }

            void IProjectionEditResolver.FillInReplacementSizes(SnapshotSpan projectionReplacementSpan, ReadOnlyCollection<SnapshotSpan> sourceReplacementSpans, string insertionText, IList<int> insertionSizes)
            {
                int index = IndexOfEditableBuffer(sourceReplacementSpans);
                if (index != -1)
                {
                    insertionSizes[index] = insertionText.Length;
                }
            }

            private int IndexOfEditableBuffer(ReadOnlyCollection<SnapshotPoint> sourceInsertionPoints)
            {
                for (int i = sourceInsertionPoints.Count - 1; i >= 0; i--)
                {
                    var insertionBuffer = sourceInsertionPoints[i].Snapshot.TextBuffer;
                    if (insertionBuffer == _window._currentLanguageBuffer || insertionBuffer == _window._standardInputBuffer)
                    {
                        return i;
                    }
                }

                return -1;
            }

            private int IndexOfEditableBuffer(ReadOnlyCollection<SnapshotSpan> sourceInsertionPoints)
            {
                for (int i = sourceInsertionPoints.Count - 1; i >= 0; i--)
                {
                    var insertionBuffer = sourceInsertionPoints[i].Snapshot.TextBuffer;
                    if (insertionBuffer == _window._currentLanguageBuffer || insertionBuffer == _window._standardInputBuffer)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        #endregion

        #region UI Dispatcher Helpers

        private Dispatcher Dispatcher => ((FrameworkElement)_textView).Dispatcher;

        internal bool OnUIThread()
        {
            return Dispatcher.CheckAccess();
        }

        private T UIThread<T>(Func<UIThreadOnly, T> func)
        {
            if (!OnUIThread())
            {
                return (T)Dispatcher.Invoke(func, _dangerous_uiOnly); // Safe because of dispatch.
            }

            return func(_dangerous_uiOnly); // Safe because of check.
        }

        private void UIThread(Action<UIThreadOnly> action)
        {
            if (!OnUIThread())
            {
                Dispatcher.Invoke(action, _dangerous_uiOnly); // Safe because of dispatch.
                return;
            }

            action(_dangerous_uiOnly); // Safe because of check.
        }

        private void RequiresUIThread()
        {
            if (!OnUIThread())
            {
                throw new InvalidOperationException(InteractiveWindowResources.RequireUIThread);
            }
        }

        private void RequiresNonUIThread()
        {
            if (OnUIThread())
            {
                throw new InvalidOperationException(InteractiveWindowResources.RequireNonUIThread);
            }
        }

        private static void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action<DispatcherFrame>(f => f.Continue = false),
                frame);

            Dispatcher.PushFrame(frame);
        }

        #endregion

        #region Testing

        internal List<ReplSpan> ProjectionSpans => _projectionSpans;

        internal event Action<State> StateChanged;

        #endregion
    }
}
