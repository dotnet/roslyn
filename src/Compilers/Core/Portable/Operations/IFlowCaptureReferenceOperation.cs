// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Represents a point of use of an intermediate result captured earlier. 
    /// The fact of capturing the result is represented by <see cref="IFlowCaptureOperation"/>.
    /// This node is produced only as part of a <see cref="ControlFlowGraph"/>.
    /// </summary>
    public interface IFlowCaptureReferenceOperation : IOperation
    {
        /// <summary>
        /// An id used to match references to the same intermediate result.
        /// </summary>
        CaptureId Id { get; }
    }
}
