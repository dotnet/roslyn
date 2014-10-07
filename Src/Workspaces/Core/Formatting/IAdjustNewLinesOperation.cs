using System;
using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// An operation that puts lineBreaks between two tokens.
    /// </summary>
    public interface IAdjustNewLinesOperation
    {
        AdjustNewLinesOption Option { get; }
        int Line { get; }
    }
}