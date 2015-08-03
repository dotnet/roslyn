// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// Implements an evaluator for a specific REPL implementation.  The evaluator is provided to the
    /// REPL implementation by the IInteractiveEngineProvider interface.
    /// </summary>
   
    public interface IInteractiveEvaluator2 : IInteractiveEvaluator
    {
        /// <summary>
        /// Re-starts the interpreter. Usually this closes the current process (if alive) and starts
        /// a new interpreter.
        /// </summary>
        /// <param name="initialize">Initialize the host process with default config file.</param>
        /// <param name="isFromSubmit">If true then this reset call was made from code typed in interactive  
        /// window else from somehwere else like the reset button in the UI.</param>
        /// <returns>Task that completes reset and initialization of the new process.</returns>
        Task<ExecutionResult> ResetAsync(bool initialize = true, bool isFromSubmit = true);
    }
}