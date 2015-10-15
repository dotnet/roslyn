// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        private class ModifiedComplexTrivia : TriviaDataWithList
        {
            private readonly ComplexTrivia _original;

            public ModifiedComplexTrivia(OptionSet optionSet, ComplexTrivia original, int lineBreaks, int space)
                : base(optionSet, original.Token1.Language)
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
            {
                return _original.WithSpace(space, context, formattingRules);
            }

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
                Action<int, TriviaData> formattingResultApplier,
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
