// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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

            return default(SyntaxToken);
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

            return default(SyntaxToken);
        }
    }
}
