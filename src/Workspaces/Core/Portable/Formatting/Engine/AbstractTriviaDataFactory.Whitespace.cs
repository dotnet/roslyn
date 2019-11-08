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
        /// <summary>
        /// represents a general trivia between two tokens. slightly more expensive than others since it
        /// needs to calculate stuff unlike other cases
        /// </summary>
        protected class Whitespace : TriviaData
        {
            private readonly bool _elastic;

            public Whitespace(OptionSet optionSet, int space, bool elastic, string language)
                : this(optionSet, lineBreaks: 0, indentation: space, elastic: elastic, language: language)
            {
                Contract.ThrowIfFalse(space >= 0);
            }

            public Whitespace(OptionSet optionSet, int lineBreaks, int indentation, bool elastic, string language)
                : base(optionSet, language)
            {
                _elastic = elastic;

                // space and line breaks can be negative during formatting. but at the end, should be normalized
                // to >= 0
                this.LineBreaks = lineBreaks;
                this.Spaces = indentation;
            }

            public override bool TreatAsElastic => _elastic;

            public override bool IsWhitespaceOnlyTrivia => true;

            public override bool ContainsChanges => false;

            public override TriviaData WithSpace(int space, FormattingContext context, ChainedFormattingRules formattingRules)
            {
                if (this is { LineBreaks: 0, Spaces: space })
                {
                    return this;
                }

                return new ModifiedWhitespace(this.OptionSet, this, /*lineBreak*/0, space, elastic: false, language: this.Language);
            }

            public override TriviaData WithLine(int line, int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(line > 0);

                if (this.LineBreaks == line && this.Spaces == indentation)
                {
                    return this;
                }

                return new ModifiedWhitespace(this.OptionSet, this, line, indentation, elastic: false, language: this.Language);
            }

            public override TriviaData WithIndentation(
                int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken)
            {
                if (this.Spaces == indentation)
                {
                    return this;
                }

                return new ModifiedWhitespace(this.OptionSet, this, this.LineBreaks, indentation, elastic: false, language: this.Language);
            }

            public override void Format(
                FormattingContext context,
                ChainedFormattingRules formattingRules,
                Action<int, TokenStream, TriviaData> formattingResultApplier,
                CancellationToken cancellationToken,
                int tokenPairIndex = TokenPairIndexNotNeeded)
            {
                // nothing changed, nothing to format
            }

            public override IEnumerable<TextChange> GetTextChanges(TextSpan span)
            {
                throw new NotImplementedException();
            }
        }
    }
}
