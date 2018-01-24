// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents that an intermediate result is being captured.
    /// This node is produced only as part of a flow graph.
    /// PROTOTYPE(dataflow): Finalize the design how capturing/referencing intermediate results is represented.
    /// </summary>
    public interface IFlowCaptureOperation : IOperation
    {
        /// <summary>
        /// An id used to match references to the same intermediate result.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Value to be captured.
        /// </summary>
        IOperation Value { get; }
    }
}

