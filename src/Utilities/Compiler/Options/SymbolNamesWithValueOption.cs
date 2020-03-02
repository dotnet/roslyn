// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    internal sealed class SymbolNamesWithValueOption<TValue> : IEquatable<SymbolNamesWithValueOption<TValue>?>
    {
        public static readonly SymbolNamesWithValueOption<TValue> Empty = new SymbolNamesWithValueOption<TValue>();

        private readonly ImmutableDictionary<string, TValue> _names;
        private readonly ImmutableDictionary<ISymbol, TValue> _symbols;

        private SymbolNamesWithValueOption(ImmutableDictionary<string, TValue> names, ImmutableDictionary<ISymbol, TValue> symbols)
        {
            Debug.Assert(!names.IsEmpty || !symbols.IsEmpty);

            _names = names;
            _symbols = symbols;
        }

        private SymbolNamesWithValueOption()
        {
            _names = ImmutableDictionary<string, TValue>.Empty;
            _symbols = ImmutableDictionary<ISymbol, TValue>.Empty;
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

            foreach (var symbolName in symbolNames)
            {
                var parts = getSymbolNamePartsFunc != null
                    ? getSymbolNamePartsFunc(symbolName)
                    : new NameParts(symbolName);

                if (parts.SymbolName.Equals(".ctor", StringComparison.Ordinal) ||
                    parts.SymbolName.Equals(".cctor", StringComparison.Ordinal) ||
                    !parts.SymbolName.Contains(".") && !parts.SymbolName.Contains(":"))
                {
                    if (!namesBuilder.ContainsKey(parts.SymbolName))
                    {
                        namesBuilder.Add(parts.SymbolName, parts.AssociatedValue);
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

            if (namesBuilder.Count == 0 && symbolsBuilder.Count == 0)
            {
                return Empty;
            }

            return new SymbolNamesWithValueOption<TValue>(namesBuilder.ToImmutableDictionaryAndFree(), symbolsBuilder.ToImmutableDictionaryAndFree());
        }

        public bool IsEmpty => ReferenceEquals(this, Empty);

        public bool Contains(ISymbol symbol)
            => _symbols.ContainsKey(symbol) || _names.ContainsKey(symbol.Name);

        /// <summary>
        /// Gets the value associated with the specified symbol in the option specification.
        /// </summary>
        public bool TryGetValue(ISymbol symbol, [NotNullWhen(true)] out TValue value) =>
            _symbols.TryGetValue(symbol, out value) || _names.TryGetValue(symbol.Name, out value);

        public override bool Equals(object obj) => Equals(obj as SymbolNamesWithValueOption<TValue>);

        public bool Equals(SymbolNamesWithValueOption<TValue>? other)
            => other != null && _names.IsEqualTo(other._names) && _symbols.IsEqualTo(other._symbols);

        public override int GetHashCode()
            => HashUtilities.Combine(HashUtilities.Combine(_names), HashUtilities.Combine(_symbols));

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