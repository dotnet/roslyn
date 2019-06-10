// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Naming
{
    internal struct IdentifierNameParts
    {
        public string BaseName;
        public ImmutableArray<string> BaseNameParts;

        public IdentifierNameParts(string baseName, ImmutableArray<string> baseNameParts)
        {
            BaseName = baseName;
            BaseNameParts = baseNameParts;
        }

        public static IdentifierNameParts GetIdentifierBaseName(ISymbol symbol, ImmutableArray<NamingRule> rules)
        {
            var baseName = symbol.Name;

            foreach (var rule in rules)
            {
                if (rule.SymbolSpecification.AppliesTo(symbol))
                {
                    // remove specified prefix
                    var prefix = rule.NamingStyle.Prefix;
                    baseName = baseName.StartsWith(prefix)
                        ? baseName.Substring(prefix.Length)
                        : baseName;

                    // remove specified suffix
                    var suffix = rule.NamingStyle.Suffix;
                    baseName = symbol.Name.EndsWith(suffix)
                        ? baseName.Substring(0, baseName.Length - suffix.Length)
                        : baseName;

                    break;
                }
            }

            // remove any common prefixes
            baseName = NamingStyle.StripCommonPrefixes(baseName, out var _);

            var parts = StringBreaker.GetWordParts(baseName);
            var result = CreateWords(parts, baseName);

            return new IdentifierNameParts(baseName, result);
        }

        private static ImmutableArray<string> CreateWords(ArrayBuilder<TextSpan> parts, string name)
        {
            var result = ArrayBuilder<string>.GetInstance(parts.Count);
            foreach (var part in parts)
            {
                result.Add(name.Substring(part.Start, part.Length));
            }

            return result.ToImmutableAndFree();
        }
    }
}
