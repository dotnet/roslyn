// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        private class MethodTypeParameterSymbol : AbstractSymbolKey<MethodTypeParameterSymbol>
        {
            private readonly SymbolKey _containerKey;
            private readonly string _metadataName;

            public MethodTypeParameterSymbol(SymbolKey containerKey, ITypeParameterSymbol symbol)
            {
                _containerKey = containerKey;
                _metadataName = symbol.MetadataName;
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var container = _containerKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);
                var typeParameters = GetAllSymbols<IMethodSymbol>(container).SelectMany(s => Resolve(compilation, s));
                return CreateSymbolInfo(typeParameters);
            }

            private IEnumerable<ITypeParameterSymbol> Resolve(Compilation compilation, IMethodSymbol container)
            {
                return container.TypeParameters.Where(t => Equals(compilation, t.MetadataName, _metadataName));
            }

            internal override bool Equals(MethodTypeParameterSymbol other, ComparisonOptions options)
            {
                return Equals(options.IgnoreCase, other._metadataName, _metadataName) &&
                    (options.CompareMethodTypeParametersByName || other._containerKey.Equals(_containerKey, options));
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
