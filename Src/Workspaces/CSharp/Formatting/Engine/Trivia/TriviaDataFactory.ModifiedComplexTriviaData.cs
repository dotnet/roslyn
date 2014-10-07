using System;
using System.Collections.Generic;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Internal;
using Roslyn.Services.Formatting;
using Roslyn.Utilities;

namespace Roslyn.Services.CSharp.Formatting
{
    internal partial class TriviaDataFactory
    {
        private class ModifiedComplexTriviaData : CSharpTriviaData
        {
            private readonly ComplexTriviaData original;

            public ModifiedComplexTriviaData(FormattingOptions options, ComplexTriviaData original, int lineBreaks, int space)
                : base(options)
            {
                Contract.ThrowIfNull(original);

                this.original = original;

                // linebreak and space can become negative during formatting. but it should be normalized to >= 0
                // at the end.
                this.LineBreaks = lineBreaks;
                this.Space = space;
            }

            public override bool ShouldReplaceOriginalWithNewString
            {
                get
                {
                    return false;
                }
            }

            public override bool TreatAsElastic
            {
                get { return this.original.TreatAsElastic; }
            }

            public override bool IsWhitespaceOnlyTrivia
            {
                get { return false; }
            }

            public override string NewString
            {
                get
                {
                    return Contract.FailWithReturn<string>("Should be never called");
                }
            }

            public override List<SyntaxTrivia> TriviaList
            {
                get
                {
                    return Contract.FailWithReturn<List<SyntaxTrivia>>("Should be never called");
                }
            }

            public override TriviaData WithSpace(int space)
            {
                return this.original.WithSpace(space);
            }

            public override TriviaData WithLine(int line, int indentation)
            {
                return this.original.WithLine(line, indentation);
            }

            public override TriviaData WithIndentation(int indentation)
            {
                return this.original.WithIndentation(indentation);
            }

            public override void Format(FormattingContext context, Action<int, TriviaData> formattingResultApplier, int tokenPairIndex)
            {
                Contract.ThrowIfFalse(this.SecondTokenIsFirstTokenOnLine);

                var triviaList = new TriviaList(this.original.Token1.TrailingTrivia, this.original.Token2.LeadingTrivia);
                Contract.ThrowIfFalse(triviaList.Count > 0);

                // okay, now, check whether we need or are able to format noisy tokens
                if (TriviaFormatter.ContainsSkippedTokensOrText(triviaList))
                {
                    return;
                }

                formattingResultApplier(tokenPairIndex, 
                    new FormattedComplexTriviaData(context, this.original.Token1, this.original.Token2, this.LineBreaks, this.Space, this.original.OriginalString));
            }
        }
    }
}