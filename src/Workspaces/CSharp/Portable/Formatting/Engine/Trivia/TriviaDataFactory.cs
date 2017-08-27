﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    /// <summary>
    /// trivia factory.
    /// 
    /// it will cache some commonly used trivia to reduce memory footprint and heap allocation
    /// </summary>
    internal partial class TriviaDataFactory : AbstractTriviaDataFactory
    {
        public TriviaDataFactory(TreeData treeInfo, OptionSet optionSet) :
            base(treeInfo, optionSet)
        {
        }

        private static bool IsCSharpWhitespace(char c)
        {
            return SyntaxFacts.IsWhitespace(c) || SyntaxFacts.IsNewLine(c);
        }

        public override TriviaData CreateLeadingTrivia(SyntaxToken token)
        {
            // no trivia
            if (!token.HasLeadingTrivia)
            {
                Debug.Assert(this.TreeInfo.GetTextBetween(default, token).All(IsCSharpWhitespace));
                return GetSpaceTriviaData(space: 0);
            }

            var result = Analyzer.Leading(token);
            var info = GetWhitespaceOnlyTriviaInfo(default, token, result);
            if (info != null)
            {
                Debug.Assert(this.TreeInfo.GetTextBetween(default, token).All(IsCSharpWhitespace));
                return info;
            }

            return new ComplexTrivia(this.OptionSet, this.TreeInfo, default, token);
        }

        public override TriviaData CreateTrailingTrivia(SyntaxToken token)
        {
            // no trivia
            if (!token.HasTrailingTrivia)
            {
                Debug.Assert(this.TreeInfo.GetTextBetween(token, default).All(IsCSharpWhitespace));
                return GetSpaceTriviaData(space: 0);
            }

            var result = Analyzer.Trailing(token);
            var info = GetWhitespaceOnlyTriviaInfo(token, default, result);
            if (info != null)
            {
                Debug.Assert(this.TreeInfo.GetTextBetween(token, default).All(IsCSharpWhitespace));
                return info;
            }

            return new ComplexTrivia(this.OptionSet, this.TreeInfo, token, default);
        }

        public override TriviaData Create(SyntaxToken token1, SyntaxToken token2)
        {
            // no trivia in between
            if (!token1.HasTrailingTrivia && !token2.HasLeadingTrivia)
            {
                Debug.Assert(string.IsNullOrWhiteSpace(this.TreeInfo.GetTextBetween(token1, token2)));
                return GetSpaceTriviaData(space: 0);
            }

            var result = Analyzer.Between(token1, token2);
            var info = GetWhitespaceOnlyTriviaInfo(token1, token2, result);
            if (info != null)
            {
                Debug.Assert(string.IsNullOrWhiteSpace(this.TreeInfo.GetTextBetween(token1, token2)));
                return info;
            }

            return new ComplexTrivia(this.OptionSet, this.TreeInfo, token1, token2);
        }

        private bool ContainsOnlyWhitespace(Analyzer.AnalysisResult result)
        {
            return
                !result.HasComments &&
                !result.HasPreprocessor &&
                !result.HasSkippedTokens &&
                !result.HasSkippedOrDisabledText &&
                !result.HasConflictMarker;
        }

        private TriviaData GetWhitespaceOnlyTriviaInfo(SyntaxToken token1, SyntaxToken token2, Analyzer.AnalysisResult result)
        {
            if (!ContainsOnlyWhitespace(result))
            {
                return null;
            }

            // only whitespace in between
            int space = GetSpaceOnSingleLine(result);
            Contract.ThrowIfFalse(space >= -1);

            if (space >= 0)
            {
                // check whether we can use cache
                return GetSpaceTriviaData(space, result.TreatAsElastic);
            }

            // tab is used in a place where it is not an indentation
            if (result.LineBreaks == 0 && result.Tab > 0)
            {
                // calculate actual space size from tab
                var spaces = CalculateSpaces(token1, token2);
                return new ModifiedWhitespace(this.OptionSet, result.LineBreaks, indentation: spaces, elastic: result.TreatAsElastic, language: LanguageNames.CSharp);
            }

            // check whether we can cache trivia info for current indentation
            var lineCountAndIndentation = GetLineBreaksAndIndentation(result);

            var canUseTriviaAsItIs = lineCountAndIndentation.Item1;
            return GetWhitespaceTriviaData(lineCountAndIndentation.Item2, lineCountAndIndentation.Item3, canUseTriviaAsItIs, result.TreatAsElastic);
        }

        private int CalculateSpaces(SyntaxToken token1, SyntaxToken token2)
        {
            var initialColumn = (token1.RawKind == 0) ? 0 : this.TreeInfo.GetOriginalColumn(this.OptionSet.GetOption(FormattingOptions.TabSize, LanguageNames.CSharp), token1) + token1.Span.Length;
            var textSnippet = this.TreeInfo.GetTextBetween(token1, token2);

            return textSnippet.ConvertTabToSpace(this.OptionSet.GetOption(FormattingOptions.TabSize, LanguageNames.CSharp), initialColumn, textSnippet.Length);
        }

        private ValueTuple<bool, int, int> GetLineBreaksAndIndentation(Analyzer.AnalysisResult result)
        {
            Debug.Assert(result.Tab >= 0);
            Debug.Assert(result.LineBreaks >= 0);

            var indentation = result.Tab * this.OptionSet.GetOption(FormattingOptions.TabSize, LanguageNames.CSharp) + result.Space;
            if (result.HasTrailingSpace || result.HasUnknownWhitespace)
            {
                return ValueTuple.Create(false, result.LineBreaks, indentation);
            }

            if (!this.OptionSet.GetOption(FormattingOptions.UseTabs, LanguageNames.CSharp))
            {
                if (result.Tab > 0)
                {
                    return ValueTuple.Create(false, result.LineBreaks, indentation);
                }

                return ValueTuple.Create(true, result.LineBreaks, indentation);
            }

            Debug.Assert(this.OptionSet.GetOption(FormattingOptions.UseTabs, LanguageNames.CSharp));

            // tab can only appear before space to be a valid tab for indentation
            if (result.HasTabAfterSpace)
            {
                return ValueTuple.Create(false, result.LineBreaks, indentation);
            }

            if (result.Space >= this.OptionSet.GetOption(FormattingOptions.TabSize, LanguageNames.CSharp))
            {
                return ValueTuple.Create(false, result.LineBreaks, indentation);
            }

            Debug.Assert((indentation / this.OptionSet.GetOption(FormattingOptions.TabSize, LanguageNames.CSharp)) == result.Tab);
            Debug.Assert((indentation % this.OptionSet.GetOption(FormattingOptions.TabSize, LanguageNames.CSharp)) == result.Space);

            return ValueTuple.Create(true, result.LineBreaks, indentation);
        }

        private int GetSpaceOnSingleLine(Analyzer.AnalysisResult result)
        {
            if (result.HasTrailingSpace || result.HasUnknownWhitespace || result.LineBreaks > 0 || result.Tab > 0)
            {
                return -1;
            }

            return result.Space;
        }
    }
}
