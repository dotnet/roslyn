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
        private INamedTypeSymbol? _currentNamedType = null;
        private ISymbol? _currentAccessorParent = null;

        public SyntaxNode Generate(ISymbol symbol)
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
                    return GenerateFieldDeclaration((IFieldSymbol)symbol);
                case SymbolKind.Label:
                    return GenerateLabelIdentifierName((ILabelSymbol)symbol);
                case SymbolKind.Local:
                    return GenerateLocalIdentifierName((ILocalSymbol)symbol);
                case SymbolKind.Method:
                    return GenerateMethodDeclaration((IMethodSymbol)symbol);
                case SymbolKind.NamedType:
                    return GenerateNamedTypeDeclaration((INamedTypeSymbol)symbol);
                case SymbolKind.Namespace:
                    return GenerateCompilationUnitOrNamespaceDeclaration((INamespaceSymbol)symbol);
                case SymbolKind.Property:
                    return GeneratePropertyOrIndexerDeclaration((IPropertySymbol)symbol);
            }

            throw new NotSupportedException($"Directly generating a {symbol.Kind} symbol is not supported");
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
