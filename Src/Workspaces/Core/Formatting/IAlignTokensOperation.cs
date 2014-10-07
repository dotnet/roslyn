using System;
using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// An operation that will align the token group to same column.
    /// </summary>
    public interface IAlignTokensOperation
    {
        AlignTokensOption Option { get; }

        CommonSyntaxToken BaseToken { get; }
        IEnumerable<CommonSyntaxToken> Tokens { get; }
    }
}