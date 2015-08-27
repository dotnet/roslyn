// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// Provides implementation of a Repl Window built on top of the VS editor using projection buffers.
    /// </summary>
    internal partial class InteractiveWindow
    {
        private readonly UIThreadOnly _dangerous_uiOnly;

        #region Initialization

        public InteractiveWindow(
            IInteractiveWindowEditorFactoryService host,
            IContentTypeRegistryService contentTypeRegistry,
            ITextBufferFactoryService bufferFactory,
            IProjectionBufferFactoryService projectionBufferFactory,
            IEditorOperationsFactoryService editorOperationsFactory,
            ITextEditorFactoryService editorFactory,
            IRtfBuilderService rtfBuilderService,
            IIntellisenseSessionStackMapService intellisenseSessionStackMap,
            ISmartIndentationService smartIndenterService,
            IInteractiveEvaluator evaluator)
        {
            if (evaluator == null)
            {
                throw new ArgumentNullException(nameof(evaluator));
            }

            _dangerous_uiOnly = new UIThreadOnly(this, host, rtfBuilderService);

            this.Properties = new PropertyCollection();

            _intellisenseSessionStackMap = intellisenseSessionStackMap;
            _smartIndenterService = smartIndenterService;

            var replContentType = contentTypeRegistry.GetContentType(PredefinedInteractiveContentTypes.InteractiveContentTypeName);
            var replOutputContentType = contentTypeRegistry.GetContentType(PredefinedInteractiveContentTypes.InteractiveOutputContentTypeName);

            _outputBuffer = bufferFactory.CreateTextBuffer(replOutputContentType);
            _standardInputBuffer = bufferFactory.CreateTextBuffer();
            _inertType = bufferFactory.InertContentType;

            var projBuffer = projectionBufferFactory.CreateProjectionBuffer(
                new EditResolver(this),
                Array.Empty<object>(),
                ProjectionBufferOptions.None,
                replContentType);

            projBuffer.Properties.AddProperty(typeof(InteractiveWindow), this);

            _projectionBuffer = projBuffer;
            _dangerous_uiOnly.AppendNewOutputProjectionBuffer(); // Constructor runs on UI thread.
            projBuffer.Changed += new EventHandler<TextContentChangedEventArgs>(ProjectionBufferChanged);

            var roleSet = editorFactory.CreateTextViewRoleSet(
                PredefinedTextViewRoles.Analyzable,
                PredefinedTextViewRoles.Editable,
                PredefinedTextViewRoles.Interactive,
                PredefinedTextViewRoles.Zoomable,
                PredefinedInteractiveTextViewRoles.InteractiveTextViewRole);

            _textView = host.CreateTextView(this, projBuffer, roleSet);

            _textView.Caret.PositionChanged += CaretPositionChanged;

            _textView.Options.SetOptionValue(DefaultTextViewHostOptions.HorizontalScrollBarId, true);
            _textView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, false);
            _textView.Options.SetOptionValue(DefaultTextViewHostOptions.OutliningMarginId, false);
            _textView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, false);
            _textView.Options.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, WordWrapStyles.None);

            _lineBreakString = _textView.Options.GetNewLineCharacter();
            _dangerous_uiOnly.EditorOperations = editorOperationsFactory.GetEditorOperations(_textView); // Constructor runs on UI thread.

            _buffer = new OutputBuffer(this);
            _outputWriter = new InteractiveWindowWriter(this, spans: null);

            SortedSpans errorSpans = new SortedSpans();
            _errorOutputWriter = new InteractiveWindowWriter(this, errorSpans);
            OutputClassifierProvider.AttachToBuffer(_outputBuffer, errorSpans);

            RequiresUIThread();
            evaluator.CurrentWindow = this;
            _evaluator = evaluator;
        }

        async Task<ExecutionResult> IInteractiveWindow.InitializeAsync()
        {
            try
            {
                RequiresUIThread();
                var uiOnly = _dangerous_uiOnly; // Verified above.

                if (uiOnly.State != State.Starting)
                {
                    throw new InvalidOperationException(InteractiveWindowResources.AlreadyInitialized);
                }

                uiOnly.State = State.Initializing;

                // Anything that reads options should wait until after this call so the evaluator can set the options first
                ExecutionResult result = await _evaluator.InitializeAsync().ConfigureAwait(continueOnCapturedContext: true);

                Debug.Assert(OnUIThread()); // ConfigureAwait should bring us back to the UI thread.

                if (result.IsSuccessful)
                {
                    uiOnly.PrepareForInput();
                }

                return result;
            }
            catch (Exception e) when (ReportAndPropagateException(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private bool ReportAndPropagateException(Exception e)
        {
            FatalError.ReportWithoutCrashUnlessCanceled(e); // Drop return value.

            ((IInteractiveWindow)this).WriteErrorLine(InteractiveWindowResources.InternalError);

            return false; // Never consider the exception handled.
        }

        #endregion

        private sealed class UIThreadOnly
        {
            private readonly InteractiveWindow _window;

            private readonly IInteractiveWindowEditorFactoryService _host;

            private readonly IReadOnlyRegion[] _outputProtection;

            public readonly History History;
            private string _historySearch;

            // Pending submissions to be processed whenever the REPL is ready to accept submissions.
            private readonly Queue<PendingSubmission> _pendingSubmissions;

            private DispatcherTimer _executionTimer;
            private Cursor _oldCursor;
            private int _currentOutputProjectionSpan;
            private int _outputTrackingCaretPosition;

            private readonly IRtfBuilderService _rtfBuilderService;

            // Read-only regions protecting initial span of the corresponding buffers:
            public readonly IReadOnlyRegion[] StandardInputProtection;

            public string UncommittedInput;

            private IEditorOperations _editorOperations;
            public IEditorOperations EditorOperations
            {
                get
                {
                    return _editorOperations;
                }
                set
                {
                    Debug.Assert(_editorOperations == null, "Assignment only happens once.");
                    Debug.Assert(value != null);
                    _editorOperations = value;
                }
            }


            public State _state;
            public State State
            {
                get
                {
                    return _state;
                }
                set
                {
                    _window.StateChanged?.Invoke(value);
                    _state = value;
                }
            }

            public UIThreadOnly(InteractiveWindow window, IInteractiveWindowEditorFactoryService host, IRtfBuilderService rtfBuilderService)
            {
                _window = window;
                _host = host;
                _rtfBuilderService = rtfBuilderService;
                History = new History();
                StandardInputProtection = new IReadOnlyRegion[2];
                _outputProtection = new IReadOnlyRegion[2];
                _pendingSubmissions = new Queue<PendingSubmission>();
                _outputTrackingCaretPosition = -1;
            }

            public async Task<ExecutionResult> ResetAsync(bool initialize)
            {
                try
                {
                    Debug.Assert(State != State.Resetting, "The button should have been disabled.");

                    if (_window._stdInputStart != null)
                    {
                        CancelStandardInput();
                    }

                    _window._buffer.Flush();

                    if (State == State.WaitingForInput)
                    {
                        var snapshot = _window._projectionBuffer.CurrentSnapshot;
                        var spanCount = snapshot.SpanCount;
                        Debug.Assert(_window.GetSpanKind(snapshot.GetSourceSpan(spanCount - 1)) == ReplSpanKind.Language);
                        StoreUncommittedInput();
                        RemoveProjectionSpans(spanCount - 2, 2);
                        _window._currentLanguageBuffer = null;
                    }

                    State = State.Resetting;
                    var executionResult = await _window._evaluator.ResetAsync(initialize).ConfigureAwait(true);
                    Debug.Assert(_window.OnUIThread()); // ConfigureAwait should bring us back to the UI thread.

                    Debug.Assert(State == State.Resetting, $"Unexpected state {State}");
                    FinishExecute(executionResult.IsSuccessful);

                    return executionResult;
                }
                catch (Exception e) when (_window.ReportAndPropagateException(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            public void ClearView()
            {
                if (_window._stdInputStart != null)
                {
                    CancelStandardInput();
                }

                _window._adornmentToMinimize = false;
                InlineAdornmentProvider.RemoveAllAdornments(_window._textView);

                // remove all the spans except our initial span from the projection buffer
                UncommittedInput = null;

                // Clear the projection and buffers last as this might trigger events that might access other state of the REPL window:
                RemoveProtection(_window._outputBuffer, _outputProtection);
                RemoveProtection(_window._standardInputBuffer, StandardInputProtection);

                using (var edit = _window._outputBuffer.CreateEdit(EditOptions.None, null, s_suppressPromptInjectionTag))
                {
                    edit.Delete(0, _window._outputBuffer.CurrentSnapshot.Length);
                    edit.Apply();
                }

                _window._buffer.Reset();
                OutputClassifierProvider.ClearSpans(_window._outputBuffer);
                _outputTrackingCaretPosition = 0;

                using (var edit = _window._standardInputBuffer.CreateEdit(EditOptions.None, null, s_suppressPromptInjectionTag))
                {
                    edit.Delete(0, _window._standardInputBuffer.CurrentSnapshot.Length);
                    edit.Apply();
                }

                RemoveProjectionSpans(0, _window._projectionBuffer.CurrentSnapshot.SpanCount);

                // Insert an empty output buffer.
                // We do it for two reasons: 
                // 1) When output is written to asynchronously we need a buffer to store it.
                //    This may happen when clearing screen while background thread is writing to the console.
                // 2) We need at least one non-inert span due to bugs in projection buffer.
                AppendNewOutputProjectionBuffer();

                History.ForgetOriginalBuffers();

                // If we were waiting for input, we need to restore the prompt that we just cleared.
                // If we are in any other state, then we'll let normal transitions trigger the next prompt.
                if (State == State.WaitingForInput)
                {
                    PrepareForInput();
                }
            }

            private void CancelStandardInput()
            {
                AppendLineNoPromptInjection(_window._standardInputBuffer);
                _window._inputValue = null;
                _window._inputEvent.Set();
            }

            private void AppendLineNoPromptInjection(ITextBuffer buffer)
            {
                using (var edit = buffer.CreateEdit(EditOptions.None, null, s_suppressPromptInjectionTag))
                {
                    edit.Insert(buffer.CurrentSnapshot.Length, _window._lineBreakString);
                    edit.Apply();
                }
            }

            public void InsertCode(string text)
            {
                if (_window._stdInputStart != null)
                {
                    return;
                }

                if (State == State.ExecutingInput)
                {
                    AppendUncommittedInput(text);
                }
                else
                {
                    if (!_window._textView.Selection.IsEmpty)
                    {
                        CutOrDeleteSelection(isCut: false);
                    }

                    EditorOperations.InsertText(text);
                }
            }

            public void Submit(PendingSubmission[] pendingSubmissions)
            {
                if (_window._stdInputStart == null)
                {
                    if (State == State.WaitingForInput && _window._currentLanguageBuffer != null)
                    {
                        StoreUncommittedInput();
                        PendSubmissions(pendingSubmissions);
                        ProcessPendingSubmissions();
                    }
                    else
                    {
                        PendSubmissions(pendingSubmissions);
                    }
                }
            }

            private void StoreUncommittedInput()
            {
                if (UncommittedInput == null)
                {
                    string activeCode = GetActiveCode();
                    if (!string.IsNullOrEmpty(activeCode))
                    {
                        UncommittedInput = activeCode;
                    }
                }
            }

            /// <summary>
            /// Returns the full text of the current active input.
            /// </summary>
            private string GetActiveCode()
            {
                return _window._currentLanguageBuffer.CurrentSnapshot.GetText();
            }

            private void PendSubmissions(IEnumerable<PendingSubmission> inputs)
            {
                foreach (var input in inputs)
                {
                    _pendingSubmissions.Enqueue(input);
                }
            }

            public void AddInput(string command)
            {
                // If the language buffer is readonly then input can not be added. Return immediately.
                // The language buffer gets marked as readonly in SubmitAsync method when input on the prompt 
                // gets submitted. So it would be readonly when the user types #reset on the prompt. In that 
                // case it is the right thing to bail out of this method.
                if (_window._currentLanguageBuffer != null && _window._currentLanguageBuffer.IsReadOnly(0))
                {
                    return;
                }

                if (State == State.ExecutingInput || _window._currentLanguageBuffer == null)
                {
                    AddLanguageBuffer();
                    _window._currentLanguageBuffer.Insert(0, command);
                }
                else
                {
                    StoreUncommittedInput();
                    SetActiveCode(command);
                }

                // Add command to history before calling FinishCurrentSubmissionInput as it adds newline 
                // to the end of the command.
                History.Add(_window._currentLanguageBuffer.CurrentSnapshot.GetExtent());
                FinishCurrentSubmissionInput();
            }

            private void AppendUncommittedInput(string text)
            {
                if (string.IsNullOrEmpty(text))
                {
                    // Do nothing.
                }
                else if (string.IsNullOrEmpty(UncommittedInput))
                {
                    UncommittedInput = text;
                }
                else
                {
                    UncommittedInput += text;
                }
            }

            private void RestoreUncommittedInput()
            {
                if (UncommittedInput != null)
                {
                    SetActiveCode(UncommittedInput);
                    UncommittedInput = null;
                }
            }

            /// <summary>
            /// Pastes from the clipboard into the text view
            /// </summary>
            public bool Paste()
            {
                MoveCaretToClosestEditableBuffer();

                string format = _window._evaluator.FormatClipboard();
                if (format != null)
                {
                    InsertCode(format);
                }
                else if (Clipboard.ContainsText())
                {
                    InsertCode(Clipboard.GetText());
                }
                else
                {
                    return false;
                }

                return true;
            }

            private void MoveCaretToClosestEditableBuffer()
            {
                SnapshotPoint currentPosition = _window._textView.Caret.Position.BufferPosition;
                SnapshotPoint newPosition = GetClosestEditablePoint(currentPosition);
                if (currentPosition != newPosition)
                {
                    _window._textView.Caret.MoveTo(newPosition);
                }
            }

            /// <summary>
            /// Finds a point in an editable buffer that is the closest towards the end to the given projection point.
            /// </summary>
            private SnapshotPoint GetClosestEditablePoint(SnapshotPoint projectionPoint)
            {
                ITextBuffer editableBuffer = (_window._stdInputStart != null) ? _window._standardInputBuffer : _window._currentLanguageBuffer;

                if (editableBuffer == null)
                {
                    return new SnapshotPoint(_window._projectionBuffer.CurrentSnapshot, _window._projectionBuffer.CurrentSnapshot.Length);
                }

                SnapshotPoint? point = _window.GetPositionInBuffer(projectionPoint, editableBuffer);
                if (point != null)
                {
                    return projectionPoint;
                }

                var projectionLine = projectionPoint.GetContainingLine();

                SnapshotPoint? lineEnd = _window._textView.BufferGraph.MapDownToBuffer(
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

                return _window._textView.BufferGraph.MapUpToBuffer(
                    editablePoint,
                    PointTrackingMode.Positive,
                    PositionAffinity.Successor,
                    _window._projectionBuffer).Value;
            }

            /// <summary>
            /// Appends given text to the last input span (standard input or active code input).
            /// </summary>
            private void AppendInput(string text)
            {
                var snapshot = _window._projectionBuffer.CurrentSnapshot;
                var spanCount = snapshot.SpanCount;
                var inputSpan = snapshot.GetSourceSpan(spanCount - 1);
                Debug.Assert(_window.GetSpanKind(inputSpan) == ReplSpanKind.Language ||
                    _window.GetSpanKind(inputSpan) == ReplSpanKind.StandardInput);

                var buffer = inputSpan.Snapshot.TextBuffer;
                var span = inputSpan.Span;
                using (var edit = buffer.CreateEdit())
                {
                    edit.Insert(edit.Snapshot.Length, text);
                    edit.Apply();
                }

                var replSpan = new CustomTrackingSpan(
                    buffer.CurrentSnapshot,
                    new Span(span.Start, span.Length + text.Length),
                    PointTrackingMode.Negative,
                    PointTrackingMode.Positive);
                ReplaceProjectionSpan(spanCount - 1, replSpan);

                _window.Caret.EnsureVisible();
            }

            public void PrepareForInput()
            {
                _window._buffer.Flush();

                AddLanguageBuffer();

                // we are prepared for processing any postponed submissions there might have been:
                ProcessPendingSubmissions();
            }

            private void ProcessPendingSubmissions()
            {
                Debug.Assert(_window._currentLanguageBuffer != null);

                if (_pendingSubmissions.Count == 0)
                {
                    RestoreUncommittedInput();

                    // move to the end (it might have been in virtual space):
                    _window.Caret.MoveTo(GetLastLine(_window.TextBuffer.CurrentSnapshot).End);
                    _window.Caret.EnsureVisible();

                    State = State.WaitingForInput;

                    var ready = _window.ReadyForInput;
                    if (ready != null)
                    {
                        ready();
                    }

                    return;
                }

                var submission = _pendingSubmissions.Dequeue();
                SetActiveCode(submission.Input);
                Debug.Assert(submission.Task == null, "Someone set PendingSubmission.Task before it was dequeued.");
                submission.Task = SubmitAsync();
                if (submission.Completion != null)
                {
                    // ContinueWith is safe since TaskCompletionSource.SetResult should not throw.
                    // Therefore, we don't need to await the task (which we would normally do to
                    // propagate any exceptions it might throw).  We also don't need an NFW
                    // exception filter around the continuation.
                    submission.Task.ContinueWith(_ => submission.Completion.SetResult(null), TaskScheduler.Current);
                }
            }

            #region Editor Helpers

            private static ITextSnapshotLine GetLastLine(ITextSnapshot snapshot)
            {
                return snapshot.GetLineFromLineNumber(snapshot.LineCount - 1);
            }

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

            #endregion

            public async Task SubmitAsync()
            {
                try
                {
                    RequiresLanguageBuffer();

                    // TODO: queue submission
                    // Ensure that the REPL doesn't try to execute if it is already
                    // executing.  If this invariant can no longer be maintained more of
                    // the code in this method will need to be bullet-proofed
                    if (State == State.ExecutingInput)
                    {
                        return;
                    }

                    // get command to save to history before calling FinishCurrentSubmissionInput
                    // as it adds newline at the end
                    var historySpan = _window._currentLanguageBuffer.CurrentSnapshot.GetExtent();
                    FinishCurrentSubmissionInput();

                    History.UncommittedInput = null;

                    var snapshotSpan = _window._currentLanguageBuffer.CurrentSnapshot.GetExtent();
                    var trimmedSpan = snapshotSpan.TrimEnd();

                    if (trimmedSpan.Length == 0)
                    {
                        // TODO: reuse the current language buffer
                        PrepareForInput();
                        return;
                    }
                    else
                    {
                        History.Add(historySpan);
                        State = State.ExecutingInput;

                        StartCursorTimer();

                        var executionResult = await _window._evaluator.ExecuteCodeAsync(snapshotSpan.GetText()).ConfigureAwait(true);
                        Debug.Assert(_window.OnUIThread()); // ConfigureAwait should bring us back to the UI thread.

                        // For reset command typed at prompt, the state should be WaitingForInput 
                        // and for all other submissions it should be Executing input
                        Debug.Assert(State == State.ExecutingInput || State == State.WaitingForInput, $"Unexpected state {State}");

                        if (State == State.ExecutingInput)
                        {
                            FinishExecute(executionResult.IsSuccessful);
                        }
                    }
                }
                catch (Exception e) when (_window.ReportAndPropagateException(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private void RequiresLanguageBuffer()
            {
                if (_window._currentLanguageBuffer == null)
                {
                    Environment.FailFast("Language buffer not available");
                }
            }

            private void FinishCurrentSubmissionInput()
            {
                AppendLineNoPromptInjection(_window._currentLanguageBuffer);
                ApplyProtection(_window._currentLanguageBuffer, regions: null);

                if (_window._adornmentToMinimize)
                {
                    // TODO (tomat): remember the index of the adornment(s) in the current output and minimize those instead of the last one 
                    InlineAdornmentProvider.MinimizeLastInlineAdornment(_window._textView);
                    _window._adornmentToMinimize = false;
                }

                NewOutputBuffer();
            }

            /// <summary>
            /// Marks the entire buffer as read-only.
            /// </summary>
            public void ApplyProtection(ITextBuffer buffer, IReadOnlyRegion[] regions, bool allowAppend = false)
            {
                using (var readonlyEdit = buffer.CreateReadOnlyRegionEdit())
                {
                    int end = buffer.CurrentSnapshot.Length;
                    Span span = new Span(0, end);

                    var region0 = allowAppend ?
                        readonlyEdit.CreateReadOnlyRegion(span, SpanTrackingMode.EdgeExclusive, EdgeInsertionMode.Allow) :
                        readonlyEdit.CreateReadOnlyRegion(span, SpanTrackingMode.EdgeExclusive, EdgeInsertionMode.Deny);

                    // Create a second read-only region to prevent insert at start of buffer.
                    var region1 = (end > 0) ? readonlyEdit.CreateReadOnlyRegion(new Span(0, 0), SpanTrackingMode.EdgeExclusive, EdgeInsertionMode.Deny) : null;

                    readonlyEdit.Apply();

                    if (regions != null)
                    {
                        regions[0] = region0;
                        regions[1] = region1;
                    }
                }
            }

            /// <summary>
            /// Removes read-only region from buffer.
            /// </summary>
            public void RemoveProtection(ITextBuffer buffer, IReadOnlyRegion[] regions)
            {
                if (regions[0] != null)
                {
                    Debug.Assert(regions[1] != null);

                    foreach (var region in regions)
                    {
                        using (var readonlyEdit = buffer.CreateReadOnlyRegionEdit())
                        {
                            readonlyEdit.RemoveReadOnlyRegion(region);
                            readonlyEdit.Apply();
                        }
                    }
                }
            }

            public void NewOutputBuffer()
            {
                // Stop growing the current output projection span.
                var sourceSpan = _window._projectionBuffer.CurrentSnapshot.GetSourceSpan(_currentOutputProjectionSpan);
                Debug.Assert(_window.GetSpanKind(sourceSpan) == ReplSpanKind.Output);
                var nonGrowingSpan = new CustomTrackingSpan(
                    sourceSpan.Snapshot,
                    sourceSpan.Span,
                    PointTrackingMode.Negative,
                    PointTrackingMode.Negative);
                ReplaceProjectionSpan(_currentOutputProjectionSpan, nonGrowingSpan);

                AppendNewOutputProjectionBuffer();
                _outputTrackingCaretPosition = _window._textView.Caret.Position.BufferPosition;
            }

            public void AppendNewOutputProjectionBuffer()
            {
                var currentSnapshot = _window._outputBuffer.CurrentSnapshot;
                var trackingSpan = new CustomTrackingSpan(
                    currentSnapshot,
                    new Span(currentSnapshot.Length, 0),
                    PointTrackingMode.Negative,
                    PointTrackingMode.Positive);

                _currentOutputProjectionSpan = AppendProjectionSpan(trackingSpan);
            }

            private int AppendProjectionSpan(ITrackingSpan span)
            {
                int index = _window._projectionBuffer.CurrentSnapshot.SpanCount;
                InsertProjectionSpan(index, span);
                return index;
            }

            private void InsertProjectionSpan(int index, object span)
            {
                _window._projectionBuffer.ReplaceSpans(index, 0, new[] { span }, EditOptions.None, editTag: s_suppressPromptInjectionTag);
            }

            public void ReplaceProjectionSpan(int spanToReplace, ITrackingSpan newSpan)
            {
                _window._projectionBuffer.ReplaceSpans(spanToReplace, 1, new[] { newSpan }, EditOptions.None, editTag: s_suppressPromptInjectionTag);
            }

            private void RemoveProjectionSpans(int index, int count)
            {
                _window._projectionBuffer.ReplaceSpans(index, count, Array.Empty<object>(), EditOptions.None, s_suppressPromptInjectionTag);
            }

            /// <summary>
            /// Appends text to the output buffer and updates projection buffer to include it.
            /// WARNING: this has to be the only method that writes to the output buffer so that 
            /// the output buffering counters are kept in sync.
            /// </summary>
            internal void AppendOutput(IEnumerable<string> output)
            {
                Debug.Assert(output.Any());

                // we maintain this invariant so that projections don't split "\r\n" in half: 
                Debug.Assert(!_window._outputBuffer.CurrentSnapshot.EndsWith('\r'));

                var projectionSpans = _window._projectionBuffer.CurrentSnapshot.GetSourceSpans();
                Debug.Assert(_window.GetSpanKind(projectionSpans[_currentOutputProjectionSpan]) == ReplSpanKind.Output);

                int lineBreakProjectionSpanIndex = _currentOutputProjectionSpan + 1;

                // insert line break projection span if there is none and the output doesn't end with a line break:
                bool hasLineBreakProjection = false;
                if (lineBreakProjectionSpanIndex < projectionSpans.Count)
                {
                    var oldSpan = projectionSpans[lineBreakProjectionSpanIndex];
                    hasLineBreakProjection = _window.GetSpanKind(oldSpan) == ReplSpanKind.LineBreak;
                }

                Debug.Assert(output.Last().Last() != '\r');
                bool endsWithLineBreak = output.Last().Last() == '\n';

                // insert text to the subject buffer.
                int oldBufferLength = _window._outputBuffer.CurrentSnapshot.Length;
                InsertOutput(output, oldBufferLength);

                if (endsWithLineBreak && hasLineBreakProjection)
                {
                    // Remove line break.
                    RemoveProjectionSpans(lineBreakProjectionSpanIndex, 1);
                }
                else if (!endsWithLineBreak && !hasLineBreakProjection)
                {
                    // Insert line break.
                    InsertProjectionSpan(lineBreakProjectionSpanIndex, _window._lineBreakString);
                }

                // caret didn't move since last time we moved it to track output:
                if (_outputTrackingCaretPosition == _window._textView.Caret.Position.BufferPosition)
                {
                    _window._textView.Caret.EnsureVisible();
                    _outputTrackingCaretPosition = _window._textView.Caret.Position.BufferPosition;
                }
            }

            private void InsertOutput(IEnumerable<string> output, int position)
            {
                RemoveProtection(_window._outputBuffer, _outputProtection);

                // append the text to output buffer and make sure it ends with a line break:
                using (var edit = _window._outputBuffer.CreateEdit(EditOptions.None, null, s_suppressPromptInjectionTag))
                {
                    foreach (string text in output)
                    {
                        edit.Insert(position, text);
                    }

                    edit.Apply();
                }

                ApplyProtection(_window._outputBuffer, _outputProtection);
            }

            private void FinishExecute(bool succeeded)
            {
                ResetCursor();

                if (!succeeded && History.Last != null)
                {
                    History.Last.Failed = true;
                }

                PrepareForInput();
            }

            public async Task ExecuteInputAsync()
            {
                try
                {
                    ITextBuffer languageBuffer = GetLanguageBuffer(_window.Caret.Position.BufferPosition);
                    if (languageBuffer == null)
                    {
                        return;
                    }

                    if (languageBuffer == _window._currentLanguageBuffer)
                    {
                        // TODO (tomat): this should rather send an abstract "finish" command that various features
                        // can implement as needed (IntelliSense, inline rename would commit, etc.).
                        // For now, commit IntelliSense:
                        var completionSession = _window.SessionStack.TopSession as ICompletionSession;
                        if (completionSession != null)
                        {
                            completionSession.Commit();
                        }

                        await SubmitAsync().ConfigureAwait(true);
                    }
                    else
                    {
                        // append text of the target buffer to the current language buffer:
                        string text = TrimTrailingEmptyLines(languageBuffer.CurrentSnapshot);
                        _window._currentLanguageBuffer.Replace(new Span(_window._currentLanguageBuffer.CurrentSnapshot.Length, 0), text);
                        EditorOperations.MoveToEndOfDocument(false);
                    }
                }
                catch (Exception e) when (_window.ReportAndPropagateException(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private static string TrimTrailingEmptyLines(ITextSnapshot snapshot)
            {
                var line = GetLastLine(snapshot);
                while (line != null && line.Length == 0)
                {
                    line = GetPreviousLine(line);
                }

                if (line == null)
                {
                    return string.Empty;
                }

                return line.Snapshot.GetText(0, line.Extent.End.Position);
            }

            private static ITextSnapshotLine GetPreviousLine(ITextSnapshotLine line)
            {
                return line.LineNumber > 0 ? line.Snapshot.GetLineFromLineNumber(line.LineNumber - 1) : null;
            }

            /// <summary>
            /// Returns the language or command text buffer that the specified point belongs to.
            /// If the point lays in a prompt returns the buffer corresponding to the prompt.
            /// </summary>
            /// <returns>The language or command buffer or null if the point doesn't belong to any.</returns>
            private ITextBuffer GetLanguageBuffer(SnapshotPoint point)
            {
                var sourceSpans = GetSourceSpans(point.Snapshot);
                int promptIndex = _window.GetPromptIndexForPoint(sourceSpans, point);
                if (promptIndex < 0)
                {
                    return null;
                }

                // Grab the span following the prompt (either language or standard input).
                var projectionSpan = sourceSpans[promptIndex + 1];
                var kind = _window.GetSpanKind(projectionSpan);
                if (kind != ReplSpanKind.Language)
                {
                    Debug.Assert(kind == ReplSpanKind.StandardInput);
                    return null;
                }

                var inputSnapshot = projectionSpan.Snapshot;
                var inputBuffer = inputSnapshot.TextBuffer;

                var projectedSpans = _window._textView.BufferGraph.MapUpToBuffer(
                    new SnapshotSpan(inputSnapshot, 0, inputSnapshot.Length),
                    SpanTrackingMode.EdgePositive,
                    _window._projectionBuffer);

                Debug.Assert(projectedSpans.Count > 0);
                var projectedSpansStart = projectedSpans.First().Start;
                var projectedSpansEnd = projectedSpans.Last().End;

                if (point < projectedSpansStart.GetContainingLine().Start)
                {
                    return null;
                }

                // If the buffer is the current buffer, the cursor might be in a virtual space behind the buffer
                // but logically it belongs to the current submission. Since the current language buffer is the last buffer in the 
                // projection we don't need to check for its end.
                if (inputBuffer == _window._currentLanguageBuffer)
                {
                    return inputBuffer;
                }

                // if the point is at the end of the buffer it might be on the next line that doesn't logically belong to the input region:
                if (point > projectedSpansEnd || (point == projectedSpansEnd && projectedSpansEnd.GetContainingLine().LineBreakLength != 0))
                {
                    return null;
                }

                return inputBuffer;
            }

            public void ResetCursor()
            {
                if (_executionTimer != null)
                {
                    _executionTimer.Stop();
                }

                if (_oldCursor != null)
                {
                    ((ContentControl)_window._textView).Cursor = _oldCursor;
                }

                _oldCursor = null;
                _executionTimer = null;
            }

            private void StartCursorTimer()
            {
                var timer = new DispatcherTimer();
                timer.Tick += SetRunningCursor;
                timer.Interval = TimeSpan.FromMilliseconds(250);
                _executionTimer = timer;
                timer.Start();
            }

            private void SetRunningCursor(object sender, EventArgs e)
            {
                var view = (ContentControl)_window._textView;

                // Save the old value of the cursor so it can be restored
                // after execution has finished
                _oldCursor = view.Cursor;

                // TODO: Design work to come up with the correct cursor to use
                // Set the repl's cursor to the "executing" cursor
                view.Cursor = Cursors.Wait;

                // Stop the timer so it doesn't fire again
                if (_executionTimer != null)
                {
                    _executionTimer.Stop();
                }
            }

            public int IndexOfLastStandardInputSpan(ReadOnlyCollection<SnapshotSpan> sourceSpans)
            {
                for (int i = sourceSpans.Count - 1; i >= 0; i--)
                {
                    if (_window.GetSpanKind(sourceSpans[i]) == ReplSpanKind.StandardInput)
                    {
                        return i;
                    }
                }

                return -1;
            }

            public void RemoveLastInputPrompt()
            {
                var snapshot = _window._projectionBuffer.CurrentSnapshot;
                var spanCount = snapshot.SpanCount;
                Debug.Assert(_window.IsPrompt(snapshot.GetSourceSpan(spanCount - SpansPerLineOfInput)));

                // projection buffer update must be the last operation as it might trigger event that accesses prompt line mapping:
                RemoveProjectionSpans(spanCount - SpansPerLineOfInput, SpansPerLineOfInput);
            }

            /// <summary>
            /// Creates and adds a new language buffer to the projection buffer.
            /// </summary>
            private void AddLanguageBuffer()
            {
                ITextBuffer buffer = _host.CreateAndActivateBuffer(_window);

                buffer.Properties.AddProperty(typeof(IInteractiveEvaluator), _window._evaluator);
                buffer.Properties.AddProperty(typeof(InteractiveWindow), _window);

                _window._currentLanguageBuffer = buffer;
                var bufferAdded = _window.SubmissionBufferAdded;
                if (bufferAdded != null)
                {
                    bufferAdded(_window, new SubmissionBufferAddedEventArgs(buffer));
                }

                // add the whole buffer to the projection buffer and set it up to expand to the right as text is appended
                var promptSpan = _window.CreatePrimaryPrompt();
                var languageSpan = new CustomTrackingSpan(
                    _window._currentLanguageBuffer.CurrentSnapshot,
                    new Span(0, 0),
                    PointTrackingMode.Negative,
                    PointTrackingMode.Positive);

                // projection buffer update must be the last operation as it might trigger event that accesses prompt line mapping:
                AppendProjectionSpans(promptSpan, languageSpan);
            }

            private void AppendProjectionSpans(object span1, object span2)
            {
                int index = _window._projectionBuffer.CurrentSnapshot.SpanCount;
                _window._projectionBuffer.ReplaceSpans(index, 0, new[] { span1, span2 }, EditOptions.None, editTag: s_suppressPromptInjectionTag);
            }

            public void ScrollToCaret()
            {
                var textView = _window._textView;
                var caretPosition = textView.Caret.Position.BufferPosition;
                var caretSpan = new SnapshotSpan(caretPosition.Snapshot, caretPosition, 0);
                textView.ViewScroller.EnsureSpanVisible(caretSpan);
            }

            public void Cancel()
            {
                ClearInput();
                EditorOperations.MoveToEndOfDocument(false);
                UncommittedInput = null;
                _historySearch = null;
            }

            private void ClearInput()
            {
                if (_window._stdInputStart != null)
                {
                    _window._standardInputBuffer.Delete(Span.FromBounds(_window._stdInputStart.Value, _window._standardInputBuffer.CurrentSnapshot.Length));
                }
                else
                {
                    _window._currentLanguageBuffer.Delete(new Span(0, _window._currentLanguageBuffer.CurrentSnapshot.Length));
                }
            }

            public void HistoryPrevious(string search)
            {
                if (_window._currentLanguageBuffer == null)
                {
                    return;
                }

                var previous = History.GetPrevious(search);
                if (previous != null)
                {
                    if (string.IsNullOrWhiteSpace(search))
                    {
                        // don't store search as an uncommitted history item
                        StoreUncommittedInputForHistory();
                    }

                    SetActiveCodeToHistory(previous);
                    EditorOperations.MoveToEndOfDocument(false);
                }
            }

            public void HistoryNext(string search)
            {
                if (_window._currentLanguageBuffer == null)
                {
                    return;
                }

                var next = History.GetNext(search);
                if (next != null)
                {
                    if (string.IsNullOrWhiteSpace(search))
                    {
                        // don't store search as an uncommitted history item
                        StoreUncommittedInputForHistory();
                    }

                    SetActiveCodeToHistory(next);
                    EditorOperations.MoveToEndOfDocument(false);
                }
                else
                {
                    string code = History.UncommittedInput;
                    History.UncommittedInput = null;
                    if (!string.IsNullOrEmpty(code))
                    {
                        SetActiveCode(code);
                        EditorOperations.MoveToEndOfDocument(false);
                    }
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

            /// <summary>
            /// Sets the active code to the specified text w/o executing it.
            /// </summary>
            private void SetActiveCode(string text)
            {
                // TODO (tomat): this should be handled by the language intellisense provider, not here:
                var completionSession = _window.SessionStack.TopSession;
                if (completionSession != null)
                {
                    completionSession.Dismiss();
                }

                using (var edit = _window._currentLanguageBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, editTag: null))
                {
                    edit.Replace(new Span(0, _window._currentLanguageBuffer.CurrentSnapshot.Length), text);
                    edit.Apply();
                }
            }

            public void HistorySearchNext()
            {
                EnsureHistorySearch();
                HistoryNext(_historySearch);
            }

            public void HistorySearchPrevious()
            {
                EnsureHistorySearch();
                HistoryPrevious(_historySearch);
            }

            private void EnsureHistorySearch()
            {
                if (_historySearch == null)
                {
                    _historySearch = _window._currentLanguageBuffer.CurrentSnapshot.GetText();
                }
            }

            private void StoreUncommittedInputForHistory()
            {
                if (History.UncommittedInput == null)
                {
                    string activeCode = GetActiveCode();
                    if (activeCode.Length > 0)
                    {
                        History.UncommittedInput = activeCode;
                    }
                }
            }

            /// <summary>
            /// Moves to the beginning of the line.
            /// </summary>
            public void Home(bool extendSelection)
            {
                var caret = _window.Caret;

                // map the end of subject buffer line:
                var subjectLineEnd = _window._textView.BufferGraph.MapDownToFirstMatch(
                    caret.Position.BufferPosition.GetContainingLine().End,
                    PointTrackingMode.Positive,
                    snapshot => snapshot.TextBuffer != _window._projectionBuffer,
                    PositionAffinity.Successor).Value;

                ITextSnapshotLine subjectLine = subjectLineEnd.GetContainingLine();

                var projectedSubjectLineStart = _window._textView.BufferGraph.MapUpToBuffer(
                    subjectLine.Start,
                    PointTrackingMode.Positive,
                    PositionAffinity.Successor,
                    _window._projectionBuffer).Value;

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
                    VirtualSnapshotPoint anchor = _window._textView.Selection.AnchorPoint;
                    caret.MoveTo(moveTo);
                    _window._textView.Selection.Select(anchor.TranslateTo(_window._textView.TextSnapshot), _window._textView.Caret.Position.VirtualBufferPosition);
                }
                else
                {
                    _window._textView.Selection.Clear();
                    caret.MoveTo(moveTo);
                }
            }

            /// <summary>
            /// Moves to the end of the line.
            /// </summary>
            public void End(bool extendSelection)
            {
                var caret = _window.Caret;

                // map the end of the subject buffer line:
                var subjectLineEnd = _window._textView.BufferGraph.MapDownToFirstMatch(
                    caret.Position.BufferPosition.GetContainingLine().End,
                    PointTrackingMode.Positive,
                    snapshot => snapshot.TextBuffer != _window._projectionBuffer,
                    PositionAffinity.Successor).Value;

                ITextSnapshotLine subjectLine = subjectLineEnd.GetContainingLine();

                var moveTo = _window._textView.BufferGraph.MapUpToBuffer(
                    subjectLine.End,
                    PointTrackingMode.Positive,
                    PositionAffinity.Successor,
                    _window._projectionBuffer).Value;

                if (extendSelection)
                {
                    VirtualSnapshotPoint anchor = _window._textView.Selection.AnchorPoint;
                    caret.MoveTo(moveTo);
                    _window._textView.Selection.Select(anchor.TranslateTo(_window._textView.TextSnapshot), _window._textView.Caret.Position.VirtualBufferPosition);
                }
                else
                {
                    _window._textView.Selection.Clear();
                    caret.MoveTo(moveTo);
                }
            }

            public void SelectAll()
            {
                SnapshotSpan? span = GetContainingRegion(_window._textView.Caret.Position.BufferPosition);

                var selection = _window._textView.Selection;

                // if the span is already selected select all text in the projection buffer:
                if (span == null || selection.SelectedSpans.Count == 1 && selection.SelectedSpans[0] == span.Value)
                {
                    var currentSnapshot = _window.TextBuffer.CurrentSnapshot;
                    span = new SnapshotSpan(currentSnapshot, new Span(0, currentSnapshot.Length));
                }

                _window._textView.Selection.Select(span.Value, isReversed: false);
            }

            /// <summary>
            /// Given a point in projection buffer calculate a span that includes the point and comprises of 
            /// subsequent projection spans forming a region, i.e. a sequence of output spans in between two subsequent submissions,
            /// a language input block, or standard input block.
            /// </summary>
            private SnapshotSpan? GetContainingRegion(SnapshotPoint point)
            {
                var sourceSpans = GetSourceSpans(point.Snapshot);
                int promptIndex = _window.GetPromptIndexForPoint(sourceSpans, point);
                if (promptIndex < 0)
                {
                    return null;
                }

                // Grab the span following the prompt (either language or standard input).
                var projectionSpan = sourceSpans[promptIndex + 1];
                var inputSnapshot = projectionSpan.Snapshot;
                var kind = _window.GetSpanKind(projectionSpan);

                Debug.Assert(kind == ReplSpanKind.Language || kind == ReplSpanKind.StandardInput);

                // Language input block is a projection of the entire snapshot;
                // std input block is a projection of a single span:
                SnapshotPoint inputBufferEnd = (kind == ReplSpanKind.Language) ?
                    new SnapshotPoint(inputSnapshot, inputSnapshot.Length) :
                    projectionSpan.End;

                var bufferGraph = _window._textView.BufferGraph;
                var textBuffer = _window.TextBuffer;

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

                    var promptProjectionSpan = sourceSpans[promptIndex];
                    if (point < projectedLanguageBufferStart - promptProjectionSpan.Length)
                    {
                        // cursor is before the first language buffer:
                        return new SnapshotSpan(new SnapshotPoint(textBuffer.CurrentSnapshot, 0), projectedLanguageBufferStart - promptProjectionSpan.Length);
                    }

                    // cursor is within the language buffer:
                    return new SnapshotSpan(projectedLanguageBufferStart, projectedInputBufferEnd);
                }

                int nextPromptIndex = -1;
                for (int i = promptIndex + 1; i < sourceSpans.Count; i++)
                {
                    if (_window.IsPrompt(sourceSpans[i]))
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

                var lastSpanBeforeNextPrompt = sourceSpans[nextPromptIndex - 1];
                Debug.Assert(_window.GetSpanKind(lastSpanBeforeNextPrompt) == ReplSpanKind.Output);

                // select all text in between the language buffer and the next prompt:
                return new SnapshotSpan(
                    projectedInputBufferEnd,
                    bufferGraph.MapUpToBuffer(
                        lastSpanBeforeNextPrompt.End,
                        PointTrackingMode.Positive,
                        PositionAffinity.Predecessor,
                        textBuffer).Value);
            }

            public bool Delete()
            {
                _historySearch = null;
                bool handled = false;
                if (!_window._textView.Selection.IsEmpty)
                {
                    if (_window._textView.Selection.Mode == TextSelectionMode.Stream || ReduceBoxSelectionToEditableBox())
                    {
                        CutOrDeleteSelection(isCut: false);
                        MoveCaretToClosestEditableBuffer();
                        handled = true;
                    }
                }

                return handled;
            }

            private bool ReduceBoxSelectionToEditableBox(bool isDelete = true)
            {
                Debug.Assert(_window._textView.Selection.Mode == TextSelectionMode.Box);

                VirtualSnapshotPoint anchor = _window._textView.Selection.AnchorPoint;
                VirtualSnapshotPoint active = _window._textView.Selection.ActivePoint;

                bool result;
                if (active < anchor)
                {
                    result = ReduceBoxSelectionToEditableBox(ref active, ref anchor, isDelete);
                }
                else
                {
                    result = ReduceBoxSelectionToEditableBox(ref anchor, ref active, isDelete);
                }

                _window._textView.Selection.Select(anchor, active);
                _window._textView.Caret.MoveTo(active);

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

            /// <summary>
            /// Returns the lengths of the longest and shortest prompts within the specified range of lines of the current submission buffer.
            /// </summary>
            private void MeasurePrompts(int startLine, int endLine, out int minPromptLength, out int maxPromptLength)
            {
                Debug.Assert(endLine > startLine);

                var projectionSnapshot = _window._projectionBuffer.CurrentSnapshot;
                var sourceSpans = projectionSnapshot.GetSourceSpans();
                var promptSpanIndex = _window.GetProjectionSpanIndexFromEditableBufferPosition(projectionSnapshot, sourceSpans.Count, startLine) - 1;
                var promptSpan = sourceSpans[promptSpanIndex];
                Debug.Assert(_window.IsPrompt(promptSpan));

                minPromptLength = maxPromptLength = promptSpan.Length;
            }

            public void Cut()
            {
                if (_window._textView.Selection.IsEmpty)
                {
                    CutOrDeleteCurrentLine(isCut: true);
                }
                else
                {
                    CutOrDeleteSelection(isCut: true);
                }

                MoveCaretToClosestEditableBuffer();
            }

            private void CutOrDeleteCurrentLine(bool isCut)
            {
                ITextSnapshotLine line;
                int column;
                _window._textView.Caret.Position.VirtualBufferPosition.GetLineAndColumn(out line, out column);

                CutOrDelete(new[] { line.ExtentIncludingLineBreak }, isCut);

                _window._textView.Caret.MoveTo(new VirtualSnapshotPoint(_window._textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(line.LineNumber), column));
            }

            /// <summary>
            /// Deletes currently selected text from the language buffer and optionally saves it to the clipboard.
            /// </summary>
            private void CutOrDeleteSelection(bool isCut)
            {
                CutOrDelete(_window._textView.Selection.SelectedSpans, isCut);

                // if the selection spans over prompts the prompts remain selected, so clear manually:
                _window._textView.Selection.Clear();
            }

            private void CutOrDelete(IEnumerable<SnapshotSpan> projectionSpans, bool isCut)
            {
                Debug.Assert(_window._currentLanguageBuffer != null);

                StringBuilder deletedText = null;

                // split into multiple deletes that only affect the language/input buffer:
                ITextBuffer affectedBuffer = (_window._stdInputStart != null) ? _window._standardInputBuffer : _window._currentLanguageBuffer;
                using (var edit = affectedBuffer.CreateEdit())
                {
                    foreach (var projectionSpan in projectionSpans)
                    {
                        var spans = _window._textView.BufferGraph.MapDownToBuffer(projectionSpan, SpanTrackingMode.EdgeInclusive, affectedBuffer);
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
                    if (_window._textView.Selection.Mode == TextSelectionMode.Box)
                    {
                        data.SetData(BoxSelectionCutCopyTag, new object());
                    }

                    data.SetText(deletedText.ToString());
                    Clipboard.SetDataObject(data, true);
                }
            }

            public void Copy()
            {
                CopySelection();
            }

            /// <summary>
            /// Copy the entire selection to the clipboard for RTF format and
            /// copy the selection minus any prompt text for other formats.
            /// That allows paste into code editors of just the code and
            /// paste of the entire content for editors that support RTF.
            /// </summary>
            private void CopySelection()
            {
                var spans = GetSelectionSpans(_window._textView);
                var data = Copy(spans);
                Clipboard.SetDataObject(data, true);
            }

            private static NormalizedSnapshotSpanCollection GetSelectionSpans(ITextView textView)
            {
                var selection = textView.Selection;
                if (selection.IsEmpty)
                {
                    // Use the current line as the selection.
                    var snapshotLine = textView.Caret.Position.VirtualBufferPosition.Position.GetContainingLine();
                    var span = new SnapshotSpan(snapshotLine.Start, snapshotLine.LengthIncludingLineBreak);
                    return new NormalizedSnapshotSpanCollection(span);
                }
                return selection.SelectedSpans;
            }

            private DataObject Copy(NormalizedSnapshotSpanCollection spans)
            {
                var text = spans.Aggregate(new StringBuilder(), GetTextWithoutPrompts, b => b.ToString());
                var rtf = _rtfBuilderService.GenerateRtf(spans, _window._textView);
                var data = new DataObject();
                data.SetData(DataFormats.StringFormat, text);
                data.SetData(DataFormats.Text, text);
                data.SetData(DataFormats.UnicodeText, text);
                data.SetData(DataFormats.Rtf, rtf);
                return data;
            }

            private StringBuilder GetTextWithoutPrompts(StringBuilder builder, SnapshotSpan span)
            {
                // Find the range of source spans that cover the span.
                var sourceSpans = GetSourceSpans(span.Snapshot);
                int n = sourceSpans.Count;
                int index = _window.GetSourceSpanIndex(sourceSpans, span.Start);
                if (index == n)
                {
                    index--;
                }

                // Add the text for all non-prompt spans within the range.
                for (; index < n; index++)
                {
                    var sourceSpan = sourceSpans[index];
                    if (sourceSpan.IsEmpty)
                    {
                        continue;
                    }
                    if (!_window.IsPrompt(sourceSpan))
                    {
                        var sourceSnapshot = sourceSpan.Snapshot;
                        var mappedSpans = _window._textView.BufferGraph.MapDownToBuffer(span, SpanTrackingMode.EdgeExclusive, sourceSnapshot.TextBuffer);
                        bool added = false;
                        foreach (var mappedSpan in mappedSpans)
                        {
                            var intersection = sourceSpan.Span.Intersection(mappedSpan);
                            if (intersection.HasValue)
                            {
                                builder.Append(sourceSnapshot.GetText(intersection.Value));
                                added = true;
                            }
                        }
                        if (!added)
                        {
                            break;
                        }
                    }
                }

                return builder;
            }

            public bool Backspace()
            {
                bool handled = false;
                if (!_window._textView.Selection.IsEmpty)
                {
                    if (_window._textView.Selection.Mode == TextSelectionMode.Stream || ReduceBoxSelectionToEditableBox())
                    {
                        CutOrDeleteSelection(isCut: false);
                        MoveCaretToClosestEditableBuffer();
                        handled = true;
                    }
                }
                else if (_window._textView.Caret.Position.VirtualSpaces == 0)
                {
                    DeletePreviousCharacter();
                    handled = true;
                }

                return handled;
            }

            /// <summary>
            /// Deletes characters preceding the current caret position in the current language buffer.
            /// </summary>
            private void DeletePreviousCharacter()
            {
                SnapshotPoint? point = MapToEditableBuffer(_window._textView.Caret.Position.BufferPosition);

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

                ScrollToCaret();
            }

            /// <summary>
            /// Maps point to the current language buffer or standard input buffer.
            /// </summary>
            private SnapshotPoint? MapToEditableBuffer(SnapshotPoint projectionPoint)
            {
                SnapshotPoint? result = null;

                if (_window._currentLanguageBuffer != null)
                {
                    result = _window.GetPositionInLanguageBuffer(projectionPoint);
                }

                if (result != null)
                {
                    return result;
                }

                if (_window._standardInputBuffer != null)
                {
                    result = _window.GetPositionInStdInputBuffer(projectionPoint);
                }

                return result;
            }

            public bool TrySubmitStandardInput()
            {
                _historySearch = null;
                if (_window._stdInputStart != null)
                {
                    if (InStandardInputRegion(_window._textView.Caret.Position.BufferPosition))
                    {
                        SubmitStandardInput();
                    }

                    return true;
                }

                return false;
            }

            private void SubmitStandardInput()
            {
                AppendLineNoPromptInjection(_window._standardInputBuffer);
                _window._inputValue = new SnapshotSpan(_window._standardInputBuffer.CurrentSnapshot, Span.FromBounds(_window._stdInputStart.Value, _window._standardInputBuffer.CurrentSnapshot.Length));
                _window._inputEvent.Set();
            }

            private bool InStandardInputRegion(SnapshotPoint point)
            {
                if (_window._stdInputStart == null)
                {
                    return false;
                }

                var stdInputPoint = _window.GetPositionInStdInputBuffer(point);
                return stdInputPoint != null && stdInputPoint.Value.Position >= _window._stdInputStart.Value;
            }

            /// <summary>
            /// Add a zero-width tracking span at the end of the projection buffer mapping to the end of the standard input buffer.
            /// </summary>
            public void AddStandardInputSpan()
            {
                var promptSpan = _window.CreateStandardInputPrompt();
                var currentSnapshot = _window._standardInputBuffer.CurrentSnapshot;
                var inputSpan = new CustomTrackingSpan(
                    currentSnapshot,
                    new Span(currentSnapshot.Length, 0),
                    PointTrackingMode.Negative,
                    PointTrackingMode.Positive);
                AppendProjectionSpans(promptSpan, inputSpan);
            }

            public bool BreakLine()
            {
                return HandlePostServicesReturn(false);
            }

            public bool Return()
            {
                _historySearch = null;
                return HandlePostServicesReturn(true);
            }

            private bool HandlePostServicesReturn(bool trySubmit)
            {
                if (_window._currentLanguageBuffer == null)
                {
                    return false;
                }

                // handle "RETURN" command that is not handled by either editor or service
                var langCaret = _window.GetPositionInLanguageBuffer(_window.Caret.Position.BufferPosition);
                if (langCaret != null)
                {
                    int caretPosition = langCaret.Value.Position;

                    // note that caret might be located in virtual space behind the current buffer end:
                    if (trySubmit && caretPosition >= _window._currentLanguageBuffer.CurrentSnapshot.Length && CanExecuteActiveCode())
                    {
                        var dummy = SubmitAsync();
                        return true;
                    }

                    // insert new line (triggers secondary prompt injection in buffer changed event):
                    _window._currentLanguageBuffer.Insert(caretPosition, _window._lineBreakString);
                    _window.IndentCurrentLine(_window._textView.Caret.Position.BufferPosition);
                    ScrollToCaret();

                    return true;
                }
                else
                {
                    MoveCaretToClosestEditableBuffer();
                }

                return false;
            }

            private bool CanExecuteActiveCode()
            {
                Debug.Assert(_window._currentLanguageBuffer != null);

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

                // If this throws, VS shows a dialog.
                return _window._evaluator.CanExecuteCode(input);
            }

            /// <summary>
            /// Returns the insertion point relative to the current language buffer.
            /// </summary>
            private int GetActiveCodeInsertionPosition()
            {
                Debug.Assert(_window._currentLanguageBuffer != null);

                var langPoint = _window._textView.BufferGraph.MapDownToBuffer(
                    new SnapshotPoint(
                        _window._projectionBuffer.CurrentSnapshot,
                        _window.Caret.Position.BufferPosition.Position),
                    PointTrackingMode.Positive,
                    _window._currentLanguageBuffer,
                    PositionAffinity.Predecessor);

                if (langPoint != null)
                {
                    return langPoint.Value;
                }

                return _window._currentLanguageBuffer.CurrentSnapshot.Length;
            }
        }

        internal enum State
        {
            /// <summary>
            /// Initial state.  <see cref="IInteractiveWindow.InitializeAsync"/> hasn't been called.
            /// Transition to <see cref="Initializing"/> when <see cref="IInteractiveWindow.InitializeAsync"/> is called.
            /// Transition to <see cref="Resetting"/> when <see cref="IInteractiveWindowOperations.ResetAsync"/> is called.
            /// </summary>
            Starting,
            /// <summary>
            /// In the process of calling <see cref="IInteractiveWindow.InitializeAsync"/>.
            /// Transition to <see cref="WaitingForInput"/> when finished (in <see cref="UIThreadOnly.ProcessPendingSubmissions"/>).
            /// Transition to <see cref="Resetting"/> when <see cref="IInteractiveWindowOperations.ResetAsync"/> is called.
            /// </summary>
            Initializing,
            /// <summary>
            /// In the process of calling <see cref="IInteractiveWindowOperations.ResetAsync"/>.
            /// Transition to <see cref="WaitingForInput"/> when finished (in <see cref="UIThreadOnly.ProcessPendingSubmissions"/>).
            /// Note: Should not see <see cref="IInteractiveWindowOperations.ResetAsync"/> calls while in this state.
            /// </summary>
            Resetting,
            /// <summary>
            /// Prompt has been displayed - waiting for the user to make the next submission.
            /// Transition to <see cref="ExecutingInput"/> when <see cref="IInteractiveWindowOperations.ExecuteInput"/> is called.
            /// Transition to <see cref="Resetting"/> when <see cref="IInteractiveWindowOperations.ResetAsync"/> is called.
            /// </summary>
            WaitingForInput,
            /// <summary>
            /// Executing the user's submission.
            /// Transition to <see cref="WaitingForInput"/> when finished (in <see cref="UIThreadOnly.ProcessPendingSubmissions"/>).
            /// Transition to <see cref="Resetting"/> when <see cref="IInteractiveWindowOperations.ResetAsync"/> is called.
            /// </summary>
            ExecutingInput,
            /// <summary>
            /// In the process of calling <see cref="IInteractiveWindow.ReadStandardInput"/>.
            /// Return to preceding state when finished.
            /// Transition to <see cref="Resetting"/> when <see cref="IInteractiveWindowOperations.ResetAsync"/> is called.
            /// </summary>
            /// <remarks>
            /// TODO: When we clean up <see cref="IInteractiveWindow.ReadStandardInput"/> (https://github.com/dotnet/roslyn/issues/3984)
            /// we should try to eliminate the "preceding state", since it substantially
            /// increases the complexity of the state machine.
            /// </remarks>
            ReadingStandardInput,
        }
    }
}
