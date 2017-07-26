// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a group or function aggregation expression inside an Into clause of a Group By or Aggregate query clause in VB.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IAggregationExpression : IOperation
    {
        /// <summary>
        /// Flag indicating if this is a group aggregation clause.
        /// </summary>
        bool IsGroupAggregation { get; }

        /// <summary>
        /// Aggregation expression.
        /// </summary>
        IOperation Expression { get; }
    }
}
