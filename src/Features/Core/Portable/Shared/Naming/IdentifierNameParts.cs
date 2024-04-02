// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Naming;

internal readonly struct IdentifierNameParts(string baseName, ImmutableArray<string> baseNameParts)
{
    public readonly string BaseName = baseName;
    public readonly ImmutableArray<string> BaseNameParts = baseNameParts;

    public static IdentifierNameParts CreateIdentifierNameParts(ISymbol symbol, ImmutableArray<NamingRule> rules)
    {
        var baseName = RemovePrefixesAndSuffixes(symbol, rules, symbol.Name);

        using var parts = TemporaryArray<TextSpan>.Empty;
        StringBreaker.AddWordParts(baseName, ref parts.AsRef());
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
                    ? newBaseName[prefix.Length..]
                    : newBaseName;

                // remove specified suffix
                var suffix = rule.NamingStyle.Suffix;
                newBaseName = newBaseName.EndsWith(suffix)
                    ? newBaseName[..^suffix.Length]
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

    private static ImmutableArray<string> CreateWords(in TemporaryArray<TextSpan> parts, string name)
    {
        using var words = TemporaryArray<string>.Empty;
        foreach (var part in parts)
            words.Add(name.Substring(part.Start, part.Length));

        return words.ToImmutableAndClear();
    }
}
