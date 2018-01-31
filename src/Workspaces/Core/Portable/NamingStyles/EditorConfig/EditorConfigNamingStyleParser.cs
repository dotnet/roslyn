// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal static partial class EditorConfigNamingStyleParser
    {
        /// <remarks>
        /// The dictionary we get from the VS editorconfig API uses the same dictionary object if there are no changes, so we can cache based on dictionary
        /// </remarks>
        private static readonly ConditionalWeakTable<IReadOnlyDictionary<string, object>, NamingStylePreferences> _cache = new ConditionalWeakTable<IReadOnlyDictionary<string, object>, NamingStylePreferences>();
        private static readonly object _cacheLock = new object();

        public static NamingStylePreferences GetNamingStylesFromDictionary(IReadOnlyDictionary<string, object> allRawConventions)
        {
            if (_cache.TryGetValue(allRawConventions, out var value))
            {
                return value;
            }

            lock (_cacheLock)
            {
                if (!_cache.TryGetValue(allRawConventions, out value))
                {
                    value = ParseDictionary(allRawConventions);
                    _cache.Add(allRawConventions, value);
                }

                return value;
            }
        }

        public static NamingStylePreferences ParseDictionary(IReadOnlyDictionary<string, object> allRawConventions)
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

            return new NamingStylePreferences(
                symbolSpecifications.ToImmutableAndFree(),
                namingStyles.ToImmutableAndFree(),
                namingRules.ToImmutableAndFree());
        }

        private static Dictionary<string, object> TrimDictionary(IReadOnlyDictionary<string, object> allRawConventions)
        {
            var trimmedDictionary = new Dictionary<string, object>(allRawConventions.Count);
            foreach (var item in allRawConventions)
            {
                var key = item.Key.Trim();
                var value = item.Value;
                trimmedDictionary[key] = value;
            }

            return trimmedDictionary;
        }

        private static IEnumerable<string> GetRuleTitles(IReadOnlyDictionary<string, object> allRawConventions)
            => (from kvp in allRawConventions
                where kvp.Key.Trim().StartsWith("dotnet_naming_rule.", StringComparison.Ordinal)
                let nameSplit = kvp.Key.Split('.')
                where nameSplit.Length == 3
                select nameSplit[1])
                .Distinct();
    }
}
