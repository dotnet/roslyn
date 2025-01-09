// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

internal static partial class EditorConfigNamingStyleParser
{
    public static NamingStylePreferences ParseDictionary(AnalyzerConfigOptions allRawConventions)
    {
        var trimmedDictionary = TrimDictionary(allRawConventions);

        var _ = ArrayBuilder<(NamingRule rule, int priority, string title)>.GetInstance(out var namingRules);

        foreach (var namingRuleTitle in GetRuleTitles(trimmedDictionary))
        {
            if (TryGetSymbolSpecification(namingRuleTitle, trimmedDictionary, out var symbolSpec) &&
                TryGetNamingStyle(namingRuleTitle, trimmedDictionary, out var namingStyle) &&
                TryGetRule(namingRuleTitle, symbolSpec, namingStyle, trimmedDictionary, out var rule, out var priority))
            {
                namingRules.Add((rule.Value, priority, namingRuleTitle));
            }
        }

        // Deterministically order the naming style rules.
        // 
        // Rules of the same priority are ordered according to the symbols matched by the rule. The rules
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
        var orderedRules = namingRules
            .OrderBy(item => item.priority)
            .ThenBy(item => item.rule, NamingRuleModifierListComparer.Instance)
            .ThenBy(item => item.rule, NamingRuleAccessibilityListComparer.Instance)
            .ThenBy(item => item.rule, NamingRuleSymbolListComparer.Instance)
            .ThenBy(item => item.title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.title, StringComparer.Ordinal);

        using var _1 = ArrayBuilder<SymbolSpecification>.GetInstance(out var symbolSpecifications);
        using var _2 = ArrayBuilder<NamingStyle>.GetInstance(out var namingStyles);
        using var _3 = ArrayBuilder<SerializableNamingRule>.GetInstance(out var serializableRules);

        foreach (var (rule, _, _) in orderedRules)
        {
            symbolSpecifications.Add(rule.SymbolSpecification);
            namingStyles.Add(rule.NamingStyle);
            serializableRules.Add(new SerializableNamingRule
            {
                SymbolSpecificationID = rule.SymbolSpecification.ID,
                NamingStyleID = rule.NamingStyle.ID,
                EnforcementLevel = rule.EnforcementLevel,
            });
        }

        return new NamingStylePreferences(
            symbolSpecifications.ToImmutable(),
            namingStyles.ToImmutable(),
            serializableRules.ToImmutable());
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

    public static IEnumerable<string> GetRuleTitles(IReadOnlyDictionary<string, string> allRawConventions)
        => (from kvp in allRawConventions
            where kvp.Key.Trim().StartsWith("dotnet_naming_rule.", StringComparison.Ordinal)
            let nameSplit = kvp.Key.Split('.')
            where nameSplit.Length == 3
            select nameSplit[1])
            .Distinct();

    private static Property<TValue> GetProperty<TValue>(
        IReadOnlyDictionary<string, string> entries,
        string group,
        string ruleName,
        string componentIdentifier,
        Func<string, TValue> parser,
        TValue defaultValue)
    {
        var key = $"{group}.{ruleName}.{componentIdentifier}";
        var value = entries.TryGetValue(key, out var str) ? parser(str) : defaultValue;
        return new(key, value);
    }

    private readonly struct Property<TValue>(string key, TValue value)
    {
        public string Key { get; } = key;
        public TValue Value { get; } = value;

        public TextSpan? GetSpan(IReadOnlyDictionary<string, TextLine> lines)
            => lines.TryGetValue(Key, out var line) ? line.Span : null;
    }

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
