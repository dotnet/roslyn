using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    public partial class FormattingOperationsFactory
    {
        internal class AnchorIndentationOperation : AbstractRangeOperation, IAnchorIndentationOperation
        {
            public AnchorIndentationOperation(CommonSyntaxToken baseToken, CommonSyntaxToken startToken, CommonSyntaxToken endToken) :
                base(TextSpan.FromBounds(startToken.Span.End, endToken.Span.End), startToken, endToken)
            {
                Contract.ThrowIfTrue(baseToken.Kind == 0);

                this.BaseToken = baseToken;
            }

            public CommonSyntaxToken BaseToken { get; private set; }
        }
    }
}
