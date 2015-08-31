// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Dumps commands in QueryStatus and Exec.
// #define DUMP_COMMANDS

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
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

        void IInteractiveWindow.Close()
        {
            UIThread(uiOnly => uiOnly.Close());
        }

        #region Misc Helpers

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

        IInteractiveEvaluator IInteractiveWindow.Evaluator => _dangerous_uiOnly.Evaluator; // Caller's responsibility

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
            UIThread(uiOnly => uiOnly.CaretPositionChangedInternal(sender, e));
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

        private void ProjectionBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            UIThread(uiOnly => uiOnly.ProjectionBufferChangedInternal(sender, e));
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
