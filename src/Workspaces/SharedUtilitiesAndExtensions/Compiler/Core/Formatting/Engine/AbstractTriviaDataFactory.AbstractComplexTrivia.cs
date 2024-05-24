// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting;

internal abstract partial class AbstractTriviaDataFactory
{
    protected abstract class AbstractComplexTrivia : TriviaDataWithList
    {
        private readonly SyntaxToken _token1;
        private readonly SyntaxToken _token2;

        public TreeData TreeInfo { get; }
        public string OriginalString { get; }

        private readonly bool _treatAsElastic;

        public AbstractComplexTrivia(LineFormattingOptions options, TreeData treeInfo, SyntaxToken token1, SyntaxToken token2)
            : base(options)
        {
            Contract.ThrowIfNull(treeInfo);

            _token1 = token1;
            _token2 = token2;

            _treatAsElastic = CommonFormattingHelpers.HasAnyWhitespaceElasticTrivia(token1, token2);

            this.TreeInfo = treeInfo;
            this.OriginalString = this.TreeInfo.GetTextBetween(token1, token2);
            ExtractLineAndSpace(this.OriginalString, out var lineBreaks, out var spaces);

            this.LineBreaks = lineBreaks;
            this.Spaces = spaces;
        }

        protected abstract void ExtractLineAndSpace(string text, out int lines, out int spaces);
        protected abstract TriviaData CreateComplexTrivia(int line, int space);
        protected abstract TriviaData CreateComplexTrivia(int line, int space, int indentation);
        protected abstract TriviaDataWithList Format(FormattingContext context, ChainedFormattingRules formattingRules, int lines, int spaces, CancellationToken cancellationToken);
        protected abstract bool ContainsSkippedTokensOrText(TriviaList list);

        public SyntaxToken Token1 => _token1;

        public SyntaxToken Token2 => _token2;

        public override bool TreatAsElastic => _treatAsElastic;

        public override bool IsWhitespaceOnlyTrivia => false;

        public override bool ContainsChanges => false;

        public override TriviaData WithSpace(int space, FormattingContext context, ChainedFormattingRules formattingRules)
        {
            // two tokens are on a single line, we don't allow changing spaces between two
            // tokens that contain noisy characters between them.
            if (!this.SecondTokenIsFirstTokenOnLine)
            {
                return this;
            }

            // okay, two tokens are on different lines, we are basically asked to remove line breaks between them
            // and make them to be on a single line. well, that is not allowed when there are noisy chars between them
            if (this.SecondTokenIsFirstTokenOnLine)
            {
                return this;
            }

            throw ExceptionUtilities.Unreachable();
        }

        public override TriviaData WithLine(
            int line, int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(line > 0);

            // if we have elastic trivia, always let it be modified
            if (this.TreatAsElastic)
            {
                return CreateComplexTrivia(line, indentation);
            }

            // two tokens are on a single line, it is always allowed to put those two tokens on a different lines
            if (!this.SecondTokenIsFirstTokenOnLine)
            {
                return CreateComplexTrivia(line, indentation);
            }

            // okay, two tokens are on different lines, now we need to see whether we can add more lines or not
            if (this.SecondTokenIsFirstTokenOnLine)
            {
                // we are asked to add more lines. sure, no problem
                if (this.LineBreaks < line)
                {
                    return CreateComplexTrivia(line, indentation);
                }

                // we already has same number of lines, but it is asking changing indentation
                if (this.LineBreaks == line)
                {
                    return WithIndentation(indentation, context, formattingRules, cancellationToken);
                }

                // sorry, we can't reduce lines if it contains noisy chars
                if (this.LineBreaks > line)
                {
                    return this;
                }
            }

            throw ExceptionUtilities.Unreachable();
        }

        public override TriviaData WithIndentation(
            int indentation, FormattingContext context, ChainedFormattingRules formattingRules, CancellationToken cancellationToken)
        {
            // if tokens are not in different line, there is nothing we can do here
            if (!this.SecondTokenIsFirstTokenOnLine)
            {
                return this;
            }

            // well, we are already in a desired format, nothing to do. return as it is.
            if (this.Spaces == indentation)
            {
                return this;
            }

            // do expansive check
            // we need to actually format here to find out indentation
            var list = new TriviaList(_token1.TrailingTrivia, _token2.LeadingTrivia);
            Contract.ThrowIfFalse(list.Count > 0);

            if (ContainsSkippedTokensOrText(list))
            {
                // we can't format
                return this;
            }

            // okay, we need to do expansive calculation to find out actual space between two tokens
            var trivia = Format(context, formattingRules, this.LineBreaks, indentation, cancellationToken);
            var triviaString = CreateString(trivia, cancellationToken);
            ExtractLineAndSpace(triviaString, out var lineBreaks, out var spaces);

            return CreateComplexTrivia(lineBreaks, spaces, indentation);
        }

        private static string CreateString(TriviaDataWithList triviaData, CancellationToken cancellationToken)
        {
            // create string from given trivia data
            var sb = StringBuilderPool.Allocate();

            foreach (var trivia in triviaData.GetTriviaList(cancellationToken))
            {
                sb.Append(trivia.ToFullString());
            }

            return StringBuilderPool.ReturnAndFree(sb);
        }
    }
}
