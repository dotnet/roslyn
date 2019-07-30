// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Naming
{
    internal readonly struct IdentifierNameParts
    {
        public readonly string BaseName;
        public readonly ImmutableArray<string> BaseNameParts;

        public IdentifierNameParts(string baseName, ImmutableArray<string> baseNameParts)
        {
            BaseName = baseName;
            BaseNameParts = baseNameParts;
        }

        public static IdentifierNameParts CreateIdentifierNameParts(ISymbol symbol, ImmutableArray<NamingRule> rules)
        {
            var baseName = RemovePrefixesAndSuffixes(symbol, rules, symbol.Name);

            var parts = StringBreaker.GetWordParts(baseName);
            var words = CreateWords(parts, baseName);

            return new IdentifierNameParts(baseName, words);
        }

        private static string RemovePrefixesAndSuffixes(ISymbol symbol, ImmutableArray<NamingRule> rules, string baseName)
        {
            var newBaseName = baseName;

            foreach (var rule in rules)
            {
                if (rule.SymbolSpecification.AppliesTo(symbol))
                {
                    // remove specified prefix
                    var prefix = rule.NamingStyle.Prefix;
                    newBaseName = newBaseName.StartsWith(prefix)
                        ? newBaseName.Substring(prefix.Length)
                        : newBaseName;

                    // remove specified suffix
                    var suffix = rule.NamingStyle.Suffix;
                    newBaseName = newBaseName.EndsWith(suffix)
                        ? newBaseName.Substring(0, newBaseName.Length - suffix.Length)
                        : newBaseName;

                    break;
                }
            }

            // remove any common prefixes
            newBaseName = NamingStyle.StripCommonPrefixes(newBaseName, out var _);

            // If no changes were made to the basename passed in, we're done
            if (newBaseName == baseName)
            {
                return baseName;
            }

            // otherwise, see if any other prefixes exist
            return RemovePrefixesAndSuffixes(symbol, rules, newBaseName);
        }

        private static ImmutableArray<string> CreateWords(ArrayBuilder<TextSpan> parts, string name)
        {
            using var result = ArrayBuilder<string>.GetInstance(parts.Count);
            foreach (var part in parts)
            {
                result.Add(name.Substring(part.Start, part.Length));
            }

            return result.ToImmutable();
        }
    }
}
