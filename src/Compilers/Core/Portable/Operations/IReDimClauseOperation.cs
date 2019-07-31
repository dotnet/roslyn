// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an individual clause of an <see cref="IReDimOperation" /> to re-allocate storage space for a single array variable.
    /// <para>
    /// Current usage:
    ///  (1) VB ReDim clause.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IReDimClauseOperation : IOperation
    {
        /// <summary>
        /// Operand whose storage space needs to be re-allocated.
        /// </summary>
        IOperation Operand { get; }
        /// <summary>
        /// Sizes of the dimensions of the created array instance.
        /// </summary>
        ImmutableArray<IOperation> DimensionSizes { get; }
    }
}
