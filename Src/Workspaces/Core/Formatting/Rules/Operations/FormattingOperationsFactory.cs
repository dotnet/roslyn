using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services.Shared.Utilities;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    // Review: this doesn't have any state. should this be a static class and remove
    // FormattingOperationsFactory property from Formatter?
    public partial class FormattingOperationsFactory
    {
        public static readonly FormattingOperationsFactory Instance = new FormattingOperationsFactory();

        private static readonly AdjustNewLinesOperation PreserveZeroLine = new AdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
        private static readonly AdjustNewLinesOperation PreserveOneLine = new AdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);

        private static readonly AdjustSpacesOperation DefaultOneSpaceIfOnSingleLine = new AdjustSpacesOperation(1, AdjustSpacesOption.DefaultSpacesIfOnSingleLine);
        private static readonly AdjustSpacesOperation ForceOneSpaceIfOnSingleLine = new AdjustSpacesOperation(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
        private static readonly AdjustSpacesOperation ForceZeroSpaceIfOnSingleLine = new AdjustSpacesOperation(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);

        // only we can initiate this class
        internal FormattingOperationsFactory()
        {
        }

        /// <summary>
        /// create anchor indentation region around start and end token
        /// start token will act as anchor token and right after anchor token to end of end token will become anchor region
        /// </summary>
        public AnchorIndentationOperation CreateAnchorIndentationOperation(CommonSyntaxToken startToken, CommonSyntaxToken endToken)
        {
            return CreateAnchorIndentationOperation(startToken, startToken, endToken, TextSpan.FromBounds(startToken.Span.End, endToken.Span.End));
        }

        /// <summary>
        /// create anchor indentation region more explicitly by providing all necessary information.
        /// </summary>
        public AnchorIndentationOperation CreateAnchorIndentationOperation(CommonSyntaxToken anchorToken, CommonSyntaxToken startToken, CommonSyntaxToken endToken, TextSpan textSpan)
        {
            return new AnchorIndentationOperation(anchorToken, startToken, endToken, textSpan);
        }

        /// <summary>
        /// create suppress region around start and end token
        /// </summary>
        public SuppressOperation CreateSuppressOperation(CommonSyntaxToken startToken, CommonSyntaxToken endToken, SuppressOption option)
        {
            return CreateSuppressOperation(startToken, endToken, TextSpan.FromBounds(startToken.Span.Start, endToken.Span.End), option);
        }

        /// <summary>
        /// create suppress region around the given text span
        /// </summary>
        private SuppressOperation CreateSuppressOperation(CommonSyntaxToken startToken, CommonSyntaxToken endToken, TextSpan textSpan, SuppressOption option)
        {
            return new SuppressOperation(startToken, endToken, textSpan, option);
        }

        /// <summary>
        /// create indent block region around the start and end token with the given indentation delta added to the existing indentation at the position of the start token
        /// </summary>
        public IndentBlockOperation CreateIndentBlockOperation(CommonSyntaxToken startToken, CommonSyntaxToken endToken, int indentationDelta, IndentBlockOption option)
        {
            var span = CommonFormattingHelpers.GetSpanIncludingTrailingAndLeadingTriviaOfAdjacentTokens(startToken, endToken);
            return CreateIndentBlockOperation(startToken, endToken, span, indentationDelta, option);
        }

        /// <summary>
        /// create indent block region around the given text span with the given indentation delta added to the existing indentation at the position of the start token
        /// </summary>
        public IndentBlockOperation CreateIndentBlockOperation(CommonSyntaxToken startToken, CommonSyntaxToken endToken, TextSpan textSpan, int indentationDelta, IndentBlockOption option)
        {
            return new IndentBlockOperation(startToken, endToken, textSpan, indentationDelta, option);
        }

        /// <summary>
        /// create indent block region around the start and end token with the given indentation delta added to the column of the base token
        /// </summary>
        public IndentBlockOperation CreateRelativeIndentBlockOperation(CommonSyntaxToken baseToken, CommonSyntaxToken startToken, CommonSyntaxToken endToken, int indentationDelta, IndentBlockOption option)
        {
            var span = CommonFormattingHelpers.GetSpanIncludingTrailingAndLeadingTriviaOfAdjacentTokens(startToken, endToken);

            return CreateRelativeIndentBlockOperation(baseToken, startToken, endToken, span, indentationDelta, option);
        }

        /// <summary>
        /// create indent block region around the given text span with the given indentation delta added to the column of the base token
        /// </summary>
        public IndentBlockOperation CreateRelativeIndentBlockOperation(CommonSyntaxToken baseToken, CommonSyntaxToken startToken, CommonSyntaxToken endToken, TextSpan textSpan, int indentationDelta, IndentBlockOption option)
        {
            return new IndentBlockOperation(baseToken, startToken, endToken, textSpan, indentationDelta, option);
        }

        /// <summary>
        /// instruct the engine to try to align first tokens on the lines among the given tokens to be aligned to the base token
        /// </summary>
        public AlignTokensOperation CreateAlignTokensOperation(CommonSyntaxToken baseToken, IEnumerable<CommonSyntaxToken> tokens, AlignTokensOption option)
        {
            return new AlignTokensOperation(baseToken, tokens, option);
        }

        /// <summary>
        /// instruct the engine to try to put the give lines between two tokens
        /// </summary>
        public AdjustNewLinesOperation CreateAdjustNewLinesOperation(int line, AdjustNewLinesOption option)
        {
            if (line == 0 && option == AdjustNewLinesOption.PreserveLines)
            {
                return PreserveZeroLine;
            }
            else if (line == 1 && option == AdjustNewLinesOption.PreserveLines)
            {
                return PreserveOneLine;
            }

            return new AdjustNewLinesOperation(line, option);
        }

        /// <summary>
        /// instruct the engine to try to put the given spaces between two tokens
        /// </summary>
        public AdjustSpacesOperation CreateAdjustSpacesOperation(int space, AdjustSpacesOption option)
        {
            if (space == 1 && option == AdjustSpacesOption.DefaultSpacesIfOnSingleLine)
            {
                return DefaultOneSpaceIfOnSingleLine;
            }
            else if (space == 0 && option == AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            {
                return ForceZeroSpaceIfOnSingleLine;
            }
            else if (space == 1 && option == AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            {
                return ForceOneSpaceIfOnSingleLine;
            }

            return new AdjustSpacesOperation(space, option);
        }
    }
}