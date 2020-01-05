// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class SyntaxTokenExtensions
    {
        public static bool IsKindOrHasMatchingText(this SyntaxToken token, SyntaxKind kind)
        {
            return token.Kind() == kind || token.HasMatchingText(kind);
        }

        public static bool HasMatchingText(this SyntaxToken token, SyntaxKind kind)
        {
            return token.ToString() == SyntaxFacts.GetText(kind);
        }

        public static bool IsKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2)
        {
            return token.Kind() == kind1
                || token.Kind() == kind2;
        }

        public static bool IsKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3)
        {
            return token.Kind() == kind1
                || token.Kind() == kind2
                || token.Kind() == kind3;
        }

        public static bool IsKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4)
        {
            return token.Kind() == kind1
                || token.Kind() == kind2
                || token.Kind() == kind3
                || token.Kind() == kind4;
        }

        public static bool IsKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4, SyntaxKind kind5)
        {
            return token.Kind() == kind1
                || token.Kind() == kind2
                || token.Kind() == kind3
                || token.Kind() == kind4
                || token.Kind() == kind5;
        }

        public static bool IsKind(this SyntaxToken token, params SyntaxKind[] kinds)
        {
            return kinds.Contains(token.Kind());
        }

        public static bool IsOpenBraceOrCommaOfObjectInitializer(this SyntaxToken token)
        {
            return (token.IsKind(SyntaxKind.OpenBraceToken) || token.IsKind(SyntaxKind.CommaToken)) &&
                token.Parent.IsKind(SyntaxKind.ObjectInitializerExpression);
        }

        public static bool IsOpenBraceOfAccessorList(this SyntaxToken token)
        {
            return token.IsKind(SyntaxKind.OpenBraceToken) && token.Parent.IsKind(SyntaxKind.AccessorList);
        }
    }
}
