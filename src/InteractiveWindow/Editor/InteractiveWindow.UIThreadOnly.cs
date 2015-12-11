// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.Utilities;
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
    internal partial class InteractiveWindow
    {
        private sealed partial class UIThreadOnly : IDisposable
        {
            private const int SpansPerLineOfInput = 2;

            private static readonly object SuppressPromptInjectionTag = new object();

            private readonly InteractiveWindow _window;

            private readonly IInteractiveWindowEditorFactoryService _factory;

            private readonly ITextBufferUndoManagerProvider _textBufferUndoManagerProvider;
            private ITextBufferUndoManager _undoManager;

            /// <summary>
            /// Returns `null` to indicate we don't record undo history for operations in standard-input buffer.
            /// This is exposed only for test purpose.
            /// </summary>
            public ITextUndoHistory UndoHistory => 
                ReadingStandardInput ? null :_undoManager.TextBufferUndoHistory;

            private readonly History _history = new History();
            private string _historySearch;

            private readonly ISmartIndentationService _smartIndenterService;

            // Pending submissions to be processed whenever the REPL is ready to accept submissions.
            private readonly Queue<PendingSubmission> _pendingSubmissions = new Queue<PendingSubmission>();

            private DispatcherTimer _executionTimer;
            private Cursor _oldCursor;
            private int _currentOutputProjectionSpan;
            private int _outputTrackingCaretPosition = -1;

            private readonly IRtfBuilderService2 _rtfBuilderService;

            // Read-only regions protecting initial span of the corresponding buffers:
            private readonly IReadOnlyRegion[] _standardInputProtection = new IReadOnlyRegion[2];
            private readonly IReadOnlyRegion[] _outputProtection = new IReadOnlyRegion[2];

            private string _uncommittedInput;

            /// <remarks>Always access through <see cref="GetStandardInputValue"/> and <see cref="SetStandardInputValue"/>.</remarks>
            private SnapshotSpan? _standardInputValue;
            /// <remarks>Don't reference directly.</remarks>
            private readonly SemaphoreSlim _standardInputValueGuard = new SemaphoreSlim(initialCount: 0, maxCount: 1);

            // State captured when we started reading standard input.
            private int _standardInputStart = -1;

            /// <remarks>Always access through <see cref="SessionStack"/>.</remarks>
            private IIntellisenseSessionStack _sessionStack; // TODO: remove
            private readonly IIntellisenseSessionStackMapService _intellisenseSessionStackMap;

            private bool _adornmentToMinimize;

            private readonly string _lineBreakString;

            private readonly IProjectionBuffer _projectionBuffer;
            private readonly IContentType _inertType;

            private readonly OutputBuffer _buffer;

            private readonly IWaitIndicator _waitIndicator;

            public readonly ITextBuffer OutputBuffer;
            public readonly ITextBuffer StandardInputBuffer;
            public ITextBuffer CurrentLanguageBuffer { get; private set; }

            public readonly TextWriter OutputWriter;
            public readonly InteractiveWindowWriter ErrorOutputWriter;

            // the language engine and content type of the active submission:
            public readonly IInteractiveEvaluator Evaluator;

            public readonly IWpfTextView TextView;

            /// <remarks>Always access through <see cref="EditorOperations"/>.</remarks>
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

            /// <remarks>Always access through <see cref="State"/>.</remarks>
            private State _state;
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

            public UIThreadOnly(
                InteractiveWindow window,
                IInteractiveWindowEditorFactoryService factory,
                IContentTypeRegistryService contentTypeRegistry,
                ITextBufferFactoryService bufferFactory,
                IProjectionBufferFactoryService projectionBufferFactory,
                IEditorOperationsFactoryService editorOperationsFactory,
                ITextBufferUndoManagerProvider textBufferUndoManagerProvider,
                ITextEditorFactoryService editorFactory,
                IRtfBuilderService rtfBuilderService,
                IIntellisenseSessionStackMapService intellisenseSessionStackMap,
                ISmartIndentationService smartIndenterService,
                IInteractiveEvaluator evaluator,
                IWaitIndicator waitIndicator)
            {
                _window = window;
                _factory = factory;
                _textBufferUndoManagerProvider = textBufferUndoManagerProvider;
                _rtfBuilderService = (IRtfBuilderService2)rtfBuilderService;
                _intellisenseSessionStackMap = intellisenseSessionStackMap;
                _smartIndenterService = smartIndenterService;
                _waitIndicator = waitIndicator;
                Evaluator = evaluator;

                var replContentType = contentTypeRegistry.GetContentType(PredefinedInteractiveContentTypes.InteractiveContentTypeName);
                var replOutputContentType = contentTypeRegistry.GetContentType(PredefinedInteractiveContentTypes.InteractiveOutputContentTypeName);

                OutputBuffer = bufferFactory.CreateTextBuffer(replOutputContentType);
                StandardInputBuffer = bufferFactory.CreateTextBuffer();
                _standardInputStart = 0;
                _inertType = bufferFactory.InertContentType;

                _projectionBuffer = projectionBufferFactory.CreateProjectionBuffer(
                    new EditResolver(window),
                    Array.Empty<object>(),
                    ProjectionBufferOptions.None,
                    replContentType);

                _projectionBuffer.Properties.AddProperty(typeof(InteractiveWindow), window);

                AppendNewOutputProjectionBuffer();
                _projectionBuffer.Changed += new EventHandler<TextContentChangedEventArgs>(ProjectionBufferChanged);

                var roleSet = editorFactory.CreateTextViewRoleSet(
                    PredefinedTextViewRoles.Analyzable,
                    PredefinedTextViewRoles.Editable,
                    PredefinedTextViewRoles.Interactive,
                    PredefinedTextViewRoles.Zoomable,
                    PredefinedInteractiveTextViewRoles.InteractiveTextViewRole);

                TextView = factory.CreateTextView(window, _projectionBuffer, roleSet);
                TextView.Caret.PositionChanged += CaretPositionChanged;

                var options = TextView.Options;
                options.SetOptionValue(DefaultTextViewHostOptions.HorizontalScrollBarId, true);
                options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, false);
                options.SetOptionValue(DefaultTextViewHostOptions.OutliningMarginId, false);
                options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, false);
                options.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, WordWrapStyles.None);

                _lineBreakString = options.GetNewLineCharacter();
                EditorOperations = editorOperationsFactory.GetEditorOperations(TextView);

                _buffer = new OutputBuffer(window);
                OutputWriter = new InteractiveWindowWriter(window, spans: null);

                SortedSpans errorSpans = new SortedSpans();
                ErrorOutputWriter = new InteractiveWindowWriter(window, errorSpans);
                OutputClassifierProvider.AttachToBuffer(OutputBuffer, errorSpans);
            }

            private bool ReadingStandardInput =>
                State == State.ExecutingInputAndReadingStandardInput ||
                State == State.WaitingForInputAndReadingStandardInput ||
                State == State.ResettingAndReadingStandardInput;

            /// <summary>Implements <see cref="IInteractiveWindowOperations.ResetAsync"/>.</summary>
            public async Task<ExecutionResult> ResetAsync(bool initialize)
            {
                try
                {
                    if (ReadingStandardInput)
                    {
                        MakeStandardInputReadonly();
                        CancelStandardInput();
                    }

                    _buffer.Flush();

                    // Nothing to clear in WaitingForInputAndReadingStandardInput, since we cleared
                    // the language prompt when we entered that state.
                    if (State == State.WaitingForInput)
                    {
                        var snapshot = _projectionBuffer.CurrentSnapshot;
                        var spanCount = snapshot.SpanCount;
                        Debug.Assert(GetSpanKind(snapshot.GetSourceSpan(spanCount - 1)) == ReplSpanKind.Input);
                        StoreUncommittedInput();
                        RemoveProjectionSpans(spanCount - 2, 2);
                        CurrentLanguageBuffer = null;
                    }

                    State = State.Resetting;
                    var executionResult = await Evaluator.ResetAsync(initialize).ConfigureAwait(true);
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

            /// <summary>Implements <see cref="IInteractiveWindow.Close"/>.</summary>
            public void Close()
            {
                TextView.Caret.PositionChanged -= CaretPositionChanged;
                TextView.Close();
            }

            /// <summary>Implements <see cref="IInteractiveWindowOperations.ClearHistory"/>.</summary>
            public void ClearHistory()
            {
                _history.Clear();
            }

            /// <summary>Implements <see cref="IInteractiveWindowOperations.ClearView"/>.</summary>
            public void ClearView()
            {
                if (ReadingStandardInput)
                {
                    CancelStandardInput();
                    State = GetStateBeforeReadingStandardInput(State);
                    Debug.Assert(State == State.ExecutingInput || State == State.WaitingForInput);
                }

                _adornmentToMinimize = false;
                InlineAdornmentProvider.RemoveAllAdornments(TextView);

                // remove all the spans except our initial span from the projection buffer
                _uncommittedInput = null;

                // We remove all spans from the projection buffer *before* updating the
                // subject buffers so that composite/merged changes to the projection
                // buffer don't have to be computed.  This seems to alleviate the problem
                // we were having with OutOfMemoryExceptions when clearing large output
                // buffers (which did not, themselves, trigger OOMs).
                RemoveProjectionSpans(0, _projectionBuffer.CurrentSnapshot.SpanCount);

                // Clear the projection and buffers last as this might trigger events that might access other state of the REPL window:
                RemoveProtection(OutputBuffer, _outputProtection);
                RemoveProtection(StandardInputBuffer, _standardInputProtection);

                using (var edit = OutputBuffer.CreateEdit(EditOptions.None, null, SuppressPromptInjectionTag))
                {
                    edit.Delete(0, OutputBuffer.CurrentSnapshot.Length);
                    edit.Apply();
                }

                _buffer.Reset();
                OutputClassifierProvider.ClearSpans(OutputBuffer);
                _outputTrackingCaretPosition = 0;

                using (var edit = StandardInputBuffer.CreateEdit(EditOptions.None, null, SuppressPromptInjectionTag))
                {
                    edit.Delete(0, StandardInputBuffer.CurrentSnapshot.Length);
                    edit.Apply();
                }

                // Insert an empty output buffer.
                // We do it for two reasons: 
                // 1) When output is written to asynchronously we need a buffer to store it.
                //    This may happen when clearing screen while background thread is writing to the console.
                // 2) We need at least one non-inert span due to bugs in projection buffer.
                AppendNewOutputProjectionBuffer();

                _history.ForgetOriginalBuffers();

                // If we were waiting for input, we need to restore the prompt that we just cleared.
                // If we are in any other state, then we'll let normal transitions trigger the next prompt.
                if (State == State.WaitingForInput)
                {
                    PrepareForInput();
                }
            }

            private static State GetStateBeforeReadingStandardInput(State state)
            {
                switch (state)
                {
                    case State.WaitingForInputAndReadingStandardInput:
                        return State.WaitingForInput;
                    case State.ExecutingInputAndReadingStandardInput:
                        return State.ExecutingInput;
                    case State.ResettingAndReadingStandardInput:
                        return State.Resetting;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(state);
                }
            }

            /// <summary>Implements the core of <see cref="IInteractiveWindow.ReadStandardInput"/>.</summary>
            public async Task<TextReader> ReadStandardInputAsync()
            {
                try
                {
                    switch (State)
                    {
                        case State.Starting:
                        case State.Initializing:
                            throw new InvalidOperationException(InteractiveWindowResources.NotInitialized);

                        case State.WaitingForInputAndReadingStandardInput:
                        case State.ExecutingInputAndReadingStandardInput:
                        case State.ResettingAndReadingStandardInput:
                            // Guarded by semaphore.
                            throw ExceptionUtilities.UnexpectedValue(State);

                        case State.WaitingForInput:
                            State = State.WaitingForInputAndReadingStandardInput;
                            break;
                        case State.ExecutingInput:
                            State = State.ExecutingInputAndReadingStandardInput;
                            break;
                        case State.Resetting:
                            State = State.ResettingAndReadingStandardInput;
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(State);
                    }

                    Debug.Assert(ReadingStandardInput);

                    // disable undo for current language buffer so undo/redo is greyed out when reading standard input
                    if (CurrentLanguageBuffer != null && CurrentLanguageBuffer.IsReadOnly(0))
                    {
                        _undoManager.UnregisterUndoHistory();
                        _textBufferUndoManagerProvider.RemoveTextBufferUndoManager(CurrentLanguageBuffer);
                    }

                    _buffer.Flush();

                    if (State == State.WaitingForInputAndReadingStandardInput)
                    {
                        var snapshot = _projectionBuffer.CurrentSnapshot;
                        var spanCount = snapshot.SpanCount;
                        if (spanCount > 0 && GetSpanKind(snapshot.GetSourceSpan(spanCount - 1)) == ReplSpanKind.Input)
                        {
                            // we need to remove our input prompt.
                            RemoveLastInputPrompt();
                        }
                    }

                    AddStandardInputSpan();

                    TextView.Caret.EnsureVisible();
                    ResetCursor();

                    _uncommittedInput = null;

                    var value = await GetStandardInputValue().ConfigureAwait(true);
                    Debug.Assert(_window.OnUIThread()); // ConfigureAwait should bring us back to the UI thread.

                    // set new start location after read is done.
                    _standardInputStart = StandardInputBuffer.CurrentSnapshot.Length;

                    return value.HasValue
                        ? new StringReader(value.GetValueOrDefault().GetText())
                        : null;
                }
                catch (Exception e) when (_window.ReportAndPropagateException(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private void CancelStandardInput()
            {
                SetStandardInputValue(null);
            }

            private void SetStandardInputValue(SnapshotSpan? value)
            {
                _standardInputValue = value;
                _standardInputValueGuard.Release();
            }

            private async Task<SnapshotSpan?> GetStandardInputValue()
            {
                try
                {
                    await _standardInputValueGuard.WaitAsync().ConfigureAwait(true);
                    Debug.Assert(_window.OnUIThread()); // ConfigureAwait should bring us back to the UI thread.
                    return _standardInputValue;
                }
                catch (Exception e) when (_window.ReportAndPropagateException(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private Span GetStandardInputSpan()
            {
                return Span.FromBounds(_standardInputStart, StandardInputBuffer.CurrentSnapshot.Length);
            }

            private void MakeStandardInputReadonly()
            {
                AppendLineNoPromptInjection(StandardInputBuffer);

                // We can also have an interleaving output span, so we'll search back for the last input span.
                var sourceSpans = _projectionBuffer.CurrentSnapshot.GetSourceSpans();

                int index = IndexOfLastStandardInputSpan(sourceSpans);
                Debug.Assert(index >= 0);

                RemoveProtection(StandardInputBuffer, _standardInputProtection);

                // replace previous span w/ a span that won't grow...
                var oldSpan = sourceSpans[index];
                var newSpan = new CustomTrackingSpan(oldSpan.Snapshot, oldSpan.Span);

                ReplaceProjectionSpan(index, newSpan);
                ApplyProtection(StandardInputBuffer, _standardInputProtection, allowAppend: true);
            }

            private void AppendLineNoPromptInjection(ITextBuffer buffer)
            {
                using (var edit = buffer.CreateEdit(EditOptions.None, null, SuppressPromptInjectionTag))
                {
                    edit.Insert(buffer.CurrentSnapshot.Length, _lineBreakString);
                    edit.Apply();
                }
            }

            /// <summary>Implements <see cref="IInteractiveWindowOperations2.TypeChar"/>.</summary>
            public void TypeChar(char typedChar)
            {
                using (var transaction = UndoHistory?.CreateTransaction(InteractiveWindowResources.TypeChar))
                {
                    if (transaction != null)
                    {
                        var mergeDirections = TextTransactionMergeDirections.Forward | TextTransactionMergeDirections.Backward;
                        // replacing selected text should be an atomic undo operation).
                        if ((!TextView.Selection.IsEmpty && !IsEmptyBoxSelection()))
                        {
                            mergeDirections = TextTransactionMergeDirections.Forward;
                        }
                        transaction.MergePolicy = new TextTransactionMergePolicy(mergeDirections);
                    }

                    if (InsertText(typedChar.ToString()))
                    {
                        transaction?.Complete();
                    }
                }
            }

            /// <summary>Implements <see cref="IInteractiveWindow.InsertCode"/>.</summary>
            public void InsertCode(string text)
            {
                if (ReadingStandardInput)
                {
                    return;
                }

                if (State == State.ExecutingInput)
                {
                    AppendUncommittedInput(text);
                }
                else
                {
                    InsertText(text);
                }
            }

            private bool InsertText(string text)
            {
                var selection = TextView.Selection;
                var caretPosition = TextView.Caret.Position.BufferPosition;
                if (!TextView.Selection.IsEmpty)
                {
                    if (!IsSelectionInsideCurrentSubmission())
                    {
                        return false;
                    }

                    DeleteSelection();

                    if (selection.Mode == TextSelectionMode.Box)
                    {
                        ReduceBoxSelectionToEditableBox(isDelete: true);
                    }
                    else
                    {
                        selection.Clear();
                        MoveCaretToClosestEditableBuffer();
                    }
                }
                else if (IsInActivePrompt(caretPosition))
                {
                    MoveCaretToClosestEditableBuffer();
                }
                else if (MapToEditableBuffer(caretPosition) == null)
                {
                    return false;
                }

                return EditorOperations.InsertText(text);
            }

            /// <summary>Implements the core of <see cref="IInteractiveWindow.SubmitAsync"/>.</summary>
            public void Submit(PendingSubmission[] pendingSubmissions)
            {
                if (!ReadingStandardInput)
                {
                    if (State == State.WaitingForInput && CurrentLanguageBuffer != null)
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
                if (_uncommittedInput == null)
                {
                    string activeCode = GetActiveCode();
                    if (!string.IsNullOrEmpty(activeCode))
                    {
                        _uncommittedInput = activeCode;
                    }
                }
            }

            /// <summary>
            /// Returns the full text of the current active input.
            /// </summary>
            private string GetActiveCode()
            {
                return CurrentLanguageBuffer.CurrentSnapshot.GetText();
            }

            private void PendSubmissions(IEnumerable<PendingSubmission> inputs)
            {
                foreach (var input in inputs)
                {
                    _pendingSubmissions.Enqueue(input);
                }
            }

            /// <summary>Implements <see cref="IInteractiveWindow.FlushOutput"/>.</summary>
            public void FlushOutput()
            {
                _buffer.Flush();
            }

            /// <summary>Implements <see cref="IInteractiveWindow.AddInput"/>.</summary>
            public void AddInput(string command)
            {
                // If the language buffer is readonly then input can not be added. Return immediately.
                // The language buffer gets marked as readonly in SubmitAsync method when input on the prompt 
                // gets submitted. So it would be readonly when the user types #reset on the prompt. In that 
                // case it is the right thing to bail out of this method.
                if (CurrentLanguageBuffer != null && CurrentLanguageBuffer.IsReadOnly(0))
                {
                    return;
                }

                if (State == State.ExecutingInput || CurrentLanguageBuffer == null)
                {
                    AddLanguageBuffer();
                    CurrentLanguageBuffer.Insert(0, command);
                }
                else
                {
                    StoreUncommittedInput();
                    SetActiveCode(command);
                }

                // Add command to history before calling FinishCurrentSubmissionInput as it adds newline 
                // to the end of the command.
                _history.Add(CurrentLanguageBuffer.CurrentSnapshot.GetExtent());
                FinishCurrentSubmissionInput();
            }

            private void AppendUncommittedInput(string text)
            {
                if (string.IsNullOrEmpty(text))
                {
                    // Do nothing.
                }
                else if (string.IsNullOrEmpty(_uncommittedInput))
                {
                    _uncommittedInput = text;
                }
                else
                {
                    _uncommittedInput += text;
                }
            }

            private void RestoreUncommittedInput()
            {
                if (_uncommittedInput != null)
                {
                    SetActiveCode(_uncommittedInput);
                    _uncommittedInput = null;
                }
            }

            private bool IsCaretOnBlankEditableLine()
            {
                var position = MapToEditableBuffer(TextView.Caret.Position.BufferPosition.GetContainingLine().EndIncludingLineBreak);
                // readonly line
                if (!position.HasValue)
                {
                    return false;
                }
                // non-blank line
                else if (IndexOfNonWhiteSpaceCharacter(position.GetValueOrDefault().GetContainingLine()) >= 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            private void MoveCaretToClosestEditableBuffer()
            {
                SnapshotPoint currentPosition = TextView.Caret.Position.BufferPosition;
                SnapshotPoint newPosition = GetClosestEditablePoint(currentPosition);
                if (currentPosition != newPosition)
                {
                    TextView.Caret.MoveTo(newPosition);
                }
            }

            /// <summary>
            /// Finds a point in an editable buffer that is the closest towards the end to the given projection point.
            /// </summary>
            private SnapshotPoint GetClosestEditablePoint(SnapshotPoint projectionPoint)
            {
                ITextBuffer editableBuffer = ReadingStandardInput ? StandardInputBuffer : CurrentLanguageBuffer;

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

                SnapshotPoint? lineEnd = TextView.BufferGraph.MapDownToBuffer(
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

                return TextView.BufferGraph.MapUpToBuffer(
                    editablePoint,
                    PointTrackingMode.Positive,
                    PositionAffinity.Successor,
                    _projectionBuffer).Value;
            }

            /// <summary>
            /// Appends given text to the last input span (standard input or active code input).
            /// </summary>
            private void AppendInput(string text)
            {
                var snapshot = _projectionBuffer.CurrentSnapshot;
                var spanCount = snapshot.SpanCount;
                var inputSpan = snapshot.GetSourceSpan(spanCount - 1);
                Debug.Assert(GetSpanKind(inputSpan) == ReplSpanKind.Input ||
                    GetSpanKind(inputSpan) == ReplSpanKind.StandardInput);

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
                    canAppend: true);
                ReplaceProjectionSpan(spanCount - 1, replSpan);

                TextView.Caret.EnsureVisible();
            }

            public void PrepareForInput()
            {
                _buffer.Flush();
                AddLanguageBuffer();
                State = State.WaitingForInput;

                // we are prepared for processing any postponed submissions there might have been:
                ProcessPendingSubmissions();
            }

            private void ProcessPendingSubmissions()
            {
                Debug.Assert(CurrentLanguageBuffer != null);

                if (_pendingSubmissions.Count == 0)
                {
                    RestoreUncommittedInput();

                    // move to the end (it might have been in virtual space):
                    TextView.Caret.MoveTo(GetLastLine(TextView.TextBuffer.CurrentSnapshot).End);
                    TextView.Caret.EnsureVisible();
                    _window.ReadyForInput?.Invoke();

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

            private async Task SubmitAsync()
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
                    var historySpan = CurrentLanguageBuffer.CurrentSnapshot.GetExtent();
                    FinishCurrentSubmissionInput();

                    _history.UncommittedInput = null;

                    var snapshotSpan = CurrentLanguageBuffer.CurrentSnapshot.GetExtent();
                    var trimmedSpan = snapshotSpan.TrimEnd();

                    if (trimmedSpan.Length == 0)
                    {
                        // TODO: reuse the current language buffer
                        PrepareForInput();
                        return;
                    }
                    else
                    {
                        _history.Add(historySpan);
                        State = State.ExecutingInput;

                        StartCursorTimer();

                        var executionResult = await Evaluator.ExecuteCodeAsync(snapshotSpan.GetText()).ConfigureAwait(true);
                        Debug.Assert(_window.OnUIThread()); // ConfigureAwait should bring us back to the UI thread.

                        // For reset command typed at prompt -> the state should be WaitingForInput. 
                        // For all other submissions on the prompt -> it should be Executing input.
                        // If reset button is clicked during a long running submission -> it could be Resetting because 
                        // oldService is disposed first as part of resetting, which leads to await call above returning, and new service is 
                        // created after that as part of completing the resetting process. 
                        Debug.Assert(State == State.ExecutingInput ||
                            State == State.WaitingForInput ||
                            State == State.Resetting, $"Unexpected state {State}");

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
                if (CurrentLanguageBuffer == null)
                {
                    Environment.FailFast("Language buffer not available");
                }
            }

            private void FinishCurrentSubmissionInput()
            {
                AppendLineNoPromptInjection(CurrentLanguageBuffer);
                ApplyProtection(CurrentLanguageBuffer, regions: null);

                if (_adornmentToMinimize)
                {
                    // TODO (tomat): remember the index of the adornment(s) in the current output and minimize those instead of the last one 
                    InlineAdornmentProvider.MinimizeLastInlineAdornment(TextView);
                    _adornmentToMinimize = false;
                }

                NewOutputBuffer();
            }

            /// <summary>
            /// Marks the entire buffer as read-only.
            /// </summary>
            private void ApplyProtection(ITextBuffer buffer, IReadOnlyRegion[] regions, bool allowAppend = false)
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
            private void RemoveProtection(ITextBuffer buffer, IReadOnlyRegion[] regions)
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

            private void NewOutputBuffer()
            {
                // Stop growing the current output projection span.
                var sourceSpan = _projectionBuffer.CurrentSnapshot.GetSourceSpan(_currentOutputProjectionSpan);
                Debug.Assert(GetSpanKind(sourceSpan) == ReplSpanKind.Output);
                var nonGrowingSpan = new CustomTrackingSpan(
                    sourceSpan.Snapshot,
                    sourceSpan.Span);
                ReplaceProjectionSpan(_currentOutputProjectionSpan, nonGrowingSpan);

                AppendNewOutputProjectionBuffer();
                _outputTrackingCaretPosition = TextView.Caret.Position.BufferPosition;
            }

            private void AppendNewOutputProjectionBuffer()
            {
                var currentSnapshot = OutputBuffer.CurrentSnapshot;
                var trackingSpan = new CustomTrackingSpan(
                    currentSnapshot,
                    new Span(currentSnapshot.Length, 0),
                    canAppend: true);

                _currentOutputProjectionSpan = AppendProjectionSpan(trackingSpan);
            }

            private int AppendProjectionSpan(ITrackingSpan span)
            {
                int index = _projectionBuffer.CurrentSnapshot.SpanCount;
                InsertProjectionSpan(index, span);
                return index;
            }

            private void InsertProjectionSpan(int index, object span)
            {
                _projectionBuffer.ReplaceSpans(index, 0, new[] { span }, EditOptions.None, editTag: SuppressPromptInjectionTag);
            }

            private void ReplaceProjectionSpan(int spanToReplace, ITrackingSpan newSpan)
            {
                _projectionBuffer.ReplaceSpans(spanToReplace, 1, new[] { newSpan }, EditOptions.None, editTag: SuppressPromptInjectionTag);
            }

            private void RemoveProjectionSpans(int index, int count)
            {
                _projectionBuffer.ReplaceSpans(index, count, Array.Empty<object>(), EditOptions.None, SuppressPromptInjectionTag);
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
                Debug.Assert(!OutputBuffer.CurrentSnapshot.EndsWith('\r'));

                var projectionSpans = _projectionBuffer.CurrentSnapshot.GetSourceSpans();
                Debug.Assert(GetSpanKind(projectionSpans[_currentOutputProjectionSpan]) == ReplSpanKind.Output);

                int lineBreakProjectionSpanIndex = _currentOutputProjectionSpan + 1;

                // insert line break projection span if there is none and the output doesn't end with a line break:
                bool hasLineBreakProjection = false;
                if (lineBreakProjectionSpanIndex < projectionSpans.Count)
                {
                    var oldSpan = projectionSpans[lineBreakProjectionSpanIndex];
                    hasLineBreakProjection = GetSpanKind(oldSpan) == ReplSpanKind.LineBreak;
                }

                Debug.Assert(output.Last().Last() != '\r');
                bool endsWithLineBreak = output.Last().Last() == '\n';

                // insert text to the subject buffer.
                int oldBufferLength = OutputBuffer.CurrentSnapshot.Length;
                InsertOutput(output, oldBufferLength);

                if (endsWithLineBreak && hasLineBreakProjection)
                {
                    // Remove line break.
                    RemoveProjectionSpans(lineBreakProjectionSpanIndex, 1);
                }
                else if (!endsWithLineBreak && !hasLineBreakProjection)
                {
                    // Insert line break.
                    InsertProjectionSpan(lineBreakProjectionSpanIndex, _lineBreakString);
                }

                // caret didn't move since last time we moved it to track output:
                if (_outputTrackingCaretPosition == TextView.Caret.Position.BufferPosition)
                {
                    TextView.Caret.EnsureVisible();
                    _outputTrackingCaretPosition = TextView.Caret.Position.BufferPosition;
                }
            }

            private void InsertOutput(IEnumerable<string> output, int position)
            {
                RemoveProtection(OutputBuffer, _outputProtection);

                // append the text to output buffer and make sure it ends with a line break:
                using (var edit = OutputBuffer.CreateEdit(EditOptions.None, null, SuppressPromptInjectionTag))
                {
                    foreach (string text in output)
                    {
                        edit.Insert(position, text);
                    }

                    edit.Apply();
                }

                ApplyProtection(OutputBuffer, _outputProtection);
            }

            private void FinishExecute(bool succeeded)
            {
                ResetCursor();

                if (!succeeded && _history.Last != null)
                {
                    _history.Last.Failed = true;
                }

                PrepareForInput();
            }

            /// <summary>Implements <see cref="IInteractiveWindowOperations.ExecuteInput"/>.</summary>
            public async Task ExecuteInputAsync()
            {
                try
                {
                    ITextBuffer languageBuffer = GetLanguageBuffer(TextView.Caret.Position.BufferPosition);
                    if (languageBuffer == null)
                    {
                        return;
                    }

                    if (languageBuffer == CurrentLanguageBuffer)
                    {
                        // TODO (tomat): this should rather send an abstract "finish" command that various features
                        // can implement as needed (IntelliSense, inline rename would commit, etc.).
                        // For now, commit IntelliSense:
                        var completionSession = SessionStack.TopSession as ICompletionSession;
                        if (completionSession != null)
                        {
                            completionSession.Commit();
                        }

                        await SubmitAsync().ConfigureAwait(true);
                        Debug.Assert(_window.OnUIThread()); // ConfigureAwait should bring us back to the UI thread.
                    }
                    else
                    {
                        // append text of the target buffer to the current language buffer:
                        string text = TrimTrailingEmptyLines(languageBuffer.CurrentSnapshot);
                        CurrentLanguageBuffer.Replace(new Span(CurrentLanguageBuffer.CurrentSnapshot.Length, 0), text);
                        EditorOperations.MoveToEndOfDocument(false);
                    }
                }
                catch (Exception e) when (_window.ReportAndPropagateException(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private IIntellisenseSessionStack SessionStack
            {
                get
                {
                    if (_sessionStack == null)
                    {
                        _sessionStack = _intellisenseSessionStackMap.GetStackForTextView(TextView);
                    }

                    return _sessionStack;
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
                int promptIndex = GetPromptIndexForPoint(sourceSpans, point);
                if (promptIndex < 0)
                {
                    return null;
                }

                // Grab the span following the prompt (either language or standard input).
                var projectionSpan = sourceSpans[promptIndex + 1];
                var kind = GetSpanKind(projectionSpan);
                if (kind != ReplSpanKind.Input)
                {
                    Debug.Assert(kind == ReplSpanKind.StandardInput);
                    return null;
                }

                var inputSnapshot = projectionSpan.Snapshot;
                var inputBuffer = inputSnapshot.TextBuffer;

                var projectedSpans = TextView.BufferGraph.MapUpToBuffer(
                    new SnapshotSpan(inputSnapshot, 0, inputSnapshot.Length),
                    SpanTrackingMode.EdgePositive,
                    _projectionBuffer);

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
                if (inputBuffer == CurrentLanguageBuffer)
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

            private int GetPromptIndexForPoint(ReadOnlyCollection<SnapshotSpan> sourceSpans, SnapshotPoint point)
            {
                int index = GetSourceSpanIndex(sourceSpans, point);
                if (index == sourceSpans.Count)
                {
                    index--;
                }
                // Find the nearest preceding prompt.
                while (index >= 0 && !IsPrompt(sourceSpans[index]))
                {
                    index--;
                }
                return index;
            }

            private bool IsInActivePrompt(SnapshotPoint point)
            {
                var editableBuffer = ReadingStandardInput ? StandardInputBuffer : CurrentLanguageBuffer;
                if (editableBuffer == null)
                {
                    return false;
                }

                var sourceSpans = GetSourceSpans(point.Snapshot);
                var index = GetSourceSpanIndex(sourceSpans, point);
                if (index == sourceSpans.Count)
                {
                    index--;
                }

                if (!IsPrompt(sourceSpans[index]))
                {
                    return false;
                }

                Debug.Assert(index + 1 < sourceSpans.Count);
                var followingSpan = sourceSpans[index + 1];
                // if the following span is editable, then the prompt is active.
                return GetPositionInBuffer(followingSpan.Start, editableBuffer) != null;
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
                    int value = CompareToSpan(TextView, sourceSpans, mid, point);
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

            private static ReadOnlyCollection<SnapshotSpan> GetSourceSpans(ITextSnapshot snapshot)
            {
                return ((IProjectionSnapshot)snapshot).GetSourceSpans();
            }

            private bool IsPrompt(SnapshotSpan span)
            {
                return GetSpanKind(span) == ReplSpanKind.Prompt;
            }

            private void ResetCursor()
            {
                if (_executionTimer != null)
                {
                    _executionTimer.Stop();
                }

                if (_oldCursor != null)
                {
                    ((ContentControl)TextView).Cursor = _oldCursor;
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
                var view = (ContentControl)TextView;

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

            private int IndexOfLastStandardInputSpan(ReadOnlyCollection<SnapshotSpan> sourceSpans)
            {
                for (int i = sourceSpans.Count - 1; i >= 0; i--)
                {
                    if (GetSpanKind(sourceSpans[i]) == ReplSpanKind.StandardInput)
                    {
                        return i;
                    }
                }

                return -1;
            }

            private void RemoveLastInputPrompt()
            {
                var snapshot = _projectionBuffer.CurrentSnapshot;
                var spanCount = snapshot.SpanCount;
                Debug.Assert(IsPrompt(snapshot.GetSourceSpan(spanCount - SpansPerLineOfInput)));

                // projection buffer update must be the last operation as it might trigger event that accesses prompt line mapping:
                RemoveProjectionSpans(spanCount - SpansPerLineOfInput, SpansPerLineOfInput);
            }

            /// <summary>
            /// Creates and adds a new language buffer to the projection buffer.
            /// </summary>
            private void AddLanguageBuffer()
            {
                ITextBuffer buffer = _factory.CreateAndActivateBuffer(_window);

                if (CurrentLanguageBuffer != null)
                {
                    _undoManager.UnregisterUndoHistory();
                    _textBufferUndoManagerProvider.RemoveTextBufferUndoManager(CurrentLanguageBuffer);
                }
                _undoManager = _textBufferUndoManagerProvider.GetTextBufferUndoManager(buffer);

                buffer.Properties.AddProperty(typeof(IInteractiveEvaluator), Evaluator);
                buffer.Properties.AddProperty(typeof(InteractiveWindow), _window);

                CurrentLanguageBuffer = buffer;
                _window.SubmissionBufferAdded?.Invoke(_window, new SubmissionBufferAddedEventArgs(buffer));

                _window.LanguageBufferCounter++;

                // add the whole buffer to the projection buffer and set it up to expand to the right as text is appended
                var promptSpan = CreatePrimaryPrompt();
                var languageSpan = new CustomTrackingSpan(
                    CurrentLanguageBuffer.CurrentSnapshot,
                    new Span(0, 0),
                    canAppend: true);

                // projection buffer update must be the last operation as it might trigger event that accesses prompt line mapping:
                AppendProjectionSpans(promptSpan, languageSpan);
            }

            private void AppendProjectionSpans(object span1, object span2)
            {
                int index = _projectionBuffer.CurrentSnapshot.SpanCount;
                _projectionBuffer.ReplaceSpans(index, 0, new[] { span1, span2 }, EditOptions.None, editTag: SuppressPromptInjectionTag);
            }

            private bool TryGetCurrentLanguageBufferExtent(IProjectionSnapshot projectionSnapshot, out Span result)
            {
                if (projectionSnapshot.SpanCount == 0)
                {
                    result = default(Span);
                    return false;
                }

                // the last source snapshot is always a projection of a language buffer:
                var snapshot = projectionSnapshot.GetSourceSpan(projectionSnapshot.SpanCount - 1).Snapshot;
                if (snapshot.TextBuffer != CurrentLanguageBuffer)
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
                if (e.EditTag == SuppressPromptInjectionTag)
                {
                    return;
                }

                // projection buffer is changed before language buffer is created (for example, output might be printed out during initialization):
                if (CurrentLanguageBuffer == null)
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
                Debug.Assert(GetSpanKind(surfaceSnapshot.GetSourceSpan(result)) == ReplSpanKind.Input);
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

                _projectionBuffer.ReplaceSpans(start, end - start, replacement, EditOptions.None, SuppressPromptInjectionTag);
            }

            private object CreateTrackingSpan(SnapshotSpan snapshotSpan)
            {
                var snapshot = snapshotSpan.Snapshot;
                if (snapshot.ContentType == _inertType)
                {
                    return snapshotSpan.GetText();
                }
                return new CustomTrackingSpan(snapshot, snapshotSpan.Span);
            }

            private ITrackingSpan CreateLanguageSpanForLine(ITextSnapshotLine languageLine)
            {
                var span = languageLine.ExtentIncludingLineBreak;
                bool lastLine = (languageLine.LineNumber == languageLine.Snapshot.LineCount - 1);
                return new CustomTrackingSpan(
                    CurrentLanguageBuffer.CurrentSnapshot,
                    span,
                    canAppend: lastLine);
            }

            private void ScrollToCaret()
            {
                var textView = TextView;
                var caretPosition = textView.Caret.Position.BufferPosition;
                var caretSpan = new SnapshotSpan(caretPosition.Snapshot, caretPosition, 0);
                textView.ViewScroller.EnsureSpanVisible(caretSpan);
            }

            private void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
            {
                // make sure language buffer exist
                if (CurrentLanguageBuffer == null)
                {
                    return;
                }

                var caretPoint = e.NewPosition.BufferPosition;

                // make sure caret is on the right line
                // 1. changes are on virtual space
                if (e.NewPosition.BufferPosition == e.OldPosition.BufferPosition)
                {
                    return;
                }

                // 2. caret is at the end of the surface line
                if (caretPoint != caretPoint.GetContainingLine().End)
                {
                    return;
                }

                // 3. subject line has length == 0
                var point = e.NewPosition.Point.GetInsertionPoint(b => b == CurrentLanguageBuffer);
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
                    TextView.Caret.PositionChanged -= CaretPositionChanged;

                    IndentCurrentLine(caretPoint);
                }
                finally
                {
                    // attach event handler
                    TextView.Caret.PositionChanged += CaretPositionChanged;
                }
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
                Debug.Assert(CurrentLanguageBuffer != null);

                var caretLine = caretPosition.GetContainingLine();
                var indentation = _smartIndenterService.GetDesiredIndentation(TextView, caretLine);

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
                        TextView.Caret.MoveTo(new VirtualSnapshotPoint(caretPosition, adjustedIndentationValue));
                    }
                    else
                    {
                        var langCaret = GetPositionInLanguageBuffer(caretPosition);
                        if (langCaret == null)
                        {
                            return;
                        }

                        // insert whitespace indentation:
                        var options = TextView.Options;
                        string whitespace = GetWhiteSpaceForVirtualSpace(adjustedIndentationValue, options.IsConvertTabsToSpacesEnabled() ? default(int?) : options.GetTabSize());
                        CurrentLanguageBuffer.Insert(langCaret.Value, whitespace);
                    }
                }
            }

            private SnapshotPoint? GetPositionInLanguageBuffer(SnapshotPoint point)
            {
                Debug.Assert(CurrentLanguageBuffer != null);
                return GetPositionInBuffer(point, CurrentLanguageBuffer);
            }

            private SnapshotPoint? GetPositionInStandardInputBuffer(SnapshotPoint point)
            {
                Debug.Assert(StandardInputBuffer != null);
                return GetPositionInBuffer(point, StandardInputBuffer);
            }

            private SnapshotPoint? GetPositionInBuffer(SnapshotPoint point, ITextBuffer buffer)
            {
                return TextView.BufferGraph.MapDownToBuffer(
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

            /// <summary>Implements <see cref="IInteractiveWindowOperations.Cancel"/>.</summary>
            public void Cancel()
            {
                ClearInput();
                EditorOperations.MoveToEndOfDocument(false);
                _uncommittedInput = null;
                _historySearch = null;
            }

            private void ClearInput()
            {
                if (ReadingStandardInput)
                {
                    StandardInputBuffer.Delete(GetStandardInputSpan());
                }
                else
                {
                    CurrentLanguageBuffer.Delete(new Span(0, CurrentLanguageBuffer.CurrentSnapshot.Length));
                }
            }

            /// <summary>Implements <see cref="IInteractiveWindowOperations.HistoryPrevious"/>.</summary>
            public void HistoryPrevious(string search)
            {
                if (CurrentLanguageBuffer == null)
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
                    EditorOperations.MoveToEndOfDocument(false);
                }
            }

            /// <summary>Implements <see cref="IInteractiveWindowOperations.HistoryNext"/>.</summary>
            public void HistoryNext(string search)
            {
                if (CurrentLanguageBuffer == null)
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
                    EditorOperations.MoveToEndOfDocument(false);
                }
                else
                {
                    string code = _history.UncommittedInput;
                    _history.UncommittedInput = null;
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
                var completionSession = SessionStack.TopSession;
                if (completionSession != null)
                {
                    completionSession.Dismiss();
                }

                using (var edit = CurrentLanguageBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, editTag: null))
                {
                    edit.Replace(new Span(0, CurrentLanguageBuffer.CurrentSnapshot.Length), text);
                    edit.Apply();
                }
            }

            /// <summary>Implements <see cref="IInteractiveWindowOperations.HistorySearchNext"/>.</summary>
            public void HistorySearchNext()
            {
                EnsureHistorySearch();
                HistoryNext(_historySearch);
            }

            /// <summary>Implements <see cref="IInteractiveWindowOperations.HistorySearchPrevious"/>.</summary>
            public void HistorySearchPrevious()
            {
                EnsureHistorySearch();
                HistoryPrevious(_historySearch);
            }

            private void EnsureHistorySearch()
            {
                if (_historySearch == null)
                {
                    _historySearch = CurrentLanguageBuffer.CurrentSnapshot.GetText();
                }
            }

            private void StoreUncommittedInputForHistory()
            {
                if (_history.UncommittedInput == null)
                {
                    string activeCode = GetActiveCode();
                    // save uncommitted input for history even if it is empty else
                    // on the next history navigation the previous history entry would 
                    // be saved as uncommitted input, which we do not want. Uncommitted 
                    // input is to save what ever user has typed and storing empty string
                    // when he hasn't typed anything does no harm.
                    _history.UncommittedInput = activeCode;
                }
            }

            /// <summary>
            /// Maps projectionBufferPoint down to first matching source buffer. 
            /// </summary>
            /// <param name="projectionBufferPoint">Must be a point in projectin buffer</param> 
            private SnapshotPoint GetSourceBufferPoint(SnapshotPoint projectionBufferPoint)
            {
                Debug.Assert(projectionBufferPoint.Snapshot.TextBuffer == _projectionBuffer);

                return TextView.BufferGraph.MapDownToFirstMatch(
                    projectionBufferPoint,
                    PointTrackingMode.Positive,
                    snapshot => snapshot.TextBuffer != _projectionBuffer,
                    PositionAffinity.Successor).Value;
            }

            /// <summary>
            /// Maps sourceBufferPoint up to projection buffer.
            /// </summary>               
            private SnapshotPoint GetProjectionBufferPoint(SnapshotPoint sourceBufferPoint)
            {
                Debug.Assert(sourceBufferPoint.Snapshot.TextBuffer != _projectionBuffer);

                return TextView.BufferGraph.MapUpToBuffer(
                    sourceBufferPoint,
                    PointTrackingMode.Positive,
                    PositionAffinity.Successor,
                    _projectionBuffer).Value;
            }

            /// <summary>
            /// Moves to the beginning of the line.
            /// Implements <see cref="IInteractiveWindowOperations.Home"/>.
            /// </summary>
            public void Home(bool extendSelection)
            {
                var caret = TextView.Caret;

                // map the end of subject buffer line:
                var subjectLineEnd = TextView.BufferGraph.MapDownToFirstMatch(
                    caret.Position.BufferPosition.GetContainingLine().End,
                    PointTrackingMode.Positive,
                    snapshot => snapshot.TextBuffer != _projectionBuffer,
                    PositionAffinity.Successor).Value;

                ITextSnapshotLine subjectLine = subjectLineEnd.GetContainingLine();

                var projectedSubjectLineStart = TextView.BufferGraph.MapUpToBuffer(
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
                    VirtualSnapshotPoint anchor = TextView.Selection.AnchorPoint;
                    caret.MoveTo(moveTo);
                    TextView.Selection.Select(anchor.TranslateTo(TextView.TextSnapshot), TextView.Caret.Position.VirtualBufferPosition);
                }
                else
                {
                    TextView.Selection.Clear();
                    caret.MoveTo(moveTo);
                }
                caret.EnsureVisible();
            }

            /// <summary>
            /// Moves to the end of the line.
            /// Implements <see cref="IInteractiveWindowOperations.End"/>.
            /// </summary>
            public void End(bool extendSelection)
            {
                var caret = TextView.Caret;

                // map the end of the subject buffer line:
                var subjectLineEnd = TextView.BufferGraph.MapDownToFirstMatch(
                    caret.Position.BufferPosition.GetContainingLine().End,
                    PointTrackingMode.Positive,
                    snapshot => snapshot.TextBuffer != _projectionBuffer,
                    PositionAffinity.Successor).Value;

                ITextSnapshotLine subjectLine = subjectLineEnd.GetContainingLine();

                var moveTo = TextView.BufferGraph.MapUpToBuffer(
                    subjectLine.End,
                    PointTrackingMode.Positive,
                    PositionAffinity.Successor,
                    _projectionBuffer).Value;

                if (extendSelection)
                {
                    VirtualSnapshotPoint anchor = TextView.Selection.AnchorPoint;
                    caret.MoveTo(moveTo);
                    TextView.Selection.Select(anchor.TranslateTo(TextView.TextSnapshot), TextView.Caret.Position.VirtualBufferPosition);
                }
                else
                {
                    TextView.Selection.Clear();
                    caret.MoveTo(moveTo);
                }
                caret.EnsureVisible();
            }

            /// <summary>Implements <see cref="IInteractiveWindowOperations.SelectAll"/>.</summary>
            public void SelectAll()
            {
                SnapshotSpan? span = GetContainingRegion(TextView.Caret.Position.BufferPosition);

                var selection = TextView.Selection;

                // if the span is already selected select all text in the projection buffer:
                if (span == null || 
                    !selection.IsEmpty && selection.SelectedSpans.Count == 1 && selection.SelectedSpans[0] == span.Value)
                {
                    var currentSnapshot = TextView.TextBuffer.CurrentSnapshot;
                    span = new SnapshotSpan(currentSnapshot, new Span(0, currentSnapshot.Length));
                }

                selection.Select(span.Value, isReversed: false);
                // SelectAll always returns stream selection
                selection.Mode = TextSelectionMode.Stream;
            }

            /// <summary>
            /// Given a point in projection buffer calculate a span that includes the point and comprises of 
            /// subsequent projection spans forming a region, i.e. a sequence of output spans in between two subsequent submissions,
            /// a language input block, or standard input block.
            /// </summary>
            private SnapshotSpan? GetContainingRegion(SnapshotPoint point)
            {
                var sourceSpans = GetSourceSpans(point.Snapshot);
                int promptIndex = GetPromptIndexForPoint(sourceSpans, point);
                if (promptIndex < 0)
                {
                    return null;
                }

                // Grab the span following the prompt (either language or standard input).
                var projectionSpan = sourceSpans[promptIndex + 1];
                var inputSnapshot = projectionSpan.Snapshot;
                var kind = GetSpanKind(projectionSpan);

                Debug.Assert(kind == ReplSpanKind.Input || kind == ReplSpanKind.StandardInput);

                // Language input block is a projection of the entire snapshot;
                // std input block is a projection of a single span:
                SnapshotPoint inputBufferEnd = (kind == ReplSpanKind.Input) ?
                    new SnapshotPoint(inputSnapshot, inputSnapshot.Length) :
                    projectionSpan.End;

                var bufferGraph = TextView.BufferGraph;
                var textBuffer = TextView.TextBuffer;

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
                    if (IsPrompt(sourceSpans[i]))
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
                Debug.Assert(GetSpanKind(lastSpanBeforeNextPrompt) == ReplSpanKind.Output);

                // select all text in between the language buffer and the next prompt:
                return new SnapshotSpan(
                    projectedInputBufferEnd,
                    bufferGraph.MapUpToBuffer(
                        lastSpanBeforeNextPrompt.End,
                        PointTrackingMode.Positive,
                        PositionAffinity.Predecessor,
                        textBuffer).Value);
            }

            private bool OverlapsWithEditableBuffer(NormalizedSnapshotSpanCollection spans)
            {
                var editableBuffer = (ReadingStandardInput) ? StandardInputBuffer : CurrentLanguageBuffer;

                foreach (var span in spans)
                {
                    var editableSpans = TextView.BufferGraph.MapDownToBuffer(span, SpanTrackingMode.EdgeInclusive, editableBuffer);
                    if (editableSpans.Count > 0)
                    {
                        return true;
                    }
                }
                return false;
            }

            private bool IsEmptyBoxSelection()
            {
                return !TextView.Selection.IsEmpty &&
                        TextView.Selection.VirtualSelectedSpans.All(s => s.IsEmpty);
            }

            private bool ReduceBoxSelectionToEditableBox(bool isDelete = true)
            {
                Debug.Assert(TextView.Selection.Mode == TextSelectionMode.Box);

                VirtualSnapshotPoint anchor = TextView.Selection.AnchorPoint;
                VirtualSnapshotPoint active = TextView.Selection.ActivePoint;

                bool result;
                if (active < anchor)
                {
                    result = ReduceBoxSelectionToEditableBox(ref active, ref anchor, isDelete);
                }
                else
                {
                    result = ReduceBoxSelectionToEditableBox(ref anchor, ref active, isDelete);
                }

                TextView.Selection.Select(anchor, active);
                TextView.Caret.MoveTo(active);

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
                if (ReadingStandardInput)
                {
                    minPromptLength = maxPromptLength = 0;
                }
                else
                {
                    MeasurePrompts(editableLine.LineNumber, selectionBottomLine.LineNumber + 1, out minPromptLength, out maxPromptLength);
                }

                bool result = true;
                if (isDelete)
                {
                    if (selectionLeftColumn > maxPromptLength || maxPromptLength == minPromptLength)
                    {
                        selectionTopLine = editableLine;
                        selectionLeftColumn = Math.Max(selectionLeftColumn, maxPromptLength);
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

                var projectionSnapshot = _projectionBuffer.CurrentSnapshot;
                var sourceSpans = projectionSnapshot.GetSourceSpans();
                var promptSpanIndex = GetProjectionSpanIndexFromEditableBufferPosition(projectionSnapshot, sourceSpans.Count, startLine) - 1;
                var promptSpan = sourceSpans[promptSpanIndex];
                Debug.Assert(IsPrompt(promptSpan));

                minPromptLength = maxPromptLength = promptSpan.Length;
            }

            /// <summary>Implements <see cref="IInteractiveWindowOperations.Backspace"/>.</summary>
            public bool Backspace()
            {
                using (var transaction = UndoHistory?.CreateTransaction(InteractiveWindowResources.Backspace))
                {

                    if (DeleteHelper(isBackspace: true))
                    {
                        transaction?.Complete();
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Implements <see cref="IInteractiveWindowOperations.Delete"/>.
            /// `Delete` will not delete anything if any part of the selection is not in 
            /// current submission (input or active prompts).
            /// </summary>
            public bool Delete()
            {
                _historySearch = null;
                using (var transaction = UndoHistory?.CreateTransaction(InteractiveWindowResources.Delete))
                {
                    if (DeleteHelper(isBackspace: false))
                    {
                        transaction?.Complete();
                        return true;
                    }
                }
                return false;
            }

            private bool DeleteHelper(bool isBackspace)
            {
                var selection = TextView.Selection;

                if (!selection.IsEmpty)
                {
                    // do not delete anything if any part of selection is not in current submission
                    if (!IsSelectionInsideCurrentSubmission())
                    {
                        return false;
                    }

                    if (IsEmptyBoxSelection())
                    {
                        return isBackspace ? EditorOperations.Backspace() : EditorOperations.Delete();                        
                    }

                    DeleteSelection();

                    if (selection.Mode == TextSelectionMode.Box)
                    {
                        ReduceBoxSelectionToEditableBox(isDelete: true);
                    }
                    else
                    {
                        selection.Clear();
                        MoveCaretToClosestEditableBuffer();
                    }
                    TextView.Caret.EnsureVisible();
                    return true;
                }
                else if (TextView.Caret.Position.VirtualSpaces == 0)
                {
                    if (IsInActivePrompt(TextView.Caret.Position.BufferPosition))
                    {
                        MoveCaretToClosestEditableBuffer();
                    }
                    return isBackspace ? DeletePreviousCharacter() : DeleteNextCharacter();
                }
                else
                {
                    if (isBackspace)
                    {
                        TextView.Caret.MoveToPreviousCaretPosition();
                    }
                    else
                    {
                        TextView.Caret.MoveToNextCaretPosition();
                    }
                    return true;
                }
            }

            /// <summary>Implements <see cref="IInteractiveWindowOperations2.DeleteLine"/>.</summary>
            public void DeleteLine()
            {
                _historySearch = null;
                CutLineOrDeleteLineHelper(isCut: false);
            }

            /// <summary>Implements <see cref="IInteractiveWindowOperations2.CutLine"/>.</summary>
            public void CutLine()
            {
                _historySearch = null;
                CutLineOrDeleteLineHelper(isCut: true);
            }

            /// <summary>Cut/Delete all selected lines, or the current line if no selection. </summary>                  
            private void CutLineOrDeleteLineHelper(bool isCut)
            {
                using (var transaction = UndoHistory?.CreateTransaction(isCut ? InteractiveWindowResources.CutLine : InteractiveWindowResources.DeleteLine))
                {
                    if (TextView.Selection.IsEmpty)
                    {
                        if (isCut)
                        {
                            CopyCurrentLine();
                        }
                        if (!DeleteCurrentLine())
                        {
                            return;
                        }
                        TextView.Caret.MoveTo(TextView.Caret.Position.BufferPosition.GetContainingLine().Start);
                    }
                    else
                    {
                        var projectionSpans = CopySelectedLines();
                        if (!IsSpanCollectionInsideCurrentSubmission(projectionSpans))
                        {
                            return;
                        }
                        DeleteSpans(projectionSpans);
                        TextView.Selection.Clear();
                    }
                    MoveCaretToClosestEditableBuffer();
                    TextView.Caret.EnsureVisible();

                    transaction?.Complete();
                }
            }
            
            /// <summary>
            /// Returns true if the entire selection is inside current submission.
            /// Current submission includes all active prompt buffers and all editable buffers
            /// </summary>
            private bool IsSelectionInsideCurrentSubmission()
            {
                var selection = TextView.Selection;
                Debug.Assert(!selection.IsEmpty);
                return IsSpanCollectionInsideCurrentSubmission(selection.SelectedSpans);
            }

            private bool IsSpanCollectionInsideCurrentSubmission(NormalizedSnapshotSpanCollection spans)
            {
                foreach (var span in spans)
                {
                    var currentLine = span.Start.GetContainingLine();
                    var end = currentLine.End;
                    if (MapToEditableBuffer(end) == null)
                    {
                        return false;
                    }
                }
                return true;
            }

            private bool DeleteSelection()
            {
                var selection = TextView.Selection;

                if (selection.IsEmpty || !IsSelectionInsideCurrentSubmission())
                {
                    return false;
                }

                DeleteSpans(selection.SelectedSpans);
                return true;
            }

            /// <summary>
            /// Delete the line where the caret is located, if the line is a part of current submission.
            /// </summary>
            private bool DeleteCurrentLine()
            {
                Debug.Assert(TextView.Selection.IsEmpty);
                var line = TextView.Caret.Position.BufferPosition.GetContainingLine();
                // the caret is located in a line with only readonly content 
                // (i.e. output line or previously submitted input line)
                if (MapToEditableBuffer(line.End) == null)
                {
                    return false;
                }

                DeleteSpans(new NormalizedSnapshotSpanCollection(line.ExtentIncludingLineBreak));
                return true;
            }

            /// <summary>
            /// Delete spans that lie in editable buffer from given spans.
            /// This method keeps selection and caret position intact,
            /// therefore it's caller's responsibility to adjust them accordingly.
            /// </summary>
            private void DeleteSpans(NormalizedSnapshotSpanCollection projectionSpans)
            {
                // split into multiple deletes that only affect the language/input buffer:
                ITextBuffer affectedBuffer = (ReadingStandardInput) ? StandardInputBuffer : CurrentLanguageBuffer;
                using (var edit = affectedBuffer.CreateEdit())
                {
                    foreach (var projectionSpan in projectionSpans)
                    {
                        var spans = TextView.BufferGraph.MapDownToBuffer(projectionSpan, SpanTrackingMode.EdgeInclusive, affectedBuffer);
                        foreach (var span in spans)
                        {
                            edit.Delete(span);
                        }
                    }
                    edit.Apply();
                }
            }

            /// <summary>
            /// Pastes from the clipboard into the text view
            /// Implements <see cref="IInteractiveWindowOperations.Paste"/>.
            /// </summary>
            public bool Paste()
            {
                bool dataHasLineCutCopyTag = false;
                bool dataHasBoxCutCopyTag = false;

                // Get text from clipboard
                string code = Evaluator.FormatClipboard();
                if (code == null)
                {
                    var data = _window.InteractiveWindowClipboard.GetDataObject();
                    if (data == null)
                    {
                        return false;
                    }

                    dataHasLineCutCopyTag = data.GetDataPresent(ClipboardLineBasedCutCopyTag);
                    dataHasBoxCutCopyTag = data.GetDataPresent(BoxSelectionCutCopyTag);

                    Debug.Assert((dataHasLineCutCopyTag && dataHasBoxCutCopyTag) == false);

                    if (_window.InteractiveWindowClipboard.ContainsData(ClipboardFormat))
                    {
                        var sb = new StringBuilder();
                        var blocks = BufferBlock.Deserialize((string)_window.InteractiveWindowClipboard.GetData(ClipboardFormat));

                        foreach (var block in blocks)
                        {
                            switch (block.Kind)
                            {
                                // the actual linebreak was converted to regular Input when copied
                                // This LineBreak block was created by coping box selection and is used as line separater when pasted
                                case ReplSpanKind.LineBreak:
                                    Debug.Assert(dataHasBoxCutCopyTag);
                                    sb.Append(block.Content);
                                    break;
                                case ReplSpanKind.Input:
                                case ReplSpanKind.Output:
                                case ReplSpanKind.StandardInput:
                                    sb.Append(block.Content);
                                    break;
                            }
                        }
                        code = sb.ToString();
                    }
                    else if (_window.InteractiveWindowClipboard.ContainsText())
                    {
                        code = _window.InteractiveWindowClipboard.GetText();
                    }
                    else
                    {
                        return false;
                    }
                }

                using (var transaction = UndoHistory?.CreateTransaction(InteractiveWindowResources.Paste))
                {
                    var selection = TextView.Selection;

                    // Delete selected text if there's any and adjust caret position
                    if (!selection.IsEmpty)
                    {
                        // do not delete and paste anything if any part of selection is not in current submission
                        if (!IsSelectionInsideCurrentSubmission())
                        {
                            return false;
                        }

                        DeleteSelection();

                        if (selection.Mode == TextSelectionMode.Box)
                        {
                            ReduceBoxSelectionToEditableBox(isDelete: true);
                        }
                        else
                        {
                            selection.Clear();
                            MoveCaretToClosestEditableBuffer();
                        }
                    }
                    else
                    {
                        var caretPosition = TextView.Caret.Position.BufferPosition;
                        var isInActivePrompt = IsInActivePrompt(caretPosition);

                        if (isInActivePrompt)
                        {
                            MoveCaretToClosestEditableBuffer();
                        }
                        else if (MapToEditableBuffer(caretPosition) == null)
                        {
                            return false;
                        }

                        // Move caret to the begining of the line for pasting full-line when selection is empty
                        // If the caret was in active prompt, it was already moved to strat of input line
                        if (dataHasLineCutCopyTag && !isInActivePrompt)
                        {
                            var endPoint = GetSourceBufferPoint(caretPosition.GetContainingLine().End);
                            TextView.Caret.MoveTo(GetProjectionBufferPoint(endPoint.GetContainingLine().Start));
                        }
                    }

                    if (dataHasBoxCutCopyTag)
                    {
                        if (selection.IsEmpty && IsCaretOnBlankEditableLine())
                        {
                            InsertText(code);
                        }
                        else
                        {
                            VirtualSnapshotPoint unusedStart, unusedEnd;
                            EditorOperations.InsertTextAsBox(code, out unusedStart, out unusedEnd);
                        }
                    }
                    else
                    {
                        InsertText(code);
                    }

                    transaction?.Complete();
                    return true;
                }
            }
            
            /// <summary>
            /// Implements <see cref="IInteractiveWindowOperations.Cut"/>.
            /// Cut is logically expressed as a combination of Copy and Delete.
            /// i.e. it always copies entire selection, but will not delete anything
            /// if any part of the selection is not in current submission (input or active prompts)
            /// /// </summary>
            public void Cut()
            {
                using (var transaction = UndoHistory?.CreateTransaction(InteractiveWindowResources.Cut))
                {
                    if (TextView.Selection.IsEmpty)
                    {
                        CopyCurrentLine();
                        if (!DeleteCurrentLine())
                        {
                            return;
                        }
                        MoveCaretToClosestEditableBuffer();
                    }
                    else
                    {
                        var selection = TextView.Selection;
                        CopySelection();
                        if (!DeleteSelection())
                        {
                            return;
                        }

                        if (selection.Mode == TextSelectionMode.Box)
                        {
                            ReduceBoxSelectionToEditableBox(isDelete: true);
                        }
                        else
                        {
                            selection.Clear();
                            MoveCaretToClosestEditableBuffer();
                        }
                        TextView.Caret.EnsureVisible();
                    }

                    transaction?.Complete();
                }
            }

            /// <summary>Implements <see cref="IInteractiveWindowOperations2.Copy"/>.</summary>
            public void Copy()
            {
                if (TextView.Selection.IsEmpty)
                {
                    CopyCurrentLine();
                }
                else
                {
                    CopySelection();
                }
            }

            /// <summary>Implements <see cref="IInteractiveWindowOperations2.CopyInputs"/>.</summary>
            public void CopyInputs()
            {
                var selection = TextView.Selection;
                NormalizedSnapshotSpanCollection spans;
                if (selection.IsEmpty)
                {
                    spans = GetSpansFromCurrentLine();
                }
                else
                {
                    spans = selection.SelectedSpans;
                }

                var inputSpans = new List<SnapshotSpan>();
                foreach (var span in spans)
                {
                    // For each selected source buffer span, if the span is in input buffer, 
                    // the lambda function generates the corresponding snapshot span in the source buffer snapshot, otherwise it returns null.
                    GetValuesFromSpan<SnapshotSpan>(inputSpans,
                        span,
                        (kind, snapshot, s) => kind == ReplSpanKind.Input ? new SnapshotSpan(snapshot, s) : (SnapshotSpan?)null);
                }
                var projectionInputSpans = new List<SnapshotSpan>();
                foreach (var inputSpan in inputSpans)
                {
                    projectionInputSpans.AddRange(TextView.BufferGraph.MapUpToSnapshot(inputSpan, SpanTrackingMode.EdgeInclusive, _projectionBuffer.CurrentSnapshot));
                }
                var projectSpans = new NormalizedSnapshotSpanCollection(projectionInputSpans);
                CopySpans(projectSpans, lineCutCopyTag: selection.IsEmpty, boxCutCopyTag: !selection.IsEmpty && selection.Mode == TextSelectionMode.Box);
            }

            private void CopySelection()
            {
                Debug.Assert(!TextView.Selection.IsEmpty);

                CopySpans(TextView.Selection.SelectedSpans, lineCutCopyTag: false, boxCutCopyTag: TextView.Selection.Mode == TextSelectionMode.Box);
            }

            private void CopyCurrentLine()
            {
                Debug.Assert(TextView.Selection.IsEmpty);
                CopySpans(GetSpansFromCurrentLine(), lineCutCopyTag: true, boxCutCopyTag: false);
            }

            private NormalizedSnapshotSpanCollection GetSpansFromCurrentLine()
            {
                var snapshotLine = TextView.Caret.Position.VirtualBufferPosition.Position.GetContainingLine();
                var span = new SnapshotSpan(snapshotLine.Start, snapshotLine.LengthIncludingLineBreak);
                return new NormalizedSnapshotSpanCollection(span);
            }

            private NormalizedSnapshotSpanCollection CopySelectedLines()
            {
                Debug.Assert(!TextView.Selection.IsEmpty);
                var selectedLines = GetSpansFromSelectedLines();
                CopySpans(selectedLines, lineCutCopyTag: true, boxCutCopyTag: false);
                return selectedLines;
            }

            private NormalizedSnapshotSpanCollection GetSpansFromSelectedLines()
            {
                var selection = TextView.Selection;
                var startPoint = selection.Start.Position.GetContainingLine().Start;
                var projectionSpans = TextView.BufferGraph.MapUpToSnapshot(new SnapshotSpan(startPoint,
                                                                                            selection.End.Position.GetContainingLine().EndIncludingLineBreak),
                                                                           SpanTrackingMode.EdgeInclusive,
                                                                           _projectionBuffer.CurrentSnapshot);
                return projectionSpans;
            }

            /// <summary>
            /// Copy contetnt of given spans.
            /// - copy with style for RTF format.
            /// - copy without style for other text formats.
            /// - copy each block with buffer info into a costum InteractiveWindow format. 
            /// This allows paste into code editors of just the code and paste of the entire content for editors that support RTF.
            /// </summary>
            private void CopySpans(NormalizedSnapshotSpanCollection spans, bool lineCutCopyTag, bool boxCutCopyTag)
            {
                if (spans == NormalizedSnapshotSpanCollection.Empty)
                {
                    return;
                }

                var data = new DataObject();

                var text = GetText(spans, boxCutCopyTag);
                data.SetData(DataFormats.Text, text);
                data.SetData(DataFormats.StringFormat, text);
                data.SetData(DataFormats.UnicodeText, text);

                var blocks = GetTextBlocks(spans, boxCutCopyTag);
                data.SetData(ClipboardFormat, blocks);

                string rtf = null;
                try
                {
                    rtf = GenerateRtf(spans, boxCutCopyTag);
                }
                catch (OperationCanceledException)
                {
                    // Ignore cancellation when doing a copy. The user may not even want RTF text 
                    // so preventing the normal text from being copied would be overkill.
                }
                if (rtf != null)
                {
                    data.SetData(DataFormats.Rtf, rtf);
                }

                //tag the data in the clipboard if requested
                if (lineCutCopyTag)
                {
                    data.SetData(ClipboardLineBasedCutCopyTag, true);
                }

                if (boxCutCopyTag)
                {
                    data.SetData(BoxSelectionCutCopyTag, true);
                }

                _window.InteractiveWindowClipboard.SetDataObject(data, true);
            }

            private string GenerateRtf(NormalizedSnapshotSpanCollection spans, bool isBoxSelection)
            {
                // This behavior is consistent with VS editor. 
                // Don't generate RTF for large spans (since it is expensive and probably not wanted).
                int length = spans.Sum((span) => span.Length);
                if (length < 1000000)
                {
                    using (var dialog = _waitIndicator.StartWait(InteractiveWindowResources.WaitTitle, InteractiveWindowResources.WaitMessage, allowCancel: true))
                    {
                        return isBoxSelection
                            ? _rtfBuilderService.GenerateRtf(spans, dialog.CancellationToken)
                            : _rtfBuilderService.GenerateRtf(spans, string.Empty, dialog.CancellationToken);
                    }
                }
                else
                {
                    return null;
                }
            }

            /// <summary>
            /// Get the text of the given spans.
            /// If there are multiple spans, then returns either concatenation of text from each span or concatenation of text plus a newline character,
            /// depends on whether the multiple spans is because box selection. Otherwise, simply returns the text of the only span.
            /// </summary>
            private string GetText(NormalizedSnapshotSpanCollection spans, bool isBoxSelection)
            {
                Debug.Assert(spans.Count > 0);

                if (spans.Count > 1)
                {
                    var newline = EditorOperations.Options.GetNewLineCharacter();
                    var builder = new StringBuilder();
                    foreach (var span in spans)
                    {
                        builder.Append(span.GetText());

                        // If spans are got from box selection, add a newline after each span.
                        if (isBoxSelection)
                        {
                            builder.Append(newline);
                        }
                    }
                    return builder.ToString();
                }
                else
                {
                    return spans[0].GetText();
                }
            }

            /// <summary>
            /// Get the text of the given spans as a serialized BufferBlock[].
            /// </summary>
            private string GetTextBlocks(NormalizedSnapshotSpanCollection spans, bool isBoxSelection)
            {
                var blocks = new List<BufferBlock>();
                foreach (var span in spans)
                {
                    // The lambda function generates a BufferBlock for each selected source buffer span
                    GetValuesFromSpan<BufferBlock>(blocks, span, (kind, snapshot, s) =>
                                                                    {
                                                                        if (kind == ReplSpanKind.LineBreak)
                                                                        {
                                                                            kind = ReplSpanKind.Output;
                                                                        }
                                                                        return new BufferBlock(kind, snapshot.GetText(s));
                                                                    });
                    // If spans are got from box selection, we use 'LineBreak' block to separate different lines of box selection.
                    if (isBoxSelection)
                    {
                        blocks.Add(new BufferBlock(ReplSpanKind.LineBreak, EditorOperations.Options.GetNewLineCharacter()));
                    }
                }
                return BufferBlock.Serialize(blocks.ToArray());
            }

            /// <summary>
            /// Maps given snapshot span into a list of spans in source buffers, and let the user provided delegate generates an object of type T 
            /// for each span and added it to the list if it is not null.
            /// </summary>
            private void GetValuesFromSpan<T>(List<T> list, SnapshotSpan span, Func<ReplSpanKind, ITextSnapshot, Span, T?> generateValue)
                where T : struct
            {
                // Find the range of source spans that cover the span.
                var sourceSpans = GetSourceSpans(span.Snapshot);
                int n = sourceSpans.Count;
                int index = GetSourceSpanIndex(sourceSpans, span.Start);
                if (index == n)
                {
                    index--;
                }

                for (; index < n; index++)
                {
                    var sourceSpan = sourceSpans[index];
                    if (sourceSpan.IsEmpty)
                    {
                        continue;
                    }
                    var sourceSnapshot = sourceSpan.Snapshot;
                    var mappedSpans = TextView.BufferGraph.MapDownToBuffer(span, SpanTrackingMode.EdgeExclusive, sourceSnapshot.TextBuffer);
                    bool added = false;
                    foreach (var mappedSpan in mappedSpans)
                    {
                        var intersection = sourceSpan.Span.Intersection(mappedSpan);
                        if (intersection.HasValue && !intersection.Value.IsEmpty)
                        {
                            var kind = GetSpanKind(sourceSpan);
                            var interactionValue = intersection.Value;
                            var content = sourceSnapshot.GetText(interactionValue);

                            var TValue = generateValue(kind, sourceSnapshot, interactionValue);
                            if (TValue.HasValue)
                            {
                                list.Add(TValue.Value);
                            }
                            added = true;
                        }
                    }
                    if (!added)
                    {
                        break;
                    }
                }
            }
            
            /// <summary>
            /// Deletes characters preceding the current caret position in the current language buffer.
            /// 
            /// Returns true if the previous character was deleted
            /// </summary>
            private bool DeletePreviousCharacter()
            {
                SnapshotPoint? point = MapToEditableBuffer(TextView.Caret.Position.BufferPosition);

                // We are not in an editable buffer, or we are at the start of the buffer, nothing to delete.
                if (point == null || point.Value == 0)
                {
                    return false;
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
                return true;
            }

            /// <summary>
            /// Deletes characters succeeding the current caret position in the current language buffer.
            /// 
            /// Returns true if the next character was deleted
            /// </summary>
            private bool DeleteNextCharacter()
            {
                SnapshotPoint? point = MapToEditableBuffer(TextView.Caret.Position.BufferPosition);

                // We are not in an editable buffer, or we are at the end of the buffer, nothing to delete.
                if (point == null || point.Value == point.Value.Snapshot.Length)
                {
                    return false;
                }

                var pointValue = point.GetValueOrDefault();

                var line = pointValue.GetContainingLine();
                int characterSize;
                if (line.End.Position == pointValue.Position)
                {
                    Debug.Assert(line.LineNumber != pointValue.Snapshot.LineCount);
                    characterSize = line.Snapshot.GetLineFromLineNumber(line.LineNumber).LineBreakLength;
                }
                else
                {
                    characterSize = 1;
                }

                pointValue.Snapshot.TextBuffer.Delete(new Span(pointValue.Position, characterSize));

                ScrollToCaret();
                return true;
            }

            /// <summary>
            /// Maps point to the current language buffer or editable region of standard input buffer.
            /// </summary>
            private SnapshotPoint? MapToEditableBuffer(SnapshotPoint projectionPoint)
            {
                SnapshotPoint? result = null;

                if (CurrentLanguageBuffer != null)
                {
                    result = GetPositionInLanguageBuffer(projectionPoint);
                }

                if (result != null)
                {
                    return result;
                }

                if (StandardInputBuffer != null && InStandardInputRegion(projectionPoint))
                {
                    result = GetPositionInStandardInputBuffer(projectionPoint);
                }

                return result;
            }

            /// <summary>Implements <see cref="IInteractiveWindowOperations.TrySubmitStandardInput"/>.</summary>
            public bool TrySubmitStandardInput()
            {
                _historySearch = null;
                if (ReadingStandardInput)
                {
                    if (InStandardInputRegion(TextView.Caret.Position.BufferPosition))
                    {
                        SubmitStandardInput();
                    }

                    return true;
                }

                return false;
            }

            private void SubmitStandardInput()
            {
                AppendLineNoPromptInjection(StandardInputBuffer);
                var inputSpan = new SnapshotSpan(StandardInputBuffer.CurrentSnapshot, GetStandardInputSpan());
                _history.Add(inputSpan);
                SetStandardInputValue(inputSpan);

                MakeStandardInputReadonly();

                // Subsequent input should appear after the input span we just finished.
                NewOutputBuffer();

                if (State == State.WaitingForInputAndReadingStandardInput)
                {
                    PrepareForInput(); // Will update State.
                }
                else
                {
                    State = GetStateBeforeReadingStandardInput(State);
                }
            }

            private bool InStandardInputRegion(SnapshotPoint point)
            {
                if (!ReadingStandardInput)
                {
                    return false;
                }

                var standardInputPoint = GetPositionInStandardInputBuffer(point);
                if (!standardInputPoint.HasValue)
                {
                    return false;
                }

                var standardInputPosition = standardInputPoint.GetValueOrDefault().Position;
                var standardInputSpan = GetStandardInputSpan();
                return standardInputSpan.Contains(standardInputPosition) || standardInputSpan.End == standardInputPosition;
            }

            /// <summary>
            /// Add a zero-width tracking span at the end of the projection buffer mapping to the end of the standard input buffer.
            /// </summary>
            private void AddStandardInputSpan()
            {
                var promptSpan = CreateStandardInputPrompt();
                var currentSnapshot = StandardInputBuffer.CurrentSnapshot;
                var inputSpan = new CustomTrackingSpan(
                    currentSnapshot,
                    new Span(currentSnapshot.Length, 0),
                    canAppend: true);
                AppendProjectionSpans(promptSpan, inputSpan);
            }

            /// <summary>Implements <see cref="IInteractiveWindowOperations.BreakLine"/>.</summary>
            public bool BreakLine()
            {
                using (var transaction = UndoHistory?.CreateTransaction(InteractiveWindowResources.BreakLine))
                {
                    if (HandlePostServicesReturn(false))
                    {
                        transaction?.Complete();
                        return true;
                    }
                }
                return false;
            }

            /// <summary>Implements <see cref="IInteractiveWindowOperations.Return"/>.</summary>
            public bool Return()
            {
                _historySearch = null;
                return HandlePostServicesReturn(true);
            }

            private bool HandlePostServicesReturn(bool trySubmit)
            {
                if (CurrentLanguageBuffer == null)
                {
                    return false;
                }

                var selection = TextView.Selection;
                if (!TextView.Selection.IsEmpty)
                {
                    if (IsSelectionInsideCurrentSubmission())
                    {
                        DeleteSelection();
                        selection.Clear();
                        MoveCaretToClosestEditableBuffer();
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (IsInActivePrompt(TextView.Caret.Position.BufferPosition))
                {
                    MoveCaretToClosestEditableBuffer();
                }

                // handle "RETURN" command that is not handled by either editor or service
                var langCaret = GetPositionInLanguageBuffer(TextView.Caret.Position.BufferPosition);

                if (langCaret != null)
                {
                    int caretPosition = langCaret.Value.Position;

                    // note that caret might be located in virtual space behind the current buffer end:
                    if (trySubmit && caretPosition >= CurrentLanguageBuffer.CurrentSnapshot.Length && CanExecuteActiveCode())
                    {
                        var dummy = SubmitAsync();
                    }
                    else
                    {
                        // insert new line (triggers secondary prompt injection in buffer changed event):
                        CurrentLanguageBuffer.Insert(caretPosition, _lineBreakString);
                        IndentCurrentLine(TextView.Caret.Position.BufferPosition);
                        ScrollToCaret();
                    }
                    return true;
                }

                return false;
            }

            private bool CanExecuteActiveCode()
            {
                Debug.Assert(CurrentLanguageBuffer != null);

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
                return Evaluator.CanExecuteCode(input);
            }

            /// <summary>
            /// Returns the insertion point relative to the current language buffer.
            /// </summary>
            private int GetActiveCodeInsertionPosition()
            {
                Debug.Assert(CurrentLanguageBuffer != null);

                var langPoint = TextView.BufferGraph.MapDownToBuffer(
                    new SnapshotPoint(
                        _projectionBuffer.CurrentSnapshot,
                        TextView.Caret.Position.BufferPosition.Position),
                    PointTrackingMode.Positive,
                    CurrentLanguageBuffer,
                    PositionAffinity.Predecessor);

                if (langPoint != null)
                {
                    return langPoint.Value;
                }

                return CurrentLanguageBuffer.CurrentSnapshot.Length;
            }

            private object CreateStandardInputPrompt()
            {
                return string.Empty;
            }

            private object CreatePrimaryPrompt()
            {
                return Evaluator.GetPrompt();
            }

            private object CreateSecondaryPrompt()
            {
                // TODO (crwilcox) format prompt used to get a blank here but now gets "> " from get prompt.
                return Evaluator.GetPrompt();
            }

            private ReplSpanKind GetSpanKind(SnapshotSpan span)
            {
                var textBuffer = span.Snapshot.TextBuffer;
                if (textBuffer == OutputBuffer)
                {
                    return ReplSpanKind.Output;
                }
                if (textBuffer == StandardInputBuffer)
                {
                    return ReplSpanKind.StandardInput;
                }
                if (textBuffer.ContentType == _inertType)
                {
                    return (span.Length == _lineBreakString.Length) && string.Equals(span.GetText(), _lineBreakString) ?
                        ReplSpanKind.LineBreak :
                        ReplSpanKind.Prompt;
                }
                return ReplSpanKind.Input;
            }

            #region Output

            /// <summary>Implements <see cref="IInteractiveWindow.Write(string)"/>.</summary>
            public Span Write(string text)
            {
                int result = _buffer.Write(text);
                return new Span(result, (text != null ? text.Length : 0));
            }

            /// <summary>Implements <see cref="IInteractiveWindow.WriteLine(string)"/>.</summary>
            public Span WriteLine(string text)
            {
                int result = _buffer.Write(text);
                _buffer.Write(_lineBreakString);
                return new Span(result, (text != null ? text.Length : 0) + _lineBreakString.Length);
            }

            /// <summary>Implements <see cref="IInteractiveWindow.WriteError(string)"/>.</summary>
            public Span WriteError(string text)
            {
                int result = _buffer.Write(text);
                var res = new Span(result, (text != null ? text.Length : 0));
                ErrorOutputWriter.Spans.Add(res);
                return res;
            }

            /// <summary>Implements <see cref="IInteractiveWindow.WriteErrorLine(string)"/>.</summary>
            public Span WriteErrorLine(string text)
            {
                int result = _buffer.Write(text);
                _buffer.Write(_lineBreakString);
                var res = new Span(result, (text != null ? text.Length : 0) + _lineBreakString.Length);
                ErrorOutputWriter.Spans.Add(res);
                return res;
            }

            /// <summary>Implements <see cref="IInteractiveWindow.Write(UIElement)"/>.</summary>
            public void Write(UIElement element)
            {
                if (element == null)
                {
                    return;
                }

                _buffer.Flush();
                InlineAdornmentProvider.AddInlineAdornment(TextView, element, OnAdornmentLoaded);
                _adornmentToMinimize = true;
                WriteLine(string.Empty);
                WriteLine(string.Empty);
            }

            private void OnAdornmentLoaded(object source, EventArgs e)
            {
                // Make sure the caret line is rendered
                DoEvents();
                TextView.Caret.EnsureVisible();
            }

            #endregion

            void IDisposable.Dispose()
            {
                if (_buffer != null)
                {
                    _buffer.Dispose();
                }
            }
        }
    }
}
