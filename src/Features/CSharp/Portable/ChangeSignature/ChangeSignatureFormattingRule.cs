// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.ChangeSignature
{
    internal sealed class ChangeSignatureFormattingRule : BaseFormattingRule
    {
        private static readonly ImmutableArray<SyntaxKind> s_allowableKinds = ImmutableArray.Create(
            SyntaxKind.ParameterList,
            SyntaxKind.ArgumentList,
            SyntaxKind.BracketedParameterList,
            SyntaxKind.BracketedArgumentList,
            SyntaxKind.AttributeArgumentList);

        public override void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, in NextIndentBlockOperationAction nextOperation)
        {
            nextOperation.Invoke();

            if (s_allowableKinds.Contains(node.Kind()))
            {
                AddChangeSignatureIndentOperation(list, node);
            }
        }

        private static void AddChangeSignatureIndentOperation(List<IndentBlockOperation> list, SyntaxNode node)
        {
            if (node.Parent != null)
            {
                var baseToken = node.Parent.GetFirstToken();
                var startToken = node.GetFirstToken();
                var endToken = node.GetLastToken();
                var span = CommonFormattingHelpers.GetSpanIncludingTrailingAndLeadingTriviaOfAdjacentTokens(startToken, endToken);
                span = TextSpan.FromBounds(Math.Max(baseToken.Span.End, span.Start), span.End);

                list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(baseToken, startToken, endToken, span, indentationDelta: 1, option: IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine));
            }
        }

        public override AdjustNewLinesOperation GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
        {
            if (previousToken.Kind() == SyntaxKind.CommaToken && s_allowableKinds.Contains(previousToken.Parent.Kind()))
            {
                return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
            }

            return base.GetAdjustNewLinesOperation(in previousToken, in currentToken, in nextOperation);
        }
    }
}
