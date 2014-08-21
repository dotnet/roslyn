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
        [Serializable]
        private class NamedTypeSymbolKey : AbstractSymbolKey<NamedTypeSymbolKey>
        {
            private readonly SymbolKey containerKey;
            private readonly string metadataName;
            private readonly int arity;
            private readonly SymbolKey[] typeArgumentKeysOpt;
            private readonly TypeKind typeKind;
            private readonly bool isUnboundGenericType;

            internal NamedTypeSymbolKey(INamedTypeSymbol symbol, Visitor visitor)
            {
                this.containerKey = GetOrCreate(symbol.ContainingSymbol, visitor);
                this.metadataName = symbol.MetadataName;
                this.arity = symbol.Arity;
                this.typeKind = symbol.TypeKind;
                this.isUnboundGenericType = symbol.IsUnboundGenericType;

                if (!symbol.Equals(symbol.ConstructedFrom) && !isUnboundGenericType)
                {
                    this.typeArgumentKeysOpt = symbol.TypeArguments.Select(a => GetOrCreate(a, visitor)).ToArray();
                }
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var containerInfo = containerKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);
                var types = GetAllSymbols<INamespaceOrTypeSymbol>(containerInfo).SelectMany(s => Resolve(compilation, s, ignoreAssemblyKey));
                return CreateSymbolInfo(types);
            }

            private IEnumerable<INamedTypeSymbol> Resolve(
                Compilation compilation,
                INamespaceOrTypeSymbol container,
                bool ignoreAssemblyKey)
            {
                var types = container.GetTypeMembers(GetName(this.metadataName), this.arity);
                var result = InstantiateTypes(compilation, ignoreAssemblyKey, types, arity, typeArgumentKeysOpt);

                return this.isUnboundGenericType
                    ? result.Select(t => t.ConstructUnboundGenericType())
                    : result;
            }

            internal override bool Equals(NamedTypeSymbolKey other, ComparisonOptions options)
            {
                var comparer = SymbolKeyComparer.GetComparer(options);
                return
                    other.arity == this.arity &&
                    Equals(options.IgnoreCase, other.metadataName, this.metadataName) &&
                    comparer.Equals(other.containerKey, this.containerKey) &&
                    SequenceEquals(other.typeArgumentKeysOpt, this.typeArgumentKeysOpt, comparer);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                // TODO(cyrusn): Consider hashing the type arguments as well.
                return
                    Hash.Combine(this.arity,
                    Hash.Combine(GetHashCode(options.IgnoreCase, this.metadataName),
                                 this.containerKey.GetHashCode(options)));
            }
        }
    }
}