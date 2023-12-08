// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class BaseArgumentListSyntaxExtensions
    {
        public static SyntaxToken GetOpenToken(this BaseArgumentListSyntax node)
            => node switch
            {
                ArgumentListSyntax list => list.OpenParenToken,
                BracketedArgumentListSyntax bracketedList => bracketedList.OpenBracketToken,
                _ => default,
            };

        public static SyntaxToken GetCloseToken(this BaseArgumentListSyntax node)
            => node switch
            {
                ArgumentListSyntax list => list.CloseParenToken,
                BracketedArgumentListSyntax bracketedList => bracketedList.CloseBracketToken,
                _ => default,
            };
    }
}
