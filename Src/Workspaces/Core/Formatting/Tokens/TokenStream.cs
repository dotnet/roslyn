using System;
using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services.Internal.Measurement;
using Roslyn.Services.Shared.Extensions;
using Roslyn.Services.Shared.Utilities;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// this class takes care of tokens consumed in the formatting engine.
    /// 
    /// it will maintain information changed compared to original token information. and
    /// answers information about tokens.
    /// </summary>
    internal partial class TokenStream : IEnumerable<TokenWithIndex>
    {
        // magic number to guess number of tokens in a file from file size
        private const int MagicFileSizeToTokensRatio = 10;

        // caches token information within given formatting span to improve perf
        private readonly List<CommonSyntaxToken> tokens;
        private readonly Dictionary<CommonSyntaxToken, int> tokenToIndexMap;

        // caches original trivia info to improve perf
        private readonly TriviaData[] cachedOriginalTriviaInfo;

        // formatting engine can be used either with syntax tree or without
        // this will reconstruct information that reside in syntax tree from root node
        // if syntax tree is not given
        private readonly TreeData treeInfo;

        // hold onto information that are made to original trivia info
        private readonly Changes changes;

        // factory that will cache trivia info
        private readonly AbstractTriviaDataFactory factory;

        // formatEngine
        private readonly AbstractFormatEngine formatEngine;

        // func caches
        private readonly Func<TokenWithIndex, TokenWithIndex, TriviaData> getTriviaInfo;
        private readonly Func<TokenWithIndex, TokenWithIndex, TriviaData> getOriginalTriviaInfo;

        public TokenStream(
            AbstractFormatEngine formatEngine,
            TextSpan spanToFormat)
        {
            Contract.ThrowIfNull(formatEngine);

            using (MeasurementBlockFactorySelector.ActiveFactory.BeginNew(FunctionId.Services_FormattingEngine_TokenStreamConstruction))
            {
                // initialize basic info
                this.formatEngine = formatEngine;
                this.factory = formatEngine.CreateTriviaFactory();
                this.treeInfo = formatEngine.TreeData;

                // use some heuristics to get initial size of list rather than blindly start from default size == 4
                int sizeOfList = spanToFormat.Length / MagicFileSizeToTokensRatio;
                this.tokens = new List<CommonSyntaxToken>(sizeOfList);
                this.tokens.AddRange(this.treeInfo.GetApplicableTokens(spanToFormat));

                Contract.ThrowIfFalse(this.TokenCount > 0);

                // initialize trivia related info
                this.changes = new Changes();
                this.cachedOriginalTriviaInfo = new TriviaData[this.TokenCount - 1];

                this.tokenToIndexMap = new Dictionary<CommonSyntaxToken, int>(this.TokenCount);
                for (int i = 0; i < this.TokenCount; i++)
                {
                    this.tokenToIndexMap.Add(this.tokens[i], i);
                }

                // Func Cache
                this.getTriviaInfo = this.GetTriviaInfo;
                this.getOriginalTriviaInfo = this.GetOriginalTriviaInfo;
            }

            DebugCheckTokenOrder();
        }

        [Conditional("DEBUG")]
        private void DebugCheckTokenOrder()
        {
            // things should be already in sorted manner, but just to make sure
            // run sort
            var previousToken = this.tokens[0];
            for (int i = 1; i < this.tokens.Count; i++)
            {
                var currentToken = this.tokens[i];
                Contract.ThrowIfFalse(previousToken.FullSpan.End <= currentToken.FullSpan.Start);

                previousToken = currentToken;
            }
        }

        public List<CommonSyntaxToken> Tokens
        {
            get
            {
                return this.tokens;
            }
        }

        public bool FormatBeginningOfFile
        {
            get
            {
                return this.treeInfo.IsFirstToken(this.FirstTokenInStream.Token);
            }
        }

        public bool FormatEndOfFile
        {
            get
            {
                // last token except end of file token
                return this.treeInfo.IsLastToken(this.LastTokenInStream.Token);
            }
        }

        public TokenWithIndex FirstTokenInStream
        {
            get
            {
                return new TokenWithIndex(this, 0, this.tokens[0]);
            }
        }

        public TokenWithIndex LastTokenInStream
        {
            get
            {
                return new TokenWithIndex(this, this.TokenCount - 1, this.tokens[this.TokenCount - 1]);
            }
        }

        public int TokenCount
        {
            get
            {
                return this.tokens.Count;
            }
        }

        public bool IsFormattingWholeDocument
        {
            get
            {
                if (!this.FormatBeginningOfFile)
                {
                    return false;
                }

                // we need to special case "end of file token" case.
                // when we actually have end of file token, we need to include that in our token pairs since
                // it could contain leading trivia.
                var lastToken = this.LastTokenInStream.Token;
                if (this.treeInfo.IsEndOfFileToken(lastToken))
                {
                    return true;
                }

                return this.FormatEndOfFile;
            }
        }

        public CommonSyntaxToken GetToken(int index)
        {
            Contract.ThrowIfFalse(0 <= index && index < this.TokenCount);
            return this.tokens[index];
        }

        public TokenWithIndex GetTokenWithIndex(CommonSyntaxToken token)
        {
            var indexInStream = GetTokenIndexInStream(token);
            return new TokenWithIndex(this, indexInStream, token);
        }

        public TokenWithIndex GetPreviousToken(TokenWithIndex tokenWithIndex)
        {
            if (tokenWithIndex.IndexInStream > 0 && tokenWithIndex.IndexInStream < this.TokenCount)
            {
                return new TokenWithIndex(this, tokenWithIndex.IndexInStream - 1, this.tokens[tokenWithIndex.IndexInStream - 1]);
            }

            // get previous token and check whether it is the last token in the stream
            var previousToken = tokenWithIndex.Token.GetPreviousToken(CommonSyntaxHelper.Any);
            var lastIndex = this.TokenCount - 1;
            if (this.tokens[lastIndex].Equals(previousToken))
            {
                return new TokenWithIndex(this, lastIndex, this.tokens[lastIndex]);
            }

            return new TokenWithIndex(this, -1, previousToken);
        }

        public TokenWithIndex GetNextToken(TokenWithIndex tokenWithIndex)
        {
            if (tokenWithIndex.IndexInStream >= 0 && tokenWithIndex.IndexInStream < this.TokenCount - 1)
            {
                return new TokenWithIndex(this, tokenWithIndex.IndexInStream + 1, this.tokens[tokenWithIndex.IndexInStream + 1]);
            }

            // get next token and check whether it is the first token in the stream
            var nextToken = tokenWithIndex.Token.GetNextToken(CommonSyntaxHelper.Any);
            if (this.tokens[0].Equals(nextToken))
            {
                return new TokenWithIndex(this, 0, this.tokens[0]);
            }

            return new TokenWithIndex(this, -1, nextToken);
        }

        public bool TwoTokensOnSameLine(CommonSyntaxToken token1, CommonSyntaxToken token2)
        {
            // this can happen on invalid code.
            if (token1.Span.End > token2.Span.Start)
            {
                return false;
            }

            var tokenWithIndex1 = GetTokenWithIndex(token1);
            var tokenWithIndex2 = GetTokenWithIndex(token2);

            TokenWithIndex previousToken = tokenWithIndex1;
            for (var current = tokenWithIndex1.GetNextTokenWithIndex();
                     current.Token.Kind != 0 && !current.Token.Equals(tokenWithIndex2.Token);
                     current = current.GetNextTokenWithIndex())
            {
                if (this.GetTriviaInfo(previousToken, current).SecondTokenIsFirstTokenOnLine)
                {
                    return false;
                }

                previousToken = current;
            }

            // check last one.
            return !this.GetTriviaInfo(previousToken, tokenWithIndex2).SecondTokenIsFirstTokenOnLine;
        }

        public void ApplyBeginningOfFileChange(TriviaData info)
        {
            Contract.ThrowIfNull(info);
            this.changes.AddOrReplace(Changes.BeginningOfFileKey, info);
        }

        public void ApplyEndOfFileChange(TriviaData info)
        {
            Contract.ThrowIfNull(info);
            this.changes.AddOrReplace(Changes.EndOfFileKey, info);
        }

        public void ApplyChange(int pairIndex, TriviaData triviaInfo)
        {
            Contract.ThrowIfNull(triviaInfo);
            Contract.ThrowIfFalse(0 <= pairIndex && pairIndex < this.TokenCount - 1);

            // do reference equality check
            var sameAsOriginal = GetOriginalTriviaInfoInStream(pairIndex) == triviaInfo;

            if (this.changes.Contains(pairIndex))
            {
                if (sameAsOriginal)
                {
                    this.changes.Remove(pairIndex);
                    return;
                }

                // okay it already exist.
                // replace existing one
                this.changes.Replace(pairIndex, triviaInfo);
                return;
            }

            // triviaInfo is same as original, nothing to do here.
            if (sameAsOriginal)
            {
                return;
            }

            this.changes.Add(pairIndex, triviaInfo);
        }

        public int GetIndentationOfToken(TokenWithIndex token)
        {
            var previousToken = this.GetPreviousToken(token);
            var triviaInfo = this.GetTriviaInfo(previousToken, token);

            // the very first token of the tree
            if (previousToken.Token.Kind == 0)
            {
                // use space info instead of indentation
                return triviaInfo.Space;
            }

            Contract.ThrowIfFalse(triviaInfo.SecondTokenIsFirstTokenOnLine);
            return triviaInfo.Space;
        }

        public int GetCurrentColumn(CommonSyntaxToken token)
        {
            var tokenWithIndex = this.GetTokenWithIndex(token);

            return GetCurrentColumn(tokenWithIndex);
        }

        public int GetCurrentColumn(TokenWithIndex tokenWithIndex)
        {
            return GetColumn(tokenWithIndex, getTriviaInfo);
        }

        public int GetOriginalColumn(CommonSyntaxToken token)
        {
            var tokenWithIndex = this.GetTokenWithIndex(token);

            return GetColumn(tokenWithIndex, getOriginalTriviaInfo);
        }

        /// <summary>
        /// Get column of the token 
        /// * column means text position on a line where all tabs are converted to spaces that first position on a line becomes 0
        /// </summary>
        private int GetColumn(TokenWithIndex currentToken, Func<TokenWithIndex, TokenWithIndex, TriviaData> triviaInfoGetter)
        {
            // at the begining of a file.
            var previousToken = currentToken.GetPreviousTokenWithIndex();
            if (previousToken.Token.Kind == 0)
            {
                // use space of the trivia data since it means indentation in this case
                return triviaInfoGetter(previousToken, currentToken).Space;
            }

            var spaces = 0;
            for (; previousToken.Token.Kind != 0; previousToken = previousToken.GetPreviousTokenWithIndex())
            {
                var triviaInfo = triviaInfoGetter(previousToken, currentToken);
                if (triviaInfo.SecondTokenIsFirstTokenOnLine)
                {
                    // current token is the first token on line.
                    // add up spaces so far and triviaInfo.Space which means indentation in this case
                    return spaces + triviaInfo.Space;
                }

                // add spaces so far
                spaces += triviaInfo.Space;

                // here, we can't just add token's length since there is token that span multiple lines.
                var tokenWidthInfo = this.formatEngine.GetTokenWidthInfo(previousToken.Token);
                if (tokenWidthInfo.ContainsLineBreak)
                {
                    // add up spaces so far and tokenWidthInfo.Width which means indentation in this case
                    return spaces + tokenWidthInfo.Width;
                }

                // add spaces so far
                spaces += tokenWidthInfo.Width;

                currentToken = previousToken;
            }

            // we reached beginning of the tree, add spaces at the beginning of the tree
            return spaces + triviaInfoGetter(previousToken, currentToken).Space;
        }

        public IEnumerable<ValueTuple<TextSpan, TriviaData>> GetAllTriviaInfoWithSpan()
        {
            // the very first trivia in the file case
            if (this.FormatBeginningOfFile)
            {
                var firstToken = this.FirstTokenInStream.Token;
                var triviaInfo = this.GetTriviaInfoAtBeginningOfFile();

                yield return this.treeInfo.GetFirstTriviaDataAndSpan(firstToken, triviaInfo);
            }

            // regular trivia cases
            for (int pairIndex = 0; pairIndex < this.TokenCount - 1; pairIndex++)
            {
                var triviaInfo = this.GetTriviaInfoInStream(pairIndex);

                yield return ValueTuple.Create(TextSpan.FromBounds(this.tokens[pairIndex].Span.End, this.tokens[pairIndex + 1].Span.Start), triviaInfo);
            }

            // the very last trivia in the file case
            if (this.FormatEndOfFile)
            {
                var lastToken = this.LastTokenInStream.Token;
                var triviaInfo = this.GetTriviaInfoAtEndOfFile();

                yield return this.treeInfo.GetLastTriviaDataAndSpan(lastToken, triviaInfo);
            }
        }

        public TriviaData GetTriviaInfo(TokenWithIndex token1, TokenWithIndex token2)
        {
            // special cases (beginning of a file, end of a file)
            if (token1.Token.Kind == 0)
            {
                return this.FormatBeginningOfFile ? GetTriviaInfoAtBeginningOfFile() : GetOriginalTriviaInfo(token1, token2);
            }

            if (this.treeInfo.IsEndOfFileToken(token2.Token))
            {
                return this.FormatEndOfFile ? GetTriviaInfoAtEndOfFile() : GetOriginalTriviaInfo(token1, token2);
            }

            // normal cases
            Contract.ThrowIfFalse(token1.Token.Span.End <= token2.Token.Span.Start);
            Contract.ThrowIfFalse(token1.IndexInStream < 0 || token2.IndexInStream < 0 || (token1.IndexInStream + 1 == token2.IndexInStream));
            Contract.ThrowIfFalse((token1.IndexInStream >= 0 && token2.IndexInStream >= 0) || token1.Token.Equals(token2.Token.GetPreviousToken(CommonSyntaxHelper.Any)));

            // one of token is out side of cached token stream
            if (token1.IndexInStream < 0 || token2.IndexInStream < 0)
            {
                return GetOriginalTriviaInfo(token1, token2);
            }

            return GetTriviaInfoInStream(token1.IndexInStream);
        }

        private TriviaData GetOriginalTriviaInfo(TokenWithIndex token1, TokenWithIndex token2)
        {
            // special cases (beginning of a file, end of a file)
            if (token1.Token.Kind == 0)
            {
                return this.factory.CreateLeadingTrivia(token2.Token);
            }
            else if (token2.Token.Kind == 0)
            {
                return this.factory.CreateTrailingTrivia(token1.Token);
            }
            else if (this.treeInfo.IsEndOfFileToken(token2.Token))
            {
                return this.factory.Create(token1.Token, token2.Token);
            }

            Contract.ThrowIfFalse(token1.Token.Span.End <= token2.Token.Span.Start);
            Contract.ThrowIfFalse(token1.IndexInStream < 0 || token2.IndexInStream < 0 || (token1.IndexInStream + 1 == token2.IndexInStream));
            Contract.ThrowIfFalse((token1.IndexInStream >= 0 && token2.IndexInStream >= 0) || token1.Token.Equals(token2.Token.GetPreviousToken(CommonSyntaxHelper.Any)));

            if (token1.IndexInStream < 0 || token2.IndexInStream < 0)
            {
                return this.factory.Create(token1.Token, token2.Token);
            }

            return GetOriginalTriviaInfoInStream(token1.IndexInStream);
        }

        public TriviaData GetTriviaInfoAtBeginningOfFile()
        {
            Contract.ThrowIfFalse(this.FormatBeginningOfFile);

            if (this.changes.Contains(Changes.BeginningOfFileKey))
            {
                return this.changes[Changes.BeginningOfFileKey];
            }

            Contract.ThrowIfFalse(this.treeInfo.IsFirstToken(this.FirstTokenInStream.Token));
            return GetOriginalTriviaInfo(default(TokenWithIndex), this.FirstTokenInStream);
        }

        public TriviaData GetTriviaInfoAtEndOfFile()
        {
            Contract.ThrowIfFalse(this.FormatEndOfFile);

            if (this.changes.Contains(Changes.EndOfFileKey))
            {
                return this.changes[Changes.EndOfFileKey];
            }

            Contract.ThrowIfFalse(this.treeInfo.IsLastToken(this.LastTokenInStream.Token));
            return GetOriginalTriviaInfo(this.LastTokenInStream, this.LastTokenInStream.GetNextTokenWithIndex());
        }

        public TriviaData GetTriviaInfoInStream(int pairIndex)
        {
            Contract.ThrowIfFalse(0 <= pairIndex && pairIndex < this.TokenCount - 1);

            if (this.changes.Contains(pairIndex))
            {
                return this.changes[pairIndex];
            }

            // no change between two tokens, return trivia info from original code
            return GetOriginalTriviaInfoInStream(pairIndex);
        }

        private TriviaData GetOriginalTriviaInfoInStream(int pairIndex)
        {
            Contract.ThrowIfFalse(0 <= pairIndex && pairIndex < this.TokenCount - 1);

            if (this.cachedOriginalTriviaInfo[pairIndex] == null)
            {
                var info = this.factory.Create(this.tokens[pairIndex], this.tokens[pairIndex + 1]);
                this.cachedOriginalTriviaInfo[pairIndex] = info;
            }

            return this.cachedOriginalTriviaInfo[pairIndex];
        }

        private TokenWithIndex GetOriginalFirstTokenOnLine(TokenWithIndex tokenWithIndex)
        {
            while (tokenWithIndex.Token.Kind != 0)
            {
                var previousTokenWithIndex = tokenWithIndex.GetPreviousTokenWithIndex();

                if (IsOriginalFirstTokenOnLine(previousTokenWithIndex, tokenWithIndex))
                {
                    return tokenWithIndex;
                }

                tokenWithIndex = previousTokenWithIndex;
            }

            return Contract.FailWithReturn<TokenWithIndex>("this can never happen");
        }

        private bool IsOriginalFirstTokenOnLine(TokenWithIndex previousTokenWithIndex, TokenWithIndex tokenWithIndex)
        {
            if (previousTokenWithIndex.Token.Kind == 0)
            {
                // reached first line inside of tree
                return true;
            }

            Contract.ThrowIfFalse(tokenWithIndex.Equals(previousTokenWithIndex.GetNextTokenWithIndex()));

            // see if there are changes for a given token pair
            return this.GetOriginalTriviaInfo(previousTokenWithIndex, tokenWithIndex).SecondTokenIsFirstTokenOnLine;
        }

        // this can be called with tokens that are outside of token stream
        public TokenWithIndex GetFirstTokenOnLine(CommonSyntaxToken token)
        {
            var tokenWithIndex = this.GetTokenWithIndex(token);
            return this.GetFirstTokenOnLine(tokenWithIndex);
        }

        private TokenWithIndex GetFirstTokenOnLine(TokenWithIndex tokenWithIndex)
        {
            while (tokenWithIndex.Token.Kind != 0)
            {
                var previousTokenWithIndex = this.GetPreviousToken(tokenWithIndex);

                if (this.IsFirstTokenOnLine(previousTokenWithIndex, tokenWithIndex))
                {
                    return tokenWithIndex;
                }

                tokenWithIndex = previousTokenWithIndex;
            }

            return Contract.FailWithReturn<TokenWithIndex>("this can never happen");
        }

        public bool IsFirstTokenOnLine(CommonSyntaxToken token)
        {
            Contract.ThrowIfTrue(token.Kind == 0);

            var tokenWithIndex = this.GetTokenWithIndex(token);
            var previousTokenWithIndex = tokenWithIndex.GetPreviousTokenWithIndex();

            return IsFirstTokenOnLine(previousTokenWithIndex, tokenWithIndex);
        }

        // this can be called with tokens that are outside of token stream
        private bool IsFirstTokenOnLine(TokenWithIndex previousTokenWithIndex, TokenWithIndex tokenWithIndex)
        {
            if (previousTokenWithIndex.Token.Kind == 0)
            {
                // reached first line inside of tree
                return true;
            }

            Debug.Assert(tokenWithIndex.Equals(previousTokenWithIndex.GetNextTokenWithIndex()));

            // see if there are changes for a given token pair
            return this.GetTriviaInfo(previousTokenWithIndex, tokenWithIndex).SecondTokenIsFirstTokenOnLine;
        }

        private int GetTokenIndexInStream(CommonSyntaxToken token)
        {
            int value;
            if (this.tokenToIndexMap.TryGetValue(token, out value))
            {
                return value;
            }

            return -1;
        }

        public IEnumerator<TokenWithIndex> GetEnumerator()
        {
            if (this.TokenCount == 0)
            {
                yield break;
            }

            // get initial one
            var token = this.FirstTokenInStream;

            // iterate through
            while (token.IndexInStream >= 0)
            {
                yield return token;
                token = token.GetNextTokenWithIndex();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
