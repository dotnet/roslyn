using System;
using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// Options for AdjustNewLinesOperation.
    /// 
    /// PreserveLines means the operation will leave lineBreaks as it is if original lineBreaks are
    /// equal or greater than given lineBreaks
    /// 
    /// ForceLines means the operation will force existing lineBreaks to the given lineBreaks.
    /// </summary>
    public enum AdjustNewLinesOption
    {
        PreserveLines,
        ForceLines
    }
}