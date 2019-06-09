// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Represents that an intermediate result is being captured.
    /// This node is produced only as part of a <see cref="ControlFlowGraph"/>.
    /// </summary>
    public interface IFlowCaptureOperation : IOperation
    {
        /// <summary>
        /// An id used to match references to the same intermediate result.
        /// </summary>
        CaptureId Id { get; }

        /// <summary>
        /// Value to be captured.
        /// </summary>
        IOperation Value { get; }
    }
}

