// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal static class CSharpCodeGenerator
    {
        #region ISymbol

        public static string GenerateString(this ISymbol symbol, string indentation = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation, string eol = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, bool elasticTrivia = false)
            => symbol.GenerateSyntax()?.NormalizeWhitespace(indentation, eol, elasticTrivia).ToFullString() ?? "";

        public static string GenerateNameString(this INamespaceOrTypeSymbol symbol, string indentation = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation, string eol = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, bool elasticTrivia = false)
            => symbol.GenerateNameSyntax().NormalizeWhitespace(indentation, eol, elasticTrivia).ToFullString();

        public static string GenerateTypeString(this INamespaceOrTypeSymbol symbol, string indentation = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation, string eol = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, bool elasticTrivia = false)
            => symbol.GenerateTypeSyntax().NormalizeWhitespace(indentation, eol, elasticTrivia).ToFullString();

        public static NameSyntax GenerateNameSyntax(this INamespaceOrTypeSymbol symbol)
            => (NameSyntax)GenerateTypeSyntax(symbol, onlyNames: true);

        public static TypeSyntax GenerateTypeSyntax(this INamespaceOrTypeSymbol symbol)
            => GenerateTypeSyntax(symbol, onlyNames: false);

        public static SyntaxNode? GenerateSyntax(this ISymbol symbol)
            => new CSharpGenerator().TryGenerate(symbol);

        private static TypeSyntax GenerateTypeSyntax(this INamespaceOrTypeSymbol symbol, bool onlyNames)
            => new CSharpGenerator().GenerateTypeSyntax(symbol, onlyNames);

        #endregion

        #region IOperation

        public static string GenerateString(this IOperation? operation, string indentation = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation, string eol = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, bool elasticTrivia = false)
            => operation.GenerateSyntax()?.NormalizeWhitespace(indentation, eol, elasticTrivia).ToFullString() ?? "";

        public static string GenerateStatementString(this IOperation? operation, string indentation = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation, string eol = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, bool elasticTrivia = false)
            => operation.GenerateStatementSyntax()?.NormalizeWhitespace(indentation, eol, elasticTrivia).ToFullString() ?? "";

        public static string GenerateExpressionString(this IOperation? operation, string indentation = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation, string eol = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, bool elasticTrivia = false)
            => operation.GenerateExpressionSyntax()?.NormalizeWhitespace(indentation, eol, elasticTrivia).ToFullString() ?? "";

        public static SyntaxNode? GenerateSyntax(this IOperation? operation)
            => new CSharpGenerator().TryGenerate(operation);

        public static StatementSyntax? GenerateStatementSyntax(this IOperation? operation)
            => new CSharpGenerator().TryGenerateStatement(operation);

        public static ExpressionSyntax? GenerateExpressionSyntax(this IOperation? operation)
            => new CSharpGenerator().TryGenerateExpression(operation);

        internal static (BlockSyntax?, ArrowExpressionClauseSyntax?, SyntaxToken) GenerateBodyParts(this IBlockOperation? block)
            => new CSharpGenerator().GenerateBodyParts(block);

        #endregion
    }
}
