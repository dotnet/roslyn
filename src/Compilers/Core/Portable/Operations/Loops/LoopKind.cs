// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

