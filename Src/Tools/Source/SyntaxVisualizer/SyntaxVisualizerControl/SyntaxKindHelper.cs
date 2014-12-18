// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Roslyn.SyntaxVisualizer.Control
{
    public static class SyntaxKindHelper
    {
        // Helpers that return the language-sepcific (C# / VB) SyntaxKind of a language-agnostic
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
                kind = node.CSharpKind().ToString();
            }
            else 
            {
                kind = node.VBKind().ToString();
            }

            return kind;
        }

        public static string GetKind(this SyntaxToken token)
        {
            var kind = string.Empty;

            if (token.Language == LanguageNames.CSharp)
            {
                kind = token.CSharpKind().ToString();
            }
            else 
            {
                kind = token.VBKind().ToString();
            }

            return kind;
        }

        public static string GetKind(this SyntaxTrivia trivia)
        {
            var kind = string.Empty;

            if (trivia.Language == LanguageNames.CSharp)
            {
                kind = trivia.CSharpKind().ToString();
            }
            else
            {
                kind = trivia.VBKind().ToString();
            }

            return kind;
        }
    }
}