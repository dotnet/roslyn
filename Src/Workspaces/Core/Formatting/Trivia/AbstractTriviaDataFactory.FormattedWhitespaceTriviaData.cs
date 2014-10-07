using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    internal abstract partial class AbstractTriviaDataFactory
    {
        protected class FormattedWhitespaceTriviaData : TriviaData
        {
            private readonly string newString;

            public FormattedWhitespaceTriviaData(FormattingOptions options, int lineBreaks, int indentation) :
                base(options)
            {
                this.LineBreaks = Math.Max(0, lineBreaks);
                this.Space = Math.Max(0, indentation);

                this.newString = CreateStringFromState();
            }

            private string CreateStringFromState()
            {
                if (this.SecondTokenIsFirstTokenOnLine)
                {
                    var builder = StringBuilderPool.Allocate();
                    for (int i = 0; i < this.LineBreaks; i++)
                    {
                        builder.AppendLine();
                    }

                    builder.Append(this.Space.CreateIndentationString(this.Options.UseTab, this.Options.TabSize));
                    return StringBuilderPool.ReturnAndFree(builder);
                }

                // space case. always use space
                return new string(' ', this.Space);
            }

            public override bool TreatAsElastic
            {
                get { return false; }
            }

            public override bool IsWhitespaceOnlyTrivia
            {
                get { return true; }
            }

            public override bool ShouldReplaceOriginalWithNewString
            {
                get
                {
                    return true;
                }
            }

            public override string NewString
            {
                get
                {
                    return this.newString;
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
