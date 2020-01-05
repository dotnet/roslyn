// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal static class MetadataNameHelpers
    {
        private static void AppendNamespace(INamespaceSymbol namespaceSymbol, StringBuilder builder)
        {
            builder.Append(namespaceSymbol.Name);
        }

        private static void AppendNamedType(INamedTypeSymbol namedTypeSymbol, StringBuilder builder)
        {
            builder.Append(namedTypeSymbol.Name);

            if (namedTypeSymbol.Arity > 0)
            {
                var typeArguments = namedTypeSymbol.TypeArguments;

                builder.Append('`');
                builder.Append(typeArguments.Length);

                // Append generic arguments
                builder.Append('[');

                for (var i = 0; i < typeArguments.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }

                    builder.Append(GetMetadataName(typeArguments[i]));
                }

                builder.Append(']');
            }
        }

        private static void AppendArrayType(IArrayTypeSymbol symbol, StringBuilder builder)
        {
            builder.Append(GetMetadataName(symbol.ElementType));

            builder.Append('[');
            builder.Append(',', symbol.Rank - 1);
            builder.Append(']');
        }

        private static void AppendPointerType(IPointerTypeSymbol symbol, StringBuilder builder)
        {
            builder.Append(GetMetadataName(symbol.PointedAtType));

            builder.Append('*');
        }

        public static string GetMetadataName(ITypeSymbol typeSymbol)
        {
            if (typeSymbol.Kind == SymbolKind.TypeParameter)
            {
                throw new ArgumentException("Type parameters are not suppported", nameof(typeSymbol));
            }

            var parts = new Stack<ISymbol>();

            ISymbol symbol = typeSymbol;
            while (symbol != null)
            {
                parts.Push(symbol);
                symbol = symbol.ContainingSymbol;
            }

            var builder = new StringBuilder();

            while (parts.Count > 0)
            {
                symbol = parts.Pop();

                if (builder.Length > 0)
                {
                    if (symbol.ContainingSymbol is ITypeSymbol)
                    {
                        builder.Append('+');
                    }
                    else
                    {
                        builder.Append('.');
                    }
                }

                switch (symbol.Kind)
                {
                    case SymbolKind.Namespace:
                        var namespaceSymbol = (INamespaceSymbol)symbol;
                        if (!namespaceSymbol.IsGlobalNamespace)
                        {
                            AppendNamespace(namespaceSymbol, builder);
                        }

                        break;

                    case SymbolKind.NamedType:
                        AppendNamedType((INamedTypeSymbol)symbol, builder);
                        break;

                    case SymbolKind.ArrayType:
                        AppendArrayType((IArrayTypeSymbol)symbol, builder);
                        break;

                    case SymbolKind.PointerType:
                        AppendPointerType((IPointerTypeSymbol)symbol, builder);
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
