// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Utilities
{
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
                var pair = previousToken.Parent.GetBracePair();
                if (pair.Item2.Kind() == SyntaxKind.None || !AreTwoTokensOnSameLine(previousToken, pair.Item2))
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
                        if (parent.Kind() == SyntaxKind.StringLiteralExpression ||
                            parent.Kind() == SyntaxKind.CharacterLiteralExpression)
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

            if ((parent is UsingDirectiveSyntax) ||
                (parent is DelegateDeclarationSyntax) ||
                (parent is FieldDeclarationSyntax) ||
                (parent is EventFieldDeclarationSyntax) ||
                (parent is MethodDeclarationSyntax) ||
                (parent is PropertyDeclarationSyntax) ||
                (parent is ConstructorDeclarationSyntax) ||
                (parent is DestructorDeclarationSyntax) ||
                (parent is OperatorDeclarationSyntax))
            {
                return ValueTuple.Create(GetAppropriatePreviousToken(parent.GetFirstToken(), canTokenBeFirstInABlock: true), parent.GetLastToken());
            }

            if (parent is AccessorDeclarationSyntax)
            {
                // if both accessors are on the same line, format the accessor list
                // { get; set; }
                if (GetEnclosingMember(endToken) is PropertyDeclarationSyntax propertyDeclaration &&
                    AreTwoTokensOnSameLine(propertyDeclaration.AccessorList.OpenBraceToken, propertyDeclaration.AccessorList.CloseBraceToken))
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
            if (parent is MemberDeclarationSyntax ||
                parent is SwitchStatementSyntax)
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
                if (parentOfParent is InitializerExpressionSyntax doubleInitializer)
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
                node.Kind() == SyntaxKind.IfStatement ||
                node.Kind() == SyntaxKind.ElseClause ||
                node.Kind() == SyntaxKind.WhileStatement ||
                node.Kind() == SyntaxKind.ForStatement ||
                node.Kind() == SyntaxKind.ForEachStatement ||
                node.Kind() == SyntaxKind.ForEachVariableStatement ||
                node.Kind() == SyntaxKind.UsingStatement ||
                node.Kind() == SyntaxKind.DoStatement ||
                node.Kind() == SyntaxKind.TryStatement ||
                node.Kind() == SyntaxKind.CatchClause ||
                node.Kind() == SyntaxKind.FinallyClause ||
                node.Kind() == SyntaxKind.LabeledStatement ||
                node.Kind() == SyntaxKind.LockStatement ||
                node.Kind() == SyntaxKind.FixedStatement ||
                node.Kind() == SyntaxKind.GetAccessorDeclaration ||
                node.Kind() == SyntaxKind.SetAccessorDeclaration ||
                node.Kind() == SyntaxKind.AddAccessorDeclaration ||
                node.Kind() == SyntaxKind.RemoveAccessorDeclaration;
        }

        private static SyntaxNode GetTopContainingNode(SyntaxNode node)
        {
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
            return token.Kind() is SyntaxKind.ColonToken && token is
            {
                Parent: SwitchLabelSyntax { ColonToken: token } switchLabel
            };
        }

        public static bool InBetweenTwoMembers(SyntaxToken previousToken, SyntaxToken currentToken)
        {
            if (previousToken.Kind() != SyntaxKind.SemicolonToken && previousToken.Kind() != SyntaxKind.CloseBraceToken)
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

        public static MemberDeclarationSyntax GetEnclosingMember(SyntaxToken token)
        {
            if (token.Kind() == SyntaxKind.CloseBraceToken)
            {
                if (token.Parent.Kind() == SyntaxKind.Block ||
                    token.Parent.Kind() == SyntaxKind.AccessorList)
                {
                    return token.Parent.Parent as MemberDeclarationSyntax;
                }
            }

            return token.Parent.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        }
    }
}
