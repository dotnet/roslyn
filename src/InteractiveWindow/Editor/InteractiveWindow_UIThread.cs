// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// Provides implementation of a Repl Window built on top of the VS editor using projection buffers.
    /// </summary>
    internal partial class InteractiveWindow : IInteractiveWindow, IInteractiveWindowOperations
    {
        private UIThreadOnly _dangerous_uiOnly;

        #region Initialization

        public InteractiveWindow(
            IInteractiveWindowEditorFactoryService host,
            IContentTypeRegistryService contentTypeRegistry,
            ITextBufferFactoryService bufferFactory,
            IProjectionBufferFactoryService projectionBufferFactory,
            IEditorOperationsFactoryService editorOperationsFactory,
            ITextEditorFactoryService editorFactory,
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

            var textContentType = contentTypeRegistry.GetContentType("text");
            var replContentType = contentTypeRegistry.GetContentType(PredefinedInteractiveContentTypes.InteractiveContentTypeName);
            var replOutputContentType = contentTypeRegistry.GetContentType(PredefinedInteractiveContentTypes.InteractiveOutputContentTypeName);

            _outputBuffer = bufferFactory.CreateTextBuffer(replOutputContentType);
            _standardInputBuffer = bufferFactory.CreateTextBuffer();

            var projBuffer = projectionBufferFactory.CreateProjectionBuffer(
                new EditResolver(this),
                Array.Empty<object>(),
                ProjectionBufferOptions.None,
                replContentType);

            // we need to set IReplPromptProvider property before TextViewHost is instantiated so that ReplPromptTaggerProvider can bind to it 
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

            _textView.Options.SetOptionValue(DefaultTextViewHostOptions.HorizontalScrollBarId, false);
            _textView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, false);
            _textView.Options.SetOptionValue(DefaultTextViewHostOptions.OutliningMarginId, false);
            _textView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, false);
            _textView.Options.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, WordWrapStyles.WordWrap);

            string lineBreak = _textView.Options.GetNewLineCharacter();
            _lineBreakOutputSpan = new ReplSpan(lineBreak, ReplSpanKind.Output);
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
            RequiresUIThread();

            if (_dangerous_uiOnly.State != State.Starting)
            {
                throw new InvalidOperationException(InteractiveWindowResources.AlreadyInitialized);
            }

            _dangerous_uiOnly.State = State.Initializing;

            // Anything that reads options should wait until after this call so the evaluator can set the options first
            ExecutionResult result = await _evaluator.InitializeAsync().ConfigureAwait(continueOnCapturedContext: true);

            Debug.Assert(OnUIThread()); // ConfigureAwait should bring us back to the UI thread.

            if (result.IsSuccessful)
            {
                _dangerous_uiOnly.PrepareForInput();
            }

            return result;
        }

        #endregion

        private class UIThreadOnly
        {
            private readonly InteractiveWindow _window;

            private readonly IInteractiveWindowEditorFactoryService _host;

            private readonly TaskScheduler _uiScheduler;

            private readonly IReadOnlyRegion[] _outputProtection;

            // Pending submissions to be processed whenever the REPL is ready to accept submissions.
            private readonly Queue<PendingSubmission> _pendingSubmissions;

            private DispatcherTimer _executionTimer;
            private Cursor _oldCursor;
            private Task<ExecutionResult> _currentTask;
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
                _uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
                StandardInputProtection = new IReadOnlyRegion[2];
                _outputProtection = new IReadOnlyRegion[2];
                _pendingSubmissions = new Queue<PendingSubmission>();
                _outputTrackingCaretPosition = -1;
            }

            public Task<ExecutionResult> ResetAsync(bool initialize)
            {
                Debug.Assert(State != State.Resetting, "The button should have been disabled.");

                if (_window._stdInputStart != null)
                {
                     CancelStandardInput();
                }

                _window._buffer.Flush();

                // replace the task being interrupted by a "reset" task:
                State = State.Resetting;
                _currentTask = _window._evaluator.ResetAsync(initialize);
                _currentTask.ContinueWith(FinishExecute, _uiScheduler);

                return _currentTask;
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
                _window._promptLineMapping.Clear();
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

                RemoveProjectionSpans(0, _window._projectionSpans.Count);

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
                if (_window._stdInputStart == null)
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

                FinishCurrentSubmissionInput();
                _window._history.Add(_window._currentLanguageBuffer.CurrentSnapshot.GetExtent().TrimEnd());
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
                var inputSpan = _window._projectionSpans[_window._projectionSpans.Count - 1];
                Debug.Assert(inputSpan.Kind == ReplSpanKind.Language || inputSpan.Kind == ReplSpanKind.StandardInput);
                Debug.Assert(inputSpan.TrackingSpan.TrackingMode == SpanTrackingMode.Custom);

                var buffer = inputSpan.TrackingSpan.TextBuffer;
                var span = inputSpan.TrackingSpan.GetSpan(buffer.CurrentSnapshot);
                using (var edit = buffer.CreateEdit())
                {
                    edit.Insert(edit.Snapshot.Length, text);
                    edit.Apply();
                }

                var replSpan = new ReplSpan(
                    new CustomTrackingSpan(
                        buffer.CurrentSnapshot,
                        new Span(span.Start, span.Length + text.Length),
                        PointTrackingMode.Negative,
                        PointTrackingMode.Positive),
                    inputSpan.Kind);

                ReplaceProjectionSpan(_window._projectionSpans.Count - 1, replSpan);

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
                    _window.Caret.MoveTo(_window.GetLastLine().End);
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

                // queue new work item:
                _window.Dispatcher.Invoke(new Action(() =>
                {
                    _window.SetActiveCode(submission.Input);
                    var taskDone = SubmitAsync();
                    if (submission.Completion != null)
                    {
                        taskDone.ContinueWith(x => submission.Completion.SetResult(null), TaskScheduler.Current);
                    }
                }));
            }

            public Task SubmitAsync()
            {
                RequiresLanguageBuffer();

                // TODO: queue submission
                // Ensure that the REPL doesn't try to execute if it is already
                // executing.  If this invariant can no longer be maintained more of
                // the code in this method will need to be bullet-proofed
                if (State == State.ExecutingInput)
                {
                    return Task.FromResult<object>(null);
                }

                FinishCurrentSubmissionInput();

                _window._history.UncommittedInput = null;

                var snapshotSpan = _window._currentLanguageBuffer.CurrentSnapshot.GetExtent();
                var trimmedSpan = snapshotSpan.TrimEnd();

                if (trimmedSpan.Length == 0)
                {
                    // TODO: reuse the current language buffer
                    PrepareForInput();
                    return Task.FromResult<object>(null);
                }
                else
                {
                    _window._history.Add(trimmedSpan);
                    State = State.ExecutingInput;

                    StartCursorTimer();

                    Debug.Assert(_currentTask == null, "Shouldn't be either executing or resetting");
                    _currentTask = _window._evaluator.ExecuteCodeAsync(snapshotSpan.GetText());
                    return _currentTask.ContinueWith(FinishExecute, _uiScheduler);
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
                Debug.Assert(_window._projectionSpans[_currentOutputProjectionSpan].Kind == ReplSpanKind.Output);
                var nonGrowingSpan = _window._projectionSpans[_currentOutputProjectionSpan].WithEndTrackingMode(PointTrackingMode.Negative);
                ReplaceProjectionSpan(_currentOutputProjectionSpan, nonGrowingSpan);

                AppendNewOutputProjectionBuffer();
                _outputTrackingCaretPosition = _window._textView.Caret.Position.BufferPosition;
            }

            // WARNING: When updating projection spans we need to update _projectionSpans list first and 
            // then projection buffer, since the projection buffer update might trigger events that might 
            // access the projection spans.

            public void AppendNewOutputProjectionBuffer()
            {
                var trackingSpan = new CustomTrackingSpan(
                    _window._outputBuffer.CurrentSnapshot,
                    new Span(_window._outputBuffer.CurrentSnapshot.Length, 0),
                    PointTrackingMode.Negative,
                    PointTrackingMode.Positive);

                _currentOutputProjectionSpan = AppendProjectionSpan(new ReplSpan(trackingSpan, ReplSpanKind.Output));
            }

            private int AppendProjectionSpan(ReplSpan span)
            {
                int index = _window._projectionSpans.Count;
                InsertProjectionSpan(index, span);
                return index;
            }

            private void InsertProjectionSpan(int index, ReplSpan span)
            {
                _window._projectionSpans.Insert(index, span);
                _window._projectionBuffer.ReplaceSpans(index, 0, new[] { span.Span }, EditOptions.None, editTag: s_suppressPromptInjectionTag);
            }

            public void ReplaceProjectionSpan(int spanToReplace, ReplSpan newSpan)
            {
                _window._projectionSpans[spanToReplace] = newSpan;
                _window._projectionBuffer.ReplaceSpans(spanToReplace, 1, new[] { newSpan.Span }, EditOptions.None, editTag: s_suppressPromptInjectionTag);
            }

            private void RemoveProjectionSpans(int index, int count)
            {
                _window._projectionSpans.RemoveRange(index, count);
                _window._projectionBuffer.ReplaceSpans(index, count, Array.Empty<object>(), EditOptions.None, s_suppressPromptInjectionTag);
            }

            /// <summary>
            /// Appends text to the output buffer and updates projection buffer to include it.
            /// WARNING: this has to be the only method that writes to the output buffer so that 
            /// the output buffering counters are kept in sync.
            /// </summary>
            internal void AppendOutput(IEnumerable<string> output, int outputLength)
            {
                Debug.Assert(output.Any());

                // we maintain this invariant so that projections don't split "\r\n" in half 
                // (the editor isn't happy about it and out line counting also gets simpler):
                Debug.Assert(!_window._outputBuffer.CurrentSnapshot.EndsWith('\r'));

                Debug.Assert(_window._projectionSpans[_currentOutputProjectionSpan].Kind == ReplSpanKind.Output);

                int lineBreakProjectionSpanIndex = _currentOutputProjectionSpan + 1;

                // insert line break projection span if there is none and the output doesn't end with a line break:
                bool hasLineBreakProjection = lineBreakProjectionSpanIndex < _window._projectionSpans.Count &&
                                              ReferenceEquals(_window._projectionSpans[lineBreakProjectionSpanIndex], _window._lineBreakOutputSpan);

                bool endsWithLineBreak;
                int newLineBreaks = CountOutputLineBreaks(output, out endsWithLineBreak);

                bool insertLineBreak = !endsWithLineBreak && !hasLineBreakProjection;
                bool removeLineBreak = endsWithLineBreak && hasLineBreakProjection;

                int lineBreakProjectionSpansDelta = (insertLineBreak ? 1 : 0) - (removeLineBreak ? 1 : 0);
                int lineCountDelta = newLineBreaks + lineBreakProjectionSpansDelta;

                // Update line to projection span index mapping for all prompts following the output span.
                if (_window._promptLineMapping.Count > 0 && (lineCountDelta != 0 || lineBreakProjectionSpansDelta != 0))
                {
                    int i = _window._promptLineMapping.Count - 1;
                    while (i >= 0 && _window._promptLineMapping[i].Value > _currentOutputProjectionSpan)
                    {
                        _window._promptLineMapping[i] = new KeyValuePair<int, int>(
                            _window._promptLineMapping[i].Key + lineCountDelta,
                            _window._promptLineMapping[i].Value + lineBreakProjectionSpansDelta);

                        i--;
                    }
                }

                // do not use the mapping until projection span is updated below:
                _window._promptLineMapping.IsInconsistentWithProjections = removeLineBreak || insertLineBreak;

                // insert text to the subject buffer.
                // WARNING: Prompt line mapping needs to be updated before this edit is applied
                // since it might trigger events that use the mapping. 
                int oldBufferLength = _window._outputBuffer.CurrentSnapshot.Length;
                InsertOutput(output, oldBufferLength);

                // mapping becomes consistent as soon as projection spans are updated:
                _window._promptLineMapping.IsInconsistentWithProjections = false;

                if (removeLineBreak)
                {
                    RemoveProjectionSpans(lineBreakProjectionSpanIndex, 1);
                }
                else if (insertLineBreak)
                {
                    InsertProjectionSpan(lineBreakProjectionSpanIndex, _window._lineBreakOutputSpan);
                }

                // projection spans and prompts are in sync now:
                CheckPromptLineMappingConsistency(_currentOutputProjectionSpan);

                // caret didn't move since last time we moved it to track output:
                if (_outputTrackingCaretPosition == _window._textView.Caret.Position.BufferPosition)
                {
                    _window._textView.Caret.EnsureVisible();
                    _outputTrackingCaretPosition = _window._textView.Caret.Position.BufferPosition;
                }
            }

            /// <summary>
            /// Counts the number of line breaks in the text appended to the given snapshot.
            /// </summary>
            private static int CountOutputLineBreaks(IEnumerable<string> output, out bool endsWithLineBreak)
            {
                int result = 0;

                // note that we rely here upon the fact that previous snapshot doesn't end with '\r':
                bool lastWasCR = false;

                string lastStr = null;
                foreach (string str in output)
                {
                    foreach (char c in str)
                    {
                        if (c == '\r')
                        {
                            result++;
                            lastWasCR = true;
                        }
                        else if (c == '\n' && !lastWasCR)
                        {
                            // if the last characters was \r we don't count \n as a new line break

                            result++;
                            lastWasCR = false;
                        }
                        else
                        {
                            lastWasCR = false;
                        }
                    }

                    lastStr = str;
                }

                Debug.Assert(lastStr.Last() != '\r');
                endsWithLineBreak = lastStr.Last() == '\n';
                return result;
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

            private void CheckPromptLineMappingConsistency(int minAffectedSpan)
            {
                if (_window._promptLineMapping.Count > 0)
                {
                    int i = _window._promptLineMapping.Count - 1;
                    while (i >= 0 && _window._promptLineMapping[i].Value > minAffectedSpan)
                    {
                        Debug.Assert(
                            _window._projectionSpans[_window._promptLineMapping[i].Value].Kind == ReplSpanKind.Prompt ||
                            _window._projectionSpans[_window._promptLineMapping[i].Value].Kind == ReplSpanKind.StandardInputPrompt);

                        i--;
                    }
                }
            }

            private void FinishExecute(Task<ExecutionResult> result)
            {
                // The finished task has been replaced by another task (e.g. reset).
                // Do not perform any task finalization, it will be done by the replacement task.
                if (_currentTask != result)
                {
                    return;
                }

                _currentTask = null;
                ResetCursor();

                if (result.Exception != null || !result.Result.IsSuccessful)
                {
                    if (_window._history.Last != null)
                    {
                        _window._history.Last.Failed = true;
                    }
                }

                PrepareForInput();
            }

            public void ExecuteInput()
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

                    SubmitAsync();
                }
                else
                {
                    // append text of the target buffer to the current language buffer:
                    string text = TrimTrailingEmptyLines(languageBuffer.CurrentSnapshot);
                    _window._currentLanguageBuffer.Replace(new Span(_window._currentLanguageBuffer.CurrentSnapshot.Length, 0), text);
                    EditorOperations.MoveToEndOfDocument(false);
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
                int primaryPromptIndex;
                ReplSpan projectionSpan = _window.GetClosestPrecedingInputSpan(point, out primaryPromptIndex);
                if (projectionSpan == null || projectionSpan.Kind != ReplSpanKind.Language)
                {
                    return null;
                }

                var inputBuffer = projectionSpan.TrackingSpan.TextBuffer;
                var inputSnapshot = inputBuffer.CurrentSnapshot;

                var projectedSnapshot = _window._textView.BufferGraph.MapUpToBuffer(
                    new SnapshotSpan(inputSnapshot, 0, inputSnapshot.Length),
                    SpanTrackingMode.EdgePositive,
                    _window._projectionBuffer);

                Debug.Assert(projectedSnapshot.Count > 0);
                var projectedSnapshotStart = projectedSnapshot.First().Start;
                var projectedSnapshotEnd = projectedSnapshot.Last().End;

                if (point < projectedSnapshotStart.GetContainingLine().Start)
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
                if (point > projectedSnapshotEnd || (point == projectedSnapshotEnd && projectedSnapshotEnd.GetContainingLine().LineBreakLength != 0))
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

            public int IndexOfLastStandardInputSpan()
            {
                for (int i = _window._projectionSpans.Count - 1; i >= 0; i--)
                {
                    if (_window._projectionSpans[i].Kind == ReplSpanKind.StandardInput)
                    {
                        return i;
                    }
                }

                return -1;
            }

            public void RemoveLastInputPrompt()
            {
                var prompt = _window._projectionSpans[_window._projectionSpans.Count - SpansPerLineOfInput];
                Debug.Assert(prompt.Kind.IsPrompt());
                if (prompt.Kind == ReplSpanKind.Prompt || prompt.Kind == ReplSpanKind.StandardInputPrompt)
                {
                    _window._promptLineMapping.RemoveLast();
                }

                // projection buffer update must be the last operation as it might trigger event that accesses prompt line mapping:
                RemoveProjectionSpans(_window._projectionSpans.Count - SpansPerLineOfInput, SpansPerLineOfInput);
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
                ReplSpan promptSpan = _window.CreatePrimaryPrompt();
                ReplSpan languageSpan = new ReplSpan(_window.CreateLanguageTrackingSpan(new Span(0, 0)), ReplSpanKind.Language);

                // projection buffer update must be the last operation as it might trigger event that accesses prompt line mapping:
                _window.AppendProjectionSpans(promptSpan, languageSpan);
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
