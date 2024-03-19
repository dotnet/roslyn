// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class BaseParameterListSyntaxExtensions
{
    public static SyntaxToken GetOpenToken(this BaseParameterListSyntax node)
        => node switch
        {
            ParameterListSyntax list => list.OpenParenToken,
            BracketedParameterListSyntax bracketedList => bracketedList.OpenBracketToken,
            _ => default,
        };

    public static SyntaxToken GetCloseToken(this BaseParameterListSyntax node)
        => node switch
        {
            ParameterListSyntax list => list.CloseParenToken,
            BracketedParameterListSyntax bracketedList => bracketedList.CloseBracketToken,
            _ => default,
        };
}
