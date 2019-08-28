// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class EditorConfigFileGenerator
    {
        public static string Generate(
            ImmutableArray<(string feature, ImmutableArray<IOption> options)> groupedOptions,
            OptionSet optionSet,
            string language)
        {
            var editorconfig = new StringBuilder();

            editorconfig.AppendLine($"# {WorkspacesResources.Remove_the_line_below_if_you_want_to_inherit_dot_editorconfig_settings_from_higher_directories}");
            editorconfig.AppendLine("root = true");
            editorconfig.AppendLine();

            if (language == LanguageNames.CSharp)
            {
                editorconfig.AppendLine($"# {WorkspacesResources.CSharp_files}");
                editorconfig.AppendLine("[*.cs]");
            }
            else if (language == LanguageNames.VisualBasic)
            {
                editorconfig.AppendLine($"# {WorkspacesResources.Visual_Basic_files}");
                editorconfig.AppendLine("[*.vb]");
            }
            editorconfig.AppendLine();

            foreach ((var feature, var options) in groupedOptions)
            {
                AppendOptionsToEditorConfig(optionSet, feature, options, language, editorconfig);
            }

            AppendNamingStylePreferencesToEditorConfig(optionSet, language, editorconfig);

            return editorconfig.ToString();
        }

        private static void AppendOptionsToEditorConfig(OptionSet optionSet, string feature, ImmutableArray<IOption> options, string language, StringBuilder editorconfig)
        {
            editorconfig.AppendLine($"#### {feature} ####");
            editorconfig.AppendLine();

            foreach (var optionGrouping in options
                                           .Where(o => o.StorageLocations.Any(l => l is IEditorConfigStorageLocation2))
                                           .GroupBy(o => (o as IOptionWithGroup)?.Group ?? OptionGroup.Default)
                                           .OrderBy(g => g.Key.Priority))
            {
                editorconfig.AppendLine($"# {optionGrouping.Key.Description}");

                var optionsAndEditorConfigLocations = optionGrouping.Select(o => (o, o.StorageLocations.OfType<IEditorConfigStorageLocation2>().First()));
                var uniqueEntries = new SortedSet<string>();
                foreach ((var option, var editorConfigLocation) in optionsAndEditorConfigLocations)
                {
                    var editorConfigString = GetEditorConfigString(option, editorConfigLocation);
                    uniqueEntries.Add(editorConfigString);
                }

                foreach (var entry in uniqueEntries)
                {
                    editorconfig.AppendLine(entry);
                }

                editorconfig.AppendLine();
            }

            string GetEditorConfigString(IOption option, IEditorConfigStorageLocation2 editorConfigLocation)
            {
                var optionKey = new OptionKey(option, option.IsPerLanguage ? language : null);
                var value = optionSet.GetOption(optionKey);
                var editorConfigString = editorConfigLocation.GetEditorConfigString(value, optionSet);
                Debug.Assert(!string.IsNullOrEmpty(editorConfigString));
                return editorConfigString;
            }
        }

        private static void AppendNamingStylePreferencesToEditorConfig(OptionSet optionSet, string language, StringBuilder editorconfig)
        {
            editorconfig.AppendLine($"#### {WorkspacesResources.Naming_styles} ####");

            var namingStylePreferences = optionSet.GetOption(SimplificationOptions.NamingPreferences, language);
            var serializedNameMap = AssignNamesToNamingStyleElements(namingStylePreferences);
            var ruleNameMap = AssignNamesToNamingStyleRules(namingStylePreferences, serializedNameMap);
            var referencedElements = new HashSet<Guid>();

            editorconfig.AppendLine();
            editorconfig.AppendLine($"# {WorkspacesResources.Naming_rules}");

            foreach (var namingRule in namingStylePreferences.NamingRules)
            {
                referencedElements.Add(namingRule.SymbolSpecificationID);
                referencedElements.Add(namingRule.NamingStyleID);

                editorconfig.AppendLine();
                editorconfig.AppendLine($"dotnet_naming_rule.{ruleNameMap[namingRule]}.severity = {namingRule.EnforcementLevel.ToNotificationOption(defaultSeverity: DiagnosticSeverity.Hidden).ToEditorConfigString()}");
                editorconfig.AppendLine($"dotnet_naming_rule.{ruleNameMap[namingRule]}.symbols = {serializedNameMap[namingRule.SymbolSpecificationID]}");
                editorconfig.AppendLine($"dotnet_naming_rule.{ruleNameMap[namingRule]}.style = {serializedNameMap[namingRule.NamingStyleID]}");
            }

            editorconfig.AppendLine();
            editorconfig.AppendLine($"# {WorkspacesResources.Symbol_specifications}");

            foreach (var symbolSpecification in namingStylePreferences.SymbolSpecifications)
            {
                if (!referencedElements.Contains(symbolSpecification.ID))
                {
                    continue;
                }

                editorconfig.AppendLine();
                editorconfig.AppendLine($"dotnet_naming_symbols.{serializedNameMap[symbolSpecification.ID]}.applicable_kinds = {symbolSpecification.ApplicableSymbolKindList.ToEditorConfigString()}");
                editorconfig.AppendLine($"dotnet_naming_symbols.{serializedNameMap[symbolSpecification.ID]}.applicable_accessibilities = {symbolSpecification.ApplicableAccessibilityList.ToEditorConfigString(language)}");
                editorconfig.AppendLine($"dotnet_naming_symbols.{serializedNameMap[symbolSpecification.ID]}.required_modifiers = {symbolSpecification.RequiredModifierList.ToEditorConfigString(language)}");
            }

            editorconfig.AppendLine();
            editorconfig.AppendLine($"# {WorkspacesResources.Naming_styles}");

            foreach (var namingStyle in namingStylePreferences.NamingStyles)
            {
                if (!referencedElements.Contains(namingStyle.ID))
                {
                    continue;
                }

                editorconfig.AppendLine();
                editorconfig.AppendLine($"dotnet_naming_style.{serializedNameMap[namingStyle.ID]}.required_prefix = {namingStyle.Prefix}");
                editorconfig.AppendLine($"dotnet_naming_style.{serializedNameMap[namingStyle.ID]}.required_suffix = {namingStyle.Suffix}");
                editorconfig.AppendLine($"dotnet_naming_style.{serializedNameMap[namingStyle.ID]}.word_separator = {namingStyle.WordSeparator}");
                editorconfig.AppendLine($"dotnet_naming_style.{serializedNameMap[namingStyle.ID]}.capitalization = {namingStyle.CapitalizationScheme.ToEditorConfigString()}");
            }
        }

        private static ImmutableDictionary<Guid, string> AssignNamesToNamingStyleElements(NamingStylePreferences namingStylePreferences)
        {
            var symbolSpecificationNames = new HashSet<string>();
            var builder = ImmutableDictionary.CreateBuilder<Guid, string>();
            foreach (var symbolSpecification in namingStylePreferences.SymbolSpecifications)
            {
                var name = ToSnakeCaseName(symbolSpecification.Name);
                if (!symbolSpecificationNames.Add(name))
                {
                    name = symbolSpecification.ID.ToString("n");
                }

                builder.Add(symbolSpecification.ID, name);
            }

            var namingStyleNames = new HashSet<string>();
            foreach (var namingStyle in namingStylePreferences.NamingStyles)
            {
                var name = ToSnakeCaseName(namingStyle.Name);
                if (!namingStyleNames.Add(name))
                {
                    name = namingStyle.ID.ToString("n");
                }

                builder.Add(namingStyle.ID, name);
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

        private static ImmutableDictionary<SerializableNamingRule, string> AssignNamesToNamingStyleRules(NamingStylePreferences namingStylePreferences, ImmutableDictionary<Guid, string> serializedNameMap)
        {
            var builder = ImmutableDictionary.CreateBuilder<SerializableNamingRule, string>();
            foreach (var rule in namingStylePreferences.NamingRules)
            {
                builder.Add(rule, $"{serializedNameMap[rule.SymbolSpecificationID]}_should_be_{serializedNameMap[rule.NamingStyleID]}");
            }

            return builder.ToImmutable();
        }
    }
}
