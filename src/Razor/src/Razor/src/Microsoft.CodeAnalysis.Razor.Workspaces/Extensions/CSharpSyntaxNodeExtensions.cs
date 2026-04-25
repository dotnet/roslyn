// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal static class CSharpSyntaxNodeExtensions
{
    extension(SyntaxNode node)
    {
        internal bool IsStringLiteral(bool multilineOnly = false)
        {
            if (node is not (InterpolatedStringTextSyntax or LiteralExpressionSyntax
                {
                    RawKind: (int)SyntaxKind.StringLiteralExpression or (int)SyntaxKind.Utf8StringLiteralExpression
                }))
            {
                return false;
            }

            if (!multilineOnly)
            {
                return true;
            }

            var sourceText = node.SyntaxTree.GetText();

            return sourceText.GetLinePositionSpan(node.Span).SpansMultipleLines();
        }

        /// <summary>
        /// Attempts to retrieve the first class declaration from the current syntax node, if present.
        /// </summary>
        /// <remarks>
        /// This method only supports the known shape of the Razor compiler generated C# source and should
        /// not be used for arbitrary C# syntax trees
        /// </remarks>
        internal bool TryGetClassDeclaration([NotNullWhen(true)] out ClassDeclarationSyntax? classDeclaration)
        {
            // Since we know how the compiler generates the C# source we can be a little specific here, and avoid
            // long tree walks. If the compiler ever changes how they generate their code, the tests for this will break
            // so we'll know about it.

            classDeclaration = node switch
            {
                CompilationUnitSyntax unit => unit switch
                {
                    { Members: [NamespaceDeclarationSyntax { Members: [ClassDeclarationSyntax c, ..] }, ..] } => c,
                    { Members: [ClassDeclarationSyntax c, ..] } => c,
                    _ => null,
                },
                _ => null,
            };

            return classDeclaration is not null;
        }
    }
}
