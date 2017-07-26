// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a query clause in C# or VB.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IQueryClause : IOperation
    {
        /// <summary>
        /// <see cref="QueryClauseKind"/> of the clause.
        /// </summary>
        QueryClauseKind ClauseKind { get; }

        /// <summary>
        /// Underlying reduced expression for the query clause. This is normally the invocation expression for the underlying linq call.
        /// </summary>
        IOperation ReducedExpression { get; }
    }
}
