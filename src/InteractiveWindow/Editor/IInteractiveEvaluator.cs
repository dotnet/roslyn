// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// Implements an evaluator for a specific REPL implementation.  The evaluator is provided to the
    /// REPL implementation by the IInteractiveEngineProvider interface.
    /// </summary>
    public interface IInteractiveEvaluator : IDisposable
    {
        /// <summary>
        /// Gets or sets Interactive Window the engine is currently attached to.
        /// </summary>
        IInteractiveWindow CurrentWindow { get; set; }

        /// <summary>
        /// Initializes the interactive session. 
        /// </summary>
        /// <returns>Task that completes the initialization.</returns>
        Task<ExecutionResult> InitializeAsync();

        /// <summary>
        /// Re-starts the interpreter. Usually this closes the current process (if alive) and starts
        /// a new interpreter.
        /// </summary>
        /// <returns>Task that completes reset and initialization of the new process.</returns>
        Task<ExecutionResult> ResetAsync(bool initialize = true);

        // Parsing and Execution

        /// <summary>
        /// Returns true if the text can be executed.  Used to determine if there is a whole statement entered
        /// in the REPL window.
        /// </summary>
        bool CanExecuteCode(string text);

        /// <summary>
        /// Asynchronously executes the specified text.
        /// </summary>
        /// <param name="text">The code snippet to execute.</param>
        /// <returns>Task that completes the execution.</returns>
        Task<ExecutionResult> ExecuteCodeAsync(string text);

        /// <summary>
        /// Formats the contents of the clipboard in a manner reasonable for the language.  Returns null if the
        /// current clipboard cannot be formatted.
        /// 
        /// </summary>
        /// <remarks>
        /// By default if the clipboard contains text it will be pasted.  The language can format
        /// additional forms here - for example CSV data can be formatted in a language compatible
        /// manner.
        /// </remarks>
        string FormatClipboard();

        /// <summary>
        /// Aborts the current running command.
        /// </summary>
        void AbortExecution();

        /// <summary>
        /// Retrieves the prompt string.
        /// </summary>
        /// <returns>The prompt string.</returns>
        string GetPrompt();
    }
}
