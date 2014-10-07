using System;
using Roslyn.Compilers.Internal;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    internal abstract partial class AbstractTriviaDataFactory
    {
        /// <summary>
        /// represents a general trivia between two tokens. slightly more expensive than others since it
        /// needs to calculate stuff unlike other cases
        /// </summary>
        protected class WhitespaceTriviaData : TriviaData
        {
            private readonly bool elastic;

            public WhitespaceTriviaData(FormattingOptions options, int lineBreaks, int indentation, bool elastic)
                : base(options)
            {
                this.elastic = elastic;

                // space and line breaks can be negative during formatting. but at the end, should be normalized
                // to >= 0
                this.LineBreaks = lineBreaks;
                this.Space = indentation;
            }

            public override bool TreatAsElastic
            {
                get { return elastic; }
            }

            public override bool IsWhitespaceOnlyTrivia
            {
                get { return true; }
            }

            public override bool ShouldReplaceOriginalWithNewString
            {
                get
                {
                    return false;
                }
            }

            public override TriviaData WithSpace(int space)
            {
                if (this.LineBreaks == 0 && this.Space == space)
                {
                    return this;
                }

                return new ModifiedWhitespaceTriviaData(this.Options, this, /*lineBreak*/0, space, elastic: false);
            }

            public override TriviaData WithLine(int line, int indentation)
            {
                Contract.ThrowIfFalse(line > 0);

                if (this.LineBreaks == line && this.Space == indentation)
                {
                    return this;
                }

                return new ModifiedWhitespaceTriviaData(this.Options, this, line, indentation, elastic: false);
            }

            public override TriviaData WithIndentation(int indentation)
            {
                if (this.Space == indentation)
                {
                    return this;
                }

                return new ModifiedWhitespaceTriviaData(this.Options, this, this.LineBreaks, indentation, elastic: false);
            }

            public override string NewString
            {
                get
                {
                    // there is no new string for this.
                    return Contract.FailWithReturn<string>("this shouldn't be called");
                }
            }

            public override void Format(FormattingContext context, Action<int, TriviaData> formattingResultApplier, int tokenPairIndex)
            {
                // nothing changed, nothing to format
            }
        }
    }
}