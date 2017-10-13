// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Kinds of loop statements.
    /// </summary>
    public enum LoopKind
    {
        /// <summary>
        /// Represents a <see cref="IDoLoopStatement"/> in C# or VB.
        /// </summary>
        Do = 0x1,

        /// <summary>
        /// Represents a <see cref="IWhileLoopStatement"/> in C# or VB.
        /// </summary>
        While = 0x2,
        
        /// <summary>
        /// Indicates a <see cref="IForLoopStatement"/> in C#.
        /// </summary>
        For = 0x3,
        
        /// <summary>
        /// Indicates a <see cref="IForToLoopStatement"/> in VB.
        /// </summary>
        ForTo = 0x4,
        
        /// <summary>
        /// Indicates a <see cref="IForEachLoopStatement"/> in C# or VB.
        /// </summary>
        ForEach = 0x5
    }
}

