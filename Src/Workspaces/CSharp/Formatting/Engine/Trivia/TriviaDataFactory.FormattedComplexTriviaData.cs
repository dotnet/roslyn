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
        private class FormattedComplexTriviaData : CSharpTriviaData
        {
            private readonly FormattingContext context;
            private readonly SyntaxToken token1;
            private readonly SyntaxToken token2;
            private readonly string originalString;
            private readonly string newString;
            private readonly bool shouldFormat;

            public FormattedComplexTriviaData(FormattingContext context, SyntaxToken token1, SyntaxToken token2, int lineBreaks, int spaces, string originalString) :
                base(context.Options)
            {
                Contract.ThrowIfNull(context);
                Contract.ThrowIfNull(originalString);

                this.context = context;
                this.token1 = token1;
                this.token2 = token2;
                this.originalString = originalString;

                this.LineBreaks = Math.Max(0, lineBreaks);
                this.Space = Math.Max(0, spaces);

                var formatter = new TriviaFormatter(this.context, this.token1, this.token2, this.LineBreaks, this.Space);
                this.newString = formatter.FormatToString();

                this.shouldFormat = !this.originalString.Equals(this.newString);
            }

            public override bool TreatAsElastic
            {
                get { return false; }
            }

            public override bool IsWhitespaceOnlyTrivia
            {
                get { return false; }
            }

            public override bool ShouldReplaceOriginalWithNewString
            {
                get { return this.shouldFormat; }
            }

            public override string NewString
            {
                get
                {
                    return this.newString;
                }
            }

            public override List<SyntaxTrivia> TriviaList
            {
                get
                {
                    var formatter = new TriviaFormatter(this.context, this.token1, this.token2, this.LineBreaks, this.Space);
                    return formatter.FormatToSyntaxTriviaList();
                }
            }

            public override TriviaData WithSpace(int space)
            {
                return Contract.FailWithReturn<TriviaData>("Shouldn't be called");
            }

            public override TriviaData WithLine(int line, int indentation)
            {
                return Contract.FailWithReturn<TriviaData>("Shouldn't be called");
            }

            public override TriviaData WithIndentation(int indentation)
            {
                return Contract.FailWithReturn<TriviaData>("Shouldn't be called");
            }

            public override void Format(FormattingContext context, Action<int, TriviaData> formattingResultApplier, int tokenPairIndex)
            {
                Contract.Fail("Shouldn't be called");
            }
        }
    }
}
