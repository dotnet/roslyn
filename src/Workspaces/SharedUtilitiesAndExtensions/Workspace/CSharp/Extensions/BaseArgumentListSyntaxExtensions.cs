﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class BaseArgumentListSyntaxExtensions
    {
        public static SyntaxToken GetOpenToken(this BaseArgumentListSyntax node)
        {
            if (node != null)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.ArgumentList:
                        return ((ArgumentListSyntax)node).OpenParenToken;
                    case SyntaxKind.BracketedArgumentList:
                        return ((BracketedArgumentListSyntax)node).OpenBracketToken;
                }
            }

            return default;
        }

        public static SyntaxToken GetCloseToken(this BaseArgumentListSyntax node)
        {
            if (node != null)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.ArgumentList:
                        return ((ArgumentListSyntax)node).CloseParenToken;
                    case SyntaxKind.BracketedArgumentList:
                        return ((BracketedArgumentListSyntax)node).CloseBracketToken;
                }
            }

            return default;
        }
    }
}
