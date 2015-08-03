// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// Provides access to low level editor operations on the REPL window.
    /// </summary>
    public interface IInteractiveWindowOperations2 : IInteractiveWindowOperations
    {
        /// <summary>
        /// Resets the execution context clearing all variables.
        /// </summary>
        /// <param name="initialize">If true then session is re initialized</param>
        /// <param name="isFromSubmit">If true then this reset call was made from code in interactive window else 
        /// from somehwere else like the reset button in the UI.</param>
        Task<ExecutionResult> ResetAsync(bool initialize = true, bool isFromSubmit = true);
    }
}