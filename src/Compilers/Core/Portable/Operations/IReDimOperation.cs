// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents the ReDim operation to re-allocate storage space for array variables.
    /// <para>
    /// Current usage:
    ///  (1) VB ReDim statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IReDimOperation : IOperation
    {
        /// <summary>
        /// Individual clauses of the ReDim operation.
        /// </summary>
        ImmutableArray<IReDimClauseOperation> Clauses { get; }
        /// <summary>
        /// Modifier used to preserve the data in the existing array when you change the size of only the last dimension.
        /// </summary>
        bool Preserve { get; }
    }
}
