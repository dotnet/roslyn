// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class TypeGenerator : ITypeGenerator
    {
        public TypeGenerator()
        {
        }

        public ITypeSymbol CreateArrayTypeSymbol(ITypeSymbol elementType, int rank)
        {
            return CodeGenerationSymbolFactory.CreateArrayTypeSymbol(elementType, rank);
        }

        public ITypeSymbol CreatePointerTypeSymbol(ITypeSymbol pointedAtType)
        {
            return CodeGenerationSymbolFactory.CreatePointerTypeSymbol(pointedAtType);
        }

        public ITypeSymbol Construct(INamedTypeSymbol namedType, ITypeSymbol[] typeArguments)
        {
            return namedType.ToCodeGenerationSymbol().Construct(typeArguments);
        }
    }
}
