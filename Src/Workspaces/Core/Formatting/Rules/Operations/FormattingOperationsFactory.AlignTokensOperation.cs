using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    public partial class FormattingOperationsFactory
    {
        internal class AlignTokensOperation : IAlignTokensOperation
        {
            public AlignTokensOperation(CommonSyntaxToken baseToken, IEnumerable<CommonSyntaxToken> tokens, AlignTokensOption option)
            {
                Contract.ThrowIfNull(tokens);
                Debug.Assert(!tokens.IsEmpty());

                this.Option = option;
                this.BaseToken = baseToken;
                this.Tokens = tokens;
            }

            public AlignTokensOption Option { get; private set; }

            public CommonSyntaxToken BaseToken { get; private set; }
            public IEnumerable<CommonSyntaxToken> Tokens { get; private set; }
        }
    }
}
