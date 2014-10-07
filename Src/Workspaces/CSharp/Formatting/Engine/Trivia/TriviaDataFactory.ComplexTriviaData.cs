using System;
using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Compilers.CSharp;
using Roslyn.Services.Formatting;
using Roslyn.Utilities;

namespace Roslyn.Services.CSharp.Formatting
{
    internal partial class TriviaDataFactory
    {
        /// <summary>   
        /// represents a general trivia between two tokens. slightly more expensive than others since it
        /// needs to calculate stuff unlike other cases
        /// </summary>
        private class ComplexTriviaData : CSharpTriviaData
        {
            public TreeData TreeInfo { get; private set; }
            public SyntaxToken Token1 { get; private set; }
            public SyntaxToken Token2 { get; private set; }
            public string OriginalString { get; private set; }

            private readonly bool treatAsElastic;

            public ComplexTriviaData(FormattingOptions options, TreeData treeInfo, SyntaxToken token1, SyntaxToken token2) :
                base(options)
            {
                Contract.ThrowIfNull(treeInfo);

                this.Token1 = token1;
                this.Token2 = token2;

                this.treatAsElastic = HasAnyWhitespaceElasticTrivia(token1, token2);

                this.TreeInfo = treeInfo;
                this.OriginalString = this.TreeInfo.GetTextBetween(token1, token2);

                int lineBreaks;
                int spaces;
                this.OriginalString.ProcessTextBetweenTokens(this.TreeInfo, token1, this.Options.TabSize, out lineBreaks, out spaces);

                this.LineBreaks = lineBreaks;
                this.Space = spaces;
            }

            public override bool TreatAsElastic
            {
                get { return this.treatAsElastic; }
            }

            public override bool IsWhitespaceOnlyTrivia
            {
                get { return false; }
            }

            public override bool ShouldReplaceOriginalWithNewString
            {
                get
                {
                    return false;
                }
            }

            public override string NewString
            {
                get
                {
                    return Contract.FailWithReturn<string>("Should never be called");
                }
            }

            public override List<SyntaxTrivia> TriviaList
            {
                get
                {
                    return Contract.FailWithReturn<List<SyntaxTrivia>>("Should never be called");
                }
            }

            public override TriviaData WithSpace(int space)
            {
                // two tokens are on a singleline, we dont allow changing spaces between two tokens that contain
                // noisy characters between them.
                if (!this.SecondTokenIsFirstTokenOnLine)
                {
                    return this;
                }

                // okay, two tokens are on different lines, we are basically asked to remove line breaks between them
                // and make them to be on a single line. well, that is not allowed when there are noisy chars between them
                if (this.SecondTokenIsFirstTokenOnLine)
                {
                    return this;
                }

                return Contract.FailWithReturn<TriviaData>("Can not reach here");
            }

            public override TriviaData WithLine(int line, int indentation)
            {
                Contract.ThrowIfFalse(line > 0);

                // if we have elastic trivia, always let it be modified
                if (this.treatAsElastic)
                {
                    return new ModifiedComplexTriviaData(this.Options, this, line, indentation);
                }

                // two tokens are on a single line, it is always allowed to put those two tokens on a different lines
                if (!this.SecondTokenIsFirstTokenOnLine)
                {
                    return new ModifiedComplexTriviaData(this.Options, this, line, indentation);
                }

                // okay, two tokens are on different lines, now we need to see whether we can add more lines or not
                if (this.SecondTokenIsFirstTokenOnLine)
                {
                    // we are asked to add more lines. sure, no problem
                    if (this.LineBreaks < line)
                    {
                        return new ModifiedComplexTriviaData(this.Options, this, line, indentation);
                    }

                    // we already has same number of lines, but it is asking changing indentation
                    if (this.LineBreaks == line)
                    {
                        return WithIndentation(indentation);
                    }

                    // sorry, we can't reduce lines if it contains noisy chars
                    if (this.LineBreaks > line)
                    {
                        return this;
                    }
                }

                return Contract.FailWithReturn<TriviaData>("Can not reach here");
            }

            public override TriviaData WithIndentation(int indentation)
            {
                // if tokens are not in different line, there is nothing we can do here
                if (!this.SecondTokenIsFirstTokenOnLine)
                {
                    return this;
                }

                // well, we are already in a desired format, nothing to do. return as it is.
                if (this.Space == indentation)
                {
                    return this;
                }

                return new ModifiedComplexTriviaData(this.Options, this, this.LineBreaks, indentation);
            }

            public override void Format(FormattingContext context, Action<int, TriviaData> formattingResultApplier, int tokenPairIndex)
            {
                var triviaList = new TriviaList(this.Token1.TrailingTrivia, this.Token2.LeadingTrivia);
                Contract.ThrowIfFalse(triviaList.Count > 0);

                // okay, now, check whether we need or are able to format noisy tokens
                if (TriviaFormatter.ContainsSkippedTokensOrText(triviaList))
                {
                    return;
                }

                if (!ShouldFormat(context, triviaList))
                {
                    return;
                }

                formattingResultApplier(tokenPairIndex,
                    new FormattedComplexTriviaData(context, this.Token1, this.Token2, this.LineBreaks, this.Space, this.OriginalString));
            }

            private bool ShouldFormat(FormattingContext context, TriviaList triviaList)
            {
                if (!this.SecondTokenIsFirstTokenOnLine)
                {
                    return TriviaFormatter.ShouldFormatTriviaOnSingleLine(triviaList);
                }

                Debug.Assert(this.SecondTokenIsFirstTokenOnLine);

                var desiredIndentation = context.GetBaseIndentation(triviaList[0].Span.Start);
                var firstTriviaInTree = this.Token1.Kind == SyntaxKind.None;
                return TriviaFormatter.ShouldFormatTriviaOnMultipleLines(context.Options, firstTriviaInTree, desiredIndentation, triviaList);
            }

            private static bool HasAnyWhitespaceElasticTrivia(SyntaxToken previousToken, SyntaxToken currentToken)
            {
                if (!previousToken.HasTrailingTrivia && !currentToken.HasLeadingTrivia)
                {
                    return false;
                }

                return HasAnyWhitespaceElasticTrivia(previousToken.TrailingTrivia) || HasAnyWhitespaceElasticTrivia(currentToken.LeadingTrivia);
            }

            private static bool HasAnyWhitespaceElasticTrivia(SyntaxTriviaList list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var trivia = list[i];

                    if (trivia.IsElastic)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}