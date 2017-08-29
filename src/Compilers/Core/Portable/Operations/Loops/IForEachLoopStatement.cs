// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a C# 'foreach' statement or a VB 'For Each' staement.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IForEachLoopStatement : ILoopStatement
    {
        /// <summary>
        /// Iteration variable of the loop.
        /// </summary>
        ILocalSymbol IterationVariable { get; }

        /// <summary>
        /// Optional loop control variable in VB that refers to the operation for declaring a new local variable or reference an existing variable or an expression.
        /// This field is always null for C#.
        /// </summary>
        IOperation LoopControlVariable { get; }

        /// <summary>
        /// Collection value over which the loop iterates.
        /// </summary>
        IOperation Collection { get; }

        /// <summary>
        /// Optional list comma separate operations to execute at loop bottom in VB.
        /// This list is always empty for C#.
        /// </summary>
        ImmutableArray<IOperation> AtLoopBottomExpressionList { get; }
    }
}

