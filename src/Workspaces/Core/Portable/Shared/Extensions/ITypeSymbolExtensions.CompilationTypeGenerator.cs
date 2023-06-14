// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal partial class ITypeSymbolExtensions
    {
        private class CompilationTypeGenerator(Compilation compilation) : ITypeGenerator
        {
            public ITypeSymbol CreateArrayTypeSymbol(ITypeSymbol elementType, int rank)
                => compilation.CreateArrayTypeSymbol(elementType, rank);

            public ITypeSymbol CreatePointerTypeSymbol(ITypeSymbol pointedAtType)
                => compilation.CreatePointerTypeSymbol(pointedAtType);

            public ITypeSymbol Construct(INamedTypeSymbol namedType, ITypeSymbol[] typeArguments)
                => namedType.Construct(typeArguments);
        }
    }
}
