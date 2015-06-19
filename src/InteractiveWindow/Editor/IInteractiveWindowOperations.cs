// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// Provides access to low level editor operations on the REPL window.
    /// </summary>
    public interface IInteractiveWindowOperations
    {
        /// <summary>
        /// Deletes the current selection or the character before the caret.
        /// </summary>
        /// <returns></returns>
        bool Backspace();

        /// <summary>
        /// Attempts to insert a line break.  Returns true if a line break is inserted, false if not.
        /// 
        /// Will not submit the input.
        /// </summary>
        bool BreakLine();

        /// <summary>
        /// Clears the input history.
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// Clears the REPL window screen.
        /// </summary>
        void ClearView();

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
        /// If the current input is a standard input this will submit the input.
        /// 
        /// Returns true if the input was submitted, false otherwise.
        /// </summary>
        bool TrySubmitStandardInput();

        /// <summary>
        /// Resets the execution context clearing all variables.
        /// </summary>
        Task<ExecutionResult> ResetAsync(bool initialize = true);

        /// <summary>
        /// Executes the current input regardless of the caret position within the input. 
        /// 
        /// If the caret is in a previously executed input then the input is pasted to the
        /// end of the current input and not executed.
        /// </summary>
        void ExecuteInput();

        /// <summary>
        /// Clears the current input.
        /// </summary>
        void Cancel();
    }
}
