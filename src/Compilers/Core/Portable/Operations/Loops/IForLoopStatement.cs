// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a C# 'for' loop statement.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IForLoopStatement : ILoopStatement
    {
        /// <summary>
        /// List of operations to execute before entry to the loop. This comes from the first clause of the for statement.
        /// </summary>
        ImmutableArray<IOperation> Before { get; }

        /// <summary>
        /// Condition of the loop. This comes from the second clause of the for statement.
        /// </summary>
        IOperation Condition { get; }

        /// <summary>
        /// List of operations to execute at the bottom of the loop. This comes from the third clause of the for statement.
        /// </summary>
        ImmutableArray<IOperation> AtLoopBottom { get; }
    }
}

