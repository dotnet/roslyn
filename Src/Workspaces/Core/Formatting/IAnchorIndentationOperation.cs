using System;
using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// An operation that specifies a base token that tokens
    /// in the specified range will follow.
    /// </summary>
    public interface IAnchorIndentationOperation
    {
        CommonSyntaxToken BaseToken { get; }

        TextSpan Span { get; }
        CommonSyntaxToken StartToken { get; }
        CommonSyntaxToken EndToken { get; }
    }
}