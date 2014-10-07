using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Formatting
{
    public partial class FormattingOperationsFactory
    {
        internal class SuppressOperation : AbstractRangeOperation, ISuppressOperation
        {
            public SuppressOperation(CommonSyntaxToken startToken, CommonSyntaxToken endToken, SuppressOption option) :
                base(TextSpan.FromBounds(startToken.Span.Start, endToken.Span.End), startToken, endToken)
            {
                this.Option = option;
            }

            public SuppressOption Option { get; private set; }
        }
    }
}
