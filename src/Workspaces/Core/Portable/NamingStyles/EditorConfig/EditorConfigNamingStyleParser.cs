// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal static partial class EditorConfigNamingStyleParser
    {
        /// <remarks>
        /// The dictionary we get from the VS editorconfig API uses the same dictionary object if there are no changes, so we can cache based on dictionary
        /// </remarks>
        // TODO: revisit this cache. The assumption that the dictionary doesn't change in the exact instance is terribly fragile,
        // and with the new .editorconfig support won't hold as well as we'd like: a single tree will have a stable instance but
        // that won't necessarily be the same across files and projects.
        private static readonly ConditionalWeakTable<IReadOnlyDictionary<string, string>, NamingStylePreferences> _cache = new ConditionalWeakTable<IReadOnlyDictionary<string, string>, NamingStylePreferences>();
        private static readonly object _cacheLock = new object();

        public static NamingStylePreferences GetNamingStylesFromDictionary(IReadOnlyDictionary<string, string> rawOptions)
        {
            if (_cache.TryGetValue(rawOptions, out var value))
            {
                return value;
            }

            lock (_cacheLock)
            {
                if (!_cache.TryGetValue(rawOptions, out value))
                {
                    value = ParseDictionary(rawOptions);
                    _cache.Add(rawOptions, value);
                }

                return value;
            }
        }

        public static NamingStylePreferences ParseDictionary(IReadOnlyDictionary<string, string> allRawConventions)
        {
            var symbolSpecifications = ArrayBuilder<SymbolSpecification>.GetInstance();
            var namingStyles = ArrayBuilder<NamingStyle>.GetInstance();
            var namingRules = ArrayBuilder<SerializableNamingRule>.GetInstance();

            var trimmedDictionary = TrimDictionary(allRawConventions);

            foreach (var namingRuleTitle in GetRuleTitles(trimmedDictionary))
            {
                if (TryGetSymbolSpec(namingRuleTitle, trimmedDictionary, out var symbolSpec) &&
                    TryGetNamingStyleData(namingRuleTitle, trimmedDictionary, out var namingStyle) &&
                    TryGetSerializableNamingRule(namingRuleTitle, symbolSpec, namingStyle, trimmedDictionary, out var serializableNamingRule))
                {
                    symbolSpecifications.Add(symbolSpec);
                    namingStyles.Add(namingStyle);
                    namingRules.Add(serializableNamingRule);
                }
            }

            var preferences = new NamingStylePreferences(
                symbolSpecifications.ToImmutableAndFree(),
                namingStyles.ToImmutableAndFree(),
                namingRules.ToImmutableAndFree());

            var orderedRules = preferences.Rules.NamingRules
                .OrderBy(rule => rule, NamingRuleAccessibilityListComparer.Instance)
                .ThenBy(rule => rule, NamingRuleModifierListComparer.Instance)
                .ThenBy(rule => rule, NamingRuleSymbolListComparer.Instance)
                .ThenBy(rule => rule.NamingStyle.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(rule => rule.NamingStyle.Name, StringComparer.Ordinal);

            return new NamingStylePreferences(
                preferences.SymbolSpecifications,
                preferences.NamingStyles,
                orderedRules.SelectAsArray(
                    rule => new SerializableNamingRule
                    {
                        SymbolSpecificationID = rule.SymbolSpecification.ID,
                        NamingStyleID = rule.NamingStyle.ID,
                        EnforcementLevel = rule.EnforcementLevel,
                    }));
        }

        private static Dictionary<string, string> TrimDictionary(IReadOnlyDictionary<string, string> allRawConventions)
        {
            var trimmedDictionary = new Dictionary<string, string>(allRawConventions.Count);
            foreach (var item in allRawConventions)
            {
                var key = item.Key.Trim();
                var value = item.Value;
                trimmedDictionary[key] = value;
            }

            return trimmedDictionary;
        }

        private static IEnumerable<string> GetRuleTitles(IReadOnlyDictionary<string, string> allRawConventions)
            => (from kvp in allRawConventions
                where kvp.Key.Trim().StartsWith("dotnet_naming_rule.", StringComparison.Ordinal)
                let nameSplit = kvp.Key.Split('.')
                where nameSplit.Length == 3
                select nameSplit[1])
                .Distinct();

        private abstract class NamingRuleSubsetComparer : IComparer<NamingRule>
        {
            protected NamingRuleSubsetComparer()
            {
            }

            public int Compare(NamingRule x, NamingRule y)
            {
                var firstIsSubset = FirstIsSubset(in x, in y);
                var secondIsSubset = FirstIsSubset(in y, in x);
                if (firstIsSubset)
                {
                    return secondIsSubset ? 0 : -1;
                }
                else
                {
                    return secondIsSubset ? 1 : 0;
                }
            }

            protected abstract bool FirstIsSubset(in NamingRule x, in NamingRule y);
        }

        private sealed class NamingRuleAccessibilityListComparer : NamingRuleSubsetComparer
        {
            internal static readonly NamingRuleAccessibilityListComparer Instance = new NamingRuleAccessibilityListComparer();

            private NamingRuleAccessibilityListComparer()
            {
            }

            protected override bool FirstIsSubset(in NamingRule x, in NamingRule y)
            {
                foreach (var accessibility in x.SymbolSpecification.ApplicableAccessibilityList)
                {
                    if (!y.SymbolSpecification.ApplicableAccessibilityList.Contains(accessibility))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private sealed class NamingRuleModifierListComparer : NamingRuleSubsetComparer
        {
            internal static readonly NamingRuleModifierListComparer Instance = new NamingRuleModifierListComparer();

            private NamingRuleModifierListComparer()
            {
            }

            protected override bool FirstIsSubset(in NamingRule x, in NamingRule y)
            {
                // Since modifiers are "match all", a subset of symbols is matched by a superset of modifiers
                foreach (var modifier in y.SymbolSpecification.RequiredModifierList)
                {
                    if (!x.SymbolSpecification.RequiredModifierList.Contains(modifier))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private sealed class NamingRuleSymbolListComparer : NamingRuleSubsetComparer
        {
            internal static readonly NamingRuleSymbolListComparer Instance = new NamingRuleSymbolListComparer();

            private NamingRuleSymbolListComparer()
            {
            }

            protected override bool FirstIsSubset(in NamingRule x, in NamingRule y)
            {
                foreach (var symbolKind in x.SymbolSpecification.ApplicableSymbolKindList)
                {
                    if (!y.SymbolSpecification.ApplicableSymbolKindList.Contains(symbolKind))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
