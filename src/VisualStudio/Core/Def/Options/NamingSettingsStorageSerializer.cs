// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using static Microsoft.VisualStudio.LanguageServices.Options.NamingSettingsConstants;

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal static class NamingSettingsStorageSerializer
{
    private static readonly string[] s_listProperties = [Properties.SymbolKinds, Properties.Accessibilities, Properties.RequiredModifiers];
    private static readonly string[] s_specificationProperties = [Properties.Id, Properties.Name, Properties.DefaultName, Properties.SymbolKinds, Properties.Accessibilities, Properties.RequiredModifiers];
    private static readonly string[] s_styleProperties = [Properties.Id, Properties.Name, Properties.DefaultName, Properties.Prefix, Properties.Suffix, Properties.WordSeparator, Properties.Capitalization];
    private static readonly string[] s_ruleProperties = [Properties.SymbolSpecificationId, Properties.NamingStyleId, Properties.Severity];

    // Registered object-array defaults support scalar properties, not nested arrays. Tokens contain no commas.
    public static ImmutableArray<IReadOnlyDictionary<string, object>> SerializeSpecifications(ImmutableArray<IReadOnlyDictionary<string, object>> items)
        => Transform(SerializeDefinitions(items, isStyle: false), serialize: true);

    public static ImmutableArray<IReadOnlyDictionary<string, object>> DeserializeSpecifications(ImmutableArray<IReadOnlyDictionary<string, object>> items)
        => Transform(items, serialize: false);

    public static ImmutableArray<IReadOnlyDictionary<string, object>> SerializeStyles(ImmutableArray<IReadOnlyDictionary<string, object>> items)
        => SerializeDefinitions(items, isStyle: true).Select(item => OrderProperties(item, s_styleProperties)).ToImmutableArray();

    public static ImmutableArray<IReadOnlyDictionary<string, object>> SerializeRules(ImmutableArray<IReadOnlyDictionary<string, object>> items)
        => items.Select(item => OrderProperties(item, s_ruleProperties)).ToImmutableArray();

    private static ImmutableArray<IReadOnlyDictionary<string, object>> SerializeDefinitions(ImmutableArray<IReadOnlyDictionary<string, object>> items, bool isStyle)
    {
        var normalized = items.Select(item => NamingSettingsGraph.NormalizeDefinition(item, isStyle)).ToImmutableArray();
        var defaults = isStyle ? NamingSettingsGraph.Default.NamingStyles : NamingSettingsGraph.Default.SymbolSpecifications;
        // Only unchanged arrays use default-name markers, so the native writer removes them instead of freezing names.
        if (normalized.Length == defaults.Length && normalized.Zip(defaults, (item, original) =>
            item.Count == original.Count && original.All(pair => item.TryGetValue(pair.Key, out var value) && ValuesEqual(value, pair.Value))).All(equal => equal))
        {
            return normalized;
        }

        return normalized.Select(item => NamingSettingsGraph.MaterializeDefinition(item, isStyle)).ToImmutableArray();

        static bool ValuesEqual(object first, object second)
            => first is IEnumerable<string> firstTokens && second is IEnumerable<string> secondTokens
                ? firstTokens.SequenceEqual(secondTokens, StringComparer.Ordinal) : Equals(first, second);
    }

    private static IReadOnlyDictionary<string, object> OrderProperties(IReadOnlyDictionary<string, object> item, string[] propertyNames)
    {
        // The native default comparer reserializes JSON without sorting object properties.
        var ordered = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var name in propertyNames)
        {
            if (item.TryGetValue(name, out var value))
                ordered.Add(name, value);
        }
        foreach (var pair in item)
        {
            if (!ordered.ContainsKey(pair.Key))
                ordered.Add(pair.Key, pair.Value);
        }
        return ordered;
    }

    private static ImmutableArray<IReadOnlyDictionary<string, object>> Transform(ImmutableArray<IReadOnlyDictionary<string, object>> items, bool serialize)
        => items.Select(item =>
        {
            var properties = item.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            foreach (var name in s_listProperties)
            {
                if (!properties.TryGetValue(name, out var value))
                    throw new InvalidDataException($"Naming property '{name}' is required.");
                if (serialize)
                {
                    if (value is not IEnumerable<string> tokens)
                        throw new InvalidDataException($"Naming property '{name}' must be an array of strings.");
                    properties[name] = string.Join(",", tokens);
                }
                else if (value is string text)
                {
                    properties[name] = text.Length == 0 ? Array.Empty<string>() : text.Split(',').Select(token => token.Trim()).ToArray();
                }
                else if (value is IEnumerable<string> tokens)
                {
                    properties[name] = tokens.ToArray();
                }
                else
                {
                    throw new InvalidDataException($"Naming property '{name}' must be a token list.");
                }
            }
            return serialize ? OrderProperties(properties, s_specificationProperties) : properties;
        }).ToImmutableArray();
}
