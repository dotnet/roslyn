// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        private class TypeParameterSymbolKey : AbstractSymbolKey<TypeParameterSymbolKey>
        {
            private readonly SymbolKey _containerKey;
            private readonly string _metadataName;

            public TypeParameterSymbolKey(ITypeParameterSymbol symbol, Visitor visitor)
            {
                _containerKey = GetOrCreate(symbol.ContainingSymbol, visitor);
                _metadataName = symbol.MetadataName;
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var container = _containerKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);
                var typeParameters = GetAllSymbols<INamedTypeSymbol>(container).SelectMany(s => Resolve(compilation, s));
                return CreateSymbolInfo(typeParameters);
            }

            private IEnumerable<ITypeParameterSymbol> Resolve(Compilation compilation, INamedTypeSymbol container)
            {
                return container.TypeParameters.Where(t => Equals(compilation, t.MetadataName, _metadataName));
            }

            internal override bool Equals(TypeParameterSymbolKey other, ComparisonOptions options)
            {
                return
                    Equals(options.IgnoreCase, other._metadataName, _metadataName) &&
                    other._containerKey.Equals(_containerKey, options);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                return Hash.Combine(
                    GetHashCode(options.IgnoreCase, _metadataName),
                    _containerKey.GetHashCode(options));
            }
        }
    }
}
