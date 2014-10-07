using System;
using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// An operation that specifies an indentation level on a specific range.
    /// </summary>
    public interface IIndentBlockOperation
    {
        IndentBlockOption Option { get; }

        bool IsRelativeIndentation { get; }
        CommonSyntaxToken BaseToken { get; }

        int IndentationDeltaOrPosition { get; }

        TextSpan Span { get; }
        CommonSyntaxToken StartToken { get; }
        CommonSyntaxToken EndToken { get; }
    }
}