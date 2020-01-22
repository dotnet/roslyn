// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    internal sealed class SymbolNamesOption : IEquatable<SymbolNamesOption?>
    {
        private const SymbolKind AllKinds = SymbolKind.ErrorType;
        private const char WildcardChar = '*';

        private static readonly KeyValuePair<string, string?> NoWildcardMatch = default;

        public static readonly SymbolNamesOption Empty = new SymbolNamesOption();

        private readonly ImmutableDictionary<string, string?> _names;
        private readonly ImmutableDictionary<ISymbol, string?> _symbols;
        private readonly ImmutableDictionary<SymbolKind, ImmutableDictionary<string, string?>> _wildcardNamesBySymbolKind;

        private SymbolNamesOption(ImmutableDictionary<string, string?> names, ImmutableDictionary<ISymbol, string?> symbols,
            ImmutableDictionary<SymbolKind, ImmutableDictionary<string, string?>> wildcardNamesBySymbolKind)
        {
            Debug.Assert(!names.IsEmpty || !symbols.IsEmpty || !wildcardNamesBySymbolKind.IsEmpty);

            _names = names;
            _symbols = symbols;
            _wildcardNamesBySymbolKind = wildcardNamesBySymbolKind;
        }

        private SymbolNamesOption()
        {
            _names = ImmutableDictionary<string, string?>.Empty;
            _symbols = ImmutableDictionary<ISymbol, string?>.Empty;
            _wildcardNamesBySymbolKind = ImmutableDictionary<SymbolKind, ImmutableDictionary<string, string?>>.Empty;
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
            var wildcardNamesBuilder = PooledDictionary<SymbolKind, PooledDictionary<string, string?>>.GetInstance();

            foreach (var symbolName in symbolNames)
            {
                var parts = getSymbolNamePartsFunc != null
                    ? getSymbolNamePartsFunc(symbolName)
                    : new NameParts(symbolName);

                var numberOfWildcards = symbolName.Count(c => c == WildcardChar);

                // More than one wildcard, or wildcard char is not last, bail-out.
                if (numberOfWildcards > 1 ||
                    (numberOfWildcards == 1 && symbolName[symbolName.Length - 1] != WildcardChar))
                {
                    continue;
                }

                if (numberOfWildcards == 1)
                {
                    Debug.Assert(parts.SymbolName[parts.SymbolName.Length - 1] == WildcardChar);

                    if (parts.SymbolName[1] != ':')
                    {
                        if (!wildcardNamesBuilder.ContainsKey(AllKinds))
                        {
                            wildcardNamesBuilder.Add(AllKinds, PooledDictionary<string, string?>.GetInstance());
                        }
                        wildcardNamesBuilder[AllKinds].Add(parts.SymbolName.Substring(0, parts.SymbolName.Length - 1), parts.Value);
                        continue;
                    }

                    var symbolKind = parts.SymbolName[0] switch
                    {
                        'E' => (SymbolKind?)SymbolKind.Event,
                        'F' => SymbolKind.Field,
                        'M' => SymbolKind.Method,
                        'N' => SymbolKind.Namespace,
                        'P' => SymbolKind.Property,
                        'T' => SymbolKind.NamedType,
                        _ => null,
                    };

                    if (symbolKind != null)
                    {
                        if (!wildcardNamesBuilder.ContainsKey(symbolKind.Value))
                        {
                            wildcardNamesBuilder.Add(symbolKind.Value, PooledDictionary<string, string?>.GetInstance());
                        }
                        wildcardNamesBuilder[symbolKind.Value].Add(parts.SymbolName.Substring(2, parts.SymbolName.Length - 3), parts.Value);
                    }
                }
                else if (parts.SymbolName.Equals(".ctor", StringComparison.Ordinal) ||
                    parts.SymbolName.Equals(".cctor", StringComparison.Ordinal) ||
                    !parts.SymbolName.Contains(".") && !parts.SymbolName.Contains(":"))
                {
                    if (!namesBuilder.ContainsKey(parts.SymbolName))
                    {
                        namesBuilder.Add(parts.SymbolName, parts.Value);
                    }
                }
                else
                {
                    var nameWithPrefix = (string.IsNullOrEmpty(optionalPrefix) || parts.SymbolName.StartsWith(optionalPrefix, StringComparison.Ordinal))
                        ? parts.SymbolName
                        : optionalPrefix + parts.SymbolName;

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
                                    symbolsBuilder.Add(constituentNamespace, parts.Value);
                                }
                            }
                        }

                        if (!symbolsBuilder.ContainsKey(symbol))
                        {
                            symbolsBuilder.Add(symbol, parts.Value);
                        }
                    }
                }
            }

            if (namesBuilder.Count == 0 && symbolsBuilder.Count == 0 && wildcardNamesBuilder.Count == 0)
            {
                return Empty;
            }

            return new SymbolNamesOption(namesBuilder.ToImmutableDictionaryAndFree(), symbolsBuilder.ToImmutableDictionaryAndFree(),
                wildcardNamesBuilder.ToImmutableDictionaryAndFree(x => x.Key, x => x.Value.ToImmutableDictionaryAndFree(), wildcardNamesBuilder.Comparer));
        }

        public bool IsEmpty => ReferenceEquals(this, Empty);

        public bool Contains(ISymbol symbol)
            => _symbols.ContainsKey(symbol) || _names.ContainsKey(symbol.Name) || TryGetFirstWildcardMatch(symbol, out _);

        /// <summary>
        /// Gets the value associated with the specified symbol in the option specification.
        /// </summary>
        public bool TryGetValue(ISymbol symbol, [NotNullWhen(true)] out string? value)
        {
            if (_symbols.TryGetValue(symbol, out value) || _names.TryGetValue(symbol.Name, out value))
            {
                return true;
            }

            if (TryGetFirstWildcardMatch(symbol, out var match))
            {
                value = match.Value;
                return true;
            }

            value = null;
            return false;
        }

        public override bool Equals(object obj) => Equals(obj as SymbolNamesOption);

        public bool Equals(SymbolNamesOption? other)
            => other != null && _names.IsEqualTo(other._names) && _symbols.IsEqualTo(other._symbols) && _wildcardNamesBySymbolKind.IsEqualTo(other._wildcardNamesBySymbolKind);

        public override int GetHashCode()
            => HashUtilities.Combine(HashUtilities.Combine(_names), HashUtilities.Combine(_symbols), HashUtilities.Combine(_wildcardNamesBySymbolKind));

        private bool TryGetFirstWildcardMatch(ISymbol symbol, out KeyValuePair<string, string?> firstMatch)
        {
            firstMatch = NoWildcardMatch;

            if (_wildcardNamesBySymbolKind.IsEmpty)
            {
                return false;
            }

            Debug.Assert(symbol.Kind == SymbolKind.Event || symbol.Kind == SymbolKind.Field || symbol.Kind == SymbolKind.Method || symbol.Kind == SymbolKind.NamedType || symbol.Kind == SymbolKind.Namespace || symbol.Kind == SymbolKind.Property);

            var symbolFullNameBuilder = new StringBuilder();
            var symbolKindsToCheck = new HashSet<SymbolKind> { symbol.Kind };

            // Try a partial check on the symbol...
            if (TryGetSymbolPartialMatch(symbolFullNameBuilder, symbol, out firstMatch))
            {
                Debug.Assert(!string.IsNullOrWhiteSpace(firstMatch.Key));
                return true;
            }

            // ...Now try a partial check looping on the symbol's containing type...
            INamedTypeSymbol? currentType = symbol.ContainingType;
            while (currentType != null)
            {
                if (TryGetSymbolPartialMatch(symbolFullNameBuilder, currentType, out firstMatch))
                {
                    Debug.Assert(!string.IsNullOrWhiteSpace(firstMatch.Key));
                    return true;
                }

                symbolKindsToCheck.Add(SymbolKind.NamedType);
                currentType = currentType.ContainingType;
            }

            // ...Now try a partial check looping on the symbol's containing namespace...
            INamespaceSymbol? currentNamespace = symbol.ContainingNamespace;
            while (currentNamespace != null && !currentNamespace.IsGlobalNamespace)
            {
                if (TryGetSymbolPartialMatch(symbolFullNameBuilder, currentNamespace, out firstMatch))
                {
                    Debug.Assert(!string.IsNullOrWhiteSpace(firstMatch.Key));
                    return true;
                }

                symbolKindsToCheck.Add(SymbolKind.Namespace);
                currentNamespace = currentNamespace.ContainingNamespace;
            }

            // ...At this point we couldn't match any part of the symbol name in the 'AllKinds' part of the list, let's try with the type fully qualified name...
            Debug.Assert(symbolFullNameBuilder.Length > 0);
            Debug.Assert(symbolKindsToCheck.Count >= 1 && symbolKindsToCheck.Count <= 3);

            var symbolFullName = symbolFullNameBuilder.ToString();

            foreach (var kind in symbolKindsToCheck)
            {
                if (!_wildcardNamesBySymbolKind.ContainsKey(kind))
                {
                    continue;
                }

                if (TryGetFirstWildcardMatch(kind, symbolFullName, out firstMatch))
                {
                    Debug.Assert(!string.IsNullOrWhiteSpace(firstMatch.Key));
                    return true;
                }
            }

            // ...No match
            Debug.Assert(firstMatch.Equals(NoWildcardMatch));
            return false;

            bool TryGetSymbolPartialMatch(StringBuilder builder, ISymbol symbol, out KeyValuePair<string, string?> firstMatch)
            {
                if (builder.Length > 0)
                {
                    builder.Insert(0, ".");
                }

                builder.Insert(0, symbol.Name);

                return TryGetFirstWildcardMatch(AllKinds, symbol.Name, out firstMatch);
            }

            bool TryGetFirstWildcardMatch(SymbolKind kind, string symbolName, out KeyValuePair<string, string?> firstMatch)
            {
                if (!_wildcardNamesBySymbolKind.ContainsKey(kind))
                {
                    firstMatch = NoWildcardMatch;
                    return false;
                }

                firstMatch = _wildcardNamesBySymbolKind[kind].FirstOrDefault(x => symbolName.StartsWith(x.Key, StringComparison.Ordinal));
                return firstMatch.Key != null;
            }
        }

        /// <summary>
        /// Represents the two parts of a symbol name option when the symbol name is tighted to some specific value.
        /// This allows to link a value to a symbol while following the symbol's documentation ID format.
        /// </summary>
        /// <example>
        /// On the rule CA1710, we allow user specific suffix to be registered for symbol names using the following format:
        /// MyClass->Suffix or T:MyNamespace.MyClass->Suffix or N:MyNamespace->Suffix.
        /// </example>
        public sealed class NameParts
        {
            public NameParts(string symbolName, string? value = null)
            {
                SymbolName = symbolName.Trim();
                Value = value?.Trim();
            }

            public string SymbolName { get; }
            public string? Value { get; }
        }
    }
}