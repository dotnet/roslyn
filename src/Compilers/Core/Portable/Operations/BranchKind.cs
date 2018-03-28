// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Kind of the branch for an <see cref="IBranchOperation"/>
    /// </summary>
    public enum BranchKind
    {
        /// <summary>
        /// Represents unknown branch kind.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Represents a continue branch kind.
        /// </summary>
        Continue = 0x1,

        /// <summary>
        /// Represents a break branch kind.
        /// </summary>
        Break = 0x2,

        /// <summary>
        /// Represents a goto branch kind.
        /// </summary>
        GoTo = 0x3
    }
}

