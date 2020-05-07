// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal static partial class CSharpCodeGenerator
    {
        public static string GenerateString(this ISymbol symbol, string indentation = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation, string eol = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, bool elasticTrivia = false)
            => symbol.GenerateSyntax(indentation, eol, elasticTrivia).ToFullString();

        public static SyntaxNode GenerateSyntax(this ISymbol symbol, string indentation = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation, string eol = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, bool elasticTrivia = false)
        {
            return GenerateSyntaxWorker(symbol).NormalizeWhitespace<SyntaxNode>(indentation, eol, elasticTrivia);
        }

        private static SyntaxNode GenerateSyntaxWorker(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Alias:
                    break;
                case SymbolKind.ArrayType:
                    break;
                case SymbolKind.Assembly:
                    break;
                case SymbolKind.DynamicType:
                    break;
                case SymbolKind.ErrorType:
                    break;
                case SymbolKind.Event:
                    break;
                case SymbolKind.Field:
                    break;
                case SymbolKind.Label:
                    return GenerateLabel((ILabelSymbol)symbol);
                case SymbolKind.Local:
                    break;
                case SymbolKind.Method:
                    break;
                case SymbolKind.NetModule:
                    break;
                case SymbolKind.NamedType:
                    break;
                case SymbolKind.Namespace:
                    return GenerateNamespace((INamespaceSymbol)symbol);
                case SymbolKind.Parameter:
                    break;
                case SymbolKind.PointerType:
                    break;
                case SymbolKind.Property:
                    break;
                case SymbolKind.RangeVariable:
                    break;
                case SymbolKind.TypeParameter:
                    break;
                case SymbolKind.Preprocessing:
                    break;
                case SymbolKind.Discard:
                    break;
                default:
                    break;
            }

            throw new NotImplementedException();
        }
    }
}
