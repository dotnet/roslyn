// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    [Flags]
    internal enum ActiveStatementFlags
    {
        None = 0,

        /// <summary>
        /// The statement is in a leaf frame.
        /// </summary>
        LeafFrame = 1,

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
    }
}
