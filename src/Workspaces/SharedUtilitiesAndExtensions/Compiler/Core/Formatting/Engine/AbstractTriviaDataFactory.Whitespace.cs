// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting;

internal abstract partial class AbstractTriviaDataFactory
{
    /// <summary>
    /// represents a general trivia between two tokens. slightly more expensive than others since it
    /// needs to calculate stuff unlike other cases
    /// </summary>
    protected class Whitespace : TriviaData
    {
        private readonly bool _elastic;

        public Whitespace(LineFormattingOptions options, int space, bool elastic)
            : this(options, lineBreaks: 0, indentation: space, elastic: elastic)
        {
            Contract.ThrowIfFalse(space >= 0);
        }

        public Whitespace(LineFormattingOptions options, int lineBreaks, int indentation, bool elastic)
            : base(options)
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
            if (this.LineBreaks == 0 && this.Spaces == space)
                return this;

            return new ModifiedWhitespace(this.Options, this, /*lineBreak*/0, space, elastic: false);
        }

        public override TriviaData WithLine(int line, int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(line > 0);

            if (this.LineBreaks == line && this.Spaces == indentation)
            {
                return this;
            }

            return new ModifiedWhitespace(this.Options, this, line, indentation, elastic: false);
        }

        public override TriviaData WithIndentation(
            int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken)
        {
            if (this.Spaces == indentation)
            {
                return this;
            }

            return new ModifiedWhitespace(this.Options, this, this.LineBreaks, indentation, elastic: false);
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
            => throw new NotImplementedException();
    }
}
