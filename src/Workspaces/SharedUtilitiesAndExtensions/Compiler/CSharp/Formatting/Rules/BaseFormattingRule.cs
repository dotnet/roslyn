// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal abstract class BaseFormattingRule : AbstractFormattingRule
{
    protected static void AddUnindentBlockOperation(
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

    protected static void AddUnindentBlockOperation(
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

    protected static void AddAbsoluteZeroIndentBlockOperation(
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

    protected static void AddIndentBlockOperation(
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

    protected static void AddIndentBlockOperation(
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

    protected static void AddIndentBlockOperation(
        List<IndentBlockOperation> list,
        SyntaxToken baseToken,
        SyntaxToken startToken,
        SyntaxToken endToken,
        IndentBlockOption option = IndentBlockOption.RelativePosition)
    {
        list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(baseToken, startToken, endToken, indentationDelta: 1, option: option));
    }

    protected static void SetAlignmentBlockOperation(
        List<IndentBlockOperation> list,
        SyntaxToken baseToken,
        SyntaxToken startToken,
        SyntaxToken endToken,
        IndentBlockOption option = IndentBlockOption.RelativePosition)
    {
        list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(baseToken, startToken, endToken, indentationDelta: 0, option: option));
    }

    protected static void AddSuppressWrappingIfOnSingleLineOperation(ArrayBuilder<SuppressOperation> list, SyntaxToken startToken, SyntaxToken endToken, SuppressOption extraOption = SuppressOption.None)
        => AddSuppressOperation(list, startToken, endToken, SuppressOption.NoWrappingIfOnSingleLine | extraOption);

    protected static void AddSuppressAllOperationIfOnMultipleLine(ArrayBuilder<SuppressOperation> list, SyntaxToken startToken, SyntaxToken endToken, SuppressOption extraOption = SuppressOption.None)
        => AddSuppressOperation(list, startToken, endToken, SuppressOption.NoSpacingIfOnMultipleLine | SuppressOption.NoWrapping | extraOption);

    protected static void AddSuppressOperation(ArrayBuilder<SuppressOperation> list, SyntaxToken startToken, SyntaxToken endToken, SuppressOption option)
    {
        if (startToken.Kind() == SyntaxKind.None || endToken.Kind() == SyntaxKind.None)
        {
            return;
        }

        list.Add(FormattingOperations.CreateSuppressOperation(startToken, endToken, option));
    }

    protected static void AddAnchorIndentationOperation(List<AnchorIndentationOperation> list, SyntaxToken anchorToken, SyntaxToken endToken)
    {
        if (anchorToken.Kind() == SyntaxKind.None || endToken.Kind() == SyntaxKind.None)
        {
            return;
        }

        list.Add(FormattingOperations.CreateAnchorIndentationOperation(anchorToken, endToken));
    }

    protected static void AddAlignIndentationOfTokensToBaseTokenOperation(List<AlignTokensOperation> list, SyntaxNode containingNode, SyntaxToken baseNode, IEnumerable<SyntaxToken> tokens, AlignTokensOption option = AlignTokensOption.AlignIndentationOfTokensToBaseToken)
    {
        if (containingNode == null || tokens == null)
        {
            return;
        }

        list.Add(FormattingOperations.CreateAlignTokensOperation(baseNode, tokens, option));
    }

    protected static AdjustNewLinesOperation CreateAdjustNewLinesOperation(int line, AdjustNewLinesOption option)
        => FormattingOperations.CreateAdjustNewLinesOperation(line, option);

    protected static AdjustSpacesOperation CreateAdjustSpacesOperation(int space, AdjustSpacesOption option)
        => FormattingOperations.CreateAdjustSpacesOperation(space, option);

    protected static void AddBraceSuppressOperations(ArrayBuilder<SuppressOperation> list, SyntaxNode node)
    {
        var bracePair = node.GetBracePair();
        if (!bracePair.IsValidBracketOrBracePair())
        {
            return;
        }

        var firstTokenOfNode = node.GetFirstToken(includeZeroWidth: true);

        if (node is MemberDeclarationSyntax memberDeclNode)
        {
            (firstTokenOfNode, _) = memberDeclNode.GetFirstAndLastMemberDeclarationTokensAfterAttributes();
        }

        if (node.IsLambdaBodyBlock())
        {
            RoslynDebug.AssertNotNull(node.Parent);

            // include lambda itself.
            firstTokenOfNode = node.Parent.GetFirstToken(includeZeroWidth: true);
        }
        else if (node.IsKind(SyntaxKind.PropertyPatternClause))
        {
            // include the pattern recursive pattern syntax and/or subpattern
            firstTokenOfNode = firstTokenOfNode.GetPreviousToken();
        }

        // suppress wrapping on whole construct that owns braces and also brace pair itself if 
        // it is on same line
        AddSuppressWrappingIfOnSingleLineOperation(list, firstTokenOfNode, bracePair.closeBrace);
        AddSuppressWrappingIfOnSingleLineOperation(list, bracePair.openBrace, bracePair.closeBrace);
    }
}
