// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class IndentUserSettingsFormattingRule : BaseFormattingRule
    {
        public override void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<IndentBlockOperation> nextOperation)
        {
            nextOperation.Invoke(list);

            AddAndRemoveBlockIndentationOperation(list, node, optionSet);
        }

        private void AddAndRemoveBlockIndentationOperation(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet)
        {
            var bracePair = node.GetBracePair();

            // don't put block indentation operation if the block only contains label statement or it is lambda expression body block
            if (node.IsLambdaBodyBlock() || node.BlockContainsOnlyLabel() || !bracePair.IsValidBracePair())
            {
                return;
            }

            if (node is BlockSyntax && optionSet.GetOption(CSharpFormattingOptions.OpenCloseBracesIndent))
            {
                AddIndentBlockOperation(list, bracePair.Item1, bracePair.Item1, bracePair.Item1.Span);
                AddIndentBlockOperation(list, bracePair.Item2, bracePair.Item2, bracePair.Item2.Span);
            }

            if (node is BlockSyntax && !optionSet.GetOption(CSharpFormattingOptions.IndentBlock))
            {
                var startToken = bracePair.Item1.GetNextToken(includeZeroWidth: true);
                var endToken = bracePair.Item2.GetPreviousToken(includeZeroWidth: true);

                RemoveIndentBlockOperation(list, startToken, endToken);
            }

            if (node is SwitchStatementSyntax && !optionSet.GetOption(CSharpFormattingOptions.IndentSwitchSection))
            {
                var startToken = bracePair.Item1.GetNextToken(includeZeroWidth: true);
                var endToken = bracePair.Item2.GetPreviousToken(includeZeroWidth: true);

                RemoveIndentBlockOperation(list, startToken, endToken);
            }
        }

        protected void RemoveIndentBlockOperation(
            List<IndentBlockOperation> list,
            SyntaxToken startToken,
            SyntaxToken endToken)
        {
            if (startToken.CSharpKind() == SyntaxKind.None || endToken.CSharpKind() == SyntaxKind.None)
            {
                return;
            }

            var span = CommonFormattingHelpers.GetSpanIncludingTrailingAndLeadingTriviaOfAdjacentTokens(startToken, endToken);

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].TextSpan == span)
                {
                    list[i] = null;
                    return;
                }
            }
        }
    }
}
