// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal static partial class CSharpCodeGenerator
    {
        public static string GenerateString(this ISymbol symbol, string indentation = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation, string eol = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, bool elasticTrivia = false)
            => symbol.GenerateSyntax().NormalizeWhitespace(indentation, eol, elasticTrivia).ToFullString();

        public static string GenerateNameString(this INamespaceOrTypeSymbol symbol, string indentation = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation, string eol = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, bool elasticTrivia = false)
            => symbol.GenerateNameSyntax().NormalizeWhitespace(indentation, eol, elasticTrivia).ToFullString();

        public static string GenerateTypeString(this INamespaceOrTypeSymbol symbol, string indentation = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation, string eol = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, bool elasticTrivia = false)
            => symbol.GenerateTypeSyntax().NormalizeWhitespace(indentation, eol, elasticTrivia).ToFullString();

        public static SyntaxNode GenerateSyntax(this ISymbol symbol)
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
                    return GenerateFieldDeclaration((IFieldSymbol)symbol);
                case SymbolKind.Label:
                    return GenerateLabelIdentifierName((ILabelSymbol)symbol);
                case SymbolKind.Local:
                    return GenerateLocalIdentifierName((ILocalSymbol)symbol);
                case SymbolKind.Method:
                    return GenerateMethodDeclaration((IMethodSymbol)symbol);
                case SymbolKind.NetModule:
                    break;
                case SymbolKind.NamedType:
                    return GenerateNamedTypeDeclaration((INamedTypeSymbol)symbol);
                case SymbolKind.Namespace:
                    return GenerateCompilationUnitOrNamespaceDeclaration((INamespaceSymbol)symbol);
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

        private static BuilderDisposer<T> GetArrayBuilder<T>(out ArrayBuilder<T> builder)
        {
            builder = ArrayBuilder<T>.GetInstance();
            return new BuilderDisposer<T>(builder);
        }

        private ref struct BuilderDisposer<T>
        {
            private readonly ArrayBuilder<T> _builder;

            public BuilderDisposer(ArrayBuilder<T> builder)
            {
                _builder = builder;
            }

            public void Dispose()
            {
                _builder.Free();
            }
        }
    }
}
