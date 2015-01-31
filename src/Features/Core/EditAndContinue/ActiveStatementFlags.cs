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
        PartiallyExecuted = 2,

        /// <summary>
        /// The statement IL is not in user code.
        /// </summary>
        NonUserCode = 4,
    }
}
