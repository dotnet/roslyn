using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// it holds onto space and wrapping operation need to run between two tokens.
    /// </summary>
    internal struct TokenPairWithOperations
    {
        private readonly TokenStream tokenStream;

        public AdjustSpacesOperation SpaceOperation { get; private set; }
        public AdjustNewLinesOperation LineOperation { get; private set; }

        public int PairIndex { get; private set; }

        public TokenPairWithOperations(
            TokenStream tokenStream,
            int tokenPairIndex,
            AdjustSpacesOperation spaceOperations,
            AdjustNewLinesOperation lineOperations) :
            this()
        {
            Contract.ThrowIfNull(tokenStream);

            Contract.ThrowIfFalse(0 <= tokenPairIndex && tokenPairIndex < tokenStream.TokenCount - 1);

            this.tokenStream = tokenStream;
            this.PairIndex = tokenPairIndex;

            SpaceOperation = spaceOperations;
            LineOperation = lineOperations;
        }

        public CommonSyntaxToken Token1
        {
            get
            {
                return this.tokenStream.GetToken(this.PairIndex);
            }
        }

        public CommonSyntaxToken Token2
        {
            get
            {
                return this.tokenStream.GetToken(this.PairIndex + 1);
            }
        }
    }
}
