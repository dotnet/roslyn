// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    internal static class SignatureHelpUtilities
    {
        private static readonly Func<BaseArgumentListSyntax, SyntaxToken> s_getBaseArgumentListOpenToken = list => list.GetOpenToken();
        private static readonly Func<TypeArgumentListSyntax, SyntaxToken> s_getTypeArgumentListOpenToken = list => list.LessThanToken;
        private static readonly Func<AttributeArgumentListSyntax, SyntaxToken> s_getAttributeArgumentListOpenToken = list => list.OpenParenToken;

        private static readonly Func<BaseArgumentListSyntax, SyntaxToken> s_getBaseArgumentListCloseToken = list => list.GetCloseToken();
        private static readonly Func<TypeArgumentListSyntax, SyntaxToken> s_getTypeArgumentListCloseToken = list => list.GreaterThanToken;
        private static readonly Func<AttributeArgumentListSyntax, SyntaxToken> s_getAttributeArgumentListCloseToken = list => list.CloseParenToken;

        private static readonly Func<BaseArgumentListSyntax, IEnumerable<SyntaxNodeOrToken>> s_getBaseArgumentListArgumentsWithSeparators =
            list => list.Arguments.GetWithSeparators();
        private static readonly Func<TypeArgumentListSyntax, IEnumerable<SyntaxNodeOrToken>> s_getTypeArgumentListArgumentsWithSeparators =
            list => list.Arguments.GetWithSeparators();
        private static readonly Func<AttributeArgumentListSyntax, IEnumerable<SyntaxNodeOrToken>> s_getAttributeArgumentListArgumentsWithSeparators =
                    list => list.Arguments.GetWithSeparators();

        private static readonly Func<BaseArgumentListSyntax, IEnumerable<string>> s_getBaseArgumentListNames =
            list => list.Arguments.Select(argument => argument.NameColon == null ? null : argument.NameColon.Name.Identifier.ValueText);
        private static readonly Func<TypeArgumentListSyntax, IEnumerable<string>> s_getTypeArgumentListNames =
            list => list.Arguments.Select(a => (string)null);
        private static readonly Func<AttributeArgumentListSyntax, IEnumerable<string>> s_getAttributeArgumentListNames =
            list => list.Arguments.Select(
                argument => argument.NameColon != null
                    ? argument.NameColon.Name.Identifier.ValueText
                    : argument.NameEquals != null
                        ? argument.NameEquals.Name.Identifier.ValueText
                        : null);

        internal static SignatureHelpState GetSignatureHelpState(BaseArgumentListSyntax argumentList, int position)
        {
            return CommonSignatureHelpUtilities.GetSignatureHelpState(
                argumentList, position,
                s_getBaseArgumentListOpenToken,
                s_getBaseArgumentListCloseToken,
                s_getBaseArgumentListArgumentsWithSeparators,
                s_getBaseArgumentListNames);
        }

        internal static SignatureHelpState GetSignatureHelpState(TypeArgumentListSyntax argumentList, int position)
        {
            return CommonSignatureHelpUtilities.GetSignatureHelpState(
                argumentList, position,
                s_getTypeArgumentListOpenToken,
                s_getTypeArgumentListCloseToken,
                s_getTypeArgumentListArgumentsWithSeparators,
                s_getTypeArgumentListNames);
        }

        internal static SignatureHelpState GetSignatureHelpState(AttributeArgumentListSyntax argumentList, int position)
        {
            return CommonSignatureHelpUtilities.GetSignatureHelpState(
                argumentList, position,
                s_getAttributeArgumentListOpenToken,
                s_getAttributeArgumentListCloseToken,
                s_getAttributeArgumentListArgumentsWithSeparators,
                s_getAttributeArgumentListNames);
        }

        internal static TextSpan GetSignatureHelpSpan(BaseArgumentListSyntax argumentList)
        {
            return CommonSignatureHelpUtilities.GetSignatureHelpSpan(argumentList, s_getBaseArgumentListCloseToken);
        }

        internal static TextSpan GetSignatureHelpSpan(TypeArgumentListSyntax argumentList)
        {
            return CommonSignatureHelpUtilities.GetSignatureHelpSpan(argumentList, s_getTypeArgumentListCloseToken);
        }

        internal static TextSpan GetSignatureHelpSpan(AttributeArgumentListSyntax argumentList)
        {
            return CommonSignatureHelpUtilities.GetSignatureHelpSpan(argumentList, s_getAttributeArgumentListCloseToken);
        }
    }
}
