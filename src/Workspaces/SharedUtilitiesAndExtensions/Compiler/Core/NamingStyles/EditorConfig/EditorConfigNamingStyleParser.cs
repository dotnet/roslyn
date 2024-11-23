// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

internal static partial class EditorConfigNamingStyleParser
{
    public static NamingStylePreferences ParseDictionary(AnalyzerConfigOptions allRawConventions)
    {
        var trimmedDictionary = TrimDictionary(allRawConventions);

        var symbolSpecifications = ArrayBuilder<SymbolSpecification>.GetInstance();
        var namingStyles = ArrayBuilder<NamingStyle>.GetInstance();
        var namingRules = ArrayBuilder<SerializableNamingRule>.GetInstance();
        var ruleNames = new Dictionary<(Guid symbolSpecificationID, Guid namingStyleID, ReportDiagnostic enforcementLevel), string>();

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

    internal static Dictionary<string, string> TrimDictionary(AnalyzerConfigOptions allRawConventions)
    {
        var trimmedDictionary = new Dictionary<string, string>(AnalyzerConfigOptions.KeyComparer);
        foreach (var key in allRawConventions.Keys)
        {
            trimmedDictionary[key.Trim()] = allRawConventions.TryGetValue(key, out var value) ? value : throw new InvalidOperationException();
        }

        return trimmedDictionary;
    }

    public static IEnumerable<string> GetRuleTitles<T>(IReadOnlyDictionary<string, T> allRawConventions)
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
        internal static readonly NamingRuleAccessibilityListComparer Instance = new();

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
        internal static readonly NamingRuleModifierListComparer Instance = new();

        private NamingRuleModifierListComparer()
        {
        }

        protected override bool FirstIsSubset(in NamingRule x, in NamingRule y)
        {
            // Since modifiers are "match all", a subset of symbols is matched by a superset of modifiers
            foreach (var modifier in y.SymbolSpecification.RequiredModifierList)
            {
                if (modifier.ModifierKindWrapper is SymbolSpecification.ModifierKindEnum.IsStatic
                    or SymbolSpecification.ModifierKindEnum.IsReadOnly)
                {
                    if (x.SymbolSpecification.RequiredModifierList.Any(static x => x.ModifierKindWrapper == SymbolSpecification.ModifierKindEnum.IsConst))
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
        internal static readonly NamingRuleSymbolListComparer Instance = new();

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
