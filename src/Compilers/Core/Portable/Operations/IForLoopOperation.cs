// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a for loop.
    /// <para>
    /// Current usage:
    ///  (1) C# 'for' loop statement
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IForLoopOperation : ILoopOperation
    {
        /// <summary>
        /// List of operations to execute before entry to the loop. For C#, this comes from the first clause of the for statement.
        /// </summary>
        ImmutableArray<IOperation> Before { get; }
        /// <summary>
        /// Locals declared within the loop Condition and are in scope throughout the <see cref="Condition" />,
        /// <see cref="ILoopOperation.Body" /> and <see cref="AtLoopBottom" />.
        /// They are considered to be declared per iteration.
        /// </summary>
        ImmutableArray<ILocalSymbol> ConditionLocals { get; }
        /// <summary>
        /// Condition of the loop. For C#, this comes from the second clause of the for statement.
        /// </summary>
        IOperation Condition { get; }
        /// <summary>
        /// List of operations to execute at the bottom of the loop. For C#, this comes from the third clause of the for statement.
        /// </summary>
        ImmutableArray<IOperation> AtLoopBottom { get; }
    }
}
