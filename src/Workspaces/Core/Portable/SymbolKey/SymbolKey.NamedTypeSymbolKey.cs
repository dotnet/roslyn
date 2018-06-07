// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class NamedTypeSymbolKey
        {
            public static void Create(INamedTypeSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteString(symbol.MetadataName);
                visitor.WriteSymbolKey(symbol.ContainingSymbol);
                visitor.WriteInteger(symbol.Arity);
                visitor.WriteInteger((int)symbol.TypeKind);
                visitor.WriteBoolean(symbol.IsUnboundGenericType);

                if (!symbol.Equals(symbol.ConstructedFrom) && !symbol.IsUnboundGenericType)
                {
                    visitor.WriteSymbolKeyArray(symbol.TypeArguments);
                }
                else
                {
                    visitor.WriteSymbolKeyArray(default(ImmutableArray<ITypeSymbol>));
                }
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var containingSymbolResolution = reader.ReadSymbolKey();
                var arity = reader.ReadInteger();
                var typeKind = (TypeKind)reader.ReadInteger();
                var isUnboundGenericType = reader.ReadBoolean();
                var typeArgumentsOpt = reader.ReadSymbolKeyArray();

                var types = containingSymbolResolution.GetAllSymbols<INamespaceOrTypeSymbol>().SelectMany(
                    s => Resolve(reader, s, metadataName, arity, typeKind, isUnboundGenericType, typeArgumentsOpt));
                return SymbolKeyResolution.Create(types);
            }

            private static IEnumerable<INamedTypeSymbol> Resolve(
                SymbolKeyReader reader,
                INamespaceOrTypeSymbol container,
                string metadataName,
                int arity,
                TypeKind typeKind,
                bool isUnboundGenericType,
                ImmutableArray<SymbolKeyResolution> typeArguments)
            {
                var types = container.GetTypeMembers(GetName(metadataName), arity);
                var result = InstantiateTypes(
                    reader.Compilation, types, arity, typeArguments);

                return isUnboundGenericType
                    ? result.Select(t => t.ConstructUnboundGenericType())
                    : result;
            }

            private static string GetName(string metadataName)
            {
                var index = metadataName.IndexOf('`');
                return index > 0
                    ? metadataName.Substring(0, index)
                    : metadataName;
            }

            private static IEnumerable<INamedTypeSymbol> InstantiateTypes(
                Compilation compilation,
                ImmutableArray<INamedTypeSymbol> types,
                int arity,
                ImmutableArray<SymbolKeyResolution> typeArgumentKeys)
            {
                if (arity == 0 || typeArgumentKeys.IsDefaultOrEmpty)
                {
                    return types;
                }

                // TODO(cyrusn): We're only accepting a type argument if it resolves unambiguously.
                // However, we could consider the case where they resolve ambiguously and return
                // different named type instances when that happens.
                var typeArguments = typeArgumentKeys.Select(a => a.GetFirstSymbol<ITypeSymbol>()).ToArray();
                return typeArguments.Any(s_typeIsNull)
                    ? SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>()
                    : types.Select(t => t.Construct(typeArguments));
            }
        }
    }
}
