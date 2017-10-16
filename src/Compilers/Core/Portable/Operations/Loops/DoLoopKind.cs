// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents different kinds of do loop statements.
    /// </summary>
    public enum DoLoopKind
    {
        /// <summary>
        /// Indicates a C# 'do while' or a VB 'Do While' loop where the loop condition is executed at the bottom of the loop, i.e. end of the loop iteration.
        /// Loop executes while the loop condition evaluates to <code>true</code>.
        /// </summary>
        DoWhileBottomLoop = 0x0,

        /// <summary>
        /// Indicates a VB 'Do While' loop with the loop condition executed at the top of the loop, i.e. beginning of the loop iteration.
        /// Loop executes while the loop condition evaluates to <code>true</code>.
        /// </summary>
        DoWhileTopLoop = 0x1,

        /// <summary>
        /// Indicates a VB 'Do Until' loop with the loop condition executed at the bottom of the loop, i.e. end of the loop iteration.
        /// Loop executes while the loop condition evaluates to <code>false</code>.
        /// </summary>
        DoUntilBottomLoop = 0x2,

        /// <summary>
        /// Indicates a VB 'Do Until' loop with the loop condition executed at the top of the loop, i.e. beginning of the loop iteration.
        /// Loop executes while the loop condition evaluates to <code>false</code>.
        /// </summary>
        DoUntilTopLoop = 0x3,

        /// <summary>
        /// Indicates an invalid loop. For example, VB 'Do While' or 'Do Until' loop with syntax errors where both the top and bottom conditions are provided.
        /// </summary>
        Invalid = 0xf,
    }
}

