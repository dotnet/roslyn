using System;
using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// Options for IndentBlockOperation
    /// </summary>
    [Flags]
    public enum IndentBlockOption
    {
        /// <summary>
        /// IndentationDeltaOrPosition will be interpreted as delta of its enclosing indentation
        /// </summary>
        RelativePosition = 0x4,

        /// <summary>
        /// IndentationDeltaOrPosition will be interpreted as absolute position
        /// </summary>
        AbsolutePosition = 0x8,

        /// <summary>
        /// Mask for position options
        /// </summary>
        PositionMask = RelativePosition | AbsolutePosition
    }
}