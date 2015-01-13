// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private readonly SymbolKey containerKey;
            private readonly string name;
            private readonly int arity;
            private readonly SymbolKey[] typeArgumentKeysOpt;

            internal ErrorTypeSymbolKey(INamedTypeSymbol symbol, Visitor visitor)
            {
                this.containerKey = symbol.ContainingSymbol is INamespaceOrTypeSymbol
                    ? GetOrCreate(symbol.ContainingSymbol, visitor)
                    : null;
                this.name = symbol.Name;
                this.arity = symbol.Arity;

                if (!symbol.Equals(symbol.ConstructedFrom))
                {
                    this.typeArgumentKeysOpt = symbol.TypeArguments.Select(a => GetOrCreate(a, visitor)).ToArray();
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
                    types = SpecializedCollections.SingletonEnumerable(CreateErrorTypeSymbol(compilation, null, name, arity));
                }

                return InstantiateTypes(compilation, ignoreAssemblyKey, types, arity, typeArgumentKeysOpt);
            }

            private IEnumerable<INamedTypeSymbol> ResolveErrorTypesWorker(Compilation compilation, bool ignoreAssemblyKey)
            {
                return containerKey == null
                    ? SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>()
                    : ResolveErrorTypeWithContainer(compilation, ignoreAssemblyKey);
            }

            private IEnumerable<INamedTypeSymbol> ResolveErrorTypeWithContainer(Compilation compilation, bool ignoreAssemblyKey)
            {
                var containerInfo = containerKey.Resolve(compilation, ignoreAssemblyKey);

                return GetAllSymbols<INamespaceOrTypeSymbol>(containerInfo).Select(s => Resolve(compilation, s));
            }

            private INamedTypeSymbol Resolve(Compilation compilation, INamespaceOrTypeSymbol container)
            {
                return CreateErrorTypeSymbol(compilation, container, name, arity);
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
                    other.arity == this.arity &&
                    Equals(options.IgnoreCase, other.name, this.name) &&
                    comparer.Equals(other.containerKey, this.containerKey) &&
                    SequenceEquals(other.typeArgumentKeysOpt, this.typeArgumentKeysOpt, comparer);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                // TODO(cyrusn): Consider hashing the type arguments as well.
                return
                    Hash.Combine(this.arity,
                    Hash.Combine(GetHashCode(options.IgnoreCase, this.name),
                                 this.containerKey.GetHashCode(options)));
            }
        }
    }
}