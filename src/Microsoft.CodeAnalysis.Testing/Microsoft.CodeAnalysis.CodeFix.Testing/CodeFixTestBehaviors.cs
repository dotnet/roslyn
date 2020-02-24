// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Options for customizing code fix test behaviors.
    /// </summary>
    [Flags]
    public enum CodeFixTestBehaviors
    {
        /// <summary>
        /// No special behaviors apply.
        /// </summary>
        None = 0,

        /// <summary>
        /// Skip the Fix All in Document check.
        /// </summary>
        SkipFixAllInDocumentCheck = 1 << 0,

        /// <summary>
        /// Skip the Fix All in Project check.
        /// </summary>
        SkipFixAllInProjectCheck = 1 << 1,

        /// <summary>
        /// Skip the Fix All in Solution check.
        /// </summary>
        SkipFixAllInSolutionCheck = 1 << 2,

        /// <summary>
        /// Skip all Fix All checks.
        /// </summary>
        SkipFixAllCheck = SkipFixAllInDocumentCheck | SkipFixAllInProjectCheck | SkipFixAllInSolutionCheck,
    }
}
