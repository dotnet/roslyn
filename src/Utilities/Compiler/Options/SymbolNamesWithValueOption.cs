// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
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
    internal sealed class SymbolNamesWithValueOption<TValue> : IEquatable<SymbolNamesWithValueOption<TValue>?>
    {
        private const SymbolKind AllKinds = SymbolKind.ErrorType;
        private const char WildcardChar = '*';

        private static readonly KeyValuePair<string, TValue> NoWildcardMatch = default;

        public static readonly SymbolNamesWithValueOption<TValue> Empty = new SymbolNamesWithValueOption<TValue>();

        private readonly ImmutableDictionary<string, TValue> _names;
        private readonly ImmutableDictionary<ISymbol, TValue> _symbols;
        /// <summary>
        /// Dictionary holding per symbol kind the wildcard entry with its suffix.
        /// The implementation only supports the following SymbolKind: Namespace, Type, Event, Field, Method, Property and ErrorType (as a way to hold the non-fully qualified types).
        /// </summary>
        /// <example>
        /// ErrorType ->
        ///     Symbol* -> "some value"
        /// Namespace ->
        ///     Analyzer.Utilities -> ""
        /// Type ->
        ///     Analyzer.Utilities.SymbolNamesWithValueOption -> ""
        /// Event ->
        ///     Analyzer.Utilities.SymbolNamesWithValueOption.MyEvent -> ""
        /// Field ->
        ///     Analyzer.Utilities.SymbolNamesWithValueOption.myField -> ""
        /// Method ->
        ///     Analyzer.Utilities.SymbolNamesWithValueOption.MyMethod() -> ""
        /// Property ->
        ///     Analyzer.Utilities.SymbolNamesWithValueOption.MyProperty -> ""
        /// </example>
        private readonly ImmutableDictionary<SymbolKind, ImmutableDictionary<string, TValue>> _wildcardNamesBySymbolKind;

        /// <summary>
        /// Cache for the wildcard matching algorithm. The current implementation can be slow so we want to make sure that once a match is performed we save its result.
        /// </summary>
        private readonly ConcurrentDictionary<ISymbol, KeyValuePair<string, TValue>> _wildcardMatchResult = new ConcurrentDictionary<ISymbol, KeyValuePair<string, TValue>>();

        private SymbolNamesWithValueOption(ImmutableDictionary<string, TValue> names, ImmutableDictionary<ISymbol, TValue> symbols,
            ImmutableDictionary<SymbolKind, ImmutableDictionary<string, TValue>> wildcardNamesBySymbolKind)
        {
            Debug.Assert(!names.IsEmpty || !symbols.IsEmpty || !wildcardNamesBySymbolKind.IsEmpty);

            _names = names;
            _symbols = symbols;
            _wildcardNamesBySymbolKind = wildcardNamesBySymbolKind;
        }

        private SymbolNamesWithValueOption()
        {
            _names = ImmutableDictionary<string, TValue>.Empty;
            _symbols = ImmutableDictionary<ISymbol, TValue>.Empty;
            _wildcardNamesBySymbolKind = ImmutableDictionary<SymbolKind, ImmutableDictionary<string, TValue>>.Empty;
        }

#pragma warning disable CA1000 // Do not declare static members on generic types
        public static SymbolNamesWithValueOption<TValue> Create(ImmutableArray<string> symbolNames, Compilation compilation, string? optionalPrefix,
#pragma warning restore CA1000 // Do not declare static members on generic types
            Func<string, NameParts>? getSymbolNamePartsFunc = null)
        {
            if (symbolNames.IsEmpty)
            {
                return Empty;
            }

            var namesBuilder = PooledDictionary<string, TValue>.GetInstance();
            var symbolsBuilder = PooledDictionary<ISymbol, TValue>.GetInstance();
            var wildcardNamesBuilder = PooledDictionary<SymbolKind, PooledDictionary<string, TValue>>.GetInstance();

            foreach (var symbolName in symbolNames)
            {
                var parts = getSymbolNamePartsFunc != null
                    ? getSymbolNamePartsFunc(symbolName)
                    : new NameParts(symbolName);

                var numberOfWildcards = parts.SymbolName.Count(c => c == WildcardChar);

                // More than one wildcard, bail-out.
                if (numberOfWildcards > 1)
                {
                    continue;
                }

                // Wildcard is not last or is the only char, bail-out
                if (numberOfWildcards == 1 &&
                    (parts.SymbolName[parts.SymbolName.Length - 1] != WildcardChar ||
                    parts.SymbolName.Length == 1))
                {
                    continue;
                }

                if (numberOfWildcards == 1)
                {
                    ProcessWildcardName(parts, wildcardNamesBuilder);
                }
                else if (parts.SymbolName.Equals(".ctor", StringComparison.Ordinal) ||
                    parts.SymbolName.Equals(".cctor", StringComparison.Ordinal) ||
                    !parts.SymbolName.Contains(".") && !parts.SymbolName.Contains(":"))
                {
                    ProcessName(parts, namesBuilder);
                }
                else
                {
                    ProcessSymbolName(parts, compilation, optionalPrefix, symbolsBuilder);
                }
            }

            if (namesBuilder.Count == 0 && symbolsBuilder.Count == 0 && wildcardNamesBuilder.Count == 0)
            {
                return Empty;
            }

            return new SymbolNamesWithValueOption<TValue>(namesBuilder.ToImmutableDictionaryAndFree(),
                symbolsBuilder.ToImmutableDictionaryAndFree(),
                wildcardNamesBuilder.ToImmutableDictionaryAndFree(x => x.Key, x => x.Value.ToImmutableDictionaryAndFree(), wildcardNamesBuilder.Comparer));

            // Local functions

            static void ProcessWildcardName(NameParts parts, PooledDictionary<SymbolKind, PooledDictionary<string, TValue>> wildcardNamesBuilder)
            {
                Debug.Assert(parts.SymbolName[parts.SymbolName.Length - 1] == WildcardChar);
                Debug.Assert(parts.SymbolName.Length >= 2);

                if (parts.SymbolName[1] != ':')
                {
                    if (!wildcardNamesBuilder.ContainsKey(AllKinds))
                    {
                        wildcardNamesBuilder.Add(AllKinds, PooledDictionary<string, TValue>.GetInstance());
                    }
                    wildcardNamesBuilder[AllKinds].Add(parts.SymbolName.Substring(0, parts.SymbolName.Length - 1), parts.AssociatedValue);
                    return;
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
                        wildcardNamesBuilder.Add(symbolKind.Value, PooledDictionary<string, TValue>.GetInstance());
                    }
                    wildcardNamesBuilder[symbolKind.Value].Add(parts.SymbolName.Substring(2, parts.SymbolName.Length - 3), parts.AssociatedValue);
                }
            }

            static void ProcessName(NameParts parts, PooledDictionary<string, TValue> namesBuilder)
            {
                if (!namesBuilder.ContainsKey(parts.SymbolName))
                {
                    namesBuilder.Add(parts.SymbolName, parts.AssociatedValue);
                }
            }

            static void ProcessSymbolName(NameParts parts, Compilation compilation, string? optionalPrefix, PooledDictionary<ISymbol, TValue> symbolsBuilder)
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
                                symbolsBuilder.Add(constituentNamespace, parts.AssociatedValue);
                            }
                        }
                    }

                    if (!symbolsBuilder.ContainsKey(symbol))
                    {
                        symbolsBuilder.Add(symbol, parts.AssociatedValue);
                    }
                }
            }
        }

        public bool IsEmpty => ReferenceEquals(this, Empty);

        public bool Contains(ISymbol symbol)
            => _symbols.ContainsKey(symbol) || _names.ContainsKey(symbol.Name) || TryGetFirstWildcardMatch(symbol, out _);

        /// <summary>
        /// Gets the value associated with the specified symbol in the option specification.
        /// </summary>
        public bool TryGetValue(ISymbol symbol, [NotNullWhen(true)] out TValue value)
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

#pragma warning disable CS8653 // A default expression introduces a null value for a type parameter.
            value = default;
#pragma warning restore CS8653 // A default expression introduces a null value for a type parameter.
            return false;
        }

        public override bool Equals(object obj) => Equals(obj as SymbolNamesWithValueOption<TValue>);

        public bool Equals(SymbolNamesWithValueOption<TValue>? other)
            => other != null && _names.IsEqualTo(other._names) && _symbols.IsEqualTo(other._symbols) && _wildcardNamesBySymbolKind.IsEqualTo(other._wildcardNamesBySymbolKind);

        public override int GetHashCode()
            => HashUtilities.Combine(HashUtilities.Combine(_names), HashUtilities.Combine(_symbols), HashUtilities.Combine(_wildcardNamesBySymbolKind));

        private bool TryGetFirstWildcardMatch(ISymbol symbol, out KeyValuePair<string, TValue> firstMatch)
        {
            firstMatch = NoWildcardMatch;

            if (_wildcardNamesBySymbolKind.IsEmpty)
            {
                return false;
            }

            Debug.Assert(symbol.Kind == SymbolKind.Event || symbol.Kind == SymbolKind.Field || symbol.Kind == SymbolKind.Method || symbol.Kind == SymbolKind.NamedType || symbol.Kind == SymbolKind.Namespace || symbol.Kind == SymbolKind.Property);

            // The matching was already processed
            if (_wildcardMatchResult.ContainsKey(symbol))
            {
                firstMatch = _wildcardMatchResult[symbol];
                return !firstMatch.Equals(NoWildcardMatch);
            }

            var symbolFullNameBuilder = new StringBuilder();
            var symbolKindsToCheck = new HashSet<SymbolKind> { symbol.Kind };

            // Try a partial check on the symbol...
            if (TryGetSymbolPartialMatch(symbolFullNameBuilder, symbol, out firstMatch))
            {
                return true;
            }

            // ...Now try a partial check looping on the symbol's containing type...
            INamedTypeSymbol? currentType = symbol.ContainingType;
            while (currentType != null)
            {
                if (TryGetSymbolPartialMatch(symbolFullNameBuilder, currentType, out firstMatch))
                {
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
                    return true;
                }
            }

            // ...No match
            Debug.Assert(firstMatch.Equals(NoWildcardMatch));
            _wildcardMatchResult.AddOrUpdate(symbol, NoWildcardMatch, (s, kvp) => NoWildcardMatch);
            return false;

            // Local functions.
            bool TryGetSymbolPartialMatch(StringBuilder builder, ISymbol symbol, out KeyValuePair<string, TValue> firstMatch)
            {
                if (builder.Length > 0)
                {
                    builder.Insert(0, ".");
                }

                builder.Insert(0, symbol.Name);

                return TryGetFirstWildcardMatch(AllKinds, symbol.Name, out firstMatch);
            }

            bool TryGetFirstWildcardMatch(SymbolKind kind, string symbolName, out KeyValuePair<string, TValue> firstMatch)
            {
                if (!_wildcardNamesBySymbolKind.ContainsKey(kind))
                {
                    firstMatch = NoWildcardMatch;
                    return false;
                }

                firstMatch = _wildcardNamesBySymbolKind[kind].FirstOrDefault(x => symbolName.StartsWith(x.Key, StringComparison.Ordinal));

                if (string.IsNullOrWhiteSpace(firstMatch.Key))
                {
                    return false;
                }

                var match = firstMatch;
                _wildcardMatchResult.AddOrUpdate(symbol, firstMatch, (s, kvp) => match);
                return true;
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
            public NameParts(string symbolName, TValue associatedValue = default)
            {
                SymbolName = symbolName.Trim();
                AssociatedValue = associatedValue;
            }

            public string SymbolName { get; }
            public TValue AssociatedValue { get; }
        }
    }
}