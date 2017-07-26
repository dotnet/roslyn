// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents an ordering expression within an <see cref="IOrderByQueryClause"/> in C# or VB.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IOrderingExpression : IOperation
    {
        /// <summary>
        /// <see cref="OrderKind"/> for the ordering expression.
        /// </summary>
        OrderKind OrderKind { get; }
        /// <summary>
        /// Underlying ordering expression for the order by query clause.
        /// </summary>
        IOperation Expression { get; }
    }
}
