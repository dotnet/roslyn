using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    public partial class FormattingOperationsFactory
    {
        internal abstract class AbstractRangeOperation
        {
            public AbstractRangeOperation(CommonSyntaxToken startToken, CommonSyntaxToken endToken)
            {
                Contract.ThrowIfTrue(startToken.Kind == 0);
                Contract.ThrowIfTrue(endToken.Kind == 0);

                this.StartToken = startToken;
                this.EndToken = endToken;
            }

            public AbstractRangeOperation(TextSpan span, CommonSyntaxToken startToken, CommonSyntaxToken endToken)
            {
                Contract.ThrowIfTrue(span.Start < 0 || span.Length < 0);
                Contract.ThrowIfTrue(startToken.Kind == 0);
                Contract.ThrowIfTrue(endToken.Kind == 0);

                this.Span = span;
                this.StartToken = startToken;
                this.EndToken = endToken;
            }

            public TextSpan Span { get; protected set; }

            public CommonSyntaxToken StartToken { get; private set; }
            public CommonSyntaxToken EndToken { get; private set; }
        }
    }
}
