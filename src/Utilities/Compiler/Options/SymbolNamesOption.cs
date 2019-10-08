// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    internal sealed class SymbolNamesOption : IEquatable<SymbolNamesOption>
    {
        public static readonly SymbolNamesOption Empty = new SymbolNamesOption();

        private readonly ImmutableHashSet<string> _names;
        private readonly ImmutableHashSet<ISymbol> _symbols;

        private SymbolNamesOption(ImmutableHashSet<string> names, ImmutableHashSet<ISymbol> symbols)
        {
            Debug.Assert(!names.IsEmpty || !symbols.IsEmpty);

            _names = names;
            _symbols = symbols;
        }

        private SymbolNamesOption()
        {
            _names = ImmutableHashSet<string>.Empty;
            _symbols = ImmutableHashSet<ISymbol>.Empty;
        }

        public static SymbolNamesOption Create(ImmutableArray<string> symbolNames, Compilation compilation, string optionalPrefix)
        {
            if (symbolNames.IsEmpty)
            {
                return Empty;
            }

            var namesBuilder = PooledHashSet<string>.GetInstance();
            var symbolsBuilder = PooledHashSet<ISymbol>.GetInstance();

            foreach (var name in symbolNames)
            {
                if (name.Equals(".ctor", StringComparison.Ordinal) ||
                    name.Equals(".cctor", StringComparison.Ordinal) ||
                    !name.Contains(".") && !name.Contains(":"))
                {
                    namesBuilder.Add(name);
                }
                else
                {
                    var nameWithPrefix = (string.IsNullOrEmpty(optionalPrefix) || name.StartsWith(optionalPrefix, StringComparison.Ordinal)) ?
                        name :
                        optionalPrefix + name;

#pragma warning disable CA1307 // Specify StringComparison - https://github.com/dotnet/roslyn-analyzers/issues/1552
                    // Documentation comment ID for constructors uses '#ctor', but '#' is a comment start token for editorconfig.
                    // We instead search for a '..ctor' in editorconfig and replace it with a '.#ctor' here.
                    // Similarly, handle static constructors ".cctor"
                    nameWithPrefix = nameWithPrefix.Replace("..ctor", ".#ctor");
                    nameWithPrefix = nameWithPrefix.Replace("..cctor", ".#cctor");
#pragma warning restore

                    foreach (var symbol in DocumentationCommentId.GetSymbolsForDeclarationId(nameWithPrefix, compilation))
                    {
                        if (symbol != null)
                        {
                            if (symbol is INamespaceSymbol namespaceSymbol &&
                                namespaceSymbol.ConstituentNamespaces.Length > 1)
                            {
                                foreach (var constituentNamespace in namespaceSymbol.ConstituentNamespaces)
                                {
                                    symbolsBuilder.Add(constituentNamespace);
                                }
                            }

                            symbolsBuilder.Add(symbol);
                        }
                    }
                }
            }

            if (namesBuilder.Count == 0 && symbolsBuilder.Count == 0)
            {
                return Empty;
            }

            return new SymbolNamesOption(namesBuilder.ToImmutableAndFree(), symbolsBuilder.ToImmutableAndFree());
        }

        public bool IsEmpty => ReferenceEquals(this, Empty);

        public bool Contains(ISymbol symbol)
            => _symbols.Contains(symbol) || _names.Contains(symbol.Name);

        public override bool Equals(object obj)
        {
            return Equals(obj as SymbolNamesOption);
        }

        public bool Equals(SymbolNamesOption other)
        {
            return other != null &&
                _names.SetEquals(other._names) &&
                _symbols.SetEquals(other._symbols);
        }

        public override int GetHashCode()
        {
            return HashUtilities.Combine(HashUtilities.Combine(_names), HashUtilities.Combine(_symbols));
        }
    }
}