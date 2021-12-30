﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    internal static class FormattingOperations
    {
        private static readonly AdjustNewLinesOperation s_preserveZeroLine = new(0, AdjustNewLinesOption.PreserveLines);
        private static readonly AdjustNewLinesOperation s_preserveOneLine = new(1, AdjustNewLinesOption.PreserveLines);
        private static readonly AdjustNewLinesOperation s_forceOneLine = new(1, AdjustNewLinesOption.ForceLines);
        private static readonly AdjustNewLinesOperation s_forceIfSameLine = new(1, AdjustNewLinesOption.ForceLinesIfOnSingleLine);

        private static readonly AdjustSpacesOperation s_defaultOneSpaceIfOnSingleLine = new(1, AdjustSpacesOption.DefaultSpacesIfOnSingleLine);
        private static readonly AdjustSpacesOperation s_forceOneSpaceIfOnSingleLine = new(1, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
        private static readonly AdjustSpacesOperation s_forceZeroSpaceIfOnSingleLine = new(0, AdjustSpacesOption.ForceSpacesIfOnSingleLine);

        // As the name suggests, the line force operation is performed by force spacing
        private static readonly AdjustSpacesOperation s_forceZeroLineUsingSpaceForce = new(1, AdjustSpacesOption.ForceSpaces);

        /// <summary>
        /// create anchor indentation region around start and end token
        /// start token will act as anchor token and right after anchor token to end of end token will become anchor region
        /// </summary>
        public static AnchorIndentationOperation CreateAnchorIndentationOperation(SyntaxToken startToken, SyntaxToken endToken)
            => CreateAnchorIndentationOperation(startToken, startToken, endToken, TextSpan.FromBounds(startToken.Span.End, endToken.Span.End));

        /// <summary>
        /// create anchor indentation region more explicitly by providing all necessary information.
        /// </summary>
        public static AnchorIndentationOperation CreateAnchorIndentationOperation(SyntaxToken anchorToken, SyntaxToken startToken, SyntaxToken endToken, TextSpan textSpan)
            => new(anchorToken, startToken, endToken, textSpan);

        /// <summary>
        /// create suppress region around start and end token
        /// </summary>
        public static SuppressOperation CreateSuppressOperation(SyntaxToken startToken, SyntaxToken endToken, SuppressOption option)
            => CreateSuppressOperation(startToken, endToken, TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End), option);

        /// <summary>
        /// create suppress region around the given text span
        /// </summary>
        private static SuppressOperation CreateSuppressOperation(SyntaxToken startToken, SyntaxToken endToken, TextSpan textSpan, SuppressOption option)
            => new(startToken, endToken, textSpan, option);

        /// <summary>
        /// create indent block region around the start and end token with the given indentation delta added to the existing indentation at the position of the start token
        /// </summary>
        public static IndentBlockOperation CreateIndentBlockOperation(SyntaxToken startToken, SyntaxToken endToken, int indentationDelta, IndentBlockOption option)
        {
            var span = CommonFormattingHelpers.GetSpanIncludingTrailingAndLeadingTriviaOfAdjacentTokens(startToken, endToken);
            return CreateIndentBlockOperation(startToken, endToken, span, indentationDelta, option);
        }

        /// <summary>
        /// create indent block region around the given text span with the given indentation delta added to the existing indentation at the position of the start token
        /// </summary>
        public static IndentBlockOperation CreateIndentBlockOperation(SyntaxToken startToken, SyntaxToken endToken, TextSpan textSpan, int indentationDelta, IndentBlockOption option)
            => new(startToken, endToken, textSpan, indentationDelta, option);

        /// <summary>
        /// create indent block region around the start and end token with the given indentation delta added to the column of the base token
        /// </summary>
        public static IndentBlockOperation CreateRelativeIndentBlockOperation(SyntaxToken baseToken, SyntaxToken startToken, SyntaxToken endToken, int indentationDelta, IndentBlockOption option)
        {
            var span = CommonFormattingHelpers.GetSpanIncludingTrailingAndLeadingTriviaOfAdjacentTokens(startToken, endToken);

            return CreateRelativeIndentBlockOperation(baseToken, startToken, endToken, span, indentationDelta, option);
        }

        /// <summary>
        /// create indent block region around the given text span with the given indentation delta added to the column of the base token
        /// </summary>
        public static IndentBlockOperation CreateRelativeIndentBlockOperation(SyntaxToken baseToken, SyntaxToken startToken, SyntaxToken endToken, TextSpan textSpan, int indentationDelta, IndentBlockOption option)
            => new(baseToken, startToken, endToken, textSpan, indentationDelta, option);

        /// <summary>
        /// instruct the engine to try to align first tokens on the lines among the given tokens to be aligned to the base token
        /// </summary>
        public static AlignTokensOperation CreateAlignTokensOperation(SyntaxToken baseToken, IEnumerable<SyntaxToken> tokens, AlignTokensOption option)
            => new(baseToken, tokens, option);

        /// <summary>
        /// instruct the engine to try to put the give lines between two tokens
        /// </summary>
        public static AdjustNewLinesOperation CreateAdjustNewLinesOperation(int line, AdjustNewLinesOption option)
        {
            if (line == 0)
            {
                if (option == AdjustNewLinesOption.PreserveLines)
                {
                    return s_preserveZeroLine;
                }
            }
            else if (line == 1)
            {
                if (option == AdjustNewLinesOption.PreserveLines)
                {
                    return s_preserveOneLine;
                }
                else if (option == AdjustNewLinesOption.ForceLines)
                {
                    return s_forceOneLine;
                }
                else if (option == AdjustNewLinesOption.ForceLinesIfOnSingleLine)
                {
                    return s_forceIfSameLine;
                }
            }

            return new AdjustNewLinesOperation(line, option);
        }

        /// <summary>
        /// instruct the engine to try to put the given spaces between two tokens
        /// </summary>
        public static AdjustSpacesOperation CreateAdjustSpacesOperation(int space, AdjustSpacesOption option)
        {
            if (space == 1 && option == AdjustSpacesOption.DefaultSpacesIfOnSingleLine)
            {
                return s_defaultOneSpaceIfOnSingleLine;
            }
            else if (space == 0 && option == AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            {
                return s_forceZeroSpaceIfOnSingleLine;
            }
            else if (space == 1 && option == AdjustSpacesOption.ForceSpacesIfOnSingleLine)
            {
                return s_forceOneSpaceIfOnSingleLine;
            }
            else if (space == 1 && option == AdjustSpacesOption.ForceSpaces)
            {
                return s_forceZeroLineUsingSpaceForce;
            }

            return new AdjustSpacesOperation(space, option);
        }

        /// <summary>
        /// return SuppressOperation for the node provided by the given formatting rules
        /// </summary>
        internal static IEnumerable<SuppressOperation> GetSuppressOperations(IEnumerable<AbstractFormattingRule> formattingRules, SyntaxNode node, AnalyzerConfigOptions options)
        {
            var chainedFormattingRules = new ChainedFormattingRules(formattingRules, options);

            var list = new List<SuppressOperation>();
            chainedFormattingRules.AddSuppressOperations(list, node);
            return list;
        }

        /// <summary>
        /// return AnchorIndentationOperation for the node provided by the given formatting rules
        /// </summary>
        internal static IEnumerable<AnchorIndentationOperation> GetAnchorIndentationOperations(IEnumerable<AbstractFormattingRule> formattingRules, SyntaxNode node, AnalyzerConfigOptions options)
        {
            var chainedFormattingRules = new ChainedFormattingRules(formattingRules, options);

            var list = new List<AnchorIndentationOperation>();
            chainedFormattingRules.AddAnchorIndentationOperations(list, node);
            return list;
        }

        /// <summary>
        /// return IndentBlockOperation for the node provided by the given formatting rules
        /// </summary>
        internal static IEnumerable<IndentBlockOperation> GetIndentBlockOperations(IEnumerable<AbstractFormattingRule> formattingRules, SyntaxNode node, AnalyzerConfigOptions options)
        {
            var chainedFormattingRules = new ChainedFormattingRules(formattingRules, options);

            var list = new List<IndentBlockOperation>();
            chainedFormattingRules.AddIndentBlockOperations(list, node);
            return list;
        }

        /// <summary>
        /// return AlignTokensOperation for the node provided by the given formatting rules
        /// </summary>
        internal static IEnumerable<AlignTokensOperation> GetAlignTokensOperations(IEnumerable<AbstractFormattingRule> formattingRules, SyntaxNode node, AnalyzerConfigOptions options)
        {
            var chainedFormattingRules = new ChainedFormattingRules(formattingRules, options);

            var list = new List<AlignTokensOperation>();
            chainedFormattingRules.AddAlignTokensOperations(list, node);
            return list;
        }

        /// <summary>
        /// return AdjustNewLinesOperation for the node provided by the given formatting rules
        /// </summary>
        internal static AdjustNewLinesOperation? GetAdjustNewLinesOperation(IEnumerable<AbstractFormattingRule> formattingRules, SyntaxToken previousToken, SyntaxToken currentToken, AnalyzerConfigOptions options)
        {
            var chainedFormattingRules = new ChainedFormattingRules(formattingRules, options);
            return chainedFormattingRules.GetAdjustNewLinesOperation(previousToken, currentToken);
        }

        /// <summary>
        /// return AdjustSpacesOperation for the node provided by the given formatting rules
        /// </summary>
        internal static AdjustSpacesOperation? GetAdjustSpacesOperation(IEnumerable<AbstractFormattingRule> formattingRules, SyntaxToken previousToken, SyntaxToken currentToken, AnalyzerConfigOptions options)
        {
            var chainedFormattingRules = new ChainedFormattingRules(formattingRules, options);
            return chainedFormattingRules.GetAdjustSpacesOperation(previousToken, currentToken);
        }
    }
}
