// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a null-coalescing expression.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ICoalesceExpression : IOperation
    {
        /// <summary>
        /// Value to be unconditionally evaluated.
        /// </summary>
        IOperation Expression { get; }
        /// <summary>
        /// Value to be evaluated if <see cref="Expression"/> evaluates to null/Nothing.
        /// </summary>
        IOperation WhenNull { get; }
    }
}

