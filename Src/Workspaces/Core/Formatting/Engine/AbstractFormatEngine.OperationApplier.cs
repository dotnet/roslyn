// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract partial class AbstractFormatEngine
    {
        /// <summary>
        /// this actually applies formatting operations to trivia between two tokens
        /// </summary>
        private class OperationApplier
        {
            private readonly FormattingContext context;
            private readonly TokenStream tokenStream;
            private readonly ChainedFormattingRules formattingRules;

            public OperationApplier(FormattingContext context, TokenStream tokenStream, ChainedFormattingRules formattingRules)
            {
                this.context = context;
                this.tokenStream = tokenStream;
                this.formattingRules = formattingRules;
            }

            public bool Apply(AdjustSpacesOperation operation, int pairIndex)
            {
                if (operation.Option == AdjustSpacesOption.PreserveSpaces)
                {
                    return ApplyPreserveSpacesOperation(operation, pairIndex);
                }

                if (operation.Option == AdjustSpacesOption.ForceSpaces)
                {
                    return ApplyForceSpacesOperation(operation, pairIndex);
                }

                return ApplySpaceIfSingleLine(operation, pairIndex);
            }

            private bool ApplyPreserveSpacesOperation(AdjustSpacesOperation operation, int pairIndex)
            {
                var triviaInfo = this.tokenStream.GetTriviaData(pairIndex);
                var space = operation.Space;

                if (triviaInfo.SecondTokenIsFirstTokenOnLine)
                {
                    return false;
                }

                Contract.ThrowIfFalse(triviaInfo.LineBreaks == 0);

                if (space <= triviaInfo.Spaces)
                {
                    return false;
                }

                this.tokenStream.ApplyChange(pairIndex, triviaInfo.WithSpace(space, context, formattingRules));
                return true;
            }

            public bool ApplyForceSpacesOperation(AdjustSpacesOperation operation, int pairIndex)
            {
                var triviaInfo = this.tokenStream.GetTriviaData(pairIndex);

                if (triviaInfo.LineBreaks == 0 && triviaInfo.Spaces == operation.Space)
                {
                    return false;
                }

                this.tokenStream.ApplyChange(pairIndex, triviaInfo.WithSpace(operation.Space, context, formattingRules));
                return true;
            }

            private bool ApplySpaceIfSingleLine(AdjustSpacesOperation operation, int pairIndex)
            {
                var triviaInfo = this.tokenStream.GetTriviaData(pairIndex);
                var space = operation.Space;

                if (triviaInfo.SecondTokenIsFirstTokenOnLine)
                {
                    return false;
                }

                Contract.ThrowIfFalse(triviaInfo.LineBreaks == 0);

                if (triviaInfo.Spaces == space)
                {
                    return false;
                }

                this.tokenStream.ApplyChange(pairIndex, triviaInfo.WithSpace(space, context, formattingRules));
                return true;
            }

            public bool Apply(AdjustNewLinesOperation operation, int pairIndex, CancellationToken cancellationToken)
            {
                if (operation.Option == AdjustNewLinesOption.PreserveLines)
                {
                    return ApplyPreserveLinesOperation(operation, pairIndex, cancellationToken);
                }
                else if (operation.Option == AdjustNewLinesOption.ForceLines)
                {
                    return ApplyForceLinesOperation(operation, pairIndex, cancellationToken);
                }
                else
                {
                    Debug.Assert(operation.Option == AdjustNewLinesOption.ForceIfSameLine);

                    // We force the tokens to the different line only they are on the same line
                    // else we leave the tokens as it is (Note: We should not preserve too. If we
                    // we do, then that will be counted as a line operation and the indentation of
                    // the second token will be modified)
                    if (tokenStream.TwoTokensOriginallyOnSameLine(tokenStream.GetToken(pairIndex),
                                                                  tokenStream.GetToken(pairIndex + 1)))
                    {
                        return ApplyForceLinesOperation(operation, pairIndex, cancellationToken);
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            private bool ApplyForceLinesOperation(AdjustNewLinesOperation operation, int pairIndex, CancellationToken cancellationToken)
            {
                var triviaInfo = this.tokenStream.GetTriviaData(pairIndex);

                var indentation = this.context.GetBaseIndentation(this.tokenStream.GetToken(pairIndex + 1));
                if (triviaInfo.LineBreaks == operation.Line && triviaInfo.Spaces == indentation && !triviaInfo.TreatAsElastic)
                {
                    // things are already in the shape we want, so we don't actually need to do
                    // anything but, conceptually, we handled this case
                    return true;
                }

                // well, force it regardless original content
                this.tokenStream.ApplyChange(pairIndex, triviaInfo.WithLine(operation.Line, indentation, context, formattingRules, cancellationToken));
                return true;
            }

            public bool ApplyPreserveLinesOperation(
                AdjustNewLinesOperation operation, int pairIndex, CancellationToken cancellationToken)
            {
                var triviaInfo = this.tokenStream.GetTriviaData(pairIndex);

                // okay, check whether there is line between token more than we want
                // check whether we should force it if it is less than given number
                var indentation = this.context.GetBaseIndentation(this.tokenStream.GetToken(pairIndex + 1));
                if (operation.Line > triviaInfo.LineBreaks)
                {
                    // alright force them
                    this.tokenStream.ApplyChange(pairIndex, triviaInfo.WithLine(operation.Line, indentation, context, formattingRules, cancellationToken));
                    return true;
                }

                // lines between tokens are as expected, but indentation is not right
                if (triviaInfo.SecondTokenIsFirstTokenOnLine &&
                    indentation != triviaInfo.Spaces)
                {
                    this.tokenStream.ApplyChange(pairIndex, triviaInfo.WithIndentation(indentation, context, formattingRules, cancellationToken));
                    return true;
                }

                // if PreserveLineOperation's line is set to 0, let space operation to override wrapping operation
                return operation.Line > 0;
            }

            private bool CanAlignBeApplied(
                SyntaxToken token,
                IEnumerable<SyntaxToken> operationTokens,
                out IList<TokenData> tokenData)
            {
                // if there are no tokens to align, or no visible
                // base token to be aligned to, then don't do anything
                if (token.Width() <= 0 || operationTokens.IsEmpty())
                {
                    tokenData = null;
                    return false;
                }

                tokenData = GetTokenWithIndices(operationTokens);

                // no valid tokens. do nothing and return
                if (tokenData.Count == 0)
                {
                    return false;
                }

                return true;
            }

            private bool ApplyAlignment(
                SyntaxToken token,
                IEnumerable<SyntaxToken> tokens,
                Dictionary<SyntaxToken, int> previousChangesMap,
                out IList<TokenData> tokenData,
                CancellationToken cancellationToken)
            {
                if (!CanAlignBeApplied(token, tokens, out tokenData))
                {
                    return false;
                }

                ApplyIndentationToAlignWithGivenToken(token, tokenData, previousChangesMap, cancellationToken);
                return true;
            }

            public bool ApplyAlignment(
                AlignTokensOperation operation, Dictionary<SyntaxToken, int> previousChangesMap, CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(previousChangesMap);

                IList<TokenData> tokenData;

                switch (operation.Option)
                {
                    case AlignTokensOption.AlignIndentationOfTokensToBaseToken:
                        {
                            if (!ApplyAlignment(operation.BaseToken, operation.Tokens, previousChangesMap, out tokenData, cancellationToken))
                            {
                                return false;
                            }

                            break;
                        }

                    case AlignTokensOption.AlignPositionOfTokensToIndentation:
                        {
                            // token will be align to current indentation. for this operation, we don't care
                            // about base token
                            if (operation.Tokens.IsEmpty())
                            {
                                return false;
                            }

                            tokenData = GetTokenWithIndices(operation.Tokens);

                            // no valid tokens. do nothing and return
                            if (tokenData.Count == 0)
                            {
                                return false;
                            }

                            ApplySpacesToAlignWithCurrentIndentation(tokenData, previousChangesMap, cancellationToken);
                            break;
                        }

                    case AlignTokensOption.AlignToFirstTokenOnBaseTokenLine:
                        {
                            if (!ApplyAlignment(tokenStream.FirstTokenOfBaseTokenLine(operation.BaseToken), operation.Tokens, previousChangesMap, out tokenData, cancellationToken))
                            {
                                return false;
                            }

                            break;
                        }

                    default:
                        {
                            return Contract.FailWithReturn<bool>("Unknown option");
                        }
                }

                ApplyIndentationChangesToDependentTokens(tokenData, previousChangesMap, cancellationToken);

                return true;
            }

            private void ApplySpacesToAlignWithCurrentIndentation(
                IList<TokenData> list, Dictionary<SyntaxToken, int> previousChangesMap, CancellationToken cancellationToken)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var currentToken = list[i];

                    // get indentation this token will align to
                    var indentation = this.context.GetBaseIndentation(currentToken.Token);

                    // get previous token
                    var previousToken = this.tokenStream.GetPreviousTokenData(currentToken);

                    // golden, current token is the first token on line
                    var triviaInfo = this.tokenStream.GetTriviaData(previousToken, currentToken);
                    if (triviaInfo.SecondTokenIsFirstTokenOnLine)
                    {
                        ApplyIndentationToGivenPosition(previousToken, currentToken, triviaInfo, indentation, previousChangesMap, cancellationToken);
                        continue;
                    }

                    // okay, we are not the first token on line. do little bit expensive one

                    // get previous token's column
                    var previousTokenColumn = this.tokenStream.GetCurrentColumn(previousToken) + previousToken.Token.Width();

                    // now check whether we can move current token
                    var spacesBetweenTwoTokens = indentation - previousTokenColumn;
                    if (spacesBetweenTwoTokens <= 0)
                    {
                        // nope, we can't move. the previous token position is passed the desired position.
                        continue;
                    }

                    // yes, now we can move.
                    // if there was a existing value, overwrite it. this could happen if a token is moved multiple times
                    // due to different alignment operations
                    previousChangesMap[currentToken.Token] = triviaInfo.Spaces;

                    // the position we are trying to move is either out of scope or already in right shape.
                    // we just pretent that we applied a change here by inserting information to the map, without
                    // actually doing anything.
                    if (previousToken.IndexInStream < 0 || triviaInfo.Spaces == spacesBetweenTwoTokens)
                    {
                        continue;
                    }

                    // before make any change, check whether spacing is allowed
                    var spanBetweenTokens = TextSpan.FromBounds(previousToken.Token.Span.End, currentToken.Token.SpanStart);
                    if (this.context.IsSpacingSuppressed(spanBetweenTokens))
                    {
                        continue;
                    }

                    // okay, update indentation
                    this.tokenStream.ApplyChange(previousToken.IndexInStream, triviaInfo.WithSpace(spacesBetweenTwoTokens, context, formattingRules));
                }
            }

            private void ApplyIndentationToAlignWithGivenToken(
                SyntaxToken token,
                IList<TokenData> list,
                Dictionary<SyntaxToken, int> previousChangesMap,
                CancellationToken cancellationToken)
            {
                // rather than having external new changes map, having snapshot concept
                // in token stream might be easier to understand.
                int baseSpaceOrIndentation = this.tokenStream.GetCurrentColumn(token);

                for (int i = 0; i < list.Count; i++)
                {
                    var currentToken = list[i];
                    var previousToken = this.tokenStream.GetPreviousTokenData(currentToken);

                    var triviaInfo = this.tokenStream.GetTriviaData(previousToken, currentToken);
                    if (!triviaInfo.SecondTokenIsFirstTokenOnLine)
                    {
                        continue;
                    }

                    ApplyIndentationToGivenPosition(
                        previousToken, currentToken, triviaInfo, baseSpaceOrIndentation, previousChangesMap, cancellationToken);
                }
            }

            private void ApplyIndentationToGivenPosition(
                TokenData previousToken,
                TokenData currentToken,
                TriviaData triviaInfo,
                int baseSpaceOrIndentation,
                Dictionary<SyntaxToken, int> previousChangesMap,
                CancellationToken cancellationToken)
            {
                // add or replace existing value. this could happen if a token get moved multiple times
                // due to one being involved in multiple alignment operations
                previousChangesMap[currentToken.Token] = triviaInfo.Spaces;

                if (previousToken.IndexInStream < 0 || triviaInfo.Spaces == baseSpaceOrIndentation)
                {
                    return;
                }

                // before make any change, check whether spacing is allowed
                var spanBetweenTokens = TextSpan.FromBounds(previousToken.Token.Span.End, currentToken.Token.SpanStart);
                if (this.context.IsSpacingSuppressed(spanBetweenTokens))
                {
                    return;
                }

                // okay, update indentation
                this.tokenStream.ApplyChange(
                    previousToken.IndexInStream,
                    triviaInfo.WithIndentation(baseSpaceOrIndentation, context, formattingRules, cancellationToken));
            }

            private IList<TokenData> GetTokenWithIndices(IEnumerable<SyntaxToken> tokens)
            {
                var list = new List<TokenData>();
                foreach (var token in tokens)
                {
                    // if the token is invisible or not exist, skip it.
                    if (token.RawKind == 0 || token.Width() <= 0)
                    {
                        continue;
                    }

                    var tokenWithIndex = this.tokenStream.GetTokenData(token);
                    if (tokenWithIndex.IndexInStream < 0)
                    {
                        // this token is not inside of the formatting span, ignore
                        continue;
                    }

                    list.Add(tokenWithIndex);
                }

                list.Sort((t1, t2) => t1.IndexInStream - t2.IndexInStream);
                return list;
            }

            private bool ApplyIndentationChangesToDependentTokens(
                IList<TokenData> tokenWithIndices, Dictionary<SyntaxToken, int> newChangesMap, CancellationToken cancellationToken)
            {
                for (int i = 0; i < tokenWithIndices.Count; i++)
                {
                    var firstToken = tokenWithIndices[i];

                    // first check whether the token moved by alignment operation have affected an anchor token. if it has,
                    // then find the last token of that anchor span.
                    var endAnchorToken = this.context.GetEndTokenForAnchorSpan(firstToken);
                    if (endAnchorToken.RawKind == 0)
                    {
                        // this means given token is not anchor token, no need to do anything
                        continue;
                    }

                    // first token was anchor token, now find last token with index
                    var lastToken = this.tokenStream.GetTokenData(endAnchorToken);
                    if (lastToken.IndexInStream < 0)
                    {
                        lastToken = this.tokenStream.LastTokenInStream;
                    }

                    ApplyBaseTokenIndentationChangesFromTo(firstToken, firstToken, lastToken, newChangesMap, cancellationToken);
                }

                return true;
            }

            private void ApplyIndentationDeltaFromTo(
                TokenData firstToken,
                TokenData lastToken,
                int indentationDelta,
                Dictionary<SyntaxToken, int> previousChangesMap,
                CancellationToken cancellationToken)
            {
                // can this run parallel? at least finding out all first token on line.
                for (var pairIndex = firstToken.IndexInStream; pairIndex < lastToken.IndexInStream; pairIndex++)
                {
                    var triviaInfo = this.tokenStream.GetTriviaData(pairIndex);
                    if (!triviaInfo.SecondTokenIsFirstTokenOnLine)
                    {
                        continue;
                    }

                    // spacing is suppressed. don't change any spacing
                    if (this.context.IsSpacingSuppressed(pairIndex))
                    {
                        continue;
                    }

                    // bail fast here.
                    // if an entity is in the map, then it means indentation has been applied to the token pair already.
                    // no reason to do same work again.
                    var currentToken = this.tokenStream.GetToken(pairIndex + 1);
                    if (previousChangesMap.ContainsKey(currentToken))
                    {
                        continue;
                    }

                    this.ApplyIndentationDelta(pairIndex, currentToken, indentationDelta, triviaInfo, previousChangesMap, cancellationToken);
                }
            }

            private void ApplyIndentationDelta(
                int pairIndex,
                SyntaxToken currentToken,
                int indentationDelta,
                TriviaData triviaInfo,
                Dictionary<SyntaxToken, int> previousChangesMap,
                CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(triviaInfo.SecondTokenIsFirstTokenOnLine);

                var indentation = triviaInfo.Spaces + indentationDelta;

                if (triviaInfo.Spaces == indentation)
                {
                    // indentation didn't actually move. nothing to change
                    return;
                }

                // record the fact that this pair has been moved
                Debug.Assert(!previousChangesMap.ContainsKey(currentToken));
                previousChangesMap.Add(currentToken, triviaInfo.Spaces);

                // okay, update indentation
                this.tokenStream.ApplyChange(pairIndex, triviaInfo.WithIndentation(indentation, context, formattingRules, cancellationToken));
            }

            public bool ApplyBaseTokenIndentationChangesFromTo(
                SyntaxToken baseToken,
                SyntaxToken startToken,
                SyntaxToken endToken,
                Dictionary<SyntaxToken, int> previousChangesMap,
                CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(baseToken.RawKind != 0 && startToken.RawKind != 0 && endToken.RawKind != 0);

                var baseTokenWithIndex = this.tokenStream.GetTokenData(baseToken);
                var firstTokenWithIndex = this.tokenStream.GetTokenData(startToken).GetPreviousTokenData();
                var lastTokenWithIndex = this.tokenStream.GetTokenData(endToken);

                return ApplyBaseTokenIndentationChangesFromTo(
                    baseTokenWithIndex, firstTokenWithIndex, lastTokenWithIndex, previousChangesMap, cancellationToken);
            }

            private bool ApplyBaseTokenIndentationChangesFromTo(
                TokenData baseToken,
                TokenData startToken,
                TokenData endToken,
                Dictionary<SyntaxToken, int> previousChangesMap,
                CancellationToken cancellationToken)
            {
                // if baseToken is not in the stream, then it is guaranteeded to be not moved.
                var tokenWithIndex = baseToken;
                if (tokenWithIndex.IndexInStream < 0)
                {
                    return false;
                }

                // now, check whether tokens on that the base token depends have been moved.
                // any token before the base token on the same line has implicit dependency over the base token.
                while (tokenWithIndex.IndexInStream >= 0)
                {
                    // check whether given token have moved
                    if (previousChangesMap.ContainsKey(tokenWithIndex.Token))
                    {
                        break;
                    }

                    // okay, this token is not moved, check one before me as long as it is on the same line
                    var tokenPairIndex = tokenWithIndex.IndexInStream - 1;
                    if (tokenPairIndex < 0 ||
                        tokenStream.GetTriviaData(tokenPairIndex).SecondTokenIsFirstTokenOnLine)
                    {
                        return false;
                    }

                    tokenWithIndex = tokenWithIndex.GetPreviousTokenData();
                }

                // didn't find anything moved
                if (tokenWithIndex.IndexInStream < 0)
                {
                    return false;
                }

                // we are not moved
                var indentationDelta = this.context.GetDeltaFromPreviousChangesMap(tokenWithIndex.Token, previousChangesMap);
                if (indentationDelta == 0)
                {
                    return false;
                }

                startToken = startToken.IndexInStream < 0 ? tokenStream.FirstTokenInStream : startToken;
                endToken = endToken.IndexInStream < 0 ? tokenStream.LastTokenInStream : endToken;

                ApplyIndentationDeltaFromTo(startToken, endToken, indentationDelta, previousChangesMap, cancellationToken);
                return true;
            }

            public bool ApplyAnchorIndentation(
                int pairIndex, Dictionary<SyntaxToken, int> previousChangesMap, CancellationToken cancellationToken)
            {
                var triviaInfo = this.tokenStream.GetTriviaData(pairIndex);

                if (!triviaInfo.SecondTokenIsFirstTokenOnLine)
                {
                    return false;
                }

                // don't apply anchor is spacing is suppressed
                if (this.context.IsSpacingSuppressed(pairIndex))
                {
                    return false;
                }

                var firstTokenOnLine = this.tokenStream.GetToken(pairIndex + 1);
                var indentation = triviaInfo.Spaces + this.context.GetAnchorDeltaFromOriginalColumn(firstTokenOnLine);

                if (triviaInfo.Spaces != indentation)
                {
                    // first save previous information
                    previousChangesMap.Add(firstTokenOnLine, triviaInfo.Spaces);

                    // okay, update indentation
                    this.tokenStream.ApplyChange(pairIndex, triviaInfo.WithIndentation(indentation, context, formattingRules, cancellationToken));
                    return true;
                }

                return false;
            }
        }
    }
}