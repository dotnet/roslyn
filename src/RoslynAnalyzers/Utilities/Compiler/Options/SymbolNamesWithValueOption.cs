// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Analyzer.Utilities
{
#if !TEST_UTILITIES
    public sealed class SymbolNamesWithValueOption<TValue>
#else
    internal sealed class SymbolNamesWithValueOption<TValue>
#endif
     : IEquatable<SymbolNamesWithValueOption<TValue>?>
    {
        internal const SymbolKind AllKinds = SymbolKind.ErrorType;
        internal const char WildcardChar = '*';

        public static readonly SymbolNamesWithValueOption<TValue> Empty = new();

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
        private readonly ConcurrentDictionary<ISymbol, KeyValuePair<string?, TValue?>> _wildcardMatchResult = new();

        private readonly ConcurrentDictionary<ISymbol, string> _symbolToDeclarationId = new();

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
            Func<string, NameParts> getSymbolNamePartsFunc)
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
                var parts = getSymbolNamePartsFunc(symbolName);

                var numberOfWildcards = parts.SymbolName.Count(c => c == WildcardChar);

                // More than one wildcard, bail-out.
                if (numberOfWildcards > 1)
                {
                    continue;
                }

                // Wildcard is not last or is the only char, bail-out
                if (numberOfWildcards == 1 &&
                    (parts.SymbolName[^1] != WildcardChar ||
                    parts.SymbolName.Length == 1))
                {
                    continue;
                }

                if (numberOfWildcards == 1)
                {
                    ProcessWildcardName(parts, wildcardNamesBuilder);
                }
#pragma warning disable CA1847 // Use 'string.Contains(char)' instead of 'string.Contains(string)' when searching for a single character
                else if (parts.SymbolName.Equals(".ctor", StringComparison.Ordinal) ||
                    parts.SymbolName.Equals(".cctor", StringComparison.Ordinal) ||
                    !parts.SymbolName.Contains(".", StringComparison.Ordinal) && !parts.SymbolName.Contains(":", StringComparison.Ordinal))
                {
                    ProcessName(parts, namesBuilder);
                }
                else
                {
                    ProcessSymbolName(parts, compilation, optionalPrefix, symbolsBuilder);
                }
#pragma warning restore CA1847 // Use 'string.Contains(char)' instead of 'string.Contains(string)' when searching for a single character
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
                Debug.Assert(parts.SymbolName[^1] == WildcardChar);
                Debug.Assert(parts.SymbolName.Length >= 2);

                if (parts.SymbolName[1] != ':')
                {
                    if (!wildcardNamesBuilder.TryGetValue(AllKinds, out var associatedValues))
                    {
                        associatedValues = PooledDictionary<string, TValue>.GetInstance();
                        wildcardNamesBuilder.Add(AllKinds, associatedValues);
                    }

                    associatedValues.Add(parts.SymbolName[0..^1], parts.AssociatedValue);
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
                    if (!wildcardNamesBuilder.TryGetValue(symbolKind.Value, out var associatedValues))
                    {
                        associatedValues = PooledDictionary<string, TValue>.GetInstance();
                        wildcardNamesBuilder.Add(symbolKind.Value, associatedValues);
                    }

                    associatedValues.Add(parts.SymbolName[2..^1], parts.AssociatedValue);
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

                // Documentation comment ID for constructors uses '#ctor', but '#' is a comment start token for editorconfig.
                // We instead search for a '..ctor' in editorconfig and replace it with a '.#ctor' here.
                // Similarly, handle static constructors ".cctor"
                nameWithPrefix = nameWithPrefix.Replace("..ctor", ".#ctor", StringComparison.Ordinal);
                nameWithPrefix = nameWithPrefix.Replace("..cctor", ".#cctor", StringComparison.Ordinal);

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
            => _symbols.ContainsKey(symbol) || _names.ContainsKey(symbol.Name) || TryGetFirstWildcardMatch(symbol, out _, out _);

        /// <summary>
        /// Gets the value associated with the specified symbol in the option specification.
        /// </summary>
        public bool TryGetValue(ISymbol symbol, [MaybeNullWhen(false)] out TValue value)
        {
            if (_symbols.TryGetValue(symbol, out value) || _names.TryGetValue(symbol.Name, out value))
            {
                return true;
            }

            if (TryGetFirstWildcardMatch(symbol, out _, out value))
            {
                return true;
            }

            value = default;
            return false;
        }

        public override bool Equals(object? obj) => Equals(obj as SymbolNamesWithValueOption<TValue>);

        public bool Equals(SymbolNamesWithValueOption<TValue>? other)
            => other != null && _names.IsEqualTo(other._names) && _symbols.IsEqualTo(other._symbols) && _wildcardNamesBySymbolKind.IsEqualTo(other._wildcardNamesBySymbolKind);

        public override int GetHashCode()
        {
            var hashCode = new RoslynHashCode();
            HashUtilities.Combine(_names, ref hashCode);
            HashUtilities.Combine(_symbols, ref hashCode);
            HashUtilities.Combine(_wildcardNamesBySymbolKind, ref hashCode);
            return hashCode.ToHashCode();
        }

        private bool TryGetFirstWildcardMatch(ISymbol symbol, [NotNullWhen(true)] out string? firstMatchName, [MaybeNullWhen(false)] out TValue firstMatchValue)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Event:
                case SymbolKind.Field:
                case SymbolKind.Method:
                case SymbolKind.NamedType:
                case SymbolKind.Namespace:
                case SymbolKind.Property:
                    break;

                case SymbolKind.Assembly:
                case SymbolKind.ErrorType:
                case SymbolKind.NetModule:
                    firstMatchName = null;
                    firstMatchValue = default;
                    return false;

                default:
                    throw new ArgumentException($"Unsupported symbol kind '{symbol.Kind}' for symbol '{symbol}'");
            }

            // No wildcard entry, let's bail-out
            if (_wildcardNamesBySymbolKind.IsEmpty)
            {
                firstMatchName = null;
                firstMatchValue = default;
                return false;
            }

            // The matching was already processed, use cached result
            if (_wildcardMatchResult.TryGetValue(symbol, out var firstMatch))
            {
                (firstMatchName, firstMatchValue) = firstMatch;
#pragma warning disable CS8762 // Parameter 'firstMatchValue' must have a non-null value when exiting with 'true'
                return firstMatchName is not null;
#pragma warning restore CS8762 // Parameter 'firstMatchValue' must have a non-null value when exiting with 'true'
            }

            var symbolDeclarationId = _symbolToDeclarationId.GetOrAdd(symbol, GetDeclarationId);

            // We start by trying to match with the most precise definition (prefix)...
            if (_wildcardNamesBySymbolKind.TryGetValue(symbol.Kind, out var names) &&
                names.FirstOrDefault(kvp => symbolDeclarationId.StartsWith(kvp.Key, StringComparison.Ordinal)) is var prefixedFirstMatchOrDefault &&
                !string.IsNullOrWhiteSpace(prefixedFirstMatchOrDefault.Key))
            {
                (firstMatchName, firstMatchValue) = prefixedFirstMatchOrDefault;
                _wildcardMatchResult.AddOrUpdate(symbol, prefixedFirstMatchOrDefault.AsNullable(), (s, match) => prefixedFirstMatchOrDefault.AsNullable());
                return true;
            }

            // If not found, then we try to match with the symbol full declaration ID...
            if (_wildcardNamesBySymbolKind.TryGetValue(AllKinds, out var value) &&
                value.FirstOrDefault(kvp => symbolDeclarationId.StartsWith(kvp.Key, StringComparison.Ordinal)) is var unprefixedFirstMatchOrDefault &&
                !string.IsNullOrWhiteSpace(unprefixedFirstMatchOrDefault.Key))
            {
                (firstMatchName, firstMatchValue) = unprefixedFirstMatchOrDefault;
                _wildcardMatchResult.AddOrUpdate(symbol, unprefixedFirstMatchOrDefault.AsNullable(), (s, match) => unprefixedFirstMatchOrDefault.AsNullable());
                return true;
            }

            // If not found, then we try to match with the symbol name...
            if (_wildcardNamesBySymbolKind.TryGetValue(AllKinds, out var allKindsValue) &&
                allKindsValue.FirstOrDefault(kvp => symbol.Name.StartsWith(kvp.Key, StringComparison.Ordinal)) is var partialFirstMatchOrDefault &&
                !string.IsNullOrWhiteSpace(partialFirstMatchOrDefault.Key))
            {
                (firstMatchName, firstMatchValue) = partialFirstMatchOrDefault;
                _wildcardMatchResult.AddOrUpdate(symbol, partialFirstMatchOrDefault.AsNullable(), (s, match) => partialFirstMatchOrDefault.AsNullable());
                return true;
            }

            // Nothing was found
            firstMatchName = null;
            firstMatchValue = default;
            _wildcardMatchResult.AddOrUpdate(symbol, new KeyValuePair<string?, TValue?>(null, default), (s, match) => new KeyValuePair<string?, TValue?>(null, default));
            return false;

            static string GetDeclarationId(ISymbol symbol)
            {
                var declarationIdWithoutPrefix = DocumentationCommentId.CreateDeclarationId(symbol)![2..];

                // Documentation comment ID for constructors uses '#ctor', but '#' is a comment start token for editorconfig.
                declarationIdWithoutPrefix = declarationIdWithoutPrefix
                    .Replace(".#ctor", "..ctor", StringComparison.Ordinal)
                    .Replace(".#cctor", "..cctor", StringComparison.Ordinal);

                return declarationIdWithoutPrefix;
            }
        }

        internal TestAccessor GetTestAccessor()
        {
            return new TestAccessor(this);
        }

        [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Does not apply to test accessors")]
        internal readonly struct TestAccessor
        {
            private readonly SymbolNamesWithValueOption<TValue> _symbolNamesWithValueOption;

            internal TestAccessor(SymbolNamesWithValueOption<TValue> symbolNamesWithValueOption)
            {
                _symbolNamesWithValueOption = symbolNamesWithValueOption;
            }

            internal ref readonly ImmutableDictionary<string, TValue> Names => ref _symbolNamesWithValueOption._names;

            internal ref readonly ImmutableDictionary<ISymbol, TValue> Symbols => ref _symbolNamesWithValueOption._symbols;

            internal ref readonly ImmutableDictionary<SymbolKind, ImmutableDictionary<string, TValue>> WildcardNamesBySymbolKind => ref _symbolNamesWithValueOption._wildcardNamesBySymbolKind;

            internal ref readonly ConcurrentDictionary<ISymbol, KeyValuePair<string?, TValue?>> WildcardMatchResult => ref _symbolNamesWithValueOption._wildcardMatchResult;

            internal ref readonly ConcurrentDictionary<ISymbol, string> SymbolToDeclarationId => ref _symbolNamesWithValueOption._symbolToDeclarationId;
        }

        /// <summary>
        /// Represents the two parts of a symbol name option when the symbol name is tighted to some specific value.
        /// This allows to link a value to a symbol while following the symbol's documentation ID format.
        /// </summary>
        /// <example>
        /// On the rule CA1710, we allow user specific suffix to be registered for symbol names using the following format:
        /// MyClass->Suffix or T:MyNamespace.MyClass->Suffix or N:MyNamespace->Suffix.
        /// </example>
#pragma warning disable CA1034 // Nested types should not be visible
        public sealed class NameParts
#pragma warning restore CA1034 // Nested types should not be visible
        {
            public NameParts(string symbolName, TValue associatedValue)
            {
                SymbolName = symbolName.Trim();
                AssociatedValue = associatedValue;
            }

            public string SymbolName { get; }
            public TValue AssociatedValue { get; }
        }
    }
}
