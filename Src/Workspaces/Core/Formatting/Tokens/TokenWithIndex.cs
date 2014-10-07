using System;
using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.Internal;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// it represents a token that is inside of token stream not also outside of token stream
    /// 
    /// it uses an index to navigate previous and after tokens in the stream to make navigation faster. and regular
    /// Previous/NextToken for tokens outside of the stream.
    /// 
    /// this object is supposed to be live very short but created a lot of time. that is why it is struct. 
    /// (same reason why CommonSyntaxToken is struct - to reduce heap allocation)
    /// </summary>
    internal struct TokenWithIndex : IEqualityComparer<TokenWithIndex>, IEquatable<TokenWithIndex>
    {
        private readonly TokenStream tokenStream;
        private readonly int indexInStream;
        private readonly CommonSyntaxToken token;

        public TokenWithIndex(TokenStream tokenStream, int indexInStream, CommonSyntaxToken token)
        {
            Contract.ThrowIfNull(tokenStream);
            Contract.ThrowIfFalse((indexInStream == -1) || (0 <= indexInStream && indexInStream < tokenStream.TokenCount));

            this.tokenStream = tokenStream;
            this.indexInStream = indexInStream;
            this.token = token;
        }

        public TokenStream TokenStream { get { return this.tokenStream; } }
        public int IndexInStream { get { return this.indexInStream; } }
        public CommonSyntaxToken Token { get { return this.token; } }

        public TokenWithIndex GetPreviousTokenWithIndex()
        {
            return this.TokenStream.GetPreviousToken(this);
        }

        public TokenWithIndex GetNextTokenWithIndex()
        {
            return this.TokenStream.GetNextToken(this);
        }

        public bool Equals(TokenWithIndex x, TokenWithIndex y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(TokenWithIndex obj)
        {
            return obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            return this.token.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is TokenWithIndex)
            {
                return this.Equals((TokenWithIndex)obj);
            }

            return false;
        }

        public bool Equals(TokenWithIndex other)
        {
            if (this.tokenStream != other.tokenStream)
            {
                return false;
            }

            if (this.IndexInStream >= 0 && this.IndexInStream == other.IndexInStream)
            {
                return true;
            }

            return this.Token.Equals(other.Token);
        }
    }
}
