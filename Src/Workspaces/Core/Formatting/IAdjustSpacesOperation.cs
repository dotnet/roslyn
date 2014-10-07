using System;
using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// An operation that puts spaces between two tokens.
    /// </summary>
    public interface IAdjustSpacesOperation
    {
        AdjustSpacesOption Option { get; }
        int Space { get; }
    }
}