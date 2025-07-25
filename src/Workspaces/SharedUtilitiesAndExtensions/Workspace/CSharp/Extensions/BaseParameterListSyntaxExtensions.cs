// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class BaseParameterListSyntaxExtensions
{
    extension(BaseParameterListSyntax node)
    {
        public SyntaxToken GetOpenToken()
        => node switch
        {
            ParameterListSyntax list => list.OpenParenToken,
            BracketedParameterListSyntax bracketedList => bracketedList.OpenBracketToken,
            _ => default,
        };

        public SyntaxToken GetCloseToken()
            => node switch
            {
                ParameterListSyntax list => list.CloseParenToken,
                BracketedParameterListSyntax bracketedList => bracketedList.CloseBracketToken,
                _ => default,
            };
    }
}
