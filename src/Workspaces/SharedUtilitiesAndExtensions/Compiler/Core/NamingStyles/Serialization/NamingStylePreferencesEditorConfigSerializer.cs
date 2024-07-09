// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.NamingStyles;

namespace Microsoft.CodeAnalysis.Options;

internal static partial class NamingStylePreferencesEditorConfigSerializer
{
    public static void AppendToEditorConfig(this NamingStylePreferences namingStylePreferences, string language, StringBuilder editorconfig)
    {
        AppendNamingStylePreferencesToEditorConfig(
            namingStylePreferences.SymbolSpecifications,
            namingStylePreferences.NamingStyles,
            namingStylePreferences.NamingRules,
            language,
            editorconfig);
    }

    public static void AppendNamingStylePreferencesToEditorConfig(
        ImmutableArray<SymbolSpecification> symbolSpecifications,
        ImmutableArray<NamingStyle> namingStyles,
        ImmutableArray<SerializableNamingRule> serializableNamingRules,
        string language,
        StringBuilder builder)
    {
        WriteNamingStylePreferencesToEditorConfig(
            symbolSpecifications,
            namingStyles,
            serializableNamingRules,
            language,
            entryWriter: (name, value) => builder.AppendLine($"{name} = {value}"),
            triviaWriter: trivia => builder.AppendLine(trivia));
    }

    public static void WriteNamingStylePreferencesToEditorConfig(
        ImmutableArray<SymbolSpecification> symbolSpecifications,
        ImmutableArray<NamingStyle> namingStyles,
        ImmutableArray<SerializableNamingRule> serializableNamingRules,
        string language,
        Action<string, string> entryWriter,
        Action<string>? triviaWriter)
    {
        triviaWriter?.Invoke($"#### {CompilerExtensionsResources.Naming_styles} ####");

        var serializedNameMap = AssignNamesToNamingStyleElements(symbolSpecifications, namingStyles);
        var ruleNameMap = AssignNamesToNamingStyleRules(serializableNamingRules, serializedNameMap);
        var referencedElements = new HashSet<Guid>();

        triviaWriter?.Invoke("");
        triviaWriter?.Invoke($"# {CompilerExtensionsResources.Naming_rules}");

        foreach (var namingRule in serializableNamingRules)
        {
            referencedElements.Add(namingRule.SymbolSpecificationID);
            referencedElements.Add(namingRule.NamingStyleID);

            triviaWriter?.Invoke("");
            entryWriter($"dotnet_naming_rule.{ruleNameMap[namingRule]}.severity", namingRule.EnforcementLevel.ToNotificationOption(defaultSeverity: DiagnosticSeverity.Hidden).ToEditorConfigString());
            entryWriter($"dotnet_naming_rule.{ruleNameMap[namingRule]}.symbols", serializedNameMap[namingRule.SymbolSpecificationID]);
            entryWriter($"dotnet_naming_rule.{ruleNameMap[namingRule]}.style", serializedNameMap[namingRule.NamingStyleID]);
        }

        triviaWriter?.Invoke("");
        triviaWriter?.Invoke($"# {CompilerExtensionsResources.Symbol_specifications}");

        foreach (var symbolSpecification in symbolSpecifications)
        {
            if (!referencedElements.Contains(symbolSpecification.ID))
            {
                continue;
            }

            triviaWriter?.Invoke("");
            entryWriter($"dotnet_naming_symbols.{serializedNameMap[symbolSpecification.ID]}.applicable_kinds", symbolSpecification.ApplicableSymbolKindList.ToEditorConfigString());
            entryWriter($"dotnet_naming_symbols.{serializedNameMap[symbolSpecification.ID]}.applicable_accessibilities", symbolSpecification.ApplicableAccessibilityList.ToEditorConfigString(language));
            entryWriter($"dotnet_naming_symbols.{serializedNameMap[symbolSpecification.ID]}.required_modifiers", symbolSpecification.RequiredModifierList.ToEditorConfigString(language));
        }

        triviaWriter?.Invoke("");
        triviaWriter?.Invoke($"# {CompilerExtensionsResources.Naming_styles}");

        foreach (var namingStyle in namingStyles)
        {
            if (!referencedElements.Contains(namingStyle.ID))
            {
                continue;
            }

            triviaWriter?.Invoke("");
            entryWriter($"dotnet_naming_style.{serializedNameMap[namingStyle.ID]}.required_prefix", namingStyle.Prefix);
            entryWriter($"dotnet_naming_style.{serializedNameMap[namingStyle.ID]}.required_suffix", namingStyle.Suffix);
            entryWriter($"dotnet_naming_style.{serializedNameMap[namingStyle.ID]}.word_separator", namingStyle.WordSeparator);
            entryWriter($"dotnet_naming_style.{serializedNameMap[namingStyle.ID]}.capitalization", namingStyle.CapitalizationScheme.ToEditorConfigString());
        }
    }

    private static ImmutableDictionary<Guid, string> AssignNamesToNamingStyleElements(
        ImmutableArray<SymbolSpecification> symbolSpecifications,
        ImmutableArray<NamingStyle> namingStyles)
    {
        var symbolSpecificationNames = new HashSet<string>();
        var builder = ImmutableDictionary.CreateBuilder<Guid, string>();
        foreach (var symbolSpecification in symbolSpecifications)
        {
            var name = ToSnakeCaseName(symbolSpecification.Name);
            if (!symbolSpecificationNames.Add(name))
            {
                name = symbolSpecification.ID.ToString("n");
            }

            builder.Add(symbolSpecification.ID, name);
        }

        var namingStyleNames = new HashSet<string>();
        foreach (var namingStyle in namingStyles)
        {
            var name = ToSnakeCaseName(namingStyle.Name);
            if (!namingStyleNames.Add(name))
            {
                name = namingStyle.ID.ToString("n");
            }

            if (!builder.TryGetKey(namingStyle.ID, out _))
            {
                builder.Add(namingStyle.ID, name);
            }
        }

        return builder.ToImmutable();

        static string ToSnakeCaseName(string name)
        {
            return new string(name
                .Select(ch =>
                {
                    if (char.IsLetterOrDigit(ch))
                    {
                        return char.ToLowerInvariant(ch);
                    }
                    else
                    {
                        return '_';
                    }
                })
                .ToArray());
        }
    }

    private static ImmutableDictionary<SerializableNamingRule, string> AssignNamesToNamingStyleRules(ImmutableArray<SerializableNamingRule> namingRules, ImmutableDictionary<Guid, string> serializedNameMap)
    {
        var builder = ImmutableDictionary.CreateBuilder<SerializableNamingRule, string>();
        foreach (var rule in namingRules)
        {
            builder.Add(rule, $"{serializedNameMap[rule.SymbolSpecificationID]}_should_be_{serializedNameMap[rule.NamingStyleID]}");
        }

        return builder.ToImmutable();
    }
}
