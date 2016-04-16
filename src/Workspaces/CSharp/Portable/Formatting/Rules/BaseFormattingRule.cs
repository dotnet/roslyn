// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Formatting;
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

        protected void AddBraceSuppressOperations(List<SuppressOperation> list, SyntaxNode node, SyntaxToken lastToken)
        {
            var bracePair = node.GetBracePair();
            if (!bracePair.IsValidBracePair())
            {
                return;
            }

            var firstTokenOfNode = node.GetFirstToken(includeZeroWidth: true);

            var memberDeclNode = node as MemberDeclarationSyntax;
            if (memberDeclNode != null)
            {
                var firstAndLastTokens = memberDeclNode.GetFirstAndLastMemberDeclarationTokensAfterAttributes();
                firstTokenOfNode = firstAndLastTokens.Item1;
            }

            if (node.IsLambdaBodyBlock())
            {
                // include lambda itself.
                firstTokenOfNode = node.Parent.GetFirstToken(includeZeroWidth: true);
            }

            // We may think we have a complete set of braces, but that may not actually be the case
            // due incomplete code.  i.e. we have something like:
            //
            // class C
            // {
            //      int Blah {
            //          get { return blah
            // }
            //
            // In this case the parse will think that the get-accessor is actually on two lines 
            // (because it will consume the close curly that more accurately belongs to the class.
            //
            // Now there are different behaviors we want depending on what the user is doing 
            // and what we are formatting.  For example, if the user hits semicolon at the end of
            // "blah", then we want to keep the accessor on a single line.  In this scenario we
            // effectively want to ignore the following close curly as it may not be important to
            // this construct in the mind of the user. 
            //
            // However, say the user hits semicolon, then hits enter, then types a close curly.
            // In this scenario we would actually want the get-accessor to be formatted over multiple 
            // lines.  The difference here is that because the user just hit close-curly here we can 
            // consider it as being part of the closest construct and we can consider its placement
            // when deciding if the construct is on a single line.

            var endToken = bracePair.Item2;
            if (lastToken.Kind() != SyntaxKind.CloseBraceToken &&
                lastToken.Kind() != SyntaxKind.EndOfFileToken &&
                !endToken.IsMissing)
            {
                // The user didn't just type the close brace.  So any close brace we have may 
                // actually belong to a containing construct.  See if any containers are missing
                // a close brace, and if so, act as if our own close brace is missing.

                if (SomeParentHasMissingCloseBrace(node.Parent))
                {
                    if (node.IsKind(SyntaxKind.Block) && ((BlockSyntax)node).Statements.Count >= 1)
                    {
                        // In the case of a block, see if the first statement is on the same line 
                        // as the open curly.  If so then we'll want to consider the end of the
                        // block as the end of the first statement.  i.e. if you have:
                        //
                        //  try { }
                        //  catch { return;     // <-- the end of this block is the end of the return statement.
                        //  Method();
                        var firstStatement = ((BlockSyntax)node).Statements[0];
                        if (FormattingRangeHelper.AreTwoTokensOnSameLine(firstTokenOfNode, firstStatement.GetFirstToken()))
                        {
                            endToken = firstStatement.GetLastToken();
                        }
                    }
                    else
                    {
                        endToken = endToken.GetPreviousToken();
                    }
                }
            }

            // suppress wrapping on whole construct that owns braces and also brace pair itself if 
            // it is on same line
            AddSuppressWrappingIfOnSingleLineOperation(list, firstTokenOfNode, endToken);
            AddSuppressWrappingIfOnSingleLineOperation(list, bracePair.Item1, endToken);
        }

        private bool SomeParentHasMissingCloseBrace(SyntaxNode node)
        {
            while (node.Kind() != SyntaxKind.CompilationUnit)
            {
                var bracePair = node.GetBracePair();
                if (bracePair.Item2.IsMissing)
                {
                    return true;
                }

                node = node.Parent;
            }

            return false;
        }
    }
}
