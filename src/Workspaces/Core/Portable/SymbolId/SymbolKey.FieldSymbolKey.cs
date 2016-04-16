// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        private class FieldSymbolKey : AbstractSymbolKey<FieldSymbolKey>
        {
            private readonly SymbolKey _containerKey;
            private readonly string _metadataName;

            internal FieldSymbolKey(IFieldSymbol symbol, Visitor visitor)
            {
                _containerKey = GetOrCreate(symbol.ContainingType, visitor);
                _metadataName = symbol.MetadataName;
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var containerInfo = _containerKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);
                var fields = GetAllSymbols<INamedTypeSymbol>(containerInfo).SelectMany(t => t.GetMembers(_metadataName)).OfType<IFieldSymbol>();
                return CreateSymbolInfo(fields);
            }

            internal override bool Equals(FieldSymbolKey other, ComparisonOptions options)
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
