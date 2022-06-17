// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

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
                visitor.WriteBoolean(symbol.IsUnboundGenericType);

                if (!symbol.Equals(symbol.ConstructedFrom) && !symbol.IsUnboundGenericType)
                {
                    visitor.WriteSymbolKeyArray(symbol.TypeArguments);
                }
                else
                {
                    visitor.WriteSymbolKeyArray(ImmutableArray<ITypeSymbol>.Empty);
                }
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
            {
                var metadataName = reader.ReadString()!;
                var containingSymbolResolution = reader.ReadSymbolKey(out var containingSymbolFailureReason);
                var arity = reader.ReadInteger();
                var isUnboundGenericType = reader.ReadBoolean();
                using var typeArguments = reader.ReadSymbolKeyArray<ITypeSymbol>(out var typeArgumentsFailureReason);

                if (containingSymbolFailureReason != null)
                {
                    failureReason = $"({nameof(NamedTypeSymbolKey)} {nameof(containingSymbolFailureReason)} failed -> {containingSymbolFailureReason})";
                    return default;
                }

                if (typeArgumentsFailureReason != null)
                {
                    failureReason = $"({nameof(NamedTypeSymbolKey)} {nameof(typeArguments)} failed -> {typeArgumentsFailureReason})";
                    return default;
                }

                if (typeArguments.IsDefault)
                {
                    failureReason = $"({nameof(NamedTypeSymbolKey)} {nameof(typeArguments)} failed)";
                    return default;
                }

                var typeArgumentArray = typeArguments.Count == 0
                    ? Array.Empty<ITypeSymbol>()
                    : typeArguments.Builder.ToArray();
                using var result = PooledArrayBuilder<INamedTypeSymbol>.GetInstance();
                foreach (var nsOrType in containingSymbolResolution.OfType<INamespaceOrTypeSymbol>())
                {
                    Resolve(
                        result, nsOrType, metadataName, arity,
                        isUnboundGenericType, typeArgumentArray);
                }

                return CreateResolution(result, $"({nameof(NamedTypeSymbolKey)} failed)", out failureReason);
            }

            // <(ContainingType)>F(Ordinal)__(SourceName)
            private static readonly Regex s_fileTypeNamePattern = new Regex(@"<[a-zA-Z_0-9]*>F\d+__", RegexOptions.Compiled);

            private static void Resolve(
                PooledArrayBuilder<INamedTypeSymbol> result,
                INamespaceOrTypeSymbol container,
                string metadataName,
                int arity,
                bool isUnboundGenericType,
                ITypeSymbol[] typeArguments)
            {
                var nameWithoutArity = removeArity(metadataName);
                // Need to do a "decoding" step to get a file type source name from its metadata name
                var sourceName = s_fileTypeNamePattern.Match(nameWithoutArity) is { Success: true, Length: var length }
                    ? nameWithoutArity.Substring(length)
                    : nameWithoutArity;

                // PERF: We avoid calling GetTypeMembers(sourceName, arity) here to reduce allocations
                foreach (var type in container.GetTypeMembers(sourceName))
                {
                    // In case we have a file type, checking the MetadataName here allows us to distinguish whether we found the file type from the appropriate file.
                    // e.g. we might have found multiple file types named 'C' in the container, but with differing metadata names such as '<FileOne>F1__C' or '<FileTwo>F2__C'.
                    if (type.Arity == arity && string.Equals(type.MetadataName, metadataName, StringComparison.Ordinal))
                    {
                        var currentType = typeArguments.Length > 0 ? type.Construct(typeArguments) : type;
                        currentType = isUnboundGenericType ? currentType.ConstructUnboundGenericType() : currentType;

                        result.AddIfNotNull(currentType);
                    }
                }

                static string removeArity(string metadataName)
                {
                    var index = metadataName.IndexOf('`');
                    return index > 0
                        ? metadataName.Substring(0, index)
                        : metadataName;
                }
            }
        }
    }
}
