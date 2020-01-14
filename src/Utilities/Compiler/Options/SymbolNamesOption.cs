// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    internal sealed class SymbolNamesOption : IEquatable<SymbolNamesOption?>
    {
        public static readonly SymbolNamesOption Empty = new SymbolNamesOption();

        private readonly ImmutableDictionary<string, string?> _names;
        private readonly ImmutableDictionary<ISymbol, string?> _symbols;

        private SymbolNamesOption(ImmutableDictionary<string, string?> names, ImmutableDictionary<ISymbol, string?> symbols)
        {
            Debug.Assert(!names.IsEmpty || !symbols.IsEmpty);

            _names = names;
            _symbols = symbols;
        }

        private SymbolNamesOption()
        {
            _names = ImmutableDictionary<string, string?>.Empty;
            _symbols = ImmutableDictionary<ISymbol, string?>.Empty;
        }

        public static SymbolNamesOption Create(ImmutableArray<string> symbolNames, Compilation compilation, string? optionalPrefix,
            Func<string, NameParts>? getSymbolNamePartsFunc = null)
        {
            if (symbolNames.IsEmpty)
            {
                return Empty;
            }

            var namesBuilder = PooledDictionary<string, string?>.GetInstance();
            var symbolsBuilder = PooledDictionary<ISymbol, string?>.GetInstance();

            foreach (var symbolName in symbolNames)
            {
                var parts = getSymbolNamePartsFunc != null
                    ? getSymbolNamePartsFunc(symbolName)
                    : new NameParts(symbolName);

                if (parts.TypeName.Equals(".ctor", StringComparison.Ordinal) ||
                    parts.TypeName.Equals(".cctor", StringComparison.Ordinal) ||
                    !parts.TypeName.Contains(".") && !parts.TypeName.Contains(":"))
                {
                    if (!namesBuilder.ContainsKey(parts.TypeName))
                    {
                        namesBuilder.Add(parts.TypeName, parts.Suffix);
                    }
                }
                else
                {
                    var nameWithPrefix = (string.IsNullOrEmpty(optionalPrefix) || parts.TypeName.StartsWith(optionalPrefix, StringComparison.Ordinal))
                        ? parts.TypeName
                        : optionalPrefix + parts.TypeName;

#pragma warning disable CA1307 // Specify StringComparison - https://github.com/dotnet/roslyn-analyzers/issues/1552
                    // Documentation comment ID for constructors uses '#ctor', but '#' is a comment start token for editorconfig.
                    // We instead search for a '..ctor' in editorconfig and replace it with a '.#ctor' here.
                    // Similarly, handle static constructors ".cctor"
                    nameWithPrefix = nameWithPrefix.Replace("..ctor", ".#ctor");
                    nameWithPrefix = nameWithPrefix.Replace("..cctor", ".#cctor");
#pragma warning restore

                    foreach (var symbol in DocumentationCommentId.GetSymbolsForDeclarationId(nameWithPrefix, compilation))
                    {
                        if (symbol == null)
                        {
                            continue;
                        }

                        if (symbol is INamespaceSymbol namespaceSymbol &&
                            namespaceSymbol.ConstituentNamespaces.Length > 1)
                        {
                            foreach (var constituentNamespace in namespaceSymbol.ConstituentNamespaces)
                            {
                                if (!symbolsBuilder.ContainsKey(constituentNamespace))
                                {
                                    symbolsBuilder.Add(constituentNamespace, parts.Suffix);
                                }
                            }
                        }

                        if (!symbolsBuilder.ContainsKey(symbol))
                        {
                            symbolsBuilder.Add(symbol, parts.Suffix);
                        }
                    }
                }
            }

            if (namesBuilder.Count == 0 && symbolsBuilder.Count == 0)
            {
                return Empty;
            }

            return new SymbolNamesOption(namesBuilder.ToImmutableDictionaryAndFree(), symbolsBuilder.ToImmutableDictionaryAndFree());
        }

        public bool IsEmpty => ReferenceEquals(this, Empty);

        public bool Contains(ISymbol symbol)
            => _symbols.ContainsKey(symbol) || _names.ContainsKey(symbol.Name);

        public bool TryGetSuffix(ISymbol symbol, [NotNullWhen(true)] out string? suffix) =>
            _symbols.TryGetValue(symbol, out suffix) || _names.TryGetValue(symbol.Name, out suffix);

        public override bool Equals(object obj)
        {
            return Equals(obj as SymbolNamesOption);
        }

        public bool Equals(SymbolNamesOption? other)
        {
            return other != null &&
                _names.Count == other._names.Count &&
                _symbols.Count == other._symbols.Count &&
                _names.Keys.All(key => other._names.ContainsKey(key) && string.Equals(_names[key], other._names[key], StringComparison.Ordinal)) &&
                _symbols.Keys.All(key => other._symbols.ContainsKey(key) && string.Equals(_symbols[key], other._symbols[key], StringComparison.Ordinal));
        }

        public override int GetHashCode()
        {
            return HashUtilities.Combine(HashUtilities.Combine(_names), HashUtilities.Combine(_symbols));
        }

        public sealed class NameParts
        {
            public NameParts(string typeName, string? suffix = null)
            {
                TypeName = typeName;
                Suffix = suffix;
            }

            public string TypeName { get; }
            public string? Suffix { get; }
        }
    }
}