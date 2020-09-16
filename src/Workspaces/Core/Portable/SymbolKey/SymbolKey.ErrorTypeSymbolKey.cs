// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class ErrorTypeSymbolKey
        {
            public static void Create(INamedTypeSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteString(symbol.Name);
                switch (symbol.ContainingSymbol)
                {
                    case INamedTypeSymbol parentType:
                        visitor.WriteInteger(0);
                        visitor.WriteSymbolKey(parentType);
                        break;
                    case INamespaceSymbol parentNamespace:
                        visitor.WriteInteger(1);
                        visitor.WriteStringArray(GetContainingNamespaceNamesInReverse(parentNamespace));
                        break;
                    default:
                        visitor.WriteInteger(2);
                        break;
                }

                var isConstructed = !symbol.Equals(symbol.ConstructedFrom);
                visitor.WriteInteger(symbol.Arity);
                visitor.WriteBoolean(isConstructed);

                if (isConstructed)
                {
                    visitor.WriteSymbolKeyArray(symbol.TypeArguments);
                }
                else
                {
                    visitor.WriteSymbolKeyArray(ImmutableArray<ITypeSymbol>.Empty);
                }
            }

            /// <summary>
            /// For a symbol like <c>System.Collections.Generic.IEnumerable</c>, this would produce <c>"Generic",
            /// "Collections", "System"</c>
            /// </summary>
            private static ImmutableArray<string> GetContainingNamespaceNamesInReverse(INamespaceSymbol namespaceSymbol)
            {
                using var _ = ArrayBuilder<string>.GetInstance(out var builder);
                while (namespaceSymbol != null && namespaceSymbol.Name != "")
                {
                    builder.Add(namespaceSymbol.Name);
                    namespaceSymbol = namespaceSymbol.ContainingNamespace;
                }

                return builder.ToImmutable();
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
            {
                var name = reader.ReadString()!;
                var containingSymbolResolution = ResolveContainer(reader, out var containingSymbolFailureReason);
                var arity = reader.ReadInteger();
                var isConstructed = reader.ReadBoolean();

                using var typeArguments = reader.ReadSymbolKeyArray<ITypeSymbol>(out var typeArgumentsFailureReason);

                if (containingSymbolFailureReason != null)
                {
                    failureReason = $"({nameof(ErrorTypeSymbolKey)} {nameof(containingSymbolResolution)} failed -> {containingSymbolFailureReason})";
                    return default;
                }

                if (typeArgumentsFailureReason != null)
                {
                    failureReason = $"({nameof(ErrorTypeSymbolKey)} {nameof(typeArguments)} failed -> {typeArgumentsFailureReason})";
                    return default;
                }

                if (typeArguments.IsDefault)
                {
                    failureReason = $"({nameof(ErrorTypeSymbolKey)} {nameof(typeArguments)} failed)";
                    return default;
                }

                using var result = PooledArrayBuilder<INamedTypeSymbol>.GetInstance();

                var typeArgumentsArray = isConstructed ? typeArguments.Builder.ToArray() : null;
                foreach (var container in containingSymbolResolution.OfType<INamespaceOrTypeSymbol>())
                {
                    var originalType = reader.Compilation.CreateErrorTypeSymbol(container, name, arity);
                    var errorType = typeArgumentsArray != null ? originalType.Construct(typeArgumentsArray) : originalType;
                    result.AddIfNotNull(errorType);
                }

                // Always ensure at least one error type was created.
                if (result.Count == 0)
                    result.AddIfNotNull(reader.Compilation.CreateErrorTypeSymbol(container: null, name, arity));

                return CreateResolution(result, $"({nameof(ErrorTypeSymbolKey)} failed)", out failureReason);
            }

            private static SymbolKeyResolution ResolveContainer(SymbolKeyReader reader, out string? failureReason)
            {
                var type = reader.ReadInteger();

                if (type == 0)
                    return reader.ReadSymbolKey(out failureReason);

                if (type == 1)
                {
#pragma warning disable IDE0007 // Use implicit type
                    using PooledArrayBuilder<string> namespaceNames = reader.ReadStringArray()!;
#pragma warning restore IDE0007 // Use implicit type
                    var currentNamespace = reader.Compilation.GlobalNamespace;

                    // have to walk the namespaces in reverse because that's how we encoded them.
                    for (var i = namespaceNames.Count - 1; i >= 0; i--)
                        currentNamespace = reader.Compilation.CreateErrorNamespaceSymbol(currentNamespace, namespaceNames[i]);

                    failureReason = null;
                    return new SymbolKeyResolution(currentNamespace);
                }

                if (type == 2)
                {
                    failureReason = null;
                    return default;
                }

                throw ExceptionUtilities.UnexpectedValue(type);
            }
        }
    }
}
