// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            => nodeOrToken.AsNode() is SyntaxNode node
                ? node.GetKind()
                : nodeOrToken.AsToken().GetKind();

        public static string GetKind(this SyntaxNode node)
            => node.Language == LanguageNames.CSharp
                ? CSharpExtensions.Kind(node).ToString()
                : VisualBasicExtensions.Kind(node).ToString();

        public static string GetKind(this SyntaxToken token)
            => token.Language == LanguageNames.CSharp
                ? CSharpExtensions.Kind(token).ToString()
                : VisualBasicExtensions.Kind(token).ToString();

        public static string GetKind(this SyntaxTrivia trivia)
            => trivia.Language == LanguageNames.CSharp
                ? CSharpExtensions.Kind(trivia).ToString()
                : VisualBasicExtensions.Kind(trivia).ToString();
    }
}
