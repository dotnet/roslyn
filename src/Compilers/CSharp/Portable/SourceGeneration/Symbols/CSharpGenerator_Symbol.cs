// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        public SyntaxNode? TryGenerate(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Assembly:
                case SymbolKind.NetModule:
                    // TODO: implement these so we can emit assembly/module attributes.
                    throw new NotImplementedException();

                case SymbolKind.Event:
                    return GenerateEventDeclaration((IEventSymbol)symbol);
                case SymbolKind.Field:
                    return TryGenerateFieldDeclaration((IFieldSymbol)symbol);
                case SymbolKind.Label:
                    return GenerateLabelIdentifierName((ILabelSymbol)symbol);
                case SymbolKind.Local:
                    return GenerateLocalIdentifierName((ILocalSymbol)symbol);
                case SymbolKind.Method:
                    return TryGenerateMethodDeclaration((IMethodSymbol)symbol);
                case SymbolKind.NamedType:
                    return TryGenerateNamedTypeDeclaration((INamedTypeSymbol)symbol);
                case SymbolKind.Namespace:
                    return GenerateCompilationUnitOrNamespaceDeclaration((INamespaceSymbol)symbol);
                case SymbolKind.Property:
                    return GeneratePropertyOrIndexerDeclaration((IPropertySymbol)symbol);
            }

            throw new NotSupportedException($"Directly generating a {symbol.Kind} symbol is not supported");
        }
    }
}
