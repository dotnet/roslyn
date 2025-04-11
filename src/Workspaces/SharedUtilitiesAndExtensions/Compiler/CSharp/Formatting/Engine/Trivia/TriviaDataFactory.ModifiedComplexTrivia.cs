// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal sealed partial class TriviaDataFactory
{
    private sealed class ModifiedComplexTrivia : TriviaDataWithList
    {
        private readonly ComplexTrivia _original;

        public ModifiedComplexTrivia(LineFormattingOptions options, ComplexTrivia original, int lineBreaks, int space)
            : base(options)
        {
            Contract.ThrowIfNull(original);

            _original = original;

            // linebreak and space can become negative during formatting. but it should be normalized to >= 0
            // at the end.
            this.LineBreaks = lineBreaks;
            this.Spaces = space;
        }

        public override bool ContainsChanges
        {
            get
            {
                return false;
            }
        }

        public override bool TreatAsElastic
        {
            get { return _original.TreatAsElastic; }
        }

        public override bool IsWhitespaceOnlyTrivia
        {
            get { return false; }
        }

        public override TriviaData WithSpace(int space, FormattingContext context, ChainedFormattingRules formattingRules)
            => _original.WithSpace(space, context, formattingRules);

        public override TriviaData WithLine(
            int line, int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken)
        {
            return _original.WithLine(line, indentation, context, formattingRules, cancellationToken);
        }

        public override TriviaData WithIndentation(
            int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken)
        {
            return _original.WithIndentation(indentation, context, formattingRules, cancellationToken);
        }

        public override void Format(
            FormattingContext context,
            ChainedFormattingRules formattingRules,
            Action<int, TokenStream, TriviaData> formattingResultApplier,
            CancellationToken cancellationToken,
            int tokenPairIndex = TokenPairIndexNotNeeded)
        {
            Contract.ThrowIfFalse(this.SecondTokenIsFirstTokenOnLine);

            var token1 = _original.Token1;
            var token2 = _original.Token2;

            var triviaList = new TriviaList(token1.TrailingTrivia, token2.LeadingTrivia);
            Contract.ThrowIfFalse(triviaList.Count > 0);

            // okay, now, check whether we need or are able to format noisy tokens
            if (CodeShapeAnalyzer.ContainsSkippedTokensOrText(triviaList))
            {
                return;
            }

            formattingResultApplier(tokenPairIndex,
                context.TokenStream,
                new FormattedComplexTrivia(
                    context,
                    formattingRules,
                    _original.Token1,
                    _original.Token2,
                    this.LineBreaks,
                    this.Spaces,
                    _original.OriginalString,
                    cancellationToken));
        }

        public override SyntaxTriviaList GetTriviaList(CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public override IEnumerable<TextChange> GetTextChanges(TextSpan span)
            => throw new NotImplementedException();
    }
}
