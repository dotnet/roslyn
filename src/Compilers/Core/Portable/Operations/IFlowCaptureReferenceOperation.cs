// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Represents a point of use of an intermediate result captured earlier.
    /// The fact of capturing the result is represented by <see cref="IFlowCaptureOperation" />.
    /// This node is produced only as part of a <see cref="ControlFlowGraph" />.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IFlowCaptureReferenceOperation : IOperation
    {
        /// <summary>
        /// An id used to match references to the same intermediate result.
        /// </summary>
        CaptureId Id { get; }
    }
}
