// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;
using VisualBasicExtensions = Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions;

namespace Roslyn.SyntaxVisualizer.Control
{
    public static class SyntaxKindHelper
    {
        // Helpers that return the language-specific (C# / VB) SyntaxKind of a language-agnostic
        // SyntaxNode / SyntaxToken / SyntaxTrivia.

        public static string GetKind(this SyntaxNodeOrToken nodeOrToken)
        {
            var kind = string.Empty;

            if (nodeOrToken.IsNode)
            {
                kind = nodeOrToken.AsNode().GetKind();
            }
            else
            {
                kind = nodeOrToken.AsToken().GetKind();
            }

            return kind;
        }

        public static string GetKind(this SyntaxNode node)
        {
            var kind = string.Empty;

            if (node.Language == LanguageNames.CSharp)
            {
                kind = CSharpExtensions.Kind(node).ToString();
            }
            else
            {
                kind = VisualBasicExtensions.Kind(node).ToString();
            }

            return kind;
        }

        public static string GetKind(this SyntaxToken token)
        {
            var kind = string.Empty;

            if (token.Language == LanguageNames.CSharp)
            {
                kind = CSharpExtensions.Kind(token).ToString();
            }
            else
            {
                kind = VisualBasicExtensions.Kind(token).ToString();
            }

            return kind;
        }

        public static string GetKind(this SyntaxTrivia trivia)
        {
            var kind = string.Empty;

            if (trivia.Language == LanguageNames.CSharp)
            {
                kind = CSharpExtensions.Kind(trivia).ToString();
            }
            else
            {
                kind = VisualBasicExtensions.Kind(trivia).ToString();
            }

            return kind;
        }
    }
}
