// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// it represents a token that is inside of token stream not also outside of token stream
    /// 
    /// it uses an index to navigate previous and after tokens in the stream to make navigation faster. and regular
    /// Previous/NextToken for tokens outside of the stream.
    /// 
    /// this object is supposed to be live very short but created a lot of time. that is why it is struct. 
    /// (same reason why SyntaxToken is struct - to reduce heap allocation)
    /// </summary>
    internal readonly struct TokenData : IEqualityComparer<TokenData>, IEquatable<TokenData>, IComparable<TokenData>, IComparer<TokenData>
    {
        public TokenData(TokenStream tokenStream, int indexInStream, SyntaxToken token)
        {
            Contract.ThrowIfNull(tokenStream);
            Contract.ThrowIfFalse((indexInStream == -1) || (0 <= indexInStream && indexInStream < tokenStream.TokenCount));

            this.TokenStream = tokenStream;
            this.IndexInStream = indexInStream;
            this.Token = token;
        }

        public TokenStream TokenStream { get; }
        public int IndexInStream { get; }
        public SyntaxToken Token { get; }

        public TokenData GetPreviousTokenData()
        {
            return this.TokenStream.GetPreviousTokenData(this);
        }

        public TokenData GetNextTokenData()
        {
            return this.TokenStream.GetNextTokenData(this);
        }

        public bool Equals(TokenData x, TokenData y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(TokenData obj)
        {
            return obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            return this.Token.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is TokenData data &&
                   this.Equals(data);
        }

        public bool Equals(TokenData other)
        {
            if (this.TokenStream != other.TokenStream)
            {
                return false;
            }

            if (this.IndexInStream >= 0 && this.IndexInStream == other.IndexInStream)
            {
                return true;
            }

            return this.Token.Equals(other.Token);
        }

        public int Compare(TokenData x, TokenData y)
        {
            return x.CompareTo(y);
        }

        public int CompareTo(TokenData other)
        {
            Contract.ThrowIfFalse(this.TokenStream == other.TokenStream);

            if (this.IndexInStream >= 0 && other.IndexInStream >= 0)
            {
                return this.IndexInStream - other.IndexInStream;
            }

            var start = this.Token.SpanStart - other.Token.SpanStart;
            if (start != 0)
            {
                return start;
            }

            var end = this.Token.Span.End - other.Token.Span.End;
            if (end != 0)
            {
                return end;
            }

            // this is expansive check. but there is no other way to check.
            var commonRoot = this.Token.GetCommonRoot(other.Token);
            Debug.Assert(commonRoot != null);

            var tokens = commonRoot.DescendantTokens();

            var index1 = Index(tokens, this.Token);
            var index2 = Index(tokens, other.Token);
            Contract.ThrowIfFalse(index1 >= 0 && index2 >= 0);

            return index1 - index2;
        }

        private int Index(IEnumerable<SyntaxToken> tokens, SyntaxToken token)
        {
            var index = 0;

            foreach (var current in tokens)
            {
                if (current == token)
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        public static bool operator <(TokenData left, TokenData right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(TokenData left, TokenData right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator ==(TokenData left, TokenData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TokenData left, TokenData right)
        {
            return left.Equals(right);
        }
    }
}
