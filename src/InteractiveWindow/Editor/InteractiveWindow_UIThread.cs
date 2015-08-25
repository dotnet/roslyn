// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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

            _dangerous_uiOnly = new UIThreadOnly(this, host);

            this.Properties = new PropertyCollection();
            _history = new History();

            _intellisenseSessionStackMap = intellisenseSessionStackMap;
            _smartIndenterService = smartIndenterService;

            var replContentType = contentTypeRegistry.GetContentType(PredefinedInteractiveContentTypes.InteractiveContentTypeName);
            var replOutputContentType = contentTypeRegistry.GetContentType(PredefinedInteractiveContentTypes.InteractiveOutputContentTypeName);

            _outputBuffer = bufferFactory.CreateTextBuffer(replOutputContentType);
            _standardInputBuffer = bufferFactory.CreateTextBuffer();
            _promptBuffer = bufferFactory.CreateTextBuffer();
            _secondaryPromptBuffer = bufferFactory.CreateTextBuffer();
            _standardInputPromptBuffer = bufferFactory.CreateTextBuffer();
            _outputLineBreakBuffer = bufferFactory.CreateTextBuffer();

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

            _rtfBuilderService = rtfBuilderService;

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

        #endregion

        private sealed class UIThreadOnly
        {
            private readonly InteractiveWindow _window;

            private readonly IInteractiveWindowEditorFactoryService _host;

            private readonly IReadOnlyRegion[] _outputProtection;

            // Pending submissions to be processed whenever the REPL is ready to accept submissions.
            private readonly Queue<PendingSubmission> _pendingSubmissions;

            private DispatcherTimer _executionTimer;
            private Cursor _oldCursor;
            private int _currentOutputProjectionSpan;
            private int _outputTrackingCaretPosition;

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

            public UIThreadOnly(InteractiveWindow window, IInteractiveWindowEditorFactoryService host)
            {
                _window = window;
                _host = host;
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
                        Debug.Assert(_window.IsLanguage(snapshot.GetSourceSpan(spanCount - 1).Snapshot));
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

                _window._history.ForgetOriginalBuffers();

                // If we were waiting for input, we need to restore the prompt that we just cleared.
                // If we are in any other state, then we'll let normal transitions trigger the next prompt.
                if (State == State.WaitingForInput)
                {
                    PrepareForInput();
                }
            }

            private void CancelStandardInput()
            {
                _window.AppendLineNoPromptInjection(_window._standardInputBuffer);
                _window._inputValue = null;
                _window._inputEvent.Set();
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
                        _window.CutOrDeleteSelection(isCut: false);
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
                    string activeCode = _window.GetActiveCode();
                    if (!string.IsNullOrEmpty(activeCode))
                    {
                        UncommittedInput = activeCode;
                    }
                }
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
                    _window.SetActiveCode(command);
                }

                // Add command to history before calling FinishCurrentSubmissionInput as it adds newline 
                // to the end of the command.
                _window._history.Add(_window._currentLanguageBuffer.CurrentSnapshot.GetExtent());
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
                    _window.SetActiveCode(UncommittedInput);
                    UncommittedInput = null;
                }
            }

            /// <summary>
            /// Pastes from the clipboard into the text view
            /// </summary>
            public bool Paste()
            {
                _window.MoveCaretToClosestEditableBuffer();

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

            /// <summary>
            /// Appends given text to the last input span (standard input or active code input).
            /// </summary>
            private void AppendInput(string text)
            {
                var snapshot = _window._projectionBuffer.CurrentSnapshot;
                var spanCount = snapshot.SpanCount;
                var inputSpan = snapshot.GetSourceSpan(spanCount - 1);
                Debug.Assert(_window.GetSpanKind(inputSpan.Snapshot) == ReplSpanKind.Language ||
                    _window.GetSpanKind(inputSpan.Snapshot) == ReplSpanKind.StandardInput);

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
                _window.SetActiveCode(submission.Input);
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

                    _window._history.UncommittedInput = null;

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
                        _window._history.Add(historySpan);
                        State = State.ExecutingInput;

                        StartCursorTimer();

                        var executionResult = await _window._evaluator.ExecuteCodeAsync(snapshotSpan.GetText()).ConfigureAwait(true);
                        Debug.Assert(_window.OnUIThread()); // ConfigureAwait should bring us back to the UI thread.

                        Debug.Assert(State == State.ExecutingInput || State == State.Resetting, $"Unexpected state {State}");

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
                _window.AppendLineNoPromptInjection(_window._currentLanguageBuffer);
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
                var sourceSnapshot = sourceSpan.Snapshot;
                Debug.Assert(_window.GetSpanKind(sourceSnapshot) == ReplSpanKind.Output);
                var nonGrowingSpan = new CustomTrackingSpan(
                    sourceSnapshot,
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

            private void InsertProjectionSpan(int index, ITrackingSpan span)
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

                // we maintain this invariant so that projections don't split "\r\n" in half 
                // (the editor isn't happy about it and our line counting also gets simpler):
                Debug.Assert(!_window._outputBuffer.CurrentSnapshot.EndsWith('\r'));

                var projectionSpans = _window._projectionBuffer.CurrentSnapshot.GetSourceSpans();
                Debug.Assert(_window.GetSpanKind(projectionSpans[_currentOutputProjectionSpan].Snapshot) == ReplSpanKind.Output);

                int lineBreakProjectionSpanIndex = _currentOutputProjectionSpan + 1;

                // insert line break projection span if there is none and the output doesn't end with a line break:
                bool hasLineBreakProjection = false;
                if (lineBreakProjectionSpanIndex < projectionSpans.Count)
                {
                    var oldSpan = projectionSpans[lineBreakProjectionSpanIndex];
                    hasLineBreakProjection = _window.GetSpanKind(oldSpan.Snapshot) == ReplSpanKind.Output && string.Equals(oldSpan.GetText(), _window._lineBreakString);
                }

                Debug.Assert(output.Last().Last() != '\r');
                bool endsWithLineBreak = output.Last().Last() == '\n';

                bool insertLineBreak = !endsWithLineBreak && !hasLineBreakProjection;
                bool removeLineBreak = endsWithLineBreak && hasLineBreakProjection;

                // insert text to the subject buffer.
                int oldBufferLength = _window._outputBuffer.CurrentSnapshot.Length;
                InsertOutput(output, oldBufferLength);

                if (removeLineBreak)
                {
                    RemoveProjectionSpans(lineBreakProjectionSpanIndex, 1);
                }
                else if (insertLineBreak)
                {
                    InsertProjectionSpan(lineBreakProjectionSpanIndex, CreateTrackingSpan(_window._outputLineBreakBuffer, _window._lineBreakString));
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

                if (!succeeded && _window._history.Last != null)
                {
                    _window._history.Last.Failed = true;
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
                var inputSnapshot = projectionSpan.Snapshot;
                if (_window.GetSpanKind(inputSnapshot) != ReplSpanKind.Language)
                {
                    Debug.Assert(_window.GetSpanKind(inputSnapshot) == ReplSpanKind.StandardInput);
                    return null;
                }

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
                    if (_window.GetSpanKind(sourceSpans[i].Snapshot) == ReplSpanKind.StandardInput)
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
                Debug.Assert(_window.IsPrompt(snapshot.GetSourceSpan(spanCount - SpansPerLineOfInput).Snapshot));

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
                _window.AppendProjectionSpans(promptSpan, languageSpan);
            }

            public void ScrollToCaret()
            {
                var textView = _window._textView;
                var caretPosition = textView.Caret.Position.BufferPosition;
                var caretSpan = new SnapshotSpan(caretPosition.Snapshot, caretPosition, 0);
                textView.ViewScroller.EnsureSpanVisible(caretSpan);
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
