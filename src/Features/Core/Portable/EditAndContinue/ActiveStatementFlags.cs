// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [Flags]
    internal enum ActiveStatementFlags
    {
        None = 0,

        /// <summary>
        /// At least one of the threads whom this active statement belongs to is in a leaf frame.
        /// </summary>
        IsLeafFrame = 1,

        /// <summary>
        /// The statement is partially executed.
        /// </summary>
        /// <remarks>
        /// An active statement is partially executed if the thread is stopped in between two sequence points.
        /// This may happen when the users steps through the code in disassembly window (stepping over machine instructions),
        /// when the compiler emits a call to Debugger.Break (VB Stop statement), etc.
        /// 
        /// Partially executed active statement can't be edited.
        /// </remarks>
        PartiallyExecuted = 2,

        /// <summary>
        /// The statement IL is not in user code.
        /// </summary>
        NonUserCode = 4,

        /// <summary>
        /// Indicates that the active statement instruction belongs to the latest version of the containing method.
        /// If not set, the containing method was updated but the active statement was not remapped yet because the thread 
        /// has not returned to that instruction yet and was not remapped to the new version.
        /// </summary>
        /// <remarks>
        /// When the debugger asks the CLR for the active statement information it compares ICorDebugFunction.GetVersionNumber()
        /// and ICorDebugFunction.GetCurrentVersionNumber() to determine the value of this flag.
        /// </remarks>
        MethodUpToDate = 8,

        /// <summary>
        /// At least one of the threads whom this active statement belongs to is in a non-leaf frame.
        /// </summary>
        IsNonLeafFrame = 16,
    }
}
