// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal partial class TriviaDataFactory
    {
        /// <summary>   
        /// represents a general trivia between two tokens. slightly more expensive than others since it
        /// needs to calculate stuff unlike other cases
        /// </summary>
        private class ComplexTrivia : AbstractComplexTrivia
        {
            public ComplexTrivia(OptionSet optionSet, TreeData treeInfo, SyntaxToken token1, SyntaxToken token2) :
                base(optionSet, treeInfo, token1, token2)
            {
            }

            protected override void ExtractLineAndSpace(string text, out int lines, out int spaces)
            {
                text.ProcessTextBetweenTokens(this.TreeInfo, this.Token1, this.OptionSet.GetOption(FormattingOptions.TabSize, LanguageNames.CSharp), out lines, out spaces);
            }

            protected override TriviaData CreateComplexTrivia(int line, int space)
            {
                return CreateModifiedComplexTrivia(line, space);
            }

            protected override TriviaData CreateComplexTrivia(int line, int space, int indentation)
            {
                return CreateModifiedComplexTrivia(line, space);
            }

            private TriviaData CreateModifiedComplexTrivia(int line, int space)
            {
                return new ModifiedComplexTrivia(this.OptionSet, this, line, space);
            }

            protected override TriviaDataWithList Format(
                FormattingContext context, ChainedFormattingRules formattingRules, int lines, int spaces, CancellationToken cancellationToken)
            {
                return new FormattedComplexTrivia(context, formattingRules, this.Token1, this.Token2, lines, spaces, this.OriginalString, cancellationToken);
            }

            protected override bool ContainsSkippedTokensOrText(TriviaList list)
            {
                return CodeShapeAnalyzer.ContainsSkippedTokensOrText(list);
            }

            private bool ShouldFormat(FormattingContext context)
            {
                var commonToken1 = this.Token1;
                var commonToken2 = this.Token2;

                var formatSpanEnd = commonToken2.Kind() == SyntaxKind.None ? commonToken1.Span.End : commonToken2.Span.Start;
                var span = TextSpan.FromBounds(commonToken1.Span.End, formatSpanEnd);
                if (context.IsSpacingSuppressed(span))
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

                if (this.OptionSet.GetOption(FormattingOptions.UseTabs, LanguageNames.CSharp))
                {
                    return true;
                }

                var firstTriviaInTree = this.Token1.Kind() == SyntaxKind.None;

                return CodeShapeAnalyzer.ShouldFormatMultiLine(context, firstTriviaInTree, triviaList);
            }

            public override void Format(
                FormattingContext context,
                ChainedFormattingRules formattingRules,
                Action<int, TriviaData> formattingResultApplier,
                CancellationToken cancellationToken,
                int tokenPairIndex = TokenPairIndexNotNeeded)
            {
                if (!ShouldFormat(context))
                {
                    return;
                }

                formattingResultApplier(tokenPairIndex, Format(context, formattingRules, this.LineBreaks, this.Spaces, cancellationToken));
            }

            public override List<SyntaxTrivia> GetTriviaList(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override IEnumerable<TextChange> GetTextChanges(TextSpan span)
            {
                throw new NotImplementedException();
            }
        }
    }
}
