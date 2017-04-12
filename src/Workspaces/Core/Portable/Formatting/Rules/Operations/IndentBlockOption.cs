// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// Options for IndentBlockOperation
    /// </summary>
    [Flags]
    internal enum IndentBlockOption
    {
        /// <summary>
        /// This indentation will be a delta to the first token in the line in which the base token is present
        /// </summary>
        RelativeToFirstTokenOnBaseTokenLine = 0x2,

        /// <summary>
        /// IndentationDeltaOrPosition will be interpreted as delta of its enclosing indentation
        /// </summary>
        RelativePosition = 0x4,

        /// <summary>
        /// IndentationDeltaOrPosition will be interpreted as absolute position
        /// </summary>
        AbsolutePosition = 0x8,

        /// <summary>
        /// Mask for relative position options
        /// </summary>
        RelativePositionMask = RelativeToFirstTokenOnBaseTokenLine | RelativePosition,

        /// <summary>
        /// Mask for position options
        /// </summary>
        PositionMask = RelativeToFirstTokenOnBaseTokenLine | RelativePosition | AbsolutePosition
    }
}
