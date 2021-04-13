// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Kinds of loop operations.
    /// </summary>
    public enum LoopKind
    {
        /// <summary>
        /// Represents unknown loop kind.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Represents an <see cref="IWhileLoopOperation"/> in C# or VB.
        /// </summary>
        While = 0x1,

        /// <summary>
        /// Indicates an <see cref="IForLoopOperation"/> in C#.
        /// </summary>
        For = 0x2,

        /// <summary>
        /// Indicates an <see cref="IForToLoopOperation"/> in VB.
        /// </summary>
        ForTo = 0x3,

        /// <summary>
        /// Indicates an <see cref="IForEachLoopOperation"/> in C# or VB.
        /// </summary>
        ForEach = 0x4
    }
}

