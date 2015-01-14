// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        private abstract class AbstractSymbolKey<TSymbolKey> : SymbolKey
            where TSymbolKey : AbstractSymbolKey<TSymbolKey>
        {
            internal sealed override bool Equals(SymbolKey other, ComparisonOptions options)
            {
                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                var type = other as TSymbolKey;
                if (ReferenceEquals(type, null))
                {
                    return false;
                }

                return Equals(type, options);
            }

            internal abstract bool Equals(TSymbolKey other, ComparisonOptions options);

            protected static IEnumerable<INamedTypeSymbol> InstantiateTypes(
                Compilation compilation,
                bool ignoreAssemblyKey,
                IEnumerable<INamedTypeSymbol> types,
                int arity,
                SymbolKey[] typeArgumentKeysOpt)
            {
                if (arity == 0 || typeArgumentKeysOpt == null)
                {
                    return types;
                }

                // TODO(cyrusn): We're only accepting a type argument if it resolves unambiguously.
                // However, we could consider the case where they resolve ambiguously and return
                // different named type instances when that happens.
                var typeArguments = typeArgumentKeysOpt.Select(a => a.Resolve(compilation, ignoreAssemblyKey).Symbol as ITypeSymbol).ToArray();
                return typeArguments.Any(a => a == null)
                    ? SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>()
                    : types.Select(t => t.Construct(typeArguments));
            }
        }
    }
}
