// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal partial class ITypeSymbolExtensions
    {
        private class CompilationTypeGenerator : ITypeGenerator
        {
            private readonly Compilation compilation;

            public CompilationTypeGenerator(Compilation compilation)
            {
                this.compilation = compilation;
            }

            public ITypeSymbol CreateArrayTypeSymbol(ITypeSymbol elementType, int rank)
            {
                return compilation.CreateArrayTypeSymbol(elementType, rank);
            }

            public ITypeSymbol CreatePointerTypeSymbol(ITypeSymbol pointedAtType)
            {
                return compilation.CreatePointerTypeSymbol(pointedAtType);
            }

            public ITypeSymbol Construct(INamedTypeSymbol namedType, ITypeSymbol[] typeArguments)
            {
                return namedType.Construct(typeArguments);
            }
        }
    }
}