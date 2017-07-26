// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a query expression in C# or VB.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IQueryExpression : IOperation
    {
        /// <summary>
        /// Last <see cref="IQueryClause"/> or <see cref="IQueryContinuation"/> in the unrolled query expression.
        /// For example, for the query expression "from x in set where x.Name != null select x.Name", the select clause is the last clause of the unrolled query expression,
        /// with the where clause as one of its descendant, and the from clause as the descendant of the where clause.
        /// </summary>
        IOperation LastClauseOrContinuation { get; }
    }
}
