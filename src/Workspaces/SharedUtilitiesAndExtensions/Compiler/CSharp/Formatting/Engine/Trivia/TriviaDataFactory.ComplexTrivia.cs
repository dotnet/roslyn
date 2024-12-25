// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal partial class TriviaDataFactory
{
    /// <summary>   
    /// represents a general trivia between two tokens. slightly more expensive than others since it
    /// needs to calculate stuff unlike other cases
    /// </summary>
    private sealed class ComplexTrivia : AbstractComplexTrivia
    {
        public ComplexTrivia(LineFormattingOptions options, TreeData treeInfo, SyntaxToken token1, SyntaxToken token2)
            : base(options, treeInfo, token1, token2)
        {
        }

        protected override void ExtractLineAndSpace(string text, out int lines, out int spaces)
            => text.ProcessTextBetweenTokens(this.TreeInfo, this.Token1, this.Options.TabSize, out lines, out spaces);

        protected override TriviaData CreateComplexTrivia(int line, int space)
            => CreateModifiedComplexTrivia(line, space);

        protected override TriviaData CreateComplexTrivia(int line, int space, int indentation)
            => CreateModifiedComplexTrivia(line, space);

        private TriviaData CreateModifiedComplexTrivia(int line, int space)
            => new ModifiedComplexTrivia(this.Options, this, line, space);

        protected override TriviaDataWithList Format(
            FormattingContext context, ChainedFormattingRules formattingRules, int lines, int spaces, CancellationToken cancellationToken)
        {
            return new FormattedComplexTrivia(context, formattingRules, this.Token1, this.Token2, lines, spaces, this.OriginalString, cancellationToken);
        }

        protected override bool ContainsSkippedTokensOrText(TriviaList list)
            => CodeShapeAnalyzer.ContainsSkippedTokensOrText(list);

        private bool ShouldFormat(FormattingContext context)
        {
            var commonToken1 = this.Token1;
            var commonToken2 = this.Token2;

            var formatSpanEnd = commonToken2.Kind() == SyntaxKind.None ? commonToken1.Span.End : commonToken2.Span.Start;
            var span = TextSpan.FromBounds(commonToken1.Span.End, formatSpanEnd);
            if (context.IsSpacingSuppressed(span, TreatAsElastic))
            {
                return false;
            }

            var triviaList = new TriviaList(commonToken1.TrailingTrivia, commonToken2.LeadingTrivia);
            Contract.ThrowIfFalse(triviaList.Count > 0);

            // okay, now, check whether we need or are able to format noisy tokens
            if (ContainsSkippedTokensOrText(triviaList))
            {
                return false;
            }

            if (!this.SecondTokenIsFirstTokenOnLine)
            {
                return CodeShapeAnalyzer.ShouldFormatSingleLine(triviaList);
            }

            Debug.Assert(this.SecondTokenIsFirstTokenOnLine);

            if (Options.UseTabs)
            {
                return true;
            }

            var firstTriviaInTree = this.Token1.Kind() == SyntaxKind.None;

            return CodeShapeAnalyzer.ShouldFormatMultiLine(context, firstTriviaInTree, triviaList);
        }

        public override void Format(
            FormattingContext context,
            ChainedFormattingRules formattingRules,
            Action<int, TokenStream, TriviaData> formattingResultApplier,
            CancellationToken cancellationToken,
            int tokenPairIndex = TokenPairIndexNotNeeded)
        {
            if (!ShouldFormat(context))
            {
                return;
            }

            formattingResultApplier(tokenPairIndex, context.TokenStream, Format(context, formattingRules, this.LineBreaks, this.Spaces, cancellationToken));
        }

        public override SyntaxTriviaList GetTriviaList(CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public override IEnumerable<TextChange> GetTextChanges(TextSpan span)
            => throw new NotImplementedException();
    }
}
