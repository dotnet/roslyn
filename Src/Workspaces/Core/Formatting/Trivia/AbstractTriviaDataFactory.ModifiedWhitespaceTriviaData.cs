using System;
using Roslyn.Compilers.Internal;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    internal abstract partial class AbstractTriviaDataFactory
    {
        protected class ModifiedWhitespaceTriviaData : WhitespaceTriviaData
        {
            private readonly WhitespaceTriviaData originalTrivia;

            public ModifiedWhitespaceTriviaData(FormattingOptions options, WhitespaceTriviaData originalTrivia, int lineBreaks, int indentation, bool elastic) :
                base(options, lineBreaks, indentation, elastic)
            {
                Contract.ThrowIfNull(originalTrivia);

                this.originalTrivia = originalTrivia;
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
                if (this.LineBreaks == this.originalTrivia.LineBreaks && this.originalTrivia.Space == space)
                {
                    return this.originalTrivia;
                }

                return base.WithSpace(space);
            }

            public override TriviaData WithLine(int line, int indentation)
            {
                if (this.originalTrivia.LineBreaks == line && this.originalTrivia.Space == indentation)
                {
                    return this.originalTrivia;
                }

                return base.WithLine(line, indentation);
            }

            public override TriviaData WithIndentation(int indentation)
            {
                if (this.LineBreaks == this.originalTrivia.LineBreaks && this.originalTrivia.Space == indentation)
                {
                    return this.originalTrivia;
                }

                return base.WithIndentation(indentation);
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
                formattingResultApplier(tokenPairIndex, new FormattedWhitespaceTriviaData(this.Options, this.LineBreaks, this.Space));
            }
        }
    }
}
