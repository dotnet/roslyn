// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

