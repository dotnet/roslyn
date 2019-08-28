// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal abstract class BaseFormattingRule : AbstractFormattingRule
    {
        protected void AddUnindentBlockOperation(
            List<IndentBlockOperation> list,
            SyntaxToken startToken,
            SyntaxToken endToken,
            TextSpan textSpan,
            IndentBlockOption option = IndentBlockOption.RelativePosition)
        {
            if (startToken.Kind() == SyntaxKind.None || endToken.Kind() == SyntaxKind.None)
            {
                return;
            }

            list.Add(FormattingOperations.CreateIndentBlockOperation(startToken, endToken, textSpan, indentationDelta: -1, option: option));
        }

        protected void AddUnindentBlockOperation(
            List<IndentBlockOperation> list,
            SyntaxToken startToken,
            SyntaxToken endToken,
            bool includeTriviaAtEnd = false,
            IndentBlockOption option = IndentBlockOption.RelativePosition)
        {
            if (startToken.Kind() == SyntaxKind.None || endToken.Kind() == SyntaxKind.None)
            {
                return;
            }

            if (includeTriviaAtEnd)
            {
                list.Add(FormattingOperations.CreateIndentBlockOperation(startToken, endToken, indentationDelta: -1, option: option));
            }
            else
            {
                var startPosition = CommonFormattingHelpers.GetStartPositionOfSpan(startToken);
                var endPosition = endToken.Span.End;

                list.Add(FormattingOperations.CreateIndentBlockOperation(startToken, endToken, TextSpan.FromBounds(startPosition, endPosition), indentationDelta: -1, option: option));
            }
        }

        protected void AddAbsoluteZeroIndentBlockOperation(
            List<IndentBlockOperation> list,
            SyntaxToken startToken,
            SyntaxToken endToken,
            IndentBlockOption option = IndentBlockOption.AbsolutePosition)
        {
            if (startToken.Kind() == SyntaxKind.None || endToken.Kind() == SyntaxKind.None)
            {
                return;
            }

            list.Add(FormattingOperations.CreateIndentBlockOperation(startToken, endToken, indentationDelta: 0, option: option));
        }

        protected void AddIndentBlockOperation(
            List<IndentBlockOperation> list,
            SyntaxToken startToken,
            SyntaxToken endToken,
            IndentBlockOption option = IndentBlockOption.RelativePosition)
        {
            if (startToken.Kind() == SyntaxKind.None || endToken.Kind() == SyntaxKind.None)
            {
                return;
            }

            list.Add(FormattingOperations.CreateIndentBlockOperation(startToken, endToken, indentationDelta: 1, option: option));
        }

        protected void AddIndentBlockOperation(
            List<IndentBlockOperation> list,
            SyntaxToken startToken,
            SyntaxToken endToken,
            TextSpan textSpan,
            IndentBlockOption option = IndentBlockOption.RelativePosition)
        {
            if (startToken.Kind() == SyntaxKind.None || endToken.Kind() == SyntaxKind.None)
            {
                return;
            }

            list.Add(FormattingOperations.CreateIndentBlockOperation(startToken, endToken, textSpan, indentationDelta: 1, option: option));
        }

        protected void AddIndentBlockOperation(
            List<IndentBlockOperation> list,
            SyntaxToken baseToken,
            SyntaxToken startToken,
            SyntaxToken endToken,
            IndentBlockOption option = IndentBlockOption.RelativePosition)
        {
            list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(baseToken, startToken, endToken, indentationDelta: 1, option: option));
        }

        protected void SetAlignmentBlockOperation(
            List<IndentBlockOperation> list,
            SyntaxToken baseToken,
            SyntaxToken startToken,
            SyntaxToken endToken,
            IndentBlockOption option = IndentBlockOption.RelativePosition)
        {
            list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(baseToken, startToken, endToken, indentationDelta: 0, option: option));
        }

        protected void AddSuppressWrappingIfOnSingleLineOperation(List<SuppressOperation> list, SyntaxToken startToken, SyntaxToken endToken, SuppressOption extraOption = SuppressOption.None)
        {
            AddSuppressOperation(list, startToken, endToken, SuppressOption.NoWrappingIfOnSingleLine | extraOption);
        }

        protected void AddSuppressAllOperationIfOnMultipleLine(List<SuppressOperation> list, SyntaxToken startToken, SyntaxToken endToken, SuppressOption extraOption = SuppressOption.None)
        {
            AddSuppressOperation(list, startToken, endToken, SuppressOption.NoSpacingIfOnMultipleLine | SuppressOption.NoWrapping | extraOption);
        }

        protected void AddSuppressOperation(List<SuppressOperation> list, SyntaxToken startToken, SyntaxToken endToken, SuppressOption option)
        {
            if (startToken.Kind() == SyntaxKind.None || endToken.Kind() == SyntaxKind.None)
            {
                return;
            }

            list.Add(FormattingOperations.CreateSuppressOperation(startToken, endToken, option));
        }

        protected void AddAnchorIndentationOperation(List<AnchorIndentationOperation> list, SyntaxToken anchorToken, SyntaxToken endToken)
        {
            if (anchorToken.Kind() == SyntaxKind.None || endToken.Kind() == SyntaxKind.None)
            {
                return;
            }

            list.Add(FormattingOperations.CreateAnchorIndentationOperation(anchorToken, endToken));
        }

        protected void AddAlignIndentationOfTokensToBaseTokenOperation(List<AlignTokensOperation> list, SyntaxNode containingNode, SyntaxToken baseNode, IEnumerable<SyntaxToken> tokens, AlignTokensOption option = AlignTokensOption.AlignIndentationOfTokensToBaseToken)
        {
            if (containingNode == null || tokens == null)
            {
                return;
            }

            list.Add(FormattingOperations.CreateAlignTokensOperation(baseNode, tokens, option));
        }

        protected AdjustNewLinesOperation CreateAdjustNewLinesOperation(int line, AdjustNewLinesOption option)
        {
            return FormattingOperations.CreateAdjustNewLinesOperation(line, option);
        }

        protected AdjustSpacesOperation CreateAdjustSpacesOperation(int space, AdjustSpacesOption option)
        {
            return FormattingOperations.CreateAdjustSpacesOperation(space, option);
        }

        protected void AddBraceSuppressOperations(List<SuppressOperation> list, SyntaxNode node)
        {
            var bracePair = node.GetBracePair();
            if (!bracePair.IsValidBracePair())
            {
                return;
            }

            var firstTokenOfNode = node.GetFirstToken(includeZeroWidth: true);

            if (node is MemberDeclarationSyntax memberDeclNode)
            {
                var firstAndLastTokens = memberDeclNode.GetFirstAndLastMemberDeclarationTokensAfterAttributes();
                firstTokenOfNode = firstAndLastTokens.Item1;
            }

            if (node.IsLambdaBodyBlock())
            {
                // include lambda itself.
                firstTokenOfNode = node.Parent.GetFirstToken(includeZeroWidth: true);
            }
            else if (node.IsKind(SyntaxKindEx.PropertyPatternClause))
            {
                // include the pattern recursive pattern syntax and/or subpattern
                firstTokenOfNode = firstTokenOfNode.GetPreviousToken();
            }

            // suppress wrapping on whole construct that owns braces and also brace pair itself if 
            // it is on same line
            AddSuppressWrappingIfOnSingleLineOperation(list, firstTokenOfNode, bracePair.Item2);
            AddSuppressWrappingIfOnSingleLineOperation(list, bracePair.Item1, bracePair.Item2);
        }
    }
}
