using System;
using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// Options for SuppressOperation
    /// 
    /// NoWrappingIfOnSingleLine means no wrapping if given tokens are on same line
    /// NoWrapping means no wrapping regardless of relative positions of two tokens
    /// NoSpacing means no spacing regardless of relative positions of two tokens
    /// 
    /// </summary>
    [Flags]
    public enum SuppressOption
    {
        NoWrappingIfOnSingleLine = 0x1,
        NoWrappingIfOnMultipleLine = 0x2,
        NoWrapping = NoWrappingIfOnSingleLine | NoWrappingIfOnMultipleLine,
        NoSpacingIfOnSingleLine = 0x4,
        NoSpacingIfOnMultipleLine = 0x8,
        NoSpacing = NoSpacingIfOnSingleLine | NoSpacingIfOnMultipleLine,
    }
}