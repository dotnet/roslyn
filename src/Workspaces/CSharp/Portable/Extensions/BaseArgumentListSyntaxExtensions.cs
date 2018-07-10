// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
