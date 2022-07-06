// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// This class takes care of tokens consumed in the formatting engine.
    /// 
    /// It will maintain information changed compared to original token information. and answers
    /// information about tokens.
    /// </summary>
    internal partial class TokenStream
    {
        // number to guess number of tokens in a formatting span
        private const int MagicTextLengthToTokensRatio = 10;

        // caches token information within given formatting span to improve perf
        private readonly SegmentedList<SyntaxToken> _tokens;

        // caches original trivia info to improve perf
        private readonly SegmentedArray<TriviaData> _cachedOriginalTriviaInfo;

        // formatting engine can be used either with syntax tree or without
        // this will reconstruct information that reside in syntax tree from root node
        // if syntax tree is not given
        private readonly TreeData _treeData;
        private readonly SyntaxFormattingOptions _options;

        // hold onto information that are made to original trivia info
        private Changes _changes;

        // factory that will cache trivia info
        private readonly AbstractTriviaDataFactory _factory;

        // func caches
        private readonly Func<TokenData, TokenData, TriviaData> _getTriviaData;
        private readonly Func<TokenData, TokenData, TriviaData> _getOriginalTriviaData;

        public TokenStream(TreeData treeData, SyntaxFormattingOptions options, TextSpan spanToFormat, AbstractTriviaDataFactory factory)
        {
            using (Logger.LogBlock(FunctionId.Formatting_TokenStreamConstruction, CancellationToken.None))
            {
                // initialize basic info
                _factory = factory;
                _treeData = treeData;
                _options = options;

                // use some heuristics to get initial size of list rather than blindly start from default size == 4
                var sizeOfList = spanToFormat.Length / MagicTextLengthToTokensRatio;
                _tokens = new SegmentedList<SyntaxToken>(sizeOfList);
                _tokens.AddRange(_treeData.GetApplicableTokens(spanToFormat));

                Debug.Assert(this.TokenCount > 0);

                // initialize trivia related info
                _cachedOriginalTriviaInfo = new SegmentedArray<TriviaData>(this.TokenCount - 1);

                // Func Cache
                _getTriviaData = this.GetTriviaData;
                _getOriginalTriviaData = this.GetOriginalTriviaData;
            }

            DebugCheckTokenOrder();
        }

        [Conditional("DEBUG")]
        private void DebugCheckTokenOrder()
        {
            // things should be already in sorted manner, but just to make sure
            // run sort
            var previousToken = _tokens[0];
            for (var i = 1; i < _tokens.Count; i++)
            {
                var currentToken = _tokens[i];
                Debug.Assert(previousToken.FullSpan.End <= currentToken.FullSpan.Start);

                previousToken = currentToken;
            }
        }

        public bool FormatBeginningOfTree
        {
            get
            {
                return _treeData.IsFirstToken(this.FirstTokenInStream.Token);
            }
        }

        public bool FormatEndOfTree
        {
            get
            {
                // last token except end of file token
                return _treeData.IsLastToken(this.LastTokenInStream.Token);
            }
        }

        public bool IsFormattingWholeDocument
        {
            get
            {
                return this.FormatBeginningOfTree && this.FormatEndOfTree;
            }
        }

        public TokenData FirstTokenInStream
        {
            get
            {
                return new TokenData(this, 0, _tokens[0]);
            }
        }

        public TokenData LastTokenInStream
        {
            get
            {
                return new TokenData(this, this.TokenCount - 1, _tokens[this.TokenCount - 1]);
            }
        }

        public int TokenCount => _tokens.Count;

        public SyntaxToken GetToken(int index)
        {
            Contract.ThrowIfFalse(0 <= index && index < this.TokenCount);
            return _tokens[index];
        }

        public TokenData GetTokenData(SyntaxToken token)
        {
            var indexInStream = GetTokenIndexInStream(token);
            return new TokenData(this, indexInStream, token);
        }

        public TokenData GetPreviousTokenData(TokenData tokenData)
        {
            if (tokenData.IndexInStream > 0 && tokenData.IndexInStream < this.TokenCount)
            {
                return new TokenData(this, tokenData.IndexInStream - 1, _tokens[tokenData.IndexInStream - 1]);
            }

            // get previous token and check whether it is the last token in the stream
            var previousToken = tokenData.Token.GetPreviousToken(includeZeroWidth: true);
            var lastIndex = this.TokenCount - 1;
            if (_tokens[lastIndex].Equals(previousToken))
            {
                return new TokenData(this, lastIndex, _tokens[lastIndex]);
            }

            return new TokenData(this, -1, previousToken);
        }

        public TokenData GetNextTokenData(TokenData tokenData)
        {
            if (tokenData.IndexInStream >= 0 && tokenData.IndexInStream < this.TokenCount - 1)
            {
                return new TokenData(this, tokenData.IndexInStream + 1, _tokens[tokenData.IndexInStream + 1]);
            }

            // get next token and check whether it is the first token in the stream
            var nextToken = tokenData.Token.GetNextToken(includeZeroWidth: true);
            if (_tokens[0].Equals(nextToken))
            {
                return new TokenData(this, 0, _tokens[0]);
            }

            return new TokenData(this, -1, nextToken);
        }

        internal SyntaxToken FirstTokenOfBaseTokenLine(SyntaxToken token)
        {
            var currentTokenData = this.GetTokenData(token);
            while (!this.IsFirstTokenOnLine(token))
            {
                var previousTokenData = this.GetPreviousTokenData(currentTokenData);
                token = previousTokenData.Token;
                currentTokenData = previousTokenData;
            }

            return token;
        }

        public bool TwoTokensOriginallyOnSameLine(SyntaxToken token1, SyntaxToken token2)
            => TwoTokensOnSameLineWorker(token1, token2, _getOriginalTriviaData);

        public bool TwoTokensOnSameLine(SyntaxToken token1, SyntaxToken token2)
            => TwoTokensOnSameLineWorker(token1, token2, _getTriviaData);

        private bool TwoTokensOnSameLineWorker(SyntaxToken token1, SyntaxToken token2, Func<TokenData, TokenData, TriviaData> triviaDataGetter)
        {
            // check easy case
            if (token1 == token2)
            {
                return true;
            }

            // this can happen on invalid code.
            if (token1.Span.End > token2.SpanStart)
            {
                return false;
            }

            // this has potential to be very expansive if everything is on same line in a big file. but chances to that happen are very low
            var tokenData1 = GetTokenData(token1);
            var tokenData2 = GetTokenData(token2);

            var previousToken = tokenData1;
            for (var current = tokenData1.GetNextTokenData(); current < tokenData2; current = current.GetNextTokenData())
            {
                if (triviaDataGetter(previousToken, current).SecondTokenIsFirstTokenOnLine)
                {
                    return false;
                }

                previousToken = current;
            }

            // check last one.
            return !triviaDataGetter(previousToken, tokenData2).SecondTokenIsFirstTokenOnLine;
        }

        public void ApplyBeginningOfTreeChange(TriviaData data)
        {
            Contract.ThrowIfNull(data);
            _changes.AddOrReplace(Changes.BeginningOfTreeKey, data);
        }

        public void ApplyEndOfTreeChange(TriviaData data)
        {
            Contract.ThrowIfNull(data);
            _changes.AddOrReplace(Changes.EndOfTreeKey, data);
        }

        public void ApplyChange(int pairIndex, TriviaData data)
        {
            Contract.ThrowIfNull(data);
            Contract.ThrowIfFalse(0 <= pairIndex && pairIndex < this.TokenCount - 1);

            // do reference equality check
            var sameAsOriginal = GetOriginalTriviaData(pairIndex) == data;
            if (sameAsOriginal)
            {
                _changes.TryRemove(pairIndex);
            }
            else
            {
                _changes.AddOrReplace(pairIndex, data);
            }
        }

        public int GetCurrentColumn(SyntaxToken token)
        {
            var tokenWithIndex = this.GetTokenData(token);

            return GetCurrentColumn(tokenWithIndex);
        }

        public int GetCurrentColumn(TokenData tokenData)
            => GetColumn(tokenData, _getTriviaData);

        public int GetOriginalColumn(SyntaxToken token)
        {
            var tokenWithIndex = this.GetTokenData(token);
            return GetColumn(tokenWithIndex, _getOriginalTriviaData);
        }

        /// <summary>
        /// Get column of the token 
        /// * column means text position on a line where all tabs are converted to spaces that first position on a line becomes 0
        /// </summary>
        private int GetColumn(TokenData tokenData, Func<TokenData, TokenData, TriviaData> triviaDataGetter)
        {
            // at the beginning of a file.
            var previousToken = tokenData.GetPreviousTokenData();

            var spaces = 0;
            for (; previousToken.Token.RawKind != 0; previousToken = previousToken.GetPreviousTokenData())
            {
                var triviaInfo = triviaDataGetter(previousToken, tokenData);
                if (triviaInfo.SecondTokenIsFirstTokenOnLine)
                {
                    // current token is the first token on line.
                    // add up spaces so far and triviaInfo.Space which means indentation in this case
                    return spaces + triviaInfo.Spaces;
                }

                // add spaces so far
                spaces += triviaInfo.Spaces;
                // here, we can't just add token's length since there is token that span multiple lines.
                GetTokenLength(previousToken.Token, out var tokenLength, out var multipleLines);

                if (multipleLines)
                {
                    return spaces + tokenLength;
                }

                spaces += tokenLength;
                tokenData = previousToken;
            }

            // we reached beginning of the tree, add spaces at the beginning of the tree
            return spaces + triviaDataGetter(previousToken, tokenData).Spaces;
        }

        public void GetTokenLength(SyntaxToken token, out int length, out bool onMultipleLines)
        {
            // here, we can't just add token's length since there is token that span multiple lines.
            var text = token.ToString();

            // multiple lines
            if (text.ContainsLineBreak())
            {
                // get indentation from last line of the text
                onMultipleLines = true;
                length = text.GetTextColumn(_options.TabSize, initialColumn: 0);
                return;
            }

            onMultipleLines = false;

            // add spaces so far
            if (text.ContainsTab())
            {
                // do expansive calculation
                var initialColumn = _treeData.GetOriginalColumn(_options.TabSize, token);
                length = text.ConvertTabToSpace(_options.TabSize, initialColumn, text.Length);
                return;
            }

            length = text.Length;
        }

        public IEnumerable<ValueTuple<ValueTuple<SyntaxToken, SyntaxToken>, TriviaData>> GetTriviaDataWithTokenPair(CancellationToken cancellationToken)
        {
            // the very first trivia in the file case
            if (this.FormatBeginningOfTree)
            {
                var token = this.FirstTokenInStream.Token;
                var trivia = this.GetTriviaDataAtBeginningOfTree();

                yield return ValueTuple.Create(ValueTuple.Create(default(SyntaxToken), token), trivia);
            }

            // regular trivia cases
            for (var pairIndex = 0; pairIndex < this.TokenCount - 1; pairIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var trivia = this.GetTriviaData(pairIndex);
                yield return ValueTuple.Create(ValueTuple.Create(_tokens[pairIndex], _tokens[pairIndex + 1]), trivia);
            }

            // the very last trivia in the file case
            if (this.FormatEndOfTree)
            {
                var token = this.LastTokenInStream.Token;
                var trivia = this.GetTriviaDataAtEndOfTree();

                yield return ValueTuple.Create(ValueTuple.Create(token, default(SyntaxToken)), trivia);
            }
        }

        public TriviaData GetTriviaData(TokenData token1, TokenData token2)
        {
            // special cases (beginning of a file, end of a file)
            if (_treeData.IsFirstToken(token2.Token))
            {
                return this.FormatBeginningOfTree ? GetTriviaDataAtBeginningOfTree() : GetOriginalTriviaData(token1, token2);
            }

            if (_treeData.IsLastToken(token1.Token))
            {
                return this.FormatEndOfTree ? GetTriviaDataAtEndOfTree() : GetOriginalTriviaData(token1, token2);
            }

            // normal cases
            Debug.Assert(token1.Token.Span.End <= token2.Token.SpanStart);
            Debug.Assert(token1.IndexInStream < 0 || token2.IndexInStream < 0 || (token1.IndexInStream + 1 == token2.IndexInStream));
            Debug.Assert((token1.IndexInStream >= 0 && token2.IndexInStream >= 0) || token1.Token.Equals(token2.Token.GetPreviousToken(includeZeroWidth: true)) || token2.Token.LeadingTrivia.Span.Contains(token1.Token.Span));

            // one of token is out side of cached token stream
            if (token1.IndexInStream < 0 || token2.IndexInStream < 0)
            {
                return GetOriginalTriviaData(token1, token2);
            }

            return GetTriviaData(token1.IndexInStream);
        }

        private TriviaData GetOriginalTriviaData(TokenData token1, TokenData token2)
        {
            // special cases (beginning of a file, end of a file)
            if (_treeData.IsFirstToken(token2.Token))
            {
                return _factory.CreateLeadingTrivia(token2.Token);
            }
            else if (_treeData.IsLastToken(token1.Token))
            {
                return _factory.CreateTrailingTrivia(token1.Token);
            }

            Debug.Assert(token1.Token.Span.End <= token2.Token.SpanStart);
            Debug.Assert(token1.IndexInStream < 0 || token2.IndexInStream < 0 || (token1.IndexInStream + 1 == token2.IndexInStream));
            Debug.Assert((token1.IndexInStream >= 0 && token2.IndexInStream >= 0) || token1.Token.Equals(token2.Token.GetPreviousToken(includeZeroWidth: true)) || token2.Token.LeadingTrivia.Span.Contains(token1.Token.Span));

            if (token1.IndexInStream < 0 || token2.IndexInStream < 0)
            {
                return _factory.Create(token1.Token, token2.Token);
            }

            return GetOriginalTriviaData(token1.IndexInStream);
        }

        public TriviaData GetTriviaDataAtBeginningOfTree()
        {
            Contract.ThrowIfFalse(this.FormatBeginningOfTree);
            if (_changes.TryGet(Changes.BeginningOfTreeKey, out var data))
            {
                return data;
            }

            Debug.Assert(_treeData.IsFirstToken(this.FirstTokenInStream.Token));
            return GetOriginalTriviaData(default, this.FirstTokenInStream);
        }

        public TriviaData GetTriviaDataAtEndOfTree()
        {
            Contract.ThrowIfFalse(this.FormatEndOfTree);
            if (_changes.TryGet(Changes.EndOfTreeKey, out var data))
            {
                return data;
            }

            Debug.Assert(_treeData.IsLastToken(this.LastTokenInStream.Token));
            return GetOriginalTriviaData(this.LastTokenInStream, default);
        }

        public TriviaData GetTriviaData(int pairIndex)
        {
            Contract.ThrowIfFalse(0 <= pairIndex && pairIndex < this.TokenCount - 1);
            if (_changes.TryGet(pairIndex, out var data))
            {
                return data;
            }

            // no change between two tokens, return trivia info from original code
            return GetOriginalTriviaData(pairIndex);
        }

        private TriviaData GetOriginalTriviaData(int pairIndex)
        {
            Contract.ThrowIfFalse(0 <= pairIndex && pairIndex < this.TokenCount - 1);

            if (_cachedOriginalTriviaInfo[pairIndex] == null)
            {
                var info = _factory.Create(_tokens[pairIndex], _tokens[pairIndex + 1]);
                _cachedOriginalTriviaInfo[pairIndex] = info;
            }

            return _cachedOriginalTriviaInfo[pairIndex];
        }

        public bool IsFirstTokenOnLine(SyntaxToken token)
        {
            Contract.ThrowIfTrue(token.RawKind == 0);

            var tokenWithIndex = this.GetTokenData(token);
            var previousTokenWithIndex = tokenWithIndex.GetPreviousTokenData();

            return IsFirstTokenOnLine(previousTokenWithIndex, tokenWithIndex);
        }

        // this can be called with tokens that are outside of token stream
        private bool IsFirstTokenOnLine(TokenData tokenData1, TokenData tokenData2)
        {
            if (tokenData1.Token.RawKind == 0)
            {
                // reached first line inside of tree
                return true;
            }

            Debug.Assert(tokenData2 == tokenData1.GetNextTokenData());

            // see if there are changes for a given token pair
            return this.GetTriviaData(tokenData1, tokenData2).SecondTokenIsFirstTokenOnLine;
        }

        private int GetTokenIndexInStream(SyntaxToken token)
        {
            var tokenIndex = _tokens.BinarySearch(token, TokenOrderComparer.Instance);
            if (tokenIndex < 0)
            {
                return -1;
            }

            // Source characters cannot be assigned to multiple tokens. If the token has non-zero width, it will be an
            // exact match for at most one token.
            if (!token.FullSpan.IsEmpty)
            {
                return _tokens[tokenIndex] == token ? tokenIndex : -1;
            }

            // Multiple tokens can have empty spans. The binary search operation will return one of them; look forward
            // and then backward to locate the desired token within the set of one or more zero-width tokens located at
            // the same position.
            Debug.Assert(token.FullSpan.IsEmpty);
            for (var i = tokenIndex; i < _tokens.Count; i++)
            {
                if (!_tokens[i].FullSpan.IsEmpty)
                {
                    // Current token can't match because the span is different, and future tokens won't match because
                    // they are lexicographically after the token we are interested in.
                    break;
                }

                if (_tokens[i] == token)
                {
                    return i;
                }
            }

            for (var i = tokenIndex - 1; i >= 0; i--)
            {
                if (!_tokens[i].FullSpan.IsEmpty)
                {
                    // Current token can't match because the span is different, and future tokens won't match because
                    // they are lexicographically before the token we are interested in.
                    break;
                }

                if (_tokens[i] == token)
                {
                    return i;
                }
            }

            return -1;
        }

        public IEnumerable<(int index, SyntaxToken currentToken, SyntaxToken nextToken)> TokenIterator
        {
            get
            {
                return new Iterator(_tokens);
            }
        }

        private sealed class TokenOrderComparer : IComparer<SyntaxToken>
        {
            public static readonly TokenOrderComparer Instance = new();

            private TokenOrderComparer() { }

            public int Compare(SyntaxToken x, SyntaxToken y)
                => x.FullSpan.CompareTo(y.FullSpan);
        }
    }
}
