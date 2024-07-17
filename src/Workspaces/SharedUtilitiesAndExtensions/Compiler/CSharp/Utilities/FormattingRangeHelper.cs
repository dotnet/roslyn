// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Utilities;

/// <summary>
/// this help finding a range of tokens to format based on given ending token
/// </summary>
internal static class FormattingRangeHelper
{
    public static ValueTuple<SyntaxToken, SyntaxToken>? FindAppropriateRange(SyntaxToken endToken, bool useDefaultRange = true)
    {
        Contract.ThrowIfTrue(endToken.Kind() == SyntaxKind.None);

        return FixupOpenBrace(FindAppropriateRangeWorker(endToken, useDefaultRange));
    }

    private static ValueTuple<SyntaxToken, SyntaxToken>? FixupOpenBrace(ValueTuple<SyntaxToken, SyntaxToken>? tokenRange)
    {
        if (!tokenRange.HasValue)
        {
            return tokenRange;
        }

        // with a auto brace completion which will do auto formatting when a user types "{", it is quite common that we will automatically put a space
        // between "{" and "}". but user might blindly type without knowing that " " has automatically inserted for him. and ends up have two spaces.
        // for those cases, whenever we see previous token of the range is "{", we expand the range to include preceding "{"
        var currentToken = tokenRange.Value.Item1;
        var previousToken = currentToken.GetPreviousToken();

        while (currentToken.Kind() != SyntaxKind.CloseBraceToken && previousToken.Kind() == SyntaxKind.OpenBraceToken)
        {
            var (_, closeBrace) = previousToken.Parent.GetBracePair();
            if (closeBrace.Kind() == SyntaxKind.None || !AreTwoTokensOnSameLine(previousToken, closeBrace))
            {
                return ValueTuple.Create(currentToken, tokenRange.Value.Item2);
            }

            currentToken = previousToken;
            previousToken = currentToken.GetPreviousToken();
        }

        return ValueTuple.Create(currentToken, tokenRange.Value.Item2);
    }

    private static ValueTuple<SyntaxToken, SyntaxToken>? FindAppropriateRangeWorker(SyntaxToken endToken, bool useDefaultRange)
    {
        // special token that we know how to find proper starting token
        switch (endToken.Kind())
        {
            case SyntaxKind.CloseBraceToken:
                {
                    return FindAppropriateRangeForCloseBrace(endToken);
                }

            case SyntaxKind.SemicolonToken:
                {
                    return FindAppropriateRangeForSemicolon(endToken);
                }

            case SyntaxKind.ColonToken:
                {
                    return FindAppropriateRangeForColon(endToken);
                }

            default:
                {
                    // default case
                    if (!useDefaultRange)
                    {
                        return null;
                    }

                    // if given token is skipped token, don't bother to find appropriate
                    // starting point
                    if (endToken.Kind() == SyntaxKind.SkippedTokensTrivia)
                    {
                        return null;
                    }

                    var parent = endToken.Parent;
                    if (parent == null)
                    {
                        // if there is no parent setup yet, nothing we can do here.
                        return null;
                    }

                    // if we are called due to things in trivia or literals, don't bother
                    // finding a starting token
                    if (parent.Kind() is SyntaxKind.StringLiteralExpression or
                        SyntaxKind.CharacterLiteralExpression)
                    {
                        return null;
                    }

                    // format whole node that containing the end token + its previous one
                    // to do indentation
                    return ValueTuple.Create(GetAppropriatePreviousToken(parent.GetFirstToken()), parent.GetLastToken());
                }
        }
    }

    private static ValueTuple<SyntaxToken, SyntaxToken>? FindAppropriateRangeForSemicolon(SyntaxToken endToken)
    {
        var parent = endToken.Parent;
        if (parent == null || parent.Kind() == SyntaxKind.SkippedTokensTrivia)
        {
            return null;
        }

        if (parent is UsingDirectiveSyntax or
            DelegateDeclarationSyntax or
            FieldDeclarationSyntax or
            EventFieldDeclarationSyntax or
            MethodDeclarationSyntax or
            PropertyDeclarationSyntax or
            ConstructorDeclarationSyntax or
            DestructorDeclarationSyntax or
            OperatorDeclarationSyntax or
            ConversionOperatorDeclarationSyntax)
        {
            return ValueTuple.Create(GetAppropriatePreviousToken(parent.GetFirstToken(), canTokenBeFirstInABlock: true), parent.GetLastToken());
        }

        if (parent is AccessorDeclarationSyntax)
        {
            // if both accessors are on the same line, format the accessor list
            // { get; set; }
            if (GetEnclosingMember(endToken) is PropertyDeclarationSyntax propertyDeclaration &&
                AreTwoTokensOnSameLine(propertyDeclaration.AccessorList!.OpenBraceToken, propertyDeclaration.AccessorList.CloseBraceToken))
            {
                return ValueTuple.Create(propertyDeclaration.AccessorList.OpenBraceToken, propertyDeclaration.AccessorList.CloseBraceToken);
            }

            // otherwise, just format the accessor
            return ValueTuple.Create(GetAppropriatePreviousToken(parent.GetFirstToken(), canTokenBeFirstInABlock: true), parent.GetLastToken());
        }

        if (parent is StatementSyntax && !endToken.IsSemicolonInForStatement())
        {
            var container = GetTopContainingNode(parent);
            if (container == null)
            {
                return ValueTuple.Create(GetAppropriatePreviousToken(parent.GetFirstToken()), parent.GetLastToken());
            }

            if (IsSpecialContainingNode(container))
            {
                return ValueTuple.Create(GetAppropriatePreviousToken(container.GetFirstToken()), container.GetLastToken());
            }

            return ValueTuple.Create(GetAppropriatePreviousToken(parent.GetFirstToken(), canTokenBeFirstInABlock: true), parent.GetLastToken());
        }

        // don't do anything
        return null;
    }

    private static ValueTuple<SyntaxToken, SyntaxToken>? FindAppropriateRangeForCloseBrace(SyntaxToken endToken)
    {
        // don't do anything if there is no proper parent
        var parent = endToken.Parent;
        if (parent == null || parent.Kind() == SyntaxKind.SkippedTokensTrivia)
        {
            return null;
        }

        // cases such as namespace, type, enum, method almost any top level elements
        if (parent is MemberDeclarationSyntax or SwitchStatementSyntax or SwitchExpressionSyntax)
        {
            return ValueTuple.Create(GetAppropriatePreviousToken(parent.GetFirstToken()), parent.GetLastToken());
        }

        // property decl body or initializer
        if (parent is AccessorListSyntax)
        {
            // include property decl
            var containerOfList = parent.Parent;
            if (containerOfList == null)
            {
                return ValueTuple.Create(GetAppropriatePreviousToken(parent.GetFirstToken()), parent.GetLastToken());
            }

            return ValueTuple.Create(containerOfList.GetFirstToken(), containerOfList.GetLastToken());
        }

        if (parent is AnonymousObjectCreationExpressionSyntax)
        {
            return ValueTuple.Create(parent.GetFirstToken(), parent.GetLastToken());
        }

        if (parent is InitializerExpressionSyntax)
        {
            var parentOfParent = parent.Parent;
            if (parentOfParent == null)
            {
                return ValueTuple.Create(GetAppropriatePreviousToken(parent.GetFirstToken()), parent.GetLastToken());
            }

            // double initializer case such as
            // { { }
            if (parentOfParent is InitializerExpressionSyntax)
            {
                // if parent block has a missing brace, and current block is on same line, then
                // don't try to indent inner block.
                var firstTokenOfInnerBlock = parent.GetFirstToken();
                var lastTokenOfInnerBlock = parent.GetLastToken();

                var twoTokensOnSameLine = AreTwoTokensOnSameLine(firstTokenOfInnerBlock, lastTokenOfInnerBlock);
                if (twoTokensOnSameLine)
                {
                    return ValueTuple.Create(firstTokenOfInnerBlock, lastTokenOfInnerBlock);
                }
            }

            // include owner of the initializer node such as creation node
            return ValueTuple.Create(parentOfParent.GetFirstToken(), parentOfParent.GetLastToken());
        }

        if (parent is BlockSyntax)
        {
            var containerOfBlock = GetTopContainingNode(parent);
            if (containerOfBlock == null)
            {
                return ValueTuple.Create(GetAppropriatePreviousToken(parent.GetFirstToken()), parent.GetLastToken());
            }

            // things like method, constructor, etc and special cases
            if (containerOfBlock is MemberDeclarationSyntax ||
                IsSpecialContainingNode(containerOfBlock))
            {
                return ValueTuple.Create(GetAppropriatePreviousToken(containerOfBlock.GetFirstToken()), containerOfBlock.GetLastToken());
            }

            // double block case on single line case
            // { { }
            if (containerOfBlock is BlockSyntax)
            {
                // if parent block has a missing brace, and current block is on same line, then
                // don't try to indent inner block.
                var firstTokenOfInnerBlock = parent.GetFirstToken();
                var lastTokenOfInnerBlock = parent.GetLastToken();

                var twoTokensOnSameLine = AreTwoTokensOnSameLine(firstTokenOfInnerBlock, lastTokenOfInnerBlock);
                if (twoTokensOnSameLine)
                {
                    return ValueTuple.Create(firstTokenOfInnerBlock, lastTokenOfInnerBlock);
                }
            }

            // okay, for block, indent regardless whether it is first one on the line
            return ValueTuple.Create(GetPreviousTokenIfNotFirstTokenInTree(parent.GetFirstToken()), parent.GetLastToken());
        }

        // don't do anything
        return null;
    }

    private static ValueTuple<SyntaxToken, SyntaxToken>? FindAppropriateRangeForColon(SyntaxToken endToken)
    {
        // don't do anything if there is no proper parent
        var parent = endToken.Parent;
        if (parent == null || parent.Kind() == SyntaxKind.SkippedTokensTrivia)
        {
            return null;
        }

        // cases such as namespace, type, enum, method almost any top level elements
        if (IsColonInSwitchLabel(endToken))
        {
            return ValueTuple.Create(GetPreviousTokenIfNotFirstTokenInTree(parent.GetFirstToken()), parent.GetLastToken());
        }

        return null;
    }

    private static SyntaxToken GetPreviousTokenIfNotFirstTokenInTree(SyntaxToken token)
    {
        var previousToken = token.GetPreviousToken();
        return previousToken.Kind() == SyntaxKind.None ? token : previousToken;
    }

    public static bool AreTwoTokensOnSameLine(SyntaxToken token1, SyntaxToken token2)
    {
        if (token1 == token2)
        {
            return true;
        }

        var tree = token1.SyntaxTree;
        if (tree != null && tree.TryGetText(out var text))
        {
            return text.AreOnSameLine(token1, token2);
        }

        return !CommonFormattingHelpers.GetTextBetween(token1, token2).ContainsLineBreak();
    }

    private static SyntaxToken GetAppropriatePreviousToken(SyntaxToken startToken, bool canTokenBeFirstInABlock = false)
    {
        var previousToken = startToken.GetPreviousToken();
        if (previousToken.Kind() == SyntaxKind.None)
        {
            // no previous token, return as it is
            return startToken;
        }

        if (AreTwoTokensOnSameLine(previousToken, startToken))
        {
            // The previous token can be '{' of a block and type declaration
            // { int s = 0;
            if (canTokenBeFirstInABlock)
            {
                if (IsOpenBraceTokenOfABlockOrTypeOrNamespace(previousToken))
                {
                    return previousToken;
                }
            }

            // there is another token on same line.
            return startToken;
        }

        // start token is the first token on line

        // now check a special case where previous token belongs to a label.
        if (previousToken.IsLastTokenInLabelStatement())
        {
            RoslynDebug.AssertNotNull(previousToken.Parent?.Parent);
            var labelNode = previousToken.Parent.Parent;
            return GetAppropriatePreviousToken(labelNode.GetFirstToken());
        }

        return previousToken;
    }

    private static bool IsOpenBraceTokenOfABlockOrTypeOrNamespace(SyntaxToken previousToken)
    {
        return previousToken.IsKind(SyntaxKind.OpenBraceToken) &&
                                (previousToken.Parent.IsKind(SyntaxKind.Block) ||
                                 previousToken.Parent is TypeDeclarationSyntax ||
                                 previousToken.Parent is NamespaceDeclarationSyntax);
    }

    private static bool IsSpecialContainingNode(SyntaxNode node)
    {
        return
            node.Kind() is SyntaxKind.IfStatement or
            SyntaxKind.ElseClause or
            SyntaxKind.WhileStatement or
            SyntaxKind.ForStatement or
            SyntaxKind.ForEachStatement or
            SyntaxKind.ForEachVariableStatement or
            SyntaxKind.UsingStatement or
            SyntaxKind.DoStatement or
            SyntaxKind.TryStatement or
            SyntaxKind.CatchClause or
            SyntaxKind.FinallyClause or
            SyntaxKind.LabeledStatement or
            SyntaxKind.LockStatement or
            SyntaxKind.FixedStatement or
            SyntaxKind.UncheckedStatement or
            SyntaxKind.CheckedStatement or
            SyntaxKind.GetAccessorDeclaration or
            SyntaxKind.SetAccessorDeclaration or
            SyntaxKind.InitAccessorDeclaration or
            SyntaxKind.AddAccessorDeclaration or
            SyntaxKind.RemoveAccessorDeclaration;
    }

    private static SyntaxNode? GetTopContainingNode([DisallowNull] SyntaxNode? node)
    {
        RoslynDebug.AssertNotNull(node.Parent);

        node = node.Parent;
        if (!IsSpecialContainingNode(node))
        {
            return node;
        }

        var lastSpecialContainingNode = node;
        node = node.Parent;

        while (node != null)
        {
            if (!IsSpecialContainingNode(node))
            {
                return lastSpecialContainingNode;
            }

            lastSpecialContainingNode = node;
            node = node.Parent;
        }

        return null;
    }

    public static bool IsColonInSwitchLabel(SyntaxToken token)
    {
        return token.Kind() == SyntaxKind.ColonToken &&
            token.Parent is SwitchLabelSyntax switchLabel &&
            switchLabel.ColonToken == token;
    }

    public static bool InBetweenTwoMembers(SyntaxToken previousToken, SyntaxToken currentToken)
    {
        if (previousToken.Kind() is not SyntaxKind.SemicolonToken and not SyntaxKind.CloseBraceToken)
        {
            return false;
        }

        if (currentToken.Kind() == SyntaxKind.CloseBraceToken)
        {
            return false;
        }

        var previousMember = GetEnclosingMember(previousToken);
        var nextMember = GetEnclosingMember(currentToken);

        return previousMember != null
            && nextMember != null
            && previousMember != nextMember;
    }

    public static MemberDeclarationSyntax? GetEnclosingMember(SyntaxToken token)
    {
        RoslynDebug.AssertNotNull(token.Parent);

        if (token.Kind() == SyntaxKind.CloseBraceToken)
        {
            if (token.Parent.Kind() is SyntaxKind.Block or
                SyntaxKind.AccessorList)
            {
                return token.Parent.Parent as MemberDeclarationSyntax;
            }
        }

        return token.Parent.FirstAncestorOrSelf<MemberDeclarationSyntax>();
    }
}
