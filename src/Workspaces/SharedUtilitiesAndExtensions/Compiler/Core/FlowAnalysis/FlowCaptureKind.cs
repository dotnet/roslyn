// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Indicates the kind of flow capture in an <see cref="IFlowCaptureOperation"/>.
    /// </summary>
    internal enum FlowCaptureKind
    {
        /// <summary>
        /// Indicates an R-Value flow capture, i.e. capture of a symbol's value.
        /// </summary>
        RValueCapture,

        /// <summary>
        /// Indicates an L-Value flow capture, i.e. captures of a symbol's location/address.
        /// </summary>
        LValueCapture,

        /// <summary>
        /// Indicates both an R-Value and an L-Value flow capture, i.e. captures of a symbol's value and location/address.
        /// These are generated for left of a compound assignment operation, such that there is conditional code on the right side of the compound assignment.
        /// </summary>
        LValueAndRValueCapture
    }
}
