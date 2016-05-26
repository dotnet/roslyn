// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        private class TupleTypeSymbolKey : AbstractSymbolKey<TupleTypeSymbolKey>
        {
            [JsonProperty] private readonly SymbolKey _underlyingType;
            [JsonProperty] private readonly string[] _names;

            [JsonConstructor]
            internal TupleTypeSymbolKey(SymbolKey _underlyingType, string[] _names)
            {
                this._underlyingType = _underlyingType;
                this._names = _names;
            }

            internal TupleTypeSymbolKey(INamedTypeSymbol symbol, Visitor visitor)
            {
                Debug.Assert(symbol.IsTupleType);

                _underlyingType = GetOrCreate(symbol.TupleUnderlyingType, visitor);
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
                INamedTypeSymbol underlyingType = _underlyingType.Resolve(compilation, ignoreAssemblyKey).Symbol as INamedTypeSymbol;
                if ((object)underlyingType == null)
                {
                    return SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>();
                }

                try
                {
                    if (_names == null)
                    {
                        return SpecializedCollections.SingletonEnumerable(compilation.CreateTupleTypeSymbol(underlyingType));
                    }
                    else
                    {
                        return SpecializedCollections.SingletonEnumerable(compilation.CreateTupleTypeSymbol(underlyingType, _names.ToImmutableArray()));
                    }
                }
                catch (ArgumentException)
                {
                    // underlyingType is not tuple-compatible
                    return SpecializedCollections.SingletonEnumerable(compilation.GetSpecialType(SpecialType.System_Object));
                }
            }

            internal override bool Equals(TupleTypeSymbolKey other, ComparisonOptions options)
            {
                return _underlyingType.Equals(other._underlyingType, options) &&
                    SequenceEquals(other._names, _names, StringComparer.Ordinal);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                // The hash of the underlying type is good enough, we don't need to include names.
                return _underlyingType.GetHashCode(options);
            }
        }
    }
}