// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// An implementation of a Read Eval Print Loop Window for iteratively developing code.
    /// 
    /// Instances of the repl window can be created by using MEF to import the IInteractiveWindowProvider interface.
    /// </summary>
    public interface IInteractiveWindow : IDisposable, IPropertyOwner
    {
        /// <summary>
        /// Gets the text view which the interactive window is running and writing output to.
        /// </summary>
        IWpfTextView TextView
        {
            get;
        }

        /// <summary>
        /// Gets the current language buffer.
        /// </summary>
        ITextBuffer CurrentLanguageBuffer
        {
            get;
        }

        /// <summary>
        /// Gets the output editor buffer.
        /// </summary>
        ITextBuffer OutputBuffer
        {
            get;
        }

        /// <summary>
        /// The language evaluator used in Repl Window
        /// </summary>
        IInteractiveEvaluator Evaluator
        {
            get;
        }

        /// <summary>
        /// Initializes the execution environment and shows the initial prompt.
        /// </summary>
        /// <returns>Returns a started task that finishes as soon as the initialization completes.</returns>
        Task<ExecutionResult> InitializeAsync();

        /// <summary>
        /// Closes the underlying text view.
        /// </summary>
        void Close();

        /// <summary>
        /// Insert the specified text to the active code buffer at the current caret position.
        /// </summary>
        /// <param name="text">Text to insert.</param>
        /// <remarks>
        /// Overwrites the current selection.
        /// 
        /// If the REPL is in the middle of code execution the text is inserted at the end of a pending input buffer.
        /// When the REPL is ready for input the pending input is inserted into the active code input.
        /// </remarks>
        void InsertCode(string text);

        /// <summary>
        /// Submits a sequence of inputs one by one.
        /// </summary>
        /// <param name="inputs">
        /// Code snippets or REPL commands to submit.
        /// </param>
        /// <remarks>
        /// Enqueues given code snippets for submission at the earliest time the REPL is prepared to
        /// accept submissions. Any submissions are postponed until execution of the current
        /// submission (if there is any) is finished or aborted.
        /// 
        /// The REPL processes the given inputs one by one creating a prompt, input span and possibly output span for each input.
        /// This method may be reentered if any of the inputs evaluates to a command that invokes this method.
        /// </remarks>
        Task SubmitAsync(IEnumerable<string> inputs);

        /// <summary>
        /// Output writer.
        /// 
        /// REVIEW: Remove, other people can wrap Write APIS
        /// </summary>
        TextWriter OutputWriter
        {
            get;
        }

        /// <summary>
        /// Error output writer.
        /// 
        /// REVIEW: Remove, other people can wrap Write APIS
        /// </summary>
        TextWriter ErrorOutputWriter
        {
            get;
        }

        /// <summary>
        /// Writes string followed by a line break into the output buffer.
        /// </summary>
        /// <param name="text">Text to write. Might be null.</param>
        /// <returns>
        /// The offset in the output subject buffer where the text is inserted and the length of the inserted text including the line break.
        /// </returns>
        /// <remarks>
        /// Note that the text might not be written to the editor buffer immediately but be buffered.
        /// The returned offsets might thus be beyond the current length of the editor buffer.
        /// </remarks>
        Span WriteLine(string text);

        /// <summary>
        /// Writes a line into the output buffer.
        /// </summary>
        /// <param name="text">Text to write. Might be null.</param>
        /// <returns>
        /// The offset in the output subject buffer where the text is inserted.
        /// </returns>
        /// <remarks>
        /// Note that the text might not be written to the editor buffer immediately but be buffered.
        /// The returned offset might thus be beyond the current length of the editor buffer.
        /// </remarks>
        Span Write(string text);

        /// <summary>
        /// Writes string followed by a line break into the error buffer.
        /// </summary>
        /// <param name="text">Text to write. Might be null.</param>
        /// <returns>
        /// The offset in the output subject buffer where the text is inserted and the length of the inserted text including the line break.
        /// </returns>
        /// <remarks>
        /// Note that the text might not be written to the editor buffer immediately but be buffered.
        /// The returned offsets might thus be beyond the current length of the editor buffer.
        /// </remarks>
        Span WriteErrorLine(string text);

        /// <summary>
        /// Writes a line into the error buffer.
        /// </summary>
        /// <param name="text">Text to write. Might be null.</param>
        /// <returns>
        /// The offset in the output subject buffer where the text is inserted.
        /// </returns>
        /// <remarks>
        /// Note that the text might not be written to the editor buffer immediately but be buffered.
        /// The returned offset might thus be beyond the current length of the editor buffer.
        /// </remarks>
        Span WriteError(string text);

        /// <summary>
        /// Writes a UI object to the REPL window.
        /// </summary>
        /// <remarks>
        /// Flushes all text previously written to the output buffer before the element is inserted.
        /// </remarks>
        void Write(UIElement element);

        void FlushOutput();

        /// <summary>
        /// Reads input from the REPL window.
        /// </summary>
        /// <returns>The entered input or null if cancelled.</returns>
        TextReader ReadStandardInput();

        /// <summary>
        /// Event triggered when the REPL is ready to accept input.
        /// </summary>
        /// <remarks>
        /// Called on the UI thread.
        /// </remarks>
        event Action ReadyForInput;

        event EventHandler<SubmissionBufferAddedEventArgs> SubmissionBufferAdded;

        /// <summary>
        /// True if there is currently an input being executed.
        /// </summary>
        /// 
        /// <remarks>
        /// This value can only be reliably queried on the UI thread, otherwise the value
        /// is transient.
        /// </remarks>
        bool IsRunning
        {
            get;
        }

        /// <summary>
        /// True if the interactive evaluator is currently resetting.
        /// </summary>
        /// 
        /// <remarks>
        /// This value can only be reliably queried on the UI thread, otherwise the value
        /// is transient.
        /// </remarks>
        bool IsResetting
        {
            get;
        }

        /// <summary>
        /// True if the interactive evaluator is currently resetting.
        /// </summary>
        /// 
        /// <remarks>
        /// This value can only be reliably queried on the UI thread, otherwise the value
        /// is transient.
        /// </remarks>
        bool IsInitializing
        {
            get;
        }

        /// <summary>
        /// Appends a input into the editor buffer and history as if it has been executed.
        /// 
        /// The input is not executed.
        /// </summary>
        void AddInput(string input);

        IInteractiveWindowOperations Operations
        {
            get;
        }
    }
}
