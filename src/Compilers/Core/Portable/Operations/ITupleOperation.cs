// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a tuple with one or more elements.
    /// <para>
    /// Current usage:
    ///  (1) C# tuple expression.
    ///  (2) VB tuple expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ITupleOperation : IOperation
    {
        /// <summary>
        /// Tuple elements.
        /// </summary>
        ImmutableArray<IOperation> Elements { get; }
        /// <summary>
        /// Natural type of the tuple, or null if tuple doesn't have a natural type.
        /// Natural type can be different from <see cref="IOperation.Type" /> depending on the
        /// conversion context, in which the tuple is used.
        /// </summary>
        ITypeSymbol NaturalType { get; }
    }
}
