// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal partial class ITypeSymbolExtensions
    {
        private class CompilationTypeGenerator : ITypeGenerator
        {
            private readonly Compilation _compilation;

            public CompilationTypeGenerator(Compilation compilation)
                => _compilation = compilation;

            public ITypeSymbol CreateArrayTypeSymbol(ITypeSymbol elementType, int rank)
                => _compilation.CreateArrayTypeSymbol(elementType, rank);

            public ITypeSymbol CreatePointerTypeSymbol(ITypeSymbol pointedAtType)
                => _compilation.CreatePointerTypeSymbol(pointedAtType);

            public ITypeSymbol Construct(INamedTypeSymbol namedType, ITypeSymbol[] typeArguments)
                => namedType.Construct(typeArguments);
        }
    }
}
