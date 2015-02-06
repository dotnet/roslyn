// Dumps commands in QueryStatus and Exec.
// #define DUMP_COMMANDS

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    // TODO: We should condense committed language buffers into a single language buffer and save the
    // classifications from the previous language buffer if the perf of having individual buffers
    // starts having problems w/ a large number of inputs.

    /// <summary>
    /// Provides implementation of a Repl Window built on top of the VS editor using projection buffers.
    /// </summary>
    internal class InteractiveWindow : IInteractiveWindow
    {
        private bool adornmentToMinimize = false;

        // true iff code is being executed:
        private bool isRunning;            // TODO: remove in favor of currentTask
        private bool isResetting;

        private Task<ExecutionResult> currentTask;

        private DispatcherTimer executionTimer;
        private Cursor oldCursor;

        private readonly IWpfTextView textView;
        private readonly IEditorOperations editorOperations;
        private readonly InteractiveOperations interactiveOperations;
        private readonly History history;
        private readonly TaskScheduler uiScheduler;
        
        public event EventHandler<SubmissionBufferAddedEventArgs> SubmissionBufferAdded;

        ////
        //// Services
        //// 

        private readonly IInteractiveWindowEditorFactoryService host;
        private readonly IContentTypeRegistryService contentTypeRegistry;
        private readonly IEditorOperationsFactoryService editorOperationsFactory;
        private readonly ITextEditorFactoryService editorFactory;
        private readonly IIntellisenseSessionStackMapService intellisenseSessionStackMap;
        private readonly ISmartIndentationService smartIndenterService;

        // the language engine and content type of the active submission:
        private bool engineInitialized;
        private IInteractiveEvaluator engine;
        private IContentType languageContentType;

        private IIntellisenseSessionStack sessionStack; // TODO: remove

        public PropertyCollection Properties { get; private set; }

        ////
        //// Buffer composition.
        //// 
        private readonly ITextBufferFactoryService bufferFactory;                          // Factory for creating output, std input, prompt and language buffers.
        private readonly IProjectionBufferFactoryService projectionBufferFactory;
        private readonly ITextBuffer outputBuffer;
        private readonly IProjectionBuffer projectionBuffer;
        private readonly ITextBuffer stdInputBuffer;
        private ITextBuffer currentLanguageBuffer;
        private string historySearch;

        // Read-only regions protecting initial span of the corresponding buffers:
        private readonly IReadOnlyRegion[] stdInputProtection = new IReadOnlyRegion[2];
        private readonly IReadOnlyRegion[] outputProtection = new IReadOnlyRegion[2];

        // List of projection buffer spans - the projection buffer doesn't allow us to enumerate spans so we need to track them manually:
        private readonly List<ReplSpan> projectionSpans = new List<ReplSpan>();
        private readonly PromptLineMapping promptLineMapping = new PromptLineMapping();

        ////
        //// Submissions.
        ////

        // Pending submissions to be processed whenever the REPL is ready to accept submissions.
        private Queue<string> pendingSubmissions;

        ////
        //// Standard input.
        ////

        // non-null if reading from stdin - position in the _inputBuffer where we map stdin
        private int? stdInputStart; // TODO (tomat): this variable is not used in thread-safe manner
        private int currentInputId = 1;
        private SnapshotSpan? inputValue;
        private string uncommittedInput;
        private readonly AutoResetEvent inputEvent = new AutoResetEvent(false);

        //// 
        //// Output.
        //// 

        private readonly OutputBuffer buffer;
        private readonly TextWriter outputWriter;
        private readonly TextWriter errorOutputWriter;
        private int currentOutputProjectionSpan;
        private int outputTrackingCaretPosition;

        private readonly ReplSpan lineBreakOutputSpan;
        private readonly ReplSpan emptySubmissionSpan;

        private static readonly char[] whitespaceChars = new[] { '\r', '\n', ' ', '\t' };
        private const string BoxSelectionCutCopyTag = "MSDEVColumnSelect";

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
            this.host = host;
            this.Properties = new PropertyCollection();
            this.history = new History();
            this.interactiveOperations = new InteractiveOperations(this);

            this.projectionBufferFactory = projectionBufferFactory;
            this.bufferFactory = bufferFactory;
            this.editorFactory = editorFactory;
            this.contentTypeRegistry = contentTypeRegistry;
            this.editorOperationsFactory = editorOperationsFactory;
            this.intellisenseSessionStackMap = intellisenseSessionStackMap;
            this.smartIndenterService = smartIndenterService;

            var textContentType = contentTypeRegistry.GetContentType("text");
            var replContentType = contentTypeRegistry.GetContentType(PredefinedInteractiveContentTypes.InteractiveContentTypeName);
            var replOutputContentType = contentTypeRegistry.GetContentType(PredefinedInteractiveContentTypes.InteractiveOutputContentTypeName);

            this.outputBuffer = bufferFactory.CreateTextBuffer(replOutputContentType);
            this.stdInputBuffer = bufferFactory.CreateTextBuffer();

            var projBuffer = projectionBufferFactory.CreateProjectionBuffer(
                new EditResolver(this),
                SpecializedCollections.EmptyList<object>(),
                ProjectionBufferOptions.None,
                replContentType);

            // we need to set IReplPromptProvider property before TextViewHost is instantiated so that ReplPromptTaggerProvider can bind to it 
            projBuffer.Properties.AddProperty(typeof(InteractiveWindow), this);

            this.projectionBuffer = projBuffer;
            AppendNewOutputProjectionBuffer();
            projBuffer.Changed += new EventHandler<TextContentChangedEventArgs>(ProjectionBufferChanged);

            this.textView = host.CreateTextView(this, projBuffer, CreateRoleSet());

            TextView.Caret.PositionChanged += CaretPositionChanged;

            TextView.Options.SetOptionValue(DefaultTextViewHostOptions.HorizontalScrollBarId, false);
            TextView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, false);
            TextView.Options.SetOptionValue(DefaultTextViewHostOptions.OutliningMarginId, false);
            TextView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, false);
            TextView.Options.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, WordWrapStyles.WordWrap);

            string lineBreak = TextView.Options.GetNewLineCharacter();
            this.lineBreakOutputSpan = new ReplSpan(lineBreak, ReplSpanKind.Output);
            this.emptySubmissionSpan = new ReplSpan(lineBreak, ReplSpanKind.Language);
            this.editorOperations = editorOperationsFactory.GetEditorOperations(TextView);
            this.uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            this.outputTrackingCaretPosition = -1;
            this.buffer = new OutputBuffer(this);
            this.outputWriter = new InteractiveWindowWriter(this, spans: null);

            SortedSpans errorSpans = new SortedSpans();
            this.errorOutputWriter = new InteractiveWindowWriter(this, errorSpans);
            OutputClassifierProvider.AttachToBuffer(outputBuffer, errorSpans);

            this.Evaluator = evaluator;
        }

        public async Task<ExecutionResult> InitializeAsync()
        {
            RequiresUIThread();

            if (engineInitialized)
            {
                throw new InvalidOperationException(InteractiveWindowResources.AlreadyInitialized);
            }

            engineInitialized = true;
            isResetting = true;

            ExecutionResult result;
            try
            {
                // Anything that reads options should wait until after this call so the evaluator can set the options first
                result = await engine.InitializeAsync().ConfigureAwait(continueOnCapturedContext: true);
            }
            finally
            {
                isResetting = false;
            }

            Debug.Assert(Dispatcher.CheckAccess());

            if (result.IsSuccessful)
            {
                PrepareForInput();
            }

            return result;
        }

        private ITextViewRoleSet CreateRoleSet()
        {
            return editorFactory.CreateTextViewRoleSet(
                PredefinedTextViewRoles.Analyzable,
                PredefinedTextViewRoles.Editable,
                PredefinedTextViewRoles.Interactive,
                PredefinedTextViewRoles.Zoomable,
                PredefinedInteractiveTextViewRoles.InteractiveTextViewRole);
        }

        public void Close()
        {
            Caret.PositionChanged -= CaretPositionChanged;

            TextView.Close();
        }

        #endregion

        #region Misc Helpers

        private string LineBreak
        {
            get { return lineBreakOutputSpan.InertValue; }
        }

        private IIntellisenseSessionStack SessionStack
        {
            get
            {
                if (sessionStack == null)
                {
                    sessionStack = intellisenseSessionStackMap.GetStackForTextView(TextView);
                }

                return sessionStack;
            }
        }

        public ITextBuffer TextBuffer
        {
            get { return TextView.TextBuffer; }
        }

        public ITextSnapshot CurrentSnapshot
        {
            get { return TextBuffer.CurrentSnapshot; }
        }

        public ITextBuffer CurrentLanguageBuffer
        {
            get { return currentLanguageBuffer; }
        }

        private void RequiresLanguageBuffer()
        {
            if (currentLanguageBuffer == null)
            {
                Environment.FailFast("Language buffer not available");
            }
        }

        public void Dispose()
        {
            if (buffer != null)
            {
                buffer.Dispose();
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
       
        public IWpfTextView TextView
        {
            get
            {
                return this.textView;
            }
        }

        public ITextBuffer OutputBuffer
        {
            get { return outputBuffer; }
        }

        public TextWriter OutputWriter
        {
            get { return outputWriter; }
        }

        public TextWriter ErrorOutputWriter
        {
            get { return errorOutputWriter; }
        }

        public IInteractiveEvaluator Evaluator
        {
            get
            {
                return engine;
            }

            set
            {
                RequiresUIThread();

                if (value == null)
                {
                    throw new ArgumentNullException("engine");
                }

                if (this.engine != value)
                {
                    value.CurrentWindow = this;

                    this.engine = value;
                    this.languageContentType = engine.ContentType;
                    this.engineInitialized = false;
                }
            }
        }

        public void ClearHistory()
        {
            if (!CheckAccess())
            {
                Dispatcher.Invoke(new Action(ClearHistory));
                return;
            }

            history.Clear();
        }

        public void ClearView()
        {
            UIThread(() => ClearView(insertInputPrompt: !isRunning));
        }

        private void ClearView(bool insertInputPrompt)
        {
            Debug.Assert(CheckAccess());

            if (stdInputStart != null)
            {
                CancelStandardInput();
            }

            adornmentToMinimize = false;
            InlineAdornmentProvider.RemoveAllAdornments(TextView);

            // remove all the spans except our initial span from the projection buffer
            promptLineMapping.Clear();
            currentInputId = 1;
            uncommittedInput = null;

            // Clear the projection and buffers last as this might trigger events that might access other state of the REPL window:
            RemoveProtection(outputBuffer, outputProtection);
            RemoveProtection(stdInputBuffer, stdInputProtection);

            using (var edit = outputBuffer.CreateEdit(EditOptions.None, null, suppressPromptInjectionTag))
            {
                edit.Delete(0, outputBuffer.CurrentSnapshot.Length);
                edit.Apply();
            }

            buffer.Reset();
            OutputClassifierProvider.ClearSpans(outputBuffer);
            outputTrackingCaretPosition = 0;

            using (var edit = stdInputBuffer.CreateEdit(EditOptions.None, null, suppressPromptInjectionTag))
            {
                edit.Delete(0, stdInputBuffer.CurrentSnapshot.Length);
                edit.Apply();
            }

            RemoveProjectionSpans(0, projectionSpans.Count);

            // Insert an empty output buffer.
            // We do it for two reasons: 
            // 1) When output is written to asynchronously we need a buffer to store it.
            //    This may happen when clearing screen while background thread is writing to the console.
            // 2) We need at least one non-inert span due to bugs in projection buffer.
            AppendNewOutputProjectionBuffer();

            history.ForgetOriginalBuffers();

            if (insertInputPrompt)
            {
                PrepareForInput();
            }
        }

        public void InsertCode(string text)
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => InsertCode(text)));
                return;
            }

            if (stdInputStart == null)
            {
                if (isRunning)
                {
                    AppendUncommittedInput(text);
                }
                else
                {
                    if (!TextView.Selection.IsEmpty)
                    {
                        CutOrDeleteSelection(isCut: false);
                    }

                    editorOperations.InsertText(text);
                }
            }
        }

        public void Submit(IEnumerable<string> inputs)
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => Submit(inputs)));
                return;
            }

            if (stdInputStart == null)
            {
                if (!isRunning && currentLanguageBuffer != null)
                {
                    StoreUncommittedInput();
                    PendSubmissions(inputs);
                    ProcessPendingSubmissions();
                }
                else
                {
                    PendSubmissions(inputs);
                }
            }
        }

        private void PendSubmissions(IEnumerable<string> inputs)
        {
            if (pendingSubmissions == null)
            {
                pendingSubmissions = new Queue<string>();
            }

            foreach (var input in inputs)
            {
                pendingSubmissions.Enqueue(input);
            }
        }

        public void AddLogicalInput(string command)
        {
            if (isRunning || currentLanguageBuffer == null)
            {
                AddLanguageBuffer();
                currentLanguageBuffer.Insert(0, command);
            }
            else
            {
                StoreUncommittedInput();
                SetActiveCode(command);
            }

            FinishCurrentSubmissionInput();
            history.Add(currentLanguageBuffer.CurrentSnapshot.GetExtent());
        }

        public Task<ExecutionResult> ResetAsync(bool initialize = true)
        {
            if (!CheckAccess())
            {
                return UIThread(() => ResetAsync(initialize));
            }

            Debug.Assert(CheckAccess());

            if (stdInputStart != null)
            {
                CancelStandardInput();
            }

            buffer.Flush();

            // replace the task being interrupted by a "reset" task:
            isRunning = true;
            isResetting = true;
            currentTask = engine.ResetAsync(initialize);
            currentTask.ContinueWith(FinishExecute, uiScheduler);

            return currentTask;
        }

        public void Flush()
        {
            buffer.Flush();
        }

        public void AbortCommand()
        {
            if (isRunning)
            {
                engine.AbortCommand();
            }
            else
            {
                UIThread(() =>
                {
                    // finish line of the current std input buffer or language buffer:
                    if (InStandardInputRegion(new SnapshotPoint(CurrentSnapshot, CurrentSnapshot.Length)))
                    {
                        CancelStandardInput();
                    }
                    else if (currentLanguageBuffer != null)
                    {
                        AppendLineNoPromptInjection(currentLanguageBuffer);
                        PrepareForInput();
                    }
                });
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Clears the current input
        /// </summary>
        public void Cancel()
        {
            ClearInput();
            editorOperations.MoveToEndOfDocument(false);
            uncommittedInput = null;
            historySearch = null;
        }

        public void HistoryPrevious(string search = null)
        {
            if (currentLanguageBuffer == null)
            {
                return;
            }

            var previous = history.GetPrevious(search);
            if (previous != null)
            {
                if (string.IsNullOrWhiteSpace(search))
                {
                    // don't store search as an uncommitted history item
                    StoreUncommittedInputForHistory();
                }

                SetActiveCodeToHistory(previous);
                editorOperations.MoveToEndOfDocument(false);
            }
        }

        public void HistoryNext(string search = null)
        {
            if (currentLanguageBuffer == null)
            {
                return;
            }

            var next = history.GetNext(search);
            if (next != null)
            {
                if (string.IsNullOrWhiteSpace(search))
                {
                    // don't store search as an uncommitted history item
                    StoreUncommittedInputForHistory();
                }

                SetActiveCodeToHistory(next);
                editorOperations.MoveToEndOfDocument(false);
            }
            else
            {
                string code = history.UncommittedInput;
                history.UncommittedInput = null;
                if (!string.IsNullOrEmpty(code))
                {
                    SetActiveCode(code);
                    editorOperations.MoveToEndOfDocument(false);
                }
            }
        }

        public void HistorySearchNext()
        {
            EnsureHistorySearch();
            HistoryNext(historySearch);
        }

        public void HistorySearchPrevious()
        {
            EnsureHistorySearch();
            HistoryPrevious(historySearch);
        }

        private void EnsureHistorySearch()
        {
            if (historySearch == null)
            {
                historySearch = CurrentLanguageBuffer.CurrentSnapshot.GetText();
            }
        }

        private void StoreUncommittedInputForHistory()
        {
            if (history.UncommittedInput == null)
            {
                string activeCode = GetActiveCode();
                if (activeCode.Length > 0)
                {
                    history.UncommittedInput = activeCode;
                }
            }
        }

        /// <summary>
        /// Moves to the beginning of the line.
        /// </summary>
        public void Home(bool extendSelection)
        {
            var caret = Caret;

            // map the end of subject buffer line:
            var subjectLineEnd = TextView.BufferGraph.MapDownToFirstMatch(
                caret.Position.BufferPosition.GetContainingLine().End,
                PointTrackingMode.Positive,
                snapshot => snapshot.TextBuffer != projectionBuffer,
                PositionAffinity.Successor).Value;

            ITextSnapshotLine subjectLine = subjectLineEnd.GetContainingLine();

            var projectedSubjectLineStart = TextView.BufferGraph.MapUpToBuffer(
                subjectLine.Start,
                PointTrackingMode.Positive,
                PositionAffinity.Successor,
                projectionBuffer).Value;

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
        }

        /// <summary>
        /// Moves to the end of the line.
        /// </summary>
        public void End(bool extendSelection)
        {
            var caret = Caret;

            // map the end of the subject buffer line:
            var subjectLineEnd = TextView.BufferGraph.MapDownToFirstMatch(
                caret.Position.BufferPosition.GetContainingLine().End,
                PointTrackingMode.Positive,
                snapshot => snapshot.TextBuffer != projectionBuffer,
                PositionAffinity.Successor).Value;

            ITextSnapshotLine subjectLine = subjectLineEnd.GetContainingLine();

            var moveTo = TextView.BufferGraph.MapUpToBuffer(
                subjectLine.End,
                PointTrackingMode.Positive,
                PositionAffinity.Successor,
                projectionBuffer).Value;

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
        }

        public void SelectAll()
        {
            SnapshotSpan? span = GetContainingRegion(TextView.Caret.Position.BufferPosition);

            // if the span is already selected select all text in the projection buffer:
            if (span == null || (TextView.Selection.SelectedSpans.Count == 1 && TextView.Selection.SelectedSpans[0] == span.Value))
            {
                span = new SnapshotSpan(TextBuffer.CurrentSnapshot, new Span(0, TextBuffer.CurrentSnapshot.Length));
            }

            TextView.Selection.Select(span.Value, isReversed: false);
        }

        /// <summary>
        /// Given a point in projection buffer calculate a span that includes the point and comprises of 
        /// subsequent projection spans forming a region, i.e. a sequence of output spans in between two subsequent submissions,
        /// a language input block, or standard input block.
        /// 
        /// Internal for testing.
        /// </summary>
        internal SnapshotSpan? GetContainingRegion(SnapshotPoint point)
        {
            int closestPrecedingPrimaryPromptIndex;
            ReplSpan projectionSpan = GetClosestPrecedingInputSpan(point, out closestPrecedingPrimaryPromptIndex);
            if (projectionSpan == null)
            {
                return null;
            }

            var inputSnapshot = projectionSpan.TrackingSpan.TextBuffer.CurrentSnapshot;

            // Language input block is a projection of the entire snapshot;
            // std input block is a projection of a single span:
            SnapshotPoint inputBufferEnd = (projectionSpan.Kind == ReplSpanKind.Language) ?
                new SnapshotPoint(inputSnapshot, inputSnapshot.Length) :
                projectionSpan.TrackingSpan.GetEndPoint(inputSnapshot);

            SnapshotPoint projectedInputBufferEnd = TextView.BufferGraph.MapUpToBuffer(
                inputBufferEnd,
                PointTrackingMode.Positive,
                PositionAffinity.Predecessor,
                TextBuffer).Value;

            // point is between the primary prompt (including) and the last character of the corresponding language/stdin buffer:
            if (point <= projectedInputBufferEnd)
            {
                var projectedLanguageBufferStart = TextView.BufferGraph.MapUpToBuffer(
                    new SnapshotPoint(inputSnapshot, 0),
                    PointTrackingMode.Positive,
                    PositionAffinity.Successor,
                    TextBuffer).Value;

                var promptProjectionSpan = projectionSpans[promptLineMapping[closestPrecedingPrimaryPromptIndex].Value];
                if (point < projectedLanguageBufferStart - promptProjectionSpan.Length)
                {
                    // cursor is before the first language buffer:
                    return new SnapshotSpan(new SnapshotPoint(TextBuffer.CurrentSnapshot, 0), projectedLanguageBufferStart - promptProjectionSpan.Length);
                }

                // cursor is within the language buffer:
                return new SnapshotSpan(projectedLanguageBufferStart, projectedInputBufferEnd);
            }

            // this was the last primary/stdin prompt - select the part of the projection buffer behind the end of the language/stdin buffer:
            if (closestPrecedingPrimaryPromptIndex + 1 == promptLineMapping.Count)
            {
                return new SnapshotSpan(
                    projectedInputBufferEnd,
                    new SnapshotPoint(TextBuffer.CurrentSnapshot, TextBuffer.CurrentSnapshot.Length));
            }

            ReplSpan lastSpanBeforeNextPrompt = projectionSpans[promptLineMapping[closestPrecedingPrimaryPromptIndex + 1].Value - 1];
            Debug.Assert(lastSpanBeforeNextPrompt.Kind == ReplSpanKind.Output);

            // select all text in between the language buffer and the next prompt:
            var trackingSpan = lastSpanBeforeNextPrompt.TrackingSpan;
            return new SnapshotSpan(
                projectedInputBufferEnd,
                TextView.BufferGraph.MapUpToBuffer(
                    trackingSpan.GetEndPoint(trackingSpan.TextBuffer.CurrentSnapshot),
                    PointTrackingMode.Positive,
                    PositionAffinity.Predecessor,
                    TextBuffer).Value);
        }

        /// <summary>
        /// Pastes from the clipboard into the text view
        /// </summary>
        public bool Paste()
        {
            return UIThread(() =>
            {
                MoveCaretToClosestEditableBuffer();

                string format = engine.FormatClipboard();
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
            });
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
            Debug.Assert(currentLanguageBuffer != null);

            var caretLine = caretPosition.GetContainingLine();
            var indentation = this.smartIndenterService.GetDesiredIndentation(TextView, caretLine);

            if (indentation != null)
            {
                if (caretPosition == caretLine.End)
                {
                    // create virtual space:
                    TextView.Caret.MoveTo(new VirtualSnapshotPoint(caretPosition, indentation.Value));
                }
                else
                {
                    var langCaret = GetPositionInLanguageBuffer(caretPosition);
                    if (langCaret == null)
                    {
                        return;
                    }

                    // insert whitespace indentation:
                    string whitespace = GetWhiteSpaceForVirtualSpace(indentation.Value);
                    currentLanguageBuffer.Insert(langCaret.Value, whitespace);
                }
            }
        }

        private SnapshotPoint? GetPositionInLanguageBuffer(SnapshotPoint point)
        {
            Debug.Assert(currentLanguageBuffer != null);
            return GetPositionInBuffer(point, currentLanguageBuffer);
        }

        private SnapshotPoint? GetPositionInStdInputBuffer(SnapshotPoint point)
        {
            Debug.Assert(stdInputBuffer != null);
            return GetPositionInBuffer(point, stdInputBuffer);
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
        private string GetWhiteSpaceForVirtualSpace(int virtualSpaces)
        {
            string textToInsert;
            if (!TextView.Options.IsConvertTabsToSpacesEnabled())
            {
                int tabSize = TextView.Options.GetTabSize();

                int spacesAfterPreviousTabStop = virtualSpaces % tabSize;
                int columnOfPreviousTabStop = virtualSpaces - spacesAfterPreviousTabStop;

                int requiredTabs = (columnOfPreviousTabStop + tabSize - 1) / tabSize;

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
            SnapshotPoint? point = MapToEditableBuffer(TextView.Caret.Position.BufferPosition);

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

            // scroll to the line being edited:
            SnapshotPoint caretPosition = TextView.Caret.Position.BufferPosition;
            TextView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(caretPosition.Snapshot, caretPosition, 0));
        }

        private void CutOrDeleteCurrentLine(bool isCut)
        {
            ITextSnapshotLine line;
            int column;
            TextView.Caret.Position.VirtualBufferPosition.GetLineAndColumn(out line, out column);

            CutOrDelete(new[] { line.ExtentIncludingLineBreak }, isCut);

            TextView.Caret.MoveTo(new VirtualSnapshotPoint(TextView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(line.LineNumber), column));
        }

        /// <summary>
        /// Deletes currently selected text from the language buffer and optionally saves it to the clipboard.
        /// </summary>
        private void CutOrDeleteSelection(bool isCut)
        {
            CutOrDelete(TextView.Selection.SelectedSpans, isCut);

            // if the selection spans over prompts the prompts remain selected, so clear manually:
            TextView.Selection.Clear();
        }

        private void CutOrDelete(IEnumerable<SnapshotSpan> projectionSpans, bool isCut)
        {
            Debug.Assert(currentLanguageBuffer != null);

            StringBuilder deletedText = null;

            // split into multiple deletes that only affect the language/input buffer:
            ITextBuffer affectedBuffer = (stdInputStart != null) ? stdInputBuffer : currentLanguageBuffer;
            using (var edit = affectedBuffer.CreateEdit())
            {
                foreach (var projectionSpan in projectionSpans)
                {
                    var spans = TextView.BufferGraph.MapDownToBuffer(projectionSpan, SpanTrackingMode.EdgeInclusive, affectedBuffer);
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
                if (TextView.Selection.Mode == TextSelectionMode.Box)
                {
                    data.SetData(BoxSelectionCutCopyTag, new object());
                }

                data.SetText(deletedText.ToString());
                Clipboard.SetDataObject(data, true);
            }
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

        public bool IsRunning
        {
            get
            {
                return isRunning;
            }
        }

        public bool IsResetting
        {
            get
            {
                return isResetting;
            }
        }

        public IInteractiveWindowOperations Operations
        {
            get
            {
                return interactiveOperations;
            }
        }

        public bool Delete()
        {
            historySearch = null;
            bool handled = false;
            if (!TextView.Selection.IsEmpty)
            {
                if (TextView.Selection.Mode == TextSelectionMode.Stream || ReduceBoxSelectionToEditableBox())
                {
                    CutOrDeleteSelection(isCut: false);
                    MoveCaretToClosestEditableBuffer();
                    handled = true;
                }
            }

            return handled;
        }

        public void Cut()
        {
            if (TextView.Selection.IsEmpty)
            {
                CutOrDeleteCurrentLine(isCut: true);
            }
            else
            {
                CutOrDeleteSelection(isCut: true);
            }

            MoveCaretToClosestEditableBuffer();
        }

        public bool Backspace()
        {
            bool handled = false;
            if (!TextView.Selection.IsEmpty)
            {
                if (TextView.Selection.Mode == TextSelectionMode.Stream || ReduceBoxSelectionToEditableBox())
                {
                    CutOrDeleteSelection(isCut: false);
                    MoveCaretToClosestEditableBuffer();
                    handled = true;
                }
            }
            else if (TextView.Caret.Position.VirtualSpaces == 0)
            {
                DeletePreviousCharacter();
                handled = true;
            }

            return handled;
        }

        public bool TrySubmitStandardInput()
        {
            historySearch = null;
            if (stdInputStart != null)
            {
                if (InStandardInputRegion(TextView.Caret.Position.BufferPosition))
                {
                    SubmitStandardInput();
                }

                return true;
            }

            return false;
        }

        public bool BreakLine()
        {
            return HandlePostServicesReturn(false);
        }

        public bool Return()
        {
            historySearch = null;
            return HandlePostServicesReturn(true);
        }

        private bool HandlePostServicesReturn(bool trySubmit)
        {
            if (currentLanguageBuffer == null)
            {
                return false;
            }

            // handle "RETURN" command that is not handled by either editor or service
            var langCaret = GetPositionInLanguageBuffer(Caret.Position.BufferPosition);
            if (langCaret != null)
            {
                int caretPosition = langCaret.Value.Position;

                // note that caret might be located in virtual space behind the current buffer end:
                if (trySubmit && caretPosition >= currentLanguageBuffer.CurrentSnapshot.Length && CanExecuteActiveCode())
                {
                    Submit();
                    return true;
                }

                // insert new line (triggers secondary prompt injection in buffer changed event):
                currentLanguageBuffer.Insert(caretPosition, LineBreak);
                IndentCurrentLine(TextView.Caret.Position.BufferPosition);

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

        private ITextCaret Caret
        {
            get { return TextView.Caret; }
        }

        private bool CaretAtEnd
        {
            get { return Caret.Position.BufferPosition.Position == CurrentSnapshot.Length; }
        }

        public bool CaretInActiveCodeRegion
        {
            get
            {
                if (currentLanguageBuffer == null)
                {
                    return false;
                }

                var point = GetPositionInLanguageBuffer(Caret.Position.BufferPosition);
                return point != null;
            }
        }

        public bool CaretInStandardInputRegion
        {
            get
            {
                if (stdInputBuffer == null)
                {
                    return false;
                }

                var point = GetPositionInStdInputBuffer(Caret.Position.BufferPosition);
                return point != null;
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
            ITextBuffer editableBuffer = (stdInputStart != null) ? stdInputBuffer : currentLanguageBuffer;

            if (editableBuffer == null)
            {
                return new SnapshotPoint(projectionBuffer.CurrentSnapshot, projectionBuffer.CurrentSnapshot.Length);
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
                projectionBuffer).Value;
        }

        /// <summary>
        /// Maps point to the current language buffer or standard input buffer.
        /// </summary>
        private SnapshotPoint? MapToEditableBuffer(SnapshotPoint projectionPoint)
        {
            SnapshotPoint? result = null;

            if (currentLanguageBuffer != null)
            {
                result = GetPositionInLanguageBuffer(projectionPoint);
            }

            if (result != null)
            {
                return result;
            }

            if (stdInputBuffer != null)
            {
                result = GetPositionInStdInputBuffer(projectionPoint);
            }

            return result;
        }

        /// <summary>
        /// Returns the language or command text buffer that the specified point belongs to.
        /// If the point lays in a prompt returns the buffer corresponding to the prompt.
        /// </summary>
        /// <returns>The language or command buffer or null if the point doesn't belong to any.</returns>
        private ITextBuffer GetLanguageBuffer(SnapshotPoint point)
        {
            int primaryPromptIndex;
            ReplSpan projectionSpan = GetClosestPrecedingInputSpan(point, out primaryPromptIndex);
            if (projectionSpan == null || projectionSpan.Kind != ReplSpanKind.Language)
            {
                return null;
            }

            var inputBuffer = projectionSpan.TrackingSpan.TextBuffer;
            var inputSnapshot = inputBuffer.CurrentSnapshot;

            var projectedSnapshot = TextView.BufferGraph.MapUpToBuffer(
                new SnapshotSpan(inputSnapshot, 0, inputSnapshot.Length),
                SpanTrackingMode.EdgePositive,
                projectionBuffer);

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
            if (inputBuffer == currentLanguageBuffer)
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

        /// <summary>
        /// Returns the insertion point relative to the current language buffer.
        /// </summary>
        private int GetActiveCodeInsertionPosition()
        {
            Debug.Assert(currentLanguageBuffer != null);

            var langPoint = TextView.BufferGraph.MapDownToBuffer(
                new SnapshotPoint(
                    projectionBuffer.CurrentSnapshot,
                    Caret.Position.BufferPosition.Position),
                PointTrackingMode.Positive,
                currentLanguageBuffer,
                PositionAffinity.Predecessor);

            if (langPoint != null)
            {
                return langPoint.Value;
            }

            return currentLanguageBuffer.CurrentSnapshot.Length;
        }

        private void ResetCursor()
        {
            if (executionTimer != null)
            {
                executionTimer.Stop();
            }

            if (oldCursor != null)
            {
                ((ContentControl)TextView).Cursor = oldCursor;
            }

            oldCursor = null;
            executionTimer = null;
        }

        private void StartCursorTimer()
        {
            var timer = new DispatcherTimer();
            timer.Tick += SetRunningCursor;
            timer.Interval = TimeSpan.FromMilliseconds(250);
            executionTimer = timer;
            timer.Start();
        }

        private void SetRunningCursor(object sender, EventArgs e)
        {
            var view = (ContentControl)TextView;

            // Save the old value of the cursor so it can be restored
            // after execution has finished
            oldCursor = view.Cursor;

            // TODO: Design work to come up with the correct cursor to use
            // Set the repl's cursor to the "executing" cursor
            view.Cursor = Cursors.Wait;

            // Stop the timer so it doesn't fire again
            if (executionTimer != null)
            {
                executionTimer.Stop();
            }
        }

        private void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            // make sure language buffer exist
            if (this.currentLanguageBuffer == null)
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
            var point = e.NewPosition.Point.GetInsertionPoint(b => b == this.currentLanguageBuffer);
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
            return currentLanguageBuffer.CurrentSnapshot.GetText();
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

            using (var edit = currentLanguageBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, editTag: null))
            {
                edit.Replace(new Span(0, currentLanguageBuffer.CurrentSnapshot.Length), text);
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

        /// <summary>
        /// Appends given text to the last input span (standard input or active code input).
        /// </summary>
        private void AppendInput(string text)
        {
            Debug.Assert(CheckAccess());

            var inputSpan = projectionSpans[projectionSpans.Count - 1];
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

            ReplaceProjectionSpan(projectionSpans.Count - 1, replSpan);

            Caret.EnsureVisible();
        }

        private void ClearInput()
        {
            Debug.Assert(projectionSpans.Count > 0);

            // Finds the last primary prompt (standard input or code input).
            // Removes all spans following the primary prompt from the projection buffer.
            int i = projectionSpans.Count - 1;
            while (i >= 0)
            {
                if (projectionSpans[i].Kind == ReplSpanKind.Prompt || projectionSpans[i].Kind == ReplSpanKind.StandardInputPrompt)
                {
                    Debug.Assert(i != projectionSpans.Count - 1);
                    break;
                }

                i--;
            }

            if (i >= 0)
            {
                if (projectionSpans[i].Kind != ReplSpanKind.StandardInputPrompt)
                {
                    currentLanguageBuffer.Delete(new Span(0, currentLanguageBuffer.CurrentSnapshot.Length));
                }
                else
                {
                    Debug.Assert(stdInputStart != null);
                    stdInputBuffer.Delete(Span.FromBounds(stdInputStart.Value, stdInputBuffer.CurrentSnapshot.Length));
                }
            }
        }

        private void PrepareForInput()
        {
            Debug.Assert(CheckAccess());

            buffer.Flush();

            AddLanguageBuffer();

            // we are prepared for processing any postponed submissions there might have been:
            ProcessPendingSubmissions();
        }

        private void ProcessPendingSubmissions()
        {
            Debug.Assert(currentLanguageBuffer != null);

            if (pendingSubmissions == null || pendingSubmissions.Count == 0)
            {
                RestoreUncommittedInput();

                // move to the end (it might have been in virtual space):
                Caret.MoveTo(GetLastLine().End);
                Caret.EnsureVisible();

                var ready = ReadyForInput;
                if (ready != null)
                {
                    ready();
                }

                return;
            }

            string submission = pendingSubmissions.Dequeue();

            // queue new work item:
            Dispatcher.Invoke(new Action(() =>
            {
                SetActiveCode(submission);
                Submit();
            }));
        }

        private void Submit()
        {
            Debug.Assert(CheckAccess());
            RequiresLanguageBuffer();

            // TODO: queue submission
            // Ensure that the REPL doesn't try to execute if it is already
            // executing.  If this invariant can no longer be maintained more of
            // the code in this method will need to be bullet-proofed
            if (isRunning)
            {
                return;
            }

            FinishCurrentSubmissionInput();

            history.UncommittedInput = null;

            var snapshotSpan = currentLanguageBuffer.CurrentSnapshot.GetExtent();
            var trimmedSpan = snapshotSpan.TrimEnd();

            if (trimmedSpan.Length == 0)
            {
                // TODO: reuse the current language buffer
                PrepareForInput();
            }
            else
            {
                history.Add(trimmedSpan);
                isRunning = true;

                StartCursorTimer();

                currentTask = engine.ExecuteTextAsync(snapshotSpan.GetText()) ?? ExecutionResult.Failed;
                currentTask.ContinueWith(FinishExecute, uiScheduler);
            }
        }

        private void FinishCurrentSubmissionInput()
        {
            Debug.Assert(CheckAccess());

            AppendLineNoPromptInjection(currentLanguageBuffer);
            ApplyProtection(currentLanguageBuffer, regions: null);

            if (adornmentToMinimize)
            {
                // TODO (tomat): remember the index of the adornment(s) in the current output and minimize those instead of the last one 
                InlineAdornmentProvider.MinimizeLastInlineAdornment(TextView);
                adornmentToMinimize = false;
            }

            // Stop growing the current output projection span.
            Debug.Assert(projectionSpans[currentOutputProjectionSpan].Kind == ReplSpanKind.Output);
            var nonGrowingSpan = projectionSpans[currentOutputProjectionSpan].WithEndTrackingMode(PointTrackingMode.Negative);
            ReplaceProjectionSpan(currentOutputProjectionSpan, nonGrowingSpan);

            AppendNewOutputProjectionBuffer();
            outputTrackingCaretPosition = TextView.Caret.Position.BufferPosition;
        }

        private void StoreUncommittedInput()
        {
            if (uncommittedInput == null)
            {
                string activeCode = GetActiveCode();
                if (!string.IsNullOrEmpty(activeCode))
                {
                    uncommittedInput = activeCode;
                }
            }
        }

        private void AppendUncommittedInput(string text)
        {
            if (string.IsNullOrEmpty(uncommittedInput))
            {
                uncommittedInput = text;
            }
            else
            {
                uncommittedInput += text;
            }
        }

        private void RestoreUncommittedInput()
        {
            if (uncommittedInput != null)
            {
                SetActiveCode(uncommittedInput);
                uncommittedInput = null;
            }
        }

        public string ReadStandardInput()
        {
            // shouldn't be called on the UI thread because we'll hang
            Debug.Assert(!CheckAccess());

            bool wasRunning = isRunning;
            UIThread(() =>
            {
                buffer.Flush();

                if (isRunning)
                {
                    isRunning = false;
                }
                else if (projectionSpans.Count > 0 && projectionSpans[projectionSpans.Count - 1].Kind == ReplSpanKind.Language)
                {
                    // we need to remove our input prompt.
                    RemoveLastInputPrompt();
                }

                AddStandardInputSpan();

                Caret.EnsureVisible();
                ResetCursor();

                isRunning = false;
                uncommittedInput = null;
                stdInputStart = stdInputBuffer.CurrentSnapshot.Length;
            });

            inputEvent.WaitOne();
            stdInputStart = null;

            UIThread(() =>
            {
                // if the user cleared the screen we cancelled the input, so we won't have our span here.
                // We can also have an interleaving output span, so we'll search back for the last input span.
                int i = IndexOfLastStandardInputSpan();
                if (i != -1)
                {
                    RemoveProtection(stdInputBuffer, stdInputProtection);

                    // replace previous span w/ a span that won't grow...
                    var newSpan = new ReplSpan(
                        projectionSpans[i].TrackingSpan.WithEndTrackingMode(PointTrackingMode.Negative),
                        ReplSpanKind.StandardInput);

                    ReplaceProjectionSpan(i, newSpan);
                    ApplyProtection(stdInputBuffer, stdInputProtection, allowAppend: true);

                    if (wasRunning)
                    {
                        isRunning = true;
                    }
                    else
                    {
                        PrepareForInput();
                    }
                }
            });

            // input has been cancelled:
            if (inputValue.HasValue)
            {
                history.Add(inputValue.Value);
                return inputValue.Value.GetText();
            }
            else
            {
                return null;
            }
        }

        private int IndexOfLastStandardInputSpan()
        {
            for (int i = projectionSpans.Count - 1; i >= 0; i--)
            {
                if (projectionSpans[i].Kind == ReplSpanKind.StandardInput)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool InStandardInputRegion(SnapshotPoint point)
        {
            if (stdInputStart == null)
            {
                return false;
            }

            var stdInputPoint = GetPositionInStdInputBuffer(point);
            return stdInputPoint != null && stdInputPoint.Value.Position >= stdInputStart.Value;
        }

        private void CancelStandardInput()
        {
            AppendLineNoPromptInjection(stdInputBuffer);
            inputValue = null;
            inputEvent.Set();
        }

        private void SubmitStandardInput()
        {
            AppendLineNoPromptInjection(stdInputBuffer);
            inputValue = new SnapshotSpan(stdInputBuffer.CurrentSnapshot, Span.FromBounds(stdInputStart.Value, stdInputBuffer.CurrentSnapshot.Length));
            inputEvent.Set();
        }

        #endregion

        #region Output

        public int Write(string text)
        {
            return buffer.Write(text);
        }

        public Span WriteLine(string text = null)
        {
            int result = buffer.Write(text);
            buffer.Write(LineBreak);
            return new Span(result, (text != null ? text.Length : 0) + LineBreak.Length);
        }

        public void Write(UIElement element)
        {
            if (element == null)
            {
                return;
            }

            buffer.Flush();
            InlineAdornmentProvider.AddInlineAdornment(TextView, element, OnAdornmentLoaded);
            OnInlineAdornmentAdded();
            WriteLine(string.Empty);
            WriteLine(string.Empty);
        }

        /// <summary>
        /// Appends text to the output buffer and updates projection buffer to include it.
        /// WARNING: this has to be the only method that writes to the output buffer so that 
        /// the output buffering counters are kept in sync.
        /// </summary>
        internal void AppendOutput(IEnumerable<string> output, int outputLength)
        {
            Debug.Assert(CheckAccess());
            Debug.Assert(output.Any());

            // we maintain this invariant so that projections don't split "\r\n" in half 
            // (the editor isn't happy about it and out line counting also gets simpler):
            Debug.Assert(!outputBuffer.CurrentSnapshot.EndsWith('\r'));

            Debug.Assert(projectionSpans[currentOutputProjectionSpan].Kind == ReplSpanKind.Output);

            int lineBreakProjectionSpanIndex = currentOutputProjectionSpan + 1;

            // insert line break projection span if there is none and the output doesn't end with a line break:
            bool hasLineBreakProjection = lineBreakProjectionSpanIndex < projectionSpans.Count &&
                                          ReferenceEquals(projectionSpans[lineBreakProjectionSpanIndex], lineBreakOutputSpan);

            bool endsWithLineBreak;
            int newLineBreaks = CountOutputLineBreaks(output, out endsWithLineBreak);

            bool insertLineBreak = !endsWithLineBreak && !hasLineBreakProjection;
            bool removeLineBreak = endsWithLineBreak && hasLineBreakProjection;

            int lineBreakProjectionSpansDelta = (insertLineBreak ? 1 : 0) - (removeLineBreak ? 1 : 0);
            int lineCountDelta = newLineBreaks + lineBreakProjectionSpansDelta;

            // Update line to projection span index mapping for all prompts following the output span.
            if (promptLineMapping.Count > 0 && (lineCountDelta != 0 || lineBreakProjectionSpansDelta != 0))
            {
                int i = promptLineMapping.Count - 1;
                while (i >= 0 && promptLineMapping[i].Value > currentOutputProjectionSpan)
                {
                    promptLineMapping[i] = new KeyValuePair<int, int>(
                        promptLineMapping[i].Key + lineCountDelta,
                        promptLineMapping[i].Value + lineBreakProjectionSpansDelta);

                    i--;
                }
            }

            // do not use the mapping until projection span is updated below:
            promptLineMapping.IsInconsistentWithProjections = removeLineBreak || insertLineBreak;

            // insert text to the subject buffer.
            // WARNING: Prompt line mapping needs to be updated before this edit is applied
            // since it might trigger events that use the mapping. 
            int oldBufferLength = outputBuffer.CurrentSnapshot.Length;
            InsertOutput(output, oldBufferLength);

            // mapping becomes consistent as soon as projection spans are updated:
            promptLineMapping.IsInconsistentWithProjections = false;

            if (removeLineBreak)
            {
                RemoveProjectionSpans(lineBreakProjectionSpanIndex, 1);
            }
            else if (insertLineBreak)
            {
                InsertProjectionSpan(lineBreakProjectionSpanIndex, lineBreakOutputSpan);
            }

            // projection spans and prompts are in sync now:
            CheckPromptLineMappingConsistency(currentOutputProjectionSpan);

            // caret didn't move since last time we moved it to track output:
            if (outputTrackingCaretPosition == TextView.Caret.Position.BufferPosition)
            {
                TextView.Caret.EnsureVisible();
                outputTrackingCaretPosition = TextView.Caret.Position.BufferPosition;
            }
        }

        internal void CheckPromptLineMappingConsistency(int minAffectedSpan)
        {
            if (promptLineMapping.Count > 0)
            {
                int i = promptLineMapping.Count - 1;
                while (i >= 0 && promptLineMapping[i].Value > minAffectedSpan)
                {
                    Debug.Assert(
                        projectionSpans[promptLineMapping[i].Value].Kind == ReplSpanKind.Prompt ||
                        projectionSpans[promptLineMapping[i].Value].Kind == ReplSpanKind.StandardInputPrompt);

                    i--;
                }
            }
        }

        internal void InsertOutput(IEnumerable<string> output, int position)
        {
            RemoveProtection(outputBuffer, outputProtection);

            // append the text to output buffer and make sure it ends with a line break:
            using (var edit = outputBuffer.CreateEdit(EditOptions.None, null, suppressPromptInjectionTag))
            {
                foreach (string text in output)
                {
                    edit.Insert(position, text);
                }

                edit.Apply();
            }

            ApplyProtection(outputBuffer, outputProtection);
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

        private void OnAdornmentLoaded(object source, EventArgs e)
        {
            // Make sure the caret line is rendered
            DoEvents();
            Caret.EnsureVisible();
        }

        private void OnInlineAdornmentAdded()
        {
            adornmentToMinimize = true;
        }

        #endregion

        #region Execution

        private bool CanExecuteActiveCode()
        {
            Debug.Assert(currentLanguageBuffer != null);

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

            return engine.CanExecuteText(input);
        }

        private void FinishExecute(Task<ExecutionResult> result)
        {
            Debug.Assert(CheckAccess());

            // The finished task has been replaced by another task (e.g. reset).
            // Do not perform any task finalization, it will be done by the replacement task.
            if (currentTask != result)
            {
                return;
            }

            isRunning = false;
            isResetting = false;
            currentTask = null;
            ResetCursor();

            if (result.Exception != null || !result.Result.IsSuccessful)
            {
                if (history.Last != null)
                {
                    history.Last.Failed = true;
                }
            }

            PrepareForInput();
        }

        public void ExecuteInput()
        {
            Debug.Assert(CheckAccess());

            ITextBuffer languageBuffer = GetLanguageBuffer(Caret.Position.BufferPosition);
            if (languageBuffer == null)
            {
                return;
            }

            if (languageBuffer == currentLanguageBuffer)
            {
                // TODO (tomat): this should rather send an abstract "finish" command that various features
                // can implement as needed (IntelliSense, inline rename would commit, etc.).
                // For now, commit IntelliSense:
                var completionSession = this.SessionStack.TopSession as ICompletionSession;
                if (completionSession != null)
                {
                    completionSession.Commit();
                }

                Submit();
            }
            else
            {
                // append text of the target buffer to the current language buffer:
                string text = TrimTrailingEmptyLines(languageBuffer.CurrentSnapshot);
                currentLanguageBuffer.Replace(new Span(currentLanguageBuffer.CurrentSnapshot.Length, 0), text);
                editorOperations.MoveToEndOfDocument(false);
            }
        }

        #endregion

        #region Buffers, Spans and Prompts
        private ReplSpan CreateStandardInputPrompt()
        {
            return CreatePrompt(string.Empty, ReplSpanKind.StandardInputPrompt);
        }

        private ReplSpan CreatePrimaryPrompt()
        {
            var result = CreatePrompt(Evaluator.GetPrompt(), ReplSpanKind.Prompt);
            currentInputId++;
            return result;
        }

        private ReplSpan CreatePrompt(string prompt, ReplSpanKind promptKind)
        {
            Debug.Assert(promptKind == ReplSpanKind.Prompt || promptKind == ReplSpanKind.StandardInputPrompt || promptKind == ReplSpanKind.SecondaryPrompt);

            var lastLine = GetLastLine();
            promptLineMapping.Add(lastLine.LineNumber, projectionSpans.Count);

            return new ReplSpan(prompt, promptKind);
        }

        private void RemoveLastInputPrompt()
        {
            var prompt = projectionSpans[projectionSpans.Count - SpansPerLineOfInput];
            Debug.Assert(prompt.Kind.IsPrompt());
            if (prompt.Kind == ReplSpanKind.Prompt || prompt.Kind == ReplSpanKind.StandardInputPrompt)
            {
                promptLineMapping.RemoveLast();
            }

            // projection buffer update must be the last operation as it might trigger event that accesses prompt line mapping:
            RemoveProjectionSpans(projectionSpans.Count - SpansPerLineOfInput, SpansPerLineOfInput);
        }

        private ReplSpan CreateSecondaryPrompt()
        {
            // TODO (crwilcox) format prompt used to get a blank here but now gets "> " from get prompt.
            return CreatePrompt(Evaluator.GetPrompt(), ReplSpanKind.SecondaryPrompt);
        }

        /// <summary>
        /// Enumerates input prompts that overlap given span. 
        /// Returns an empty set if we are in the middle of operation changing the mapping and/or projection buffer.
        /// </summary>
        internal IEnumerable<KeyValuePair<ReplSpanKind, SnapshotPoint>> GetOverlappingPrompts(SnapshotSpan span)
        {
            if (projectionSpans.Count == 0 || promptLineMapping.Count == 0 || promptLineMapping.IsInconsistentWithProjections)
            {
                yield break;
            }

            var currentSnapshotSpan = span.TranslateTo(CurrentSnapshot, SpanTrackingMode.EdgeInclusive);
            var startLine = currentSnapshotSpan.Start.GetContainingLine();
            var endLine = currentSnapshotSpan.End.GetContainingLine();

            var promptMappingIndex = promptLineMapping.GetMappingIndexByLineNumber(startLine.LineNumber);

            do
            {
                int lineNumber = promptLineMapping[promptMappingIndex].Key;
                int promptIndex = promptLineMapping[promptMappingIndex].Value;

                // no overlapping prompts will be found beyond the last line of the span:
                if (lineNumber > endLine.LineNumber)
                {
                    break;
                }

                // enumerate all prompts of the input block (primary and secondary):
                do
                {
                    var line = CurrentSnapshot.GetLineFromLineNumber(lineNumber);
                    ReplSpan projectionSpan = projectionSpans[promptIndex];
                    Debug.Assert(projectionSpan.Kind.IsPrompt());

                    if (line.Start.Position >= currentSnapshotSpan.Span.Start || line.Start.Position < currentSnapshotSpan.Span.End)
                    {
                        yield return new KeyValuePair<ReplSpanKind, SnapshotPoint>(
                            projectionSpan.Kind,
                            new SnapshotPoint(CurrentSnapshot, line.Start));
                    }

                    promptIndex += SpansPerLineOfInput;
                    lineNumber++;
                }
                while (promptIndex < projectionSpans.Count && projectionSpans[promptIndex].Kind == ReplSpanKind.SecondaryPrompt);

                // next input block:
                promptMappingIndex++;
            }
            while (promptMappingIndex < promptLineMapping.Count);
        }

        private void IndexOfLastPrompt(out int lastPrimary, out int last)
        {
            last = -1;
            lastPrimary = -1;
            for (int i = projectionSpans.Count - 1; i >= 0; i--)
            {
                switch (projectionSpans[i].Kind)
                {
                    case ReplSpanKind.Prompt:
                        lastPrimary = i;
                        if (last == -1)
                        {
                            last = i;
                        }

                        return;

                    case ReplSpanKind.SecondaryPrompt:
                    case ReplSpanKind.StandardInputPrompt:
                        if (last == -1)
                        {
                            last = i;
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// Returns the lengths of the longest and shortest prompts within the specified range of lines of the current submission buffer.
        /// </summary>
        private void MeasurePrompts(int startLine, int endLine, out int minPromptLength, out int maxPromptLength)
        {
            Debug.Assert(endLine > startLine);

            var promptSpanIndex = GetProjectionSpanIndexFromEditableBufferPosition(projectionBuffer.CurrentSnapshot, projectionSpans.Count, startLine) - 1;
            Debug.Assert(projectionSpans[promptSpanIndex].Kind.IsPrompt());

                var promptSpan = projectionSpans[promptSpanIndex];
                minPromptLength = maxPromptLength = promptSpan.Length;
        }

        /// <summary>
        /// Returns <see cref="ReplSpan"/> representing the closest language or stdin span preceding given point.
        /// </summary>
        /// <param name="point">Snapshot point.</param>
        /// <param name="primaryPromptIndex">
        /// The index to <see cref="promptLineMapping"/> of the primary prompt corresponding to the returned input span.
        /// </param>
        private ReplSpan GetClosestPrecedingInputSpan(SnapshotPoint point, out int primaryPromptIndex)
        {
            if (projectionSpans.Count == 0 || promptLineMapping.Count == 0 || promptLineMapping.IsInconsistentWithProjections)
            {
                primaryPromptIndex = -1;
                return null;
            }

            primaryPromptIndex = promptLineMapping.GetMappingIndexByLineNumber(point.GetContainingLine().LineNumber);
            ReplSpan result = projectionSpans[promptLineMapping[primaryPromptIndex].Value + 1];
            Debug.Assert(result.Kind == ReplSpanKind.Language || result.Kind == ReplSpanKind.StandardInput);
            return result;
        }

        /// <summary>
        /// Creates and adds a new language buffer to the projection buffer.
        /// </summary>
        private void AddLanguageBuffer()
        {
            ITextBuffer buffer = host.CreateAndActivateBuffer(this, languageContentType);

            buffer.Properties.AddProperty(typeof(IInteractiveEvaluator), engine);
            buffer.Properties.AddProperty(typeof(InteractiveWindow), this);

            currentLanguageBuffer = buffer;
            var bufferAdded = SubmissionBufferAdded;
            if (bufferAdded != null)
            {
                bufferAdded(this, new SubmissionBufferAddedEventArgs(buffer));
            }

            // add the whole buffer to the projection buffer and set it up to expand to the right as text is appended
            ReplSpan promptSpan = CreatePrimaryPrompt();
            ReplSpan languageSpan = new ReplSpan(CreateLanguageTrackingSpan(new Span(0, 0)), ReplSpanKind.Language);

            // projection buffer update must be the last operation as it might trigger event that accesses prompt line mapping:
            AppendProjectionSpans(promptSpan, languageSpan);
        }

        /// <summary>
        /// Creates the language span for the last line of the active input.  This span
        /// is effectively edge inclusive so it will grow as the user types at the end.
        /// </summary>
        private CustomTrackingSpan CreateLanguageTrackingSpan(Span span)
        {
            return new CustomTrackingSpan(
                currentLanguageBuffer.CurrentSnapshot,
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
                currentLanguageBuffer.CurrentSnapshot,
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

            var stdInputSpan = new CustomTrackingSpan(
                stdInputBuffer.CurrentSnapshot,
                new Span(stdInputBuffer.CurrentSnapshot.Length, 0),
                PointTrackingMode.Negative,
                PointTrackingMode.Positive);

            ReplSpan inputSpan = new ReplSpan(stdInputSpan, ReplSpanKind.StandardInput);

            AppendProjectionSpans(promptSpan, inputSpan);
        }

        /// <summary>
        /// Marks the entire buffer as read-only.
        /// </summary>
        private static void ApplyProtection(ITextBuffer buffer, IReadOnlyRegion[] regions, bool allowAppend = false)
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
        private static void RemoveProtection(ITextBuffer buffer, IReadOnlyRegion[] regions)
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

        private const int SpansPerLineOfInput = 2;

        private static readonly object suppressPromptInjectionTag = new object();

        private struct SpanRangeEdit
        {
            public int Start;
            public int Count;
            public ReplSpan[] Replacement;

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
            if (snapshot.TextBuffer != currentLanguageBuffer)
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
            if (e.EditTag == suppressPromptInjectionTag)
            {
                return;
            }

            // projection buffer is changed before language buffer is created (for example, output might be printed out during initialization):
            if (currentLanguageBuffer == null)
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
            int oldProjectionSpanCount = projectionSpans.Count;

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

                var oldSurfaceStartLineNumber = e.Before.GetLineNumberFromPosition(oldSurfaceIntersection.Start);
                var oldSurfaceEndLineNumber = e.Before.GetLineNumberFromPosition(oldSurfaceIntersection.End);

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
                int oldStartSpanIndex = GetProjectionSpanIndexFromEditableBufferPosition(e.Before, oldProjectionSpanCount, oldSurfaceStartLineNumber);
                int oldEndSpanIndex = GetProjectionSpanIndexFromEditableBufferPosition(e.Before, oldProjectionSpanCount, oldSurfaceEndLineNumber);

                int spansToReplace = oldEndSpanIndex - oldStartSpanIndex + 1;
                Debug.Assert(spansToReplace >= 1);

                var newSubjectStartLine = newSnapshot.MapToSourceSnapshot(newSurfaceIntersection.Start).GetContainingLine();
                var newSubjectEndLine = newSnapshot.MapToSourceSnapshot(newSurfaceIntersection.End).GetContainingLine();

                int i = 0;
                int lineBreakCount = newSubjectEndLine.LineNumber - newSubjectStartLine.LineNumber;
                var newSpans = new ReplSpan[lineBreakCount * SpansPerLineOfInput + 1];

                var subjectLine = newSubjectStartLine;
                while (true)
                {
                    if (subjectLine.LineNumber != newSubjectStartLine.LineNumber)
                    {
                        // TODO (crwilcox): do we need two prompts?  Can I tell it to not do this?  Or perhaps we do want this since we want different markings?
                        newSpans[i++] = CreateSecondaryPrompt();
                    }

                    newSpans[i++] = CreateLanguageSpanForLine(subjectLine);
                    if (subjectLine.LineNumber == newSubjectEndLine.LineNumber)
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

        private int GetProjectionSpanIndexFromEditableBufferPosition(ITextSnapshot surfaceSnapshot, int projectionSpansCount, int surfaceLineNumber)
        {
            // The current language buffer is projected to a set of projections interleaved regularly by prompt projections 
            // and ending at the end of the projection buffer, each language buffer projection is on a separate line:
            //   [prompt)[language)...[prompt)[language)<end of projection buffer>
            int result = projectionSpansCount - (surfaceSnapshot.LineCount - surfaceLineNumber) * SpansPerLineOfInput + 1;
            Debug.Assert(projectionSpans[result].Kind == ReplSpanKind.Language);
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
                    replacement.AddRange(projectionSpans.Skip(lastEnd).Take(gap));
                    replacement.AddRange(edit.Replacement);
                }

                lastEnd = edit.Start + edit.Count;
            }

            ReplaceProjectionSpans(start, end - start, replacement);
        }

        private ReplSpan CreateLanguageSpanForLine(ITextSnapshotLine languageLine)
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

            return new ReplSpan(languageSpan, ReplSpanKind.Language);
        }

        private void AppendLineNoPromptInjection(ITextBuffer buffer)
        {
            using (var edit = buffer.CreateEdit(EditOptions.None, null, suppressPromptInjectionTag))
            {
                edit.Insert(buffer.CurrentSnapshot.Length, LineBreak);
                edit.Apply();
            }
        }

        // WARNING: When updating projection spans we need to update _projectionSpans list first and 
        // then projection buffer, since the projection buffer update might trigger events that might 
        // access the projection spans.

        private void AppendNewOutputProjectionBuffer()
        {
            var trackingSpan = new CustomTrackingSpan(
                outputBuffer.CurrentSnapshot,
                new Span(outputBuffer.CurrentSnapshot.Length, 0),
                PointTrackingMode.Negative,
                PointTrackingMode.Positive);

            this.currentOutputProjectionSpan = AppendProjectionSpan(new ReplSpan(trackingSpan, ReplSpanKind.Output));
        }

        private int AppendProjectionSpan(ReplSpan span)
        {
            int index = projectionSpans.Count;
            InsertProjectionSpan(index, span);
            return index;
        }

        private int AppendProjectionSpans(ReplSpan span1, ReplSpan span2)
        {
            int index = projectionSpans.Count;
            InsertProjectionSpans(index, span1, span2);
            return index;
        }

        private void InsertProjectionSpan(int index, ReplSpan span)
        {
            projectionSpans.Insert(index, span);
            projectionBuffer.ReplaceSpans(index, 0, new[] { span.Span }, EditOptions.None, editTag: suppressPromptInjectionTag);
        }

        private void InsertProjectionSpans(int index, ReplSpan span1, ReplSpan span2)
        {
            projectionSpans.Insert(index, span1);
            projectionSpans.Insert(index + 1, span2);
            projectionBuffer.ReplaceSpans(index, 0, new[] { span1.Span, span2.Span }, EditOptions.None, editTag: suppressPromptInjectionTag);
        }

        private void ReplaceProjectionSpan(int spanToReplace, ReplSpan newSpan)
        {
            projectionSpans[spanToReplace] = newSpan;
            projectionBuffer.ReplaceSpans(spanToReplace, 1, new[] { newSpan.Span }, EditOptions.None, editTag: suppressPromptInjectionTag);
        }

        private void ReplaceProjectionSpans(int position, int count, IList<ReplSpan> newSpans)
        {
            projectionSpans.RemoveRange(position, count);
            projectionSpans.InsertRange(position, newSpans);

            object[] trackingSpans = new object[newSpans.Count];
            for (int i = 0; i < trackingSpans.Length; i++)
            {
                trackingSpans[i] = newSpans[i].Span;
            }

            projectionBuffer.ReplaceSpans(position, count, trackingSpans, EditOptions.None, suppressPromptInjectionTag);
        }

        private void RemoveProjectionSpans(int index, int count)
        {
            projectionSpans.RemoveRange(index, count);
            projectionBuffer.ReplaceSpans(index, count, SpecializedCollections.EmptyList<object>(), EditOptions.None, suppressPromptInjectionTag);
        }

        #endregion

        #region Editor Helpers

        private ITextSnapshotLine GetLastLine()
        {
            return GetLastLine(CurrentSnapshot);
        }

        private static ITextSnapshotLine GetLastLine(ITextSnapshot snapshot)
        {
            return snapshot.GetLineFromLineNumber(snapshot.LineCount - 1);
        }

        private static ITextSnapshotLine GetPreviousLine(ITextSnapshotLine line)
        {
            return line.LineNumber > 0 ? line.Snapshot.GetLineFromLineNumber(line.LineNumber - 1) : null;
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

        private sealed class EditResolver : IProjectionEditResolver
        {
            private readonly InteractiveWindow window;

            public EditResolver(InteractiveWindow window)
            {
                this.window = window;
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

            public void FillInInsertionSizes(SnapshotPoint projectionInsertionPoint, ReadOnlyCollection<SnapshotPoint> sourceInsertionPoints, string insertionText, IList<int> insertionSizes)
            {
                int index = IndexOfEditableBuffer(sourceInsertionPoints);
                if (index != -1)
                {
                    insertionSizes[index] = insertionText.Length;
                }
            }

            public int GetTypicalInsertionPosition(SnapshotPoint projectionInsertionPoint, ReadOnlyCollection<SnapshotPoint> sourceInsertionPoints)
            {
                int index = IndexOfEditableBuffer(sourceInsertionPoints);
                return index != -1 ? index : 0;
            }

            public void FillInReplacementSizes(SnapshotSpan projectionReplacementSpan, ReadOnlyCollection<SnapshotSpan> sourceReplacementSpans, string insertionText, IList<int> insertionSizes)
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
                    if (insertionBuffer == window.currentLanguageBuffer || insertionBuffer == window.stdInputBuffer)
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
                    if (insertionBuffer == window.currentLanguageBuffer || insertionBuffer == window.stdInputBuffer)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        #endregion

        #region UI Dispatcher Helpers

        private Dispatcher Dispatcher
        {
            get { return ((FrameworkElement)TextView).Dispatcher; }
        }

        internal bool CheckAccess()
        {
            return Dispatcher.CheckAccess();
        }

        private T UIThread<T>(Func<T> func)
        {
            if (!CheckAccess())
            {
                return (T)Dispatcher.Invoke(func);
            }

            return func();
        }

        internal void UIThread(Action action)
        {
            if (!CheckAccess())
            {
                Dispatcher.Invoke(action);
                return;
            }

            action();
        }

        private void RequiresUIThread()
        {
            if (!CheckAccess())
            {
                throw new InvalidOperationException("Must be called on UI thread.");
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

        internal List<ReplSpan> ProjectionSpans
        {
            get { return projectionSpans; }
        }

        #endregion

        class InteractiveOperations : IInteractiveWindowOperations
        {
            private readonly InteractiveWindow window;

            public InteractiveOperations(InteractiveWindow window)
            {
                this.window = window;
            }

            public bool Backspace()
            {
                return window.Backspace();
            }

            public bool BreakLine()
            {
                return window.BreakLine();
            }

            public void Cancel()
            {
                window.Cancel();
            }

            public void ClearHistory()
            {
                window.ClearHistory();
            }

            public void ClearView()
            {
                window.ClearView();
            }

            public void Cut()
            {
                window.Cut();
            }

            public bool Delete()
            {
                return window.Delete();
            }

            public void End(bool extendSelection)
            {
                window.End(extendSelection);
            }

            public void ExecuteInput()
            {
                window.ExecuteInput();
            }

            public void HistoryNext(string search = null)
            {
                window.HistoryNext(search);
            }

            public void HistoryPrevious(string search = null)
            {
                window.HistoryPrevious(search);
            }

            public void HistorySearchNext()
            {
                window.HistorySearchNext();
            }

            public void HistorySearchPrevious()
            {
                window.HistorySearchPrevious();
            }

            public void Home(bool extendSelection)
            {
                window.Home(extendSelection);
            }

            public bool Paste()
            {
                return window.Paste();
            }

            public Task<ExecutionResult> ResetAsync(bool initialize = true)
            {
                return window.ResetAsync(initialize);
            }

            public bool Return()
            {
                return window.Return();
            }

            public void SelectAll()
            {
                window.SelectAll();
            }

            public bool TrySubmitStandardInput()
            {
                return window.TrySubmitStandardInput();
            }
        }
    }
}
