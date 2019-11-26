// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Extensions
{
    internal static class UriExtensions
    {
        private static readonly ImmutableHashSet<string> s_uriWords = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "uri", "urn", "url");

        public static bool ParameterNamesContainUriWordSubstring(this IEnumerable<IParameterSymbol> parameters, CancellationToken cancellationToken)
        {
            foreach (IParameterSymbol parameter in parameters)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (SymbolNameContainsUriWordSubstring(parameter, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool SymbolNameContainsUriWordSubstring(this ISymbol symbol, CancellationToken cancellationToken)
        {
            foreach (string word in s_uriWords)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (symbol.Name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<IParameterSymbol> GetParametersThatContainUriWords(this IEnumerable<IParameterSymbol> parameters, CancellationToken cancellationToken)
        {
            foreach (IParameterSymbol parameter in parameters)
            {
                if (SymbolNameContainsUriWords(parameter, cancellationToken))
                {
                    yield return parameter;
                }
            }
        }

        public static bool SymbolNameContainsUriWords(this ISymbol symbol, CancellationToken cancellationToken)
        {
            if (symbol.Name == null || !symbol.SymbolNameContainsUriWordSubstring(cancellationToken))
            {
                // quick check failed
                return false;
            }

            string? word;
            var parser = new WordParser(symbol.Name, WordParserOptions.SplitCompoundWords);
            while ((word = parser.NextWord()) != null)
            {
                if (s_uriWords.Contains(word))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
