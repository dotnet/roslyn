// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private readonly SymbolKey containerKey;
            private readonly string metadataName;

            public MethodTypeParameterSymbol(SymbolKey containerKey, ITypeParameterSymbol symbol)
            {
                this.containerKey = containerKey;
                this.metadataName = symbol.MetadataName;
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var container = containerKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);
                var typeParameters = GetAllSymbols<IMethodSymbol>(container).SelectMany(s => Resolve(compilation, s));
                return CreateSymbolInfo(typeParameters);
            }

            private IEnumerable<ITypeParameterSymbol> Resolve(Compilation compilation, IMethodSymbol container)
            {
                return container.TypeParameters.Where(t => Equals(compilation, t.MetadataName, this.metadataName));
            }

            internal override bool Equals(MethodTypeParameterSymbol other, ComparisonOptions options)
            {
                return Equals(options.IgnoreCase, other.metadataName, this.metadataName) &&
                    (options.CompareMethodTypeParametersByName || other.containerKey.Equals(this.containerKey, options));
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                return Hash.Combine(
                     GetHashCode(options.IgnoreCase, this.metadataName),
                     this.containerKey.GetHashCode(options));
            }
        }
    }
}