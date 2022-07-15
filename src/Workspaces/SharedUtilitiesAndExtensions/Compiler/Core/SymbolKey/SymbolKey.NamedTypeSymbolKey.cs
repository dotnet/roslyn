// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class NamedTypeSymbolKey
        {
            public static void Create(INamedTypeSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteSymbolKey(symbol.ContainingSymbol);
                visitor.WriteString(symbol.Name);
                visitor.WriteInteger(symbol.Arity);
                visitor.WriteString(symbol.IsFileLocal
                    ? symbol.DeclaringSyntaxReferences[0].SyntaxTree.FilePath
                    : null);
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
                var contextualType = reader.CurrentContextualSymbol as INamedTypeSymbol;

                var containingSymbolResolution = reader.ReadSymbolKey(contextualType?.ContainingSymbol, out var containingSymbolFailureReason);
                var name = reader.ReadRequiredString();
                var arity = reader.ReadInteger();
                var filePath = reader.ReadString();
                var isUnboundGenericType = reader.ReadBoolean();

                using var typeArguments = reader.ReadSymbolKeyArray<INamedTypeSymbol, ITypeSymbol>(
                    contextualType,
                    getContextualType: static (contextualType, i) => SafeGet(contextualType.TypeArguments, i),
                    out var typeArgumentsFailureReason);

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

                if (containingSymbolFailureReason != null)
                {
                    // we weren't able to bind the container of this named type to something legitimate.  In normal
                    // cases, that would be the end of resolution.  However, if we are binding in a scenario where we
                    // have a contextual type that is an error type, we can see if our symbol key is a viable match for
                    // that error type.  
                    //
                    // For example, consider if our symbol key references System.String, but we're resolving against a
                    // compilation that is missing a reference to System.String, but has a method `Goo(string s)` which
                    // references it.  This `string s` in `Goo` will be an error type symbol for `System.String` and we
                    // *do* want to allow this to match.
                    //
                    // This is fundamentally inverted from normal resolution.  Normal resolution walks top down from teh
                    // root of the compilation to find the match.  However, error symbols cannot be found in that
                    // fashion.  Instead, we have to structurally match this error symbol against our own symbol key to
                    // see if it is valid.
                    if (contextualType is not IErrorTypeSymbol)
                    {
                        failureReason = $"({nameof(NamedTypeSymbolKey)} {nameof(containingSymbolFailureReason)} failed -> {containingSymbolFailureReason})";
                        return default;
                    }
                }

                var typeArgumentArray = typeArguments.Count == 0
                    ? Array.Empty<ITypeSymbol>()
                    : typeArguments.Builder.ToArray();
                using var result = PooledArrayBuilder<INamedTypeSymbol>.GetInstance();
                foreach (var nsOrType in containingSymbolResolution.OfType<INamespaceOrTypeSymbol>())
                {
                    Resolve(
                        result, nsOrType, name, arity, filePath,
                        isUnboundGenericType, typeArgumentArray);
                }

                return CreateResolution(result, $"({nameof(NamedTypeSymbolKey)} failed)", out failureReason);
            }

            private static void Resolve(
                PooledArrayBuilder<INamedTypeSymbol> result,
                INamespaceOrTypeSymbol container,
                string name,
                int arity,
                string? filePath,
                bool isUnboundGenericType,
                ITypeSymbol[] typeArguments)
            {
                foreach (var type in container.GetTypeMembers(name, arity))
                {
                    // if this is a file-local type, then only resolve to a file-local type from this same file
                    if (filePath != null)
                    {
                        if (!type.IsFileLocal ||
                            // note: if we found 'IsFile' returned true, we can assume DeclaringSyntaxReferences is non-empty.
                            type.DeclaringSyntaxReferences[0].SyntaxTree.FilePath != filePath)
                        {
                            continue;
                        }
                    }
                    else if (type.IsFileLocal)
                    {
                        // since this key lacks a file path it can't match against a file-local type
                        continue;
                    }

                    var currentType = typeArguments.Length > 0 ? type.Construct(typeArguments) : type;
                    currentType = isUnboundGenericType ? currentType.ConstructUnboundGenericType() : currentType;

                    result.AddIfNotNull(currentType);
                }
            }
        }
    }
}
