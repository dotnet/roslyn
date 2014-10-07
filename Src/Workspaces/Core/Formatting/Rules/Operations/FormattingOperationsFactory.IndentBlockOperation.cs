using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services.Shared.Utilities;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    public partial class FormattingOperationsFactory
    {
        internal class IndentBlockOperation : AbstractRangeOperation, IIndentBlockOperation
        {
            public IndentBlockOperation(
                CommonSyntaxToken startToken,
                CommonSyntaxToken endToken,
                int indentationDelta,
                IndentBlockOption option) :
                this(default(CommonSyntaxToken),
                         startToken,
                         endToken,
                         indentationDelta,
                         option,
                         isRelativeIndentation: false)
            {
            }

            public IndentBlockOperation(
                CommonSyntaxToken baseToken,
                CommonSyntaxToken startToken,
                CommonSyntaxToken endToken,
                int indentationDelta,
                IndentBlockOption option) :
                this(baseToken,
                         startToken,
                         endToken,
                         indentationDelta,
                         option,
                         isRelativeIndentation: true)
            {
            }

            protected IndentBlockOperation(
                CommonSyntaxToken baseToken,
                CommonSyntaxToken startToken,
                CommonSyntaxToken endToken,
                int indentationDeltaOrPosition,
                IndentBlockOption option,
                bool isRelativeIndentation) :
                base(startToken, endToken)
            {
                Contract.ThrowIfFalse(option.IsMaskOn(IndentBlockOption.IncludeTriviaMask));
                Contract.ThrowIfFalse(option.IsMaskOn(IndentBlockOption.PositionMask));

                // if it is relative indentation, it must be relative position
                Contract.ThrowIfTrue(isRelativeIndentation && !option.IsFlagOn(IndentBlockOption.RelativePosition));

                this.Span = option.IsFlagOn(IndentBlockOption.IncludeOnlyItsOwnTrivia) ?
                    TextSpan.FromBounds(startToken.FullSpan.Start, endToken.FullSpan.End) :
                    GetIndentBlockOperationSpan(startToken, endToken);

                Contract.ThrowIfFalse(baseToken.Span.End <= this.Span.Start);

                this.Option = option;

                this.IsRelativeIndentation = isRelativeIndentation;
                this.BaseToken = baseToken;

                this.IndentationDeltaOrPosition = indentationDeltaOrPosition;
            }

            private static CommonSyntaxNode GetParentThatContainsGivenSpan(CommonSyntaxNode node, int position, bool forward)
            {
                while (node != null)
                {
                    var fullSpan = node.FullSpan;
                    if (forward)
                    {
                        if (fullSpan.Start < position)
                        {
                            return node;
                        }
                    }
                    else
                    {
                        if (position > fullSpan.End)
                        {
                            return node;
                        }
                    }

                    node = node.Parent;
                }

                return null;
            }

            private static TextSpan GetIndentBlockOperationSpan(CommonSyntaxToken startToken, CommonSyntaxToken endToken)
            {
                // unlike other operation, ident block operation requires to include trivia belong to its previous and next tokens.
                // this will create right span for indent block.
                var startPosition = GetStartPositionOfSpan(startToken);
                var endPosition = GetEndPositionOfSpan(endToken);

                return TextSpan.FromBounds(startPosition, endPosition);
            }

            private static int GetEndPositionOfSpan(CommonSyntaxToken token)
            {
                var nextToken = token.GetNextToken();
                if (nextToken.Kind != 0)
                {
                    return nextToken.Span.Start;
                }

                var backwardPosition = token.FullSpan.End;
                var parentNode = GetParentThatContainsGivenSpan(token.Parent, backwardPosition, forward: false);
                if (parentNode == null)
                {
                    // reached the end of tree
                    return token.FullSpan.End;
                }

                Contract.ThrowIfFalse(backwardPosition < parentNode.FullSpan.End);

                nextToken = parentNode.FindToken(backwardPosition + 1);

                Contract.ThrowIfTrue(nextToken.Kind == 0);

                return nextToken.Span.Start;
            }

            private static int GetStartPositionOfSpan(CommonSyntaxToken token)
            {
                var previousToken = token.GetPreviousToken();
                if (previousToken.Kind != 0)
                {
                    return previousToken.Span.End;
                }

                // first token in the tree
                var forwardPosition = token.FullSpan.Start;
                if (forwardPosition <= 0)
                {
                    return 0;
                }

                var parentNode = GetParentThatContainsGivenSpan(token.Parent, forwardPosition, forward: true);
                if (parentNode == null)
                {
                    return Contract.FailWithReturn<int>("This can't happen");
                }

                Contract.ThrowIfFalse(parentNode.FullSpan.Start < forwardPosition);

                previousToken = parentNode.FindToken(forwardPosition + 1);

                Contract.ThrowIfTrue(previousToken.Kind == 0);

                return previousToken.Span.End;
            }

            public IndentBlockOption Option { get; private set; }

            public bool IsRelativeIndentation { get; private set; }
            public CommonSyntaxToken BaseToken { get; private set; }
            public int IndentationDeltaOrPosition { get; private set; }
        }
    }
}
