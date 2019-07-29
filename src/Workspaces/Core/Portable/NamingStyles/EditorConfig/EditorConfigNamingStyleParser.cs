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
            var ruleNames = new Dictionary<(Guid symbolSpecificationID, Guid namingStyleID, ReportDiagnostic enforcementLevel), string>();

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

                    var ruleKey = (serializableNamingRule.SymbolSpecificationID, serializableNamingRule.NamingStyleID, serializableNamingRule.EnforcementLevel);
                    if (ruleNames.TryGetValue(ruleKey, out var existingName))
                    {
                        // For duplicated rules, only preserve the one with a name that would sort first
                        var ordinalIgnoreCaseOrdering = StringComparer.OrdinalIgnoreCase.Compare(namingRuleTitle, existingName);
                        if (ordinalIgnoreCaseOrdering > 0)
                        {
                            continue;
                        }
                        else if (ordinalIgnoreCaseOrdering == 0)
                        {
                            var ordinalOrdering = StringComparer.Ordinal.Compare(namingRuleTitle, existingName);
                            if (ordinalOrdering > 0)
                            {
                                continue;
                            }
                        }
                    }

                    ruleNames[ruleKey] = namingRuleTitle;
                }
            }

            var preferences = new NamingStylePreferences(
                symbolSpecifications.ToImmutableAndFree(),
                namingStyles.ToImmutableAndFree(),
                namingRules.ToImmutableAndFree());

            // Deterministically order the naming style rules according to the symbols matched by the rule. The rules
            // are applied in order; later rules are only relevant if earlier rules fail to specify an order.
            //
            // 1. If the modifiers required by rule 'x' are a strict superset of the modifiers required by rule 'y',
            //    then rule 'x' is evaluated before rule 'y'.
            // 2. If the accessibilities allowed by rule 'x' are a strict subset of the accessibilities allowed by rule
            //    'y', then rule 'x' is evaluated before rule 'y'.
            // 3. If the set of symbols matched by rule 'x' are a strict subset of the symbols matched by rule 'y', then
            //    rule 'x' is evaluated before rule 'y'.
            //
            // If none of the above produces an order between two rules 'x' and 'y', then the rules are ordered
            // according to their name, first by OrdinalIgnoreCase and finally by Ordinal.
            //
            // Historical note: rules used to be ordered by their position in the .editorconfig file. However, this
            // relied on an implementation detail of the .editorconfig parser which is not preserved by all
            // implementations. In a review of .editorconfig files in the wild, the rules applied in this section were
            // the closest deterministic match for the files without having any reliance on order. For any pair of rules
            // which a user has trouble ordering, the intersection of the two rules can be broken out into a new rule
            // will always match earlier than the broader rules it was derived from.
            var orderedRules = preferences.Rules.NamingRules
                .OrderBy(rule => rule, NamingRuleModifierListComparer.Instance)
                .ThenBy(rule => rule, NamingRuleAccessibilityListComparer.Instance)
                .ThenBy(rule => rule, NamingRuleSymbolListComparer.Instance)
                .ThenBy(rule => ruleNames[(rule.SymbolSpecification.ID, rule.NamingStyle.ID, rule.EnforcementLevel)], StringComparer.OrdinalIgnoreCase)
                .ThenBy(rule => ruleNames[(rule.SymbolSpecification.ID, rule.NamingStyle.ID, rule.EnforcementLevel)], StringComparer.Ordinal);

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

            /// <summary>
            /// Determines if <paramref name="x"/> matches a subset of the symbols matched by <paramref name="y"/>. The
            /// implementation determines which properties of <see cref="NamingRule"/> are considered for this
            /// evaluation. The subset relation does not necessarily indicate a proper subset.
            /// </summary>
            /// <param name="x">The first naming rule.</param>
            /// <param name="y">The second naming rule.</param>
            /// <returns><see langword="true"/> if <paramref name="x"/> matches a subset of the symbols matched by
            /// <paramref name="y"/> on some implementation-defined properties; otherwise, <see langword="false"/>.</returns>
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
                    if (modifier.ModifierKindWrapper == SymbolSpecification.ModifierKindEnum.IsStatic
                        || modifier.ModifierKindWrapper == SymbolSpecification.ModifierKindEnum.IsReadOnly)
                    {
                        if (x.SymbolSpecification.RequiredModifierList.Any(x => x.ModifierKindWrapper == SymbolSpecification.ModifierKindEnum.IsConst))
                        {
                            // 'const' implies both 'readonly' and 'static'
                            continue;
                        }
                    }

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
