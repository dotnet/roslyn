using System;
using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// An operation that specifies a range in which any
    /// wrapping operation will be suppressed.
    /// </summary>
    public interface ISuppressOperation
    {
        SuppressOption Option { get; }

        TextSpan Span { get; }
        CommonSyntaxToken StartToken { get; }
        CommonSyntaxToken EndToken { get; }
    }
}