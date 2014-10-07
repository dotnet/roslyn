using System;
using Roslyn.Compilers.Internal;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    internal abstract partial class AbstractTriviaDataFactory
    {
        protected class ModifiedWithoutOriginalTriviaData : WhitespaceTriviaData
        {
            public ModifiedWithoutOriginalTriviaData(FormattingOptions options, int lineBreaks, int indentation, bool elastic) :
                base(options, lineBreaks, indentation, elastic)
            {
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
