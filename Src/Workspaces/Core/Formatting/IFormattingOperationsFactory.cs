using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// A factory that creates formatting operations.
    /// </summary>
    public interface IFormattingOperationsFactory
    {
        IAnchorIndentationOperation CreateAnchorIndentationOperation(CommonSyntaxToken baseToken, CommonSyntaxToken startToken, CommonSyntaxToken endToken);
        ISuppressOperation CreateSuppressOperation(CommonSyntaxToken startToken, CommonSyntaxToken endToken, SuppressOption option);
        IIndentBlockOperation CreateIndentBlockOperation(CommonSyntaxToken startToken, CommonSyntaxToken endToken, int indentationDelta, IndentBlockOption option);
        IIndentBlockOperation CreateRelativeIndentBlockOperation(CommonSyntaxToken baseToken, CommonSyntaxToken startToken, CommonSyntaxToken endToken, int indentationDelta, IndentBlockOption option);
        IAdjustNewLinesOperation CreateAdjustNewLinesOperation(int line, AdjustNewLinesOption option);
        IAdjustSpacesOperation CreateAdjustSpacesOperation(int space, AdjustSpacesOption option);
        IAlignTokensOperation CreateAlignTokensOperation(CommonSyntaxToken baseToken, IEnumerable<CommonSyntaxToken> tokens, AlignTokensOption option);
    }
}