﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
