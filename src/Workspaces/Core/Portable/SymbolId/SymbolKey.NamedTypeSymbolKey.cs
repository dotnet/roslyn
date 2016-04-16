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
        private class NamedTypeSymbolKey : AbstractSymbolKey<NamedTypeSymbolKey>
        {
            private readonly SymbolKey _containerKey;
            private readonly string _metadataName;
            private readonly int _arity;
            private readonly SymbolKey[] _typeArgumentKeysOpt;
            private readonly TypeKind _typeKind;
            private readonly bool _isUnboundGenericType;

            internal NamedTypeSymbolKey(INamedTypeSymbol symbol, Visitor visitor)
            {
                _containerKey = GetOrCreate(symbol.ContainingSymbol, visitor);
                _metadataName = symbol.MetadataName;
                _arity = symbol.Arity;
                _typeKind = symbol.TypeKind;
                _isUnboundGenericType = symbol.IsUnboundGenericType;

                if (!symbol.Equals(symbol.ConstructedFrom) && !_isUnboundGenericType)
                {
                    _typeArgumentKeysOpt = symbol.TypeArguments.Select(a => GetOrCreate(a, visitor)).ToArray();
                }
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var containerInfo = _containerKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);
                var types = GetAllSymbols<INamespaceOrTypeSymbol>(containerInfo).SelectMany(s => Resolve(compilation, s, ignoreAssemblyKey));
                return CreateSymbolInfo(types);
            }

            private IEnumerable<INamedTypeSymbol> Resolve(
                Compilation compilation,
                INamespaceOrTypeSymbol container,
                bool ignoreAssemblyKey)
            {
                var types = container.GetTypeMembers(GetName(_metadataName), _arity);
                var result = InstantiateTypes(compilation, ignoreAssemblyKey, types, _arity, _typeArgumentKeysOpt);

                return _isUnboundGenericType
                    ? result.Select(t => t.ConstructUnboundGenericType())
                    : result;
            }

            internal override bool Equals(NamedTypeSymbolKey other, ComparisonOptions options)
            {
                var comparer = SymbolKeyComparer.GetComparer(options);
                return
                    other._arity == _arity &&
                    Equals(options.IgnoreCase, other._metadataName, _metadataName) &&
                    comparer.Equals(other._containerKey, _containerKey) &&
                    SequenceEquals(other._typeArgumentKeysOpt, _typeArgumentKeysOpt, comparer);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                // TODO(cyrusn): Consider hashing the type arguments as well.
                return
                    Hash.Combine(_arity,
                    Hash.Combine(GetHashCode(options.IgnoreCase, _metadataName),
                                 _containerKey.GetHashCode(options)));
            }
        }
    }
}
