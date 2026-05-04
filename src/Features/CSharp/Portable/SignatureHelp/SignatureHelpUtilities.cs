// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp;

internal static class SignatureHelpUtilities
{
    private static readonly Func<BaseArgumentListSyntax, SyntaxToken> s_getBaseArgumentListOpenToken = list => list.GetOpenToken();
    private static readonly Func<TypeArgumentListSyntax, SyntaxToken> s_getTypeArgumentListOpenToken = list => list.LessThanToken;
    private static readonly Func<InitializerExpressionSyntax, SyntaxToken> s_getInitializerExpressionOpenToken = e => e.OpenBraceToken;
    private static readonly Func<AttributeArgumentListSyntax, SyntaxToken> s_getAttributeArgumentListOpenToken = list => list.OpenParenToken;

    private static readonly Func<BaseArgumentListSyntax, SyntaxToken> s_getBaseArgumentListCloseToken = list => list.GetCloseToken();
    private static readonly Func<TypeArgumentListSyntax, SyntaxToken> s_getTypeArgumentListCloseToken = list => list.GreaterThanToken;
    private static readonly Func<InitializerExpressionSyntax, SyntaxToken> s_getInitializerExpressionCloseToken = e => e.CloseBraceToken;
    private static readonly Func<AttributeArgumentListSyntax, SyntaxToken> s_getAttributeArgumentListCloseToken = list => list.CloseParenToken;

    private static readonly Func<BaseArgumentListSyntax, SyntaxNodeOrTokenList> s_getBaseArgumentListArgumentsWithSeparators =
        list => list.Arguments.GetWithSeparators();
    private static readonly Func<TypeArgumentListSyntax, SyntaxNodeOrTokenList> s_getTypeArgumentListArgumentsWithSeparators =
        list => list.Arguments.GetWithSeparators();
    private static readonly Func<InitializerExpressionSyntax, SyntaxNodeOrTokenList> s_getInitializerExpressionArgumentsWithSeparators =
        e => e.Expressions.GetWithSeparators();
    private static readonly Func<AttributeArgumentListSyntax, SyntaxNodeOrTokenList> s_getAttributeArgumentListArgumentsWithSeparators =
        list => list.Arguments.GetWithSeparators();

    private static readonly Func<BaseArgumentListSyntax, IEnumerable<string?>> s_getBaseArgumentListNames =
        list => list.Arguments.Select(argument => argument.NameColon?.Name.Identifier.ValueText);
    private static readonly Func<TypeArgumentListSyntax, IEnumerable<string?>> s_getTypeArgumentListNames =
        list => list.Arguments.Select(a => (string?)null);
    private static readonly Func<InitializerExpressionSyntax, IEnumerable<string?>> s_getInitializerExpressionNames =
        e => e.Expressions.Select(a => (string?)null);
    private static readonly Func<AttributeArgumentListSyntax, IEnumerable<string?>> s_getAttributeArgumentListNames =
        list => list.Arguments.Select(
            argument => argument.NameColon?.Name.Identifier.ValueText ?? argument.NameEquals?.Name.Identifier.ValueText);

    public static SignatureHelpState? GetSignatureHelpState(BaseArgumentListSyntax argumentList, int position)
    {
        return CommonSignatureHelpUtilities.GetSignatureHelpState(
            argumentList, position,
            s_getBaseArgumentListOpenToken,
            s_getBaseArgumentListCloseToken,
            s_getBaseArgumentListArgumentsWithSeparators,
            s_getBaseArgumentListNames);
    }

    internal static SignatureHelpState? GetSignatureHelpState(TypeArgumentListSyntax argumentList, int position)
    {
        return CommonSignatureHelpUtilities.GetSignatureHelpState(
            argumentList, position,
            s_getTypeArgumentListOpenToken,
            s_getTypeArgumentListCloseToken,
            s_getTypeArgumentListArgumentsWithSeparators,
            s_getTypeArgumentListNames);
    }

    internal static SignatureHelpState? GetSignatureHelpState(InitializerExpressionSyntax argumentList, int position)
    {
        return CommonSignatureHelpUtilities.GetSignatureHelpState(
            argumentList, position,
            s_getInitializerExpressionOpenToken,
            s_getInitializerExpressionCloseToken,
            s_getInitializerExpressionArgumentsWithSeparators,
            s_getInitializerExpressionNames);
    }

    internal static SignatureHelpState? GetSignatureHelpState(AttributeArgumentListSyntax argumentList, int position)
    {
        return CommonSignatureHelpUtilities.GetSignatureHelpState(
            argumentList, position,
            s_getAttributeArgumentListOpenToken,
            s_getAttributeArgumentListCloseToken,
            s_getAttributeArgumentListArgumentsWithSeparators,
            s_getAttributeArgumentListNames);
    }

    internal static TextSpan GetSignatureHelpSpan(BaseArgumentListSyntax argumentList)
        => CommonSignatureHelpUtilities.GetSignatureHelpSpan(argumentList, s_getBaseArgumentListCloseToken);

    internal static TextSpan GetSignatureHelpSpan(TypeArgumentListSyntax argumentList)
        => CommonSignatureHelpUtilities.GetSignatureHelpSpan(argumentList, s_getTypeArgumentListCloseToken);

    internal static TextSpan GetSignatureHelpSpan(InitializerExpressionSyntax initializer)
        => CommonSignatureHelpUtilities.GetSignatureHelpSpan(initializer, initializer.SpanStart, s_getInitializerExpressionCloseToken);

    internal static TextSpan GetSignatureHelpSpan(AttributeArgumentListSyntax argumentList)
        => CommonSignatureHelpUtilities.GetSignatureHelpSpan(argumentList, s_getAttributeArgumentListCloseToken);

    internal static bool IsTriggerParenOrComma<TSyntaxNode>(SyntaxToken token, ImmutableArray<char> triggerCharacters) where TSyntaxNode : SyntaxNode
    {
        // Don't dismiss if the user types ( to start a parenthesized expression or tuple
        // Note that the tuple initially parses as a parenthesized expression 
        if (token.IsKind(SyntaxKind.OpenParenToken) &&
            token.Parent is ParenthesizedExpressionSyntax parenExpr)
        {
            var parenthesizedExpr = parenExpr.WalkUpParentheses();
            if (parenthesizedExpr.Parent is ArgumentSyntax)
            {
                var parent = parenthesizedExpr.Parent;
                var grandParent = parent.Parent;
                if (grandParent is ArgumentListSyntax && grandParent.Parent is TSyntaxNode)
                {
                    // Argument to TSyntaxNode's argument list
                    return true;
                }
                else
                {
                    // Argument to a tuple in TSyntaxNode's argument list
                    return grandParent is TupleExpressionSyntax && parenthesizedExpr.GetAncestor<TSyntaxNode>() != null;
                }
            }
            else
            {
                // Not an argument
                return false;
            }
        }

        // Don't dismiss if the user types ',' to add a member to a tuple
        if (token.IsKind(SyntaxKind.CommaToken) && token.Parent is TupleExpressionSyntax && token.GetAncestor<TSyntaxNode>() != null)
        {
            return true;
        }

        return !token.IsKind(SyntaxKind.None) &&
            token.ValueText.Length == 1 &&
            triggerCharacters.Contains(token.ValueText[0]) &&
            token.Parent is ArgumentListSyntax &&
            token.Parent.Parent is TSyntaxNode;
    }
}
