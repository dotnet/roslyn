// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract partial class AbstractTriviaDataFactory
    {
        protected class FormattedWhitespace : TriviaData
        {
            private readonly string _newString;

            public FormattedWhitespace(OptionSet optionSet, int lineBreaks, int indentation, string language)
                : base(optionSet, language)
            {
                this.LineBreaks = Math.Max(0, lineBreaks);
                this.Spaces = Math.Max(0, indentation);

                _newString = CreateString(this.OptionSet.GetOption(FormattingOptions.NewLine, language));
            }

            private string CreateString(string newLine)
            {
                if (this.SecondTokenIsFirstTokenOnLine)
                {
                    var builder = StringBuilderPool.Allocate();
                    for (var i = 0; i < this.LineBreaks; i++)
                    {
                        builder.Append(newLine);
                    }

                    builder.AppendIndentationString(this.Spaces, this.OptionSet.GetOption(FormattingOptions.UseTabs, this.Language), this.OptionSet.GetOption(FormattingOptions.TabSize, this.Language));
                    return StringBuilderPool.ReturnAndFree(builder);
                }

                // space case. always use space
                return new string(' ', this.Spaces);
            }

            public override bool TreatAsElastic => false;

            public override bool IsWhitespaceOnlyTrivia => true;

            public override bool ContainsChanges => true;

            public override IEnumerable<TextChange> GetTextChanges(TextSpan textSpan)
            {
                return SpecializedCollections.SingletonEnumerable<TextChange>(new TextChange(textSpan, _newString));
            }

            public override TriviaData WithSpace(int space, FormattingContext context, ChainedFormattingRules formattingRules)
            {
                throw new NotImplementedException();
            }

            public override TriviaData WithLine(int line, int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override TriviaData WithIndentation(int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override void Format(
                FormattingContext context, ChainedFormattingRules formattingRules, Action<int, TokenStream, TriviaData> formattingResultApplier, CancellationToken cancellationToken, int tokenPairIndex = TokenPairIndexNotNeeded)
            {
                throw new NotImplementedException();
            }
        }
    }
}
