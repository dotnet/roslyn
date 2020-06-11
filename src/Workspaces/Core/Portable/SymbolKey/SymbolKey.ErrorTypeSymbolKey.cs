// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

                visitor.WriteInteger(symbol.Arity);
                if (!symbol.Equals(symbol.ConstructedFrom))
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

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var name = reader.ReadString();
                var containingSymbolResolution = ResolveContainer(reader);
                var arity = reader.ReadInteger();

                using var typeArguments = reader.ReadSymbolKeyArray<ITypeSymbol>();
                if (typeArguments.IsDefault)
                {
                    return default;
                }

                using var result = PooledArrayBuilder<INamedTypeSymbol>.GetInstance();

                var typeArgumentsArray = arity > 0 ? typeArguments.Builder.ToArray() : null;
                foreach (var container in containingSymbolResolution.OfType<INamespaceOrTypeSymbol>())
                {
                    result.AddIfNotNull(Construct(
                        reader, container, name, arity, typeArgumentsArray));
                }

                // Always ensure at least one error type was created.
                if (result.Count == 0)
                {
                    result.AddIfNotNull(Construct(
                        reader, container: null, name, arity, typeArgumentsArray));
                }

                return CreateResolution(result);
            }

            private static SymbolKeyResolution ResolveContainer(SymbolKeyReader reader)
            {
                var type = reader.ReadInteger();

                if (type == 0)
                    return reader.ReadSymbolKey();

                if (type == 1)
                {
                    using var namespaceNames = reader.ReadStringArray();
                    var currentNamespace = reader.Compilation.GlobalNamespace;

                    // have to walk the namespaces in reverse because that's how we encoded them.
                    for (var i = namespaceNames.Count - 1; i >= 0; i--)
                        currentNamespace = reader.Compilation.CreateErrorNamespaceSymbol(currentNamespace, namespaceNames[i]);

                    return new SymbolKeyResolution(currentNamespace);
                }

                if (type == 2)
                    return default;

                throw ExceptionUtilities.UnexpectedValue(type);
            }

            private static INamedTypeSymbol Construct(SymbolKeyReader reader, INamespaceOrTypeSymbol container, string name, int arity, ITypeSymbol[] typeArguments)
            {
                var result = reader.Compilation.CreateErrorTypeSymbol(container, name, arity);
                return typeArguments != null ? result.Construct(typeArguments) : result;
            }
        }
    }
}
