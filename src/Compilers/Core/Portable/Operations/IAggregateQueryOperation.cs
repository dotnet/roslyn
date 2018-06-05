// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a standalone VB query Aggregate operation with more than one item in Into clause.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// PROTOTYPE(dataflow): Figure out how to make this API public. See BoundAggregateClause node in VB compiler.
    /// </remarks>
    internal interface IAggregateQueryOperation : IOperation
    {
        IOperation Group { get; }

        // PROTOTYPE(dataflow): At the moment, this node uses IPlaceholderOperation to refer to the Group.
        //                      Need to come up with a better design for the public API.
        IOperation Aggregation { get; }
    }
}
