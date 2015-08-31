// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Dumps commands in QueryStatus and Exec.
// #define DUMP_COMMANDS

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    // TODO: We should condense committed language buffers into a single language buffer and save the
    // classifications from the previous language buffer if the perf of having individual buffers
    // starts having problems w/ a large number of inputs.

    /// <summary>
    /// Provides implementation of a Repl Window built on top of the VS editor using projection buffers.
    /// </summary>
    internal partial class InteractiveWindow : IInteractiveWindow, IInteractiveWindowOperations2
    {
        private bool _adornmentToMinimize;

        private readonly IWpfTextView _textView;

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
        private readonly IContentType _inertType;

        private ITextBuffer _currentLanguageBuffer;

        ////
        //// Standard input.
        ////

        private readonly SemaphoreSlim _inputReaderSemaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);

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
            _textView.Caret.PositionChanged -= CaretPositionChanged;

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

        /// <remarks>
        /// Normally, an async method would have an NFW exception filter.  This
        /// one doesn't because it just calls other async methods that already
        /// have filters.
        /// </remarks>
        async Task IInteractiveWindow.SubmitAsync(IEnumerable<string> inputs)
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

            // This indicates that the last submission has completed.
            await completion.Task.ConfigureAwait(false);

            // These should all have finished already, but we'll await them so that their
            // statuses are folded into the task we return.
            await Task.WhenAll(pendingSubmissions.Select(p => p.Task)).ConfigureAwait(false);
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
            return UIThread(uiOnly => uiOnly.ResetAsync(initialize));
        }

        void IInteractiveWindowOperations.ClearHistory()
        {
            UIThread(uiOnly => uiOnly.ClearHistory());
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
            UIThread(uiOnly => uiOnly.ExecuteInputAsync());
        }

        /// <summary>
        /// Appends text to the output buffer and updates projection buffer to include it.
        /// WARNING: this has to be the only method that writes to the output buffer so that 
        /// the output buffering counters are kept in sync.
        /// </summary>
        internal void AppendOutput(IEnumerable<string> output)
        {
            RequiresUIThread();
            _dangerous_uiOnly.AppendOutput(output);
        }

        /// <summary>
        /// Clears the current input
        /// </summary>
        void IInteractiveWindowOperations.Cancel()
        {
            UIThread(uiOnly => uiOnly.Cancel());
        }

        void IInteractiveWindowOperations.HistoryPrevious(string search)
        {
            UIThread(uiOnly => uiOnly.HistoryPrevious(search));
        }

        void IInteractiveWindowOperations.HistoryNext(string search)
        {
            UIThread(uiOnly => uiOnly.HistoryNext(search));
        }

        void IInteractiveWindowOperations.HistorySearchNext()
        {
            UIThread(uiOnly => uiOnly.HistorySearchNext());
        }

        void IInteractiveWindowOperations.HistorySearchPrevious()
        {
            UIThread(uiOnly => uiOnly.HistorySearchPrevious());
        }

        /// <summary>
        /// Moves to the beginning of the line.
        /// </summary>
        void IInteractiveWindowOperations.Home(bool extendSelection)
        {
            UIThread(uiOnly => uiOnly.Home(extendSelection));
        }

        /// <summary>
        /// Moves to the end of the line.
        /// </summary>
        void IInteractiveWindowOperations.End(bool extendSelection)
        {
            UIThread(uiOnly => uiOnly.End(extendSelection));
        }

        void IInteractiveWindowOperations.SelectAll()
        {
            UIThread(uiOnly => uiOnly.SelectAll());
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
                var sourceSpans = GetSourceSpans(caretPosition.Snapshot);
                var promptIndex = GetPromptIndexForPoint(sourceSpans, caretPosition);
                var promptSpan = sourceSpans[promptIndex];
                Debug.Assert(IsPrompt(promptSpan));
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

        private SnapshotPoint? GetPositionInStandardInputBuffer(SnapshotPoint point)
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

        #endregion

        #region Keyboard Commands

        /// <remarks>Only consistent on the UI thread.</remarks>
        bool IInteractiveWindow.IsRunning => _dangerous_uiOnly.State != State.WaitingForInput;

        /// <remarks>Only consistent on the UI thread.</remarks>
        bool IInteractiveWindow.IsResetting => _dangerous_uiOnly.State == State.Resetting || _dangerous_uiOnly.State == State.ResettingAndReadingStandardInput;

        /// <remarks>Only consistent on the UI thread.</remarks>
        bool IInteractiveWindow.IsInitializing => _dangerous_uiOnly.State == State.Starting || _dangerous_uiOnly.State == State.Initializing;

        IInteractiveWindowOperations IInteractiveWindow.Operations => this;

        bool IInteractiveWindowOperations.Delete()
        {
            return UIThread(uiOnly => uiOnly.Delete());
        }

        void IInteractiveWindowOperations.Cut()
        {
            UIThread(uiOnly => uiOnly.Cut());
        }

        void IInteractiveWindowOperations2.Copy()
        {
            UIThread(uiOnly => uiOnly.Copy());
        }

        bool IInteractiveWindowOperations.Backspace()
        {
            return UIThread(uiOnly => uiOnly.Backspace());
        }

        bool IInteractiveWindowOperations.TrySubmitStandardInput()
        {
            return UIThread(uiOnly => uiOnly.TrySubmitStandardInput());
        }

        bool IInteractiveWindowOperations.BreakLine()
        {
            return UIThread(uiOnly => uiOnly.BreakLine());
        }

        bool IInteractiveWindowOperations.Return()
        {
            return UIThread(uiOnly => uiOnly.Return());
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
                _textView.Caret.PositionChanged -= CaretPositionChanged;

                IndentCurrentLine(caret);
            }
            finally
            {
                // attach event handler
                _textView.Caret.PositionChanged += CaretPositionChanged;
            }
        }

        #endregion

        #region Active Code and Standard Input

        TextReader IInteractiveWindow.ReadStandardInput()
        {
            // shouldn't be called on the UI thread because we'll hang
            RequiresNonUIThread();
            return ReadStandardInputAsync().GetAwaiter().GetResult();
        }

        private async Task<TextReader> ReadStandardInputAsync()
        {
            try
            {
                // True because this is a public API and we want to use the same
                // thread as the caller (esp for blocking).
                await _inputReaderSemaphore.WaitAsync().ConfigureAwait(true); // Only one thread can read from standard input at a time.
                try
                {
                    return await UIThread(uiOnly => uiOnly.ReadStandardInputAsync()).ConfigureAwait(true);
                }
                finally
                {
                    _inputReaderSemaphore.Release();
                }
            }
            catch (Exception e) when (ReportAndPropagateException(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
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
            _textView.Caret.EnsureVisible();
        }

#endregion

#region Buffers, Spans and Prompts
        private object CreateStandardInputPrompt()
        {
            return string.Empty;
        }

        private object CreatePrimaryPrompt()
        {
            return _evaluator.GetPrompt();
        }

        private object CreateSecondaryPrompt()
        {
            // TODO (crwilcox) format prompt used to get a blank here but now gets "> " from get prompt.
            return _evaluator.GetPrompt();
        }

        private ReplSpanKind GetSpanKind(SnapshotSpan span)
        {
            var textBuffer = span.Snapshot.TextBuffer;
            if (textBuffer == _outputBuffer)
            {
                return ReplSpanKind.Output;
            }
            if (textBuffer == _standardInputBuffer)
            {
                return ReplSpanKind.StandardInput;
            }
            if (textBuffer.ContentType == _inertType)
            {
                return (span.Length == _lineBreakString.Length) && string.Equals(span.GetText(), _lineBreakString) ?
                    ReplSpanKind.LineBreak :
                    ReplSpanKind.Prompt;
            }
            return ReplSpanKind.Language;
        }

        private bool IsPrompt(SnapshotSpan span)
        {
            return GetSpanKind(span) == ReplSpanKind.Prompt;
        }

        private static ReadOnlyCollection<SnapshotSpan> GetSourceSpans(ITextSnapshot snapshot)
        {
            return ((IProjectionSnapshot)snapshot).GetSourceSpans();
        }

        private int GetPromptIndexForPoint(ReadOnlyCollection<SnapshotSpan> sourceSpans, SnapshotPoint point)
        {
            int index = GetSourceSpanIndex(sourceSpans, point);
            if (index == sourceSpans.Count)
            {
                index--;
            }
            // Find the nearest preceding prompt.
            while (!IsPrompt(sourceSpans[index]))
            {
                index--;
            }
            return index;
        }

        /// <summary>
        /// Return the index of the span containing the point. Returns the
        /// length of the collection if the point is at the end of the last span.
        /// </summary>
        private int GetSourceSpanIndex(ReadOnlyCollection<SnapshotSpan> sourceSpans, SnapshotPoint point)
        {
            int low = 0;
            int high = sourceSpans.Count;
            while (low < high)
            {
                int mid = low + (high - low) / 2;
                int value = CompareToSpan(_textView, sourceSpans, mid, point);
                if (value == 0)
                {
                    return mid;
                }
                else if (value < 0)
                {
                    high = mid - 1;
                }
                else
                {
                    low = mid + 1;
                }
            }
            Debug.Assert(low >= 0);
            Debug.Assert(low <= sourceSpans.Count);
            return low;
        }

        /// <summary>
        /// Returns negative value if the point is less than the span start,
        /// positive if greater than or equal to the span end, and 0 otherwise.
        /// </summary>
        private static int CompareToSpan(ITextView textView, ReadOnlyCollection<SnapshotSpan> sourceSpans, int index, SnapshotPoint point)
        {
            // If this span is zero-width and there are multiple projections of the
            // containing snapshot in the projection buffer, MapUpToBuffer will return
            // multiple (ambiguous) projection spans. To avoid that, we compare the
            // point to the end point of the nearest non-zero width span instead.
            int indexToCompare = index;
            while (sourceSpans[indexToCompare].IsEmpty)
            {
                if (indexToCompare == 0)
                {
                    // Empty span at start of buffer. Point
                    // must be to the right of span.
                    return 1;
                }
                indexToCompare--;
            }

            var sourceSpan = sourceSpans[indexToCompare];
            Debug.Assert(sourceSpan.Length > 0);

            var mappedSpans = textView.BufferGraph.MapUpToBuffer(sourceSpan, SpanTrackingMode.EdgeInclusive, textView.TextBuffer);
            Debug.Assert(mappedSpans.Count == 1);

            var mappedSpan = mappedSpans[0];
            Debug.Assert(mappedSpan.Length == sourceSpan.Length);

            if (indexToCompare < index)
            {
                var result = point.CompareTo(mappedSpan.End);
                return (result == 0) ? 1 : result;
            }
            else
            {
                var result = point.CompareTo(mappedSpan.Start);
                if (result <= 0)
                {
                    return result;
                }
                result = point.CompareTo(mappedSpan.End);
                return (result < 0) ? 0 : 1;
            }
        }

        private const int SpansPerLineOfInput = 2;

        private static readonly object s_suppressPromptInjectionTag = new object();

        private bool TryGetCurrentLanguageBufferExtent(IProjectionSnapshot projectionSnapshot, out Span result)
        {
            if (projectionSnapshot.SpanCount == 0)
            {
                result = default(Span);
                return false;
            }

            // the last source snapshot is always a projection of a language buffer:
            var snapshot = projectionSnapshot.GetSourceSpan(projectionSnapshot.SpanCount - 1).Snapshot;
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
            var oldProjectionSpans = oldSnapshot.GetSourceSpans();
            int oldProjectionSpanCount = oldProjectionSpans.Count;

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
                var newSpans = new object[lineBreakCount * SpansPerLineOfInput + 1];

                var subjectLine = newSubjectStartLine;
                while (true)
                {
                    if (subjectLine.LineNumber != newSubjectStartLine.LineNumber)
                    {
                        // TODO (crwilcox): do we need two prompts?  Can I tell it to not do this?  Or perhaps we do want this since we want different markings?
                        newSpans[i++] = CreateSecondaryPrompt();
                    }

                    newSpans[i++] = CreateLanguageSpanForLine(subjectLine);

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
                ReplaceProjectionSpans(oldProjectionSpans, spanEdits);
            }

            CheckProjectionSpans();
        }

        /// <remarks>
        /// This should only be called from within the current language buffer.  If there are
        /// any output or standard input buffers between the specified line and the end of the
        /// surface buffer, then the result will be incorrect.
        /// </remarks>
        private int GetProjectionSpanIndexFromEditableBufferPosition(IProjectionSnapshot surfaceSnapshot, int projectionSpansCount, int surfaceLineNumber)
        {
            // The current language buffer is projected to a set of projections interleaved regularly by prompt projections 
            // and ending at the end of the projection buffer, each language buffer projection is on a separate line:
            //   [prompt)[language)...[prompt)[language)<end of projection buffer>
            int result = projectionSpansCount - (surfaceSnapshot.LineCount - surfaceLineNumber) * SpansPerLineOfInput + 1;
            Debug.Assert(GetSpanKind(surfaceSnapshot.GetSourceSpan(result)) == ReplSpanKind.Language);
            return result;
        }

        private void ReplaceProjectionSpans(ReadOnlyCollection<SnapshotSpan> oldProjectionSpans, List<SpanRangeEdit> spanEdits)
        {
            Debug.Assert(spanEdits.Count > 0);

            int start = spanEdits[0].Start;
            int end = spanEdits[spanEdits.Count - 1].End;

            var replacement = new List<object>();
            replacement.AddRange(spanEdits[0].Replacement);
            int lastEnd = spanEdits[0].End;

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
                    replacement.AddRange(oldProjectionSpans.Skip(lastEnd).Take(gap).Select(CreateTrackingSpan));
                    replacement.AddRange(edit.Replacement);
                }

                lastEnd = edit.End;
            }

            _projectionBuffer.ReplaceSpans(start, end - start, replacement, EditOptions.None, s_suppressPromptInjectionTag);
        }

        private object CreateTrackingSpan(SnapshotSpan snapshotSpan)
        {
            var snapshot = snapshotSpan.Snapshot;
            if (snapshot.ContentType == _inertType)
            {
                return snapshotSpan.GetText();
            }
            return new CustomTrackingSpan(snapshot, snapshotSpan.Span, PointTrackingMode.Negative, PointTrackingMode.Negative);
        }

        private ITrackingSpan CreateLanguageSpanForLine(ITextSnapshotLine languageLine)
        {
            var span = languageLine.ExtentIncludingLineBreak;
            bool lastLine = (languageLine.LineNumber == languageLine.Snapshot.LineCount - 1);
            return new CustomTrackingSpan(
                _currentLanguageBuffer.CurrentSnapshot,
                span,
                PointTrackingMode.Negative,
                lastLine ? PointTrackingMode.Positive : PointTrackingMode.Negative);
        }

        // Verify spans and GetSourceSpanIndex.
        [Conditional("DEBUG")]
        private void CheckProjectionSpans()
        {
            var snapshot = _projectionBuffer.CurrentSnapshot;
            var sourceSpans = snapshot.GetSourceSpans();
            int n = sourceSpans.Count;

            // Spans should be contiguous and span the entire buffer.
            int offset = 0;
            for (int i = 0; i < n; i++)
            {
                // Determine the index of the first non-zero width
                // span starting at the same point as current span.
                int expectedIndex = i;
                while (sourceSpans[expectedIndex].IsEmpty)
                {
                    expectedIndex++;
                    if (expectedIndex == n)
                    {
                        break;
                    }
                }
                // Verify GetSourceSpanIndex returns the expected
                // index for the start of the span.
                int index = GetSourceSpanIndex(sourceSpans, new SnapshotPoint(snapshot, offset));
                Debug.Assert(index == expectedIndex);
                // If this is a non-empty span, verify GetSourceSpanIndex
                // returns the index for the midpoint of the span.
                int length = sourceSpans[i].Length;
                if (length > 0)
                {
                    index = GetSourceSpanIndex(sourceSpans, new SnapshotPoint(snapshot, offset + length / 2));
                    Debug.Assert(index == i);
                }
                offset += length;
            }

            Debug.Assert(offset == snapshot.Length);

            if (n > 0)
            {
                int index = GetSourceSpanIndex(sourceSpans, new SnapshotPoint(snapshot, snapshot.Length));
                Debug.Assert(index == n);
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

        internal event Action<State> StateChanged;

#endregion
    }
}
