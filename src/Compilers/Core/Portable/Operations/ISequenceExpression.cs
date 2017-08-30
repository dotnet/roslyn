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
        /// Side effects of the expression.
        /// </summary>
        ImmutableArray<IOperation> SideEffects { get; }
        /// <summary>
        /// The value of the expression.
        /// </summary>
        IOperation Value { get; }
        /// <summary>
        /// Local declarations contained within the expression.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
    }
}
