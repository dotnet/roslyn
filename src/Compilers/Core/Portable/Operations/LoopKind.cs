// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Kinds of loops.
    /// </summary>
    public enum LoopKind
    {
        None = 0x0,

        /// <summary>
        /// Indicates a C# while or do loop, or a VB While or Do loop.
        /// </summary>
        WhileUntil = 0x1,
        /// <summary>
        /// Indicates a C# for loop or a VB For loop.
        /// </summary>
        For = 0x2,
        /// <summary>
        /// Indicates a C# foreach loop or a VB For Each loop.
        /// </summary>
        ForEach = 0x3
    }
}

