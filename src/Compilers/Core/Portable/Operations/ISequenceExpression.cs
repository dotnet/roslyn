// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a sequence of expressions.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ISequenceExpression : IOperation
    {
        /// <summary>
        /// Expressions contained within the sequence.
        /// </summary>
        ImmutableArray<IOperation> Expressions { get; }
        /// <summary>
        /// The value of the whole sequence expression.
        /// </summary>
        IOperation Value { get; }
        /// <summary>
        /// Local declarations contained within the sequence.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
    }
}
