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
        private class ErrorTypeSymbolKey : AbstractSymbolKey<ErrorTypeSymbolKey>
        {
            private readonly SymbolKey _containerKey;
            private readonly string _name;
            private readonly int _arity;
            private readonly SymbolKey[] _typeArgumentKeysOpt;

            internal ErrorTypeSymbolKey(INamedTypeSymbol symbol, Visitor visitor)
            {
                _containerKey = symbol.ContainingSymbol is INamespaceOrTypeSymbol
                    ? GetOrCreate(symbol.ContainingSymbol, visitor)
                    : null;
                _name = symbol.Name;
                _arity = symbol.Arity;

                if (!symbol.Equals(symbol.ConstructedFrom))
                {
                    _typeArgumentKeysOpt = symbol.TypeArguments.Select(a => GetOrCreate(a, visitor)).ToArray();
                }
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var types = ResolveErrorTypes(compilation, ignoreAssemblyKey);
                return CreateSymbolInfo(types);
            }

            private IEnumerable<INamedTypeSymbol> ResolveErrorTypes(Compilation compilation, bool ignoreAssemblyKey)
            {
                var types = ResolveErrorTypesWorker(compilation, ignoreAssemblyKey);
                if (!types.Any())
                {
                    types = SpecializedCollections.SingletonEnumerable(CreateErrorTypeSymbol(compilation, null, _name, _arity));
                }

                return InstantiateTypes(compilation, ignoreAssemblyKey, types, _arity, _typeArgumentKeysOpt);
            }

            private IEnumerable<INamedTypeSymbol> ResolveErrorTypesWorker(Compilation compilation, bool ignoreAssemblyKey)
            {
                return _containerKey == null
                    ? SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>()
                    : ResolveErrorTypeWithContainer(compilation, ignoreAssemblyKey);
            }

            private IEnumerable<INamedTypeSymbol> ResolveErrorTypeWithContainer(Compilation compilation, bool ignoreAssemblyKey)
            {
                var containerInfo = _containerKey.Resolve(compilation, ignoreAssemblyKey);

                return GetAllSymbols<INamespaceOrTypeSymbol>(containerInfo).Select(s => Resolve(compilation, s));
            }

            private INamedTypeSymbol Resolve(Compilation compilation, INamespaceOrTypeSymbol container)
            {
                return CreateErrorTypeSymbol(compilation, container, _name, _arity);
            }

            private INamedTypeSymbol CreateErrorTypeSymbol(
                Compilation compilation, INamespaceOrTypeSymbol container, string name, int arity)
            {
                return compilation.CreateErrorTypeSymbol(container, name, arity);
            }

            internal override bool Equals(ErrorTypeSymbolKey other, ComparisonOptions options)
            {
                var comparer = SymbolKeyComparer.GetComparer(options);
                return
                    other._arity == _arity &&
                    Equals(options.IgnoreCase, other._name, _name) &&
                    comparer.Equals(other._containerKey, _containerKey) &&
                    SequenceEquals(other._typeArgumentKeysOpt, _typeArgumentKeysOpt, comparer);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                // TODO(cyrusn): Consider hashing the type arguments as well.
                return
                    Hash.Combine(_arity,
                    Hash.Combine(GetHashCode(options.IgnoreCase, _name),
                                 _containerKey.GetHashCode(options)));
            }
        }
    }
}
