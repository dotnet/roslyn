using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Roslyn.Editor.InteractiveWindow
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
        /// Clears the REPL window screen.
        /// </summary>
        void ClearView();

        /// <summary>
        /// Clears the input history.
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// Clears the current input.
        /// </summary>
        void Cancel();

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
        void Submit(IEnumerable<string> inputs);

        /// <summary>
        /// Resets the execution context clearing all variables.
        /// </summary>
        Task<ExecutionResult> ResetAsync(bool initialize = true);

        /// <summary>
        /// Aborts the current command which is executing.
        /// 
        /// REVIEW: Remove?  Engine.AbortCommand can be called directly, non-running behavior is a little random.
        /// REVIEW: in C# engine there is handling for non-running.  I fear that getting rid of this here would 
        /// // remove the ability to handle such things.
        /// </summary>
        void AbortCommand();

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
        Span WriteLine(string text = null);

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
        int Write(string text);

        /// <summary>
        /// Writes a UI object to the REPL window.
        /// </summary>
        /// <remarks>
        /// Flushes all text previously written to the output buffer before the element is inserted.
        /// </remarks>
        void Write(UIElement element);

        void Flush();

        /// <summary>
        /// Reads input from the REPL window.
        /// </summary>
        /// <returns>The entered input or null if cancelled.</returns>
        string ReadStandardInput();

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
        bool IsRunning
        {
            get;
        }

        /// <summary>
        /// True if the interactive evaluator is currently resetting.
        /// </summary>
        bool IsResetting
        {
            get;
        }

        /// <summary>
        /// Attempts to insert a line break.  Returns true if a line break is inserted, false if not.
        /// 
        /// Will not submit the input.
        /// </summary>
        bool BreakLine();

        /// <summary>
        /// Handles the user pressing return/enter.
        /// 
        /// If the caret is at the end of an input submits the current input.  Otherwise if the caret is
        /// in a language buffer it inserts a newline.
        /// 
        /// If not inside of a buffer the caret well be moved to the current language buffer if possible.
        /// 
        /// Returns true if the return was successfully processed.
        /// </summary>
        bool Return();

        /// <summary>
        /// Executes the current input regardless of the caret position within the input. 
        /// 
        /// If the caret is in a previously executed input then the input is pasted to the
        /// end of the current input and not executed.
        /// </summary>
        void ExecuteInput();

        /// <summary>
        /// If the current input is a standard input this will submit the input.
        /// 
        /// Returns true if the input was submitted, false otherwise.
        /// </summary>
        bool TrySubmitStandardInput();

        /// <summary>
        /// Appends a input into the editor buffer and history as if it has been executed.
        /// 
        /// The input is not executed.
        /// </summary>
        void AddLogicalInput(string input);

        /// <summary>
        /// Advances to the next item in history.
        /// </summary>
        void HistoryNext(string search = null);

        /// <summary>
        /// Advanced to the previous item in history.
        /// </summary>
        void HistoryPrevious(string search = null);

        /// <summary>
        /// If no search has been performed captures the current input as
        /// the search string.  Then searches through history for the next
        /// match against the current search string.
        /// </summary>
        void HistorySearchNext();

        /// <summary>
        /// If no search has been performed captures the current input as
        /// the search string.  Then searches through history for the previous
        /// match against the current search string.
        /// </summary>
        void HistorySearchPrevious();
        
        /// <summary>
        /// Deletes the current selection or the character before the caret.
        /// </summary>
        /// <returns></returns>
        bool Backspace();

        /// <summary>
        /// Moves to the beginning of the line.  
        /// 
        /// When in a language buffer the caret is moved to the beginning of the
        /// input region not into the prompt region.
        /// 
        /// The caret is moved to the first non-whitespace character.
        /// </summary>
        /// <param name="extendSelection">True to extend the selection from the current caret position.</param>
        void Home(bool extendSelection);

        /// <summary>
        /// Moves to the end of the line.
        /// </summary>
        /// <param name="extendSelection">True to extend the selection from the current caret position.</param>
        void End(bool extendSelection);

        /// <summary>
        /// Selects all of the text in the buffer
        /// </summary>
        void SelectAll();

        /// <summary>
        /// Pastes the current clipboard contents into the interactive window.
        /// </summary>
        /// <returns></returns>
        bool Paste();
        
        /// <summary>
        /// Cuts the current selection to the clipboard.
        /// </summary>
        void Cut();
        
        /// <summary>
        /// Deletes the current selection.
        /// 
        /// Returns true if the selection was deleted
        /// </summary>
        bool Delete();
    }
}
