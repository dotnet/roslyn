// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        private class TupleTypeSymbolKey : AbstractSymbolKey<TupleTypeSymbolKey>
        {
            private readonly SymbolKey[] _types;
            private readonly string[] _names;

            internal TupleTypeSymbolKey(INamedTypeSymbol symbol, Visitor visitor)
            {
                Debug.Assert(symbol.IsTupleType);

                _types = symbol.TupleElementTypes.Select(t => GetOrCreate(t, visitor)).ToArray();
                _names = symbol.TupleElementNames.IsDefault ? null : symbol.TupleElementNames.ToArray();
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                return CreateSymbolInfo(Resolve(compilation, ignoreAssemblyKey));
            }

            private IEnumerable<INamedTypeSymbol> Resolve(
                Compilation compilation,
                bool ignoreAssemblyKey)
            {
                // We need all types to have a resolution and we ignore ambiguous candidates
                ITypeSymbol[] types = _types.Select(a => a.Resolve(compilation, ignoreAssemblyKey).Symbol as ITypeSymbol).ToArray();
                if (types.Any(a => a == null))
                {
                    return SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>();
                }

                if (_names == null)
                {
                    return SpecializedCollections.SingletonEnumerable(compilation.CreateTupleTypeSymbol(types.ToImmutableArray()));
                }
                else
                {
                    return SpecializedCollections.SingletonEnumerable(compilation.CreateTupleTypeSymbol(types.ToImmutableArray(), _names.ToImmutableArray()));
                }
            }

            internal override bool Equals(TupleTypeSymbolKey other, ComparisonOptions options)
            {
                var comparer = SymbolKeyComparer.GetComparer(options);

                return SequenceEquals(other._types, _types, comparer)
                       && SequenceEquals(other._names, _names, StringComparer.Ordinal);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                // Types are good enough for hash code, we don't need to include names.
                return Hash.CombineValues(_types);
            }
        }
    }
}
