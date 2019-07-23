// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal partial class ITypeSymbolExtensions
    {
        private class CompilationTypeGenerator : ITypeGenerator
        {
            private readonly Compilation _compilation;

            public CompilationTypeGenerator(Compilation compilation)
            {
                _compilation = compilation;
            }

            public ITypeSymbol CreateArrayTypeSymbol(ITypeSymbol elementType, int rank)
            {
                return _compilation.CreateArrayTypeSymbol(elementType, rank);
            }

            public ITypeSymbol CreatePointerTypeSymbol(ITypeSymbol pointedAtType)
            {
                return _compilation.CreatePointerTypeSymbol(pointedAtType);
            }

            public ITypeSymbol Construct(INamedTypeSymbol namedType, ITypeSymbol[] typeArguments)
            {
                return namedType.ConstructWithNullability(typeArguments);
            }
        }
    }
}
