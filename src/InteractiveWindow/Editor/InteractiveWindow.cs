// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Dumps commands in QueryStatus and Exec.
// #define DUMP_COMMANDS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
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
    internal partial class InteractiveWindow : IInteractiveWindow, IInteractiveWindowOperations2
    {
        internal const string ClipboardFormat = "89344A36-9821-495A-8255-99A63969F87D";
        internal int LanguageBufferCounter = 0;

        public event EventHandler<SubmissionBufferAddedEventArgs> SubmissionBufferAdded;

        PropertyCollection IPropertyOwner.Properties { get; } = new PropertyCollection();

        private readonly SemaphoreSlim _inputReaderSemaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        /// <remarks>
        /// WARNING: Members of this object should only be accessed from the UI thread.
        /// </remarks>
        private readonly UIThreadOnly _uiOnly;
                     
        // Setter for InteractiveWindowClipboard is a test hook.  
        internal InteractiveWindowClipboard InteractiveWindowClipboard { get; set; } = new SystemClipboard();

        #region Initialization

        public InteractiveWindow(
            IInteractiveWindowEditorFactoryService host,
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
            if (evaluator == null)
            {
                throw new ArgumentNullException(nameof(evaluator));
            }

            _uiOnly = new UIThreadOnly(
                this,
                host,
                contentTypeRegistry,
                bufferFactory,
                projectionBufferFactory,
                editorOperationsFactory,
                textBufferUndoManagerProvider,
                editorFactory,
                rtfBuilderService,
                intellisenseSessionStackMap,
                smartIndenterService,
                evaluator,
                waitIndicator);

            evaluator.CurrentWindow = this;

            RequiresUIThread();
        }

        async Task<ExecutionResult> IInteractiveWindow.InitializeAsync()
        {
            try
            {
                RequiresUIThread();
                var uiOnly = _uiOnly; // Verified above.

                if (uiOnly.State != State.Starting)
                {
                    throw new InvalidOperationException(InteractiveWindowResources.AlreadyInitialized);
                }

                uiOnly.State = State.Initializing;

                // Anything that reads options should wait until after this call so the evaluator can set the options first
                ExecutionResult result = await uiOnly.Evaluator.InitializeAsync().ConfigureAwait(continueOnCapturedContext: true);
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

        void IInteractiveWindow.Close()
        {
            UIThread(uiOnly => uiOnly.Close());
        }

        #region Misc Helpers

        /// <remarks>
        /// The caller is responsible for using the buffer in a thread-safe manner.
        /// </remarks>
        public ITextBuffer CurrentLanguageBuffer => _uiOnly.CurrentLanguageBuffer;

        void IDisposable.Dispose()
        {
            UIThread(uiOnly => ((IDisposable)uiOnly).Dispose());
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

        /// <remarks>
        /// The caller is responsible for using the text view in a thread-safe manner.
        /// </remarks>
        IWpfTextView IInteractiveWindow.TextView => _uiOnly.TextView;

        /// <remarks>
        /// The caller is responsible for using the buffer in a thread-safe manner.
        /// </remarks>
        ITextBuffer IInteractiveWindow.OutputBuffer => _uiOnly.OutputBuffer;

        /// <remarks>
        /// The caller is responsible for using the writer in a thread-safe manner.
        /// </remarks>
        TextWriter IInteractiveWindow.OutputWriter => _uiOnly.OutputWriter;

        /// <remarks>
        /// The caller is responsible for using the writer in a thread-safe manner.
        /// </remarks>
        TextWriter IInteractiveWindow.ErrorOutputWriter => _uiOnly.ErrorOutputWriter;

        /// <remarks>
        /// The caller is responsible for using the evaluator in a thread-safe manner.
        /// </remarks>
        IInteractiveEvaluator IInteractiveWindow.Evaluator => _uiOnly.Evaluator;

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
            UIThread(uiOnly => uiOnly.FlushOutput());
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

        /// <remarks>
        /// Test hook.
        /// </remarks>
        internal Task ExecuteInputAsync()
        {
            return UIThread(uiOnly => uiOnly.ExecuteInputAsync());
        }

        /// <summary>
        /// Appends text to the output buffer and updates projection buffer to include it.
        /// WARNING: this has to be the only method that writes to the output buffer so that 
        /// the output buffering counters are kept in sync.
        /// </summary>
        internal void AppendOutput(IEnumerable<string> output)
        {
            RequiresUIThread();
            _uiOnly.AppendOutput(output); // Verified above.
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

        #endregion

        #region Keyboard Commands

        /// <remarks>Only consistent on the UI thread.</remarks>
        bool IInteractiveWindow.IsRunning => _uiOnly.State != State.WaitingForInput;

        /// <remarks>Only consistent on the UI thread.</remarks>
        bool IInteractiveWindow.IsResetting => _uiOnly.State == State.Resetting || _uiOnly.State == State.ResettingAndReadingStandardInput;

        /// <remarks>Only consistent on the UI thread.</remarks>
        bool IInteractiveWindow.IsInitializing => _uiOnly.State == State.Starting || _uiOnly.State == State.Initializing;

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

        void IInteractiveWindowOperations2.DeleteLine()
        {
            UIThread(uiOnly => uiOnly.DeleteLine());
        }

        void IInteractiveWindowOperations2.CutLine()
        {
            UIThread(uiOnly => uiOnly.CutLine());
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
            return UIThread(uiOnly => uiOnly.Write(text));
        }

        Span IInteractiveWindow.WriteLine(string text)
        {
            return UIThread(uiOnly => uiOnly.WriteLine(text));
        }

        Span IInteractiveWindow.WriteError(string text)
        {
            return UIThread(uiOnly => uiOnly.WriteError(text));
        }

        Span IInteractiveWindow.WriteErrorLine(string text)
        {
            return UIThread(uiOnly => uiOnly.WriteErrorLine(text));
        }

        void IInteractiveWindow.Write(UIElement element)
        {
            UIThread(uiOnly => uiOnly.Write(element));
        }

        #endregion

        #region UI Dispatcher Helpers

        private Dispatcher Dispatcher => ((FrameworkElement)_uiOnly.TextView).Dispatcher; // Always safe to access the dispatcher.

        internal bool OnUIThread()
        {
            return Dispatcher.CheckAccess();
        }

        private T UIThread<T>(Func<UIThreadOnly, T> func)
        {
            if (!OnUIThread())
            {
                return (T)Dispatcher.Invoke(func, _uiOnly); // Safe because of dispatch.
            }

            return func(_uiOnly); // Safe because of check.
        }

        private void UIThread(Action<UIThreadOnly> action)
        {
            if (!OnUIThread())
            {
                Dispatcher.Invoke(action, _uiOnly); // Safe because of dispatch.
                return;
            }

            action(_uiOnly); // Safe because of check.
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

        internal void Undo_TestOnly(int count)
        {
            UIThread(uiOnly => uiOnly.UndoHistory_TestOnly.Undo(count));
        }

        internal void Redo_TestOnly(int count)
        {
            UIThread(uiOnly => uiOnly.UndoHistory_TestOnly.Redo(count));
        }

        #endregion
    }
}
