// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.EditorConfig;
using Microsoft.CodeAnalysis.EditorConfig.Parsing;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using NamingStylesParser = Microsoft.CodeAnalysis.EditorConfig.Parsing.NamingStyles.EditorConfigNamingStylesParser;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;

internal static class SourceTextExtensions
{
    public static SourceText WithNamingStyles(this SourceText sourceText, IGlobalOptionService globalOptions)
    {
        var (common, csharp, visualBasic) = GetPreferencesForAllLanguages(globalOptions);

        sourceText = WithNamingStyles(sourceText, csharp, Language.CSharp);
        sourceText = WithNamingStyles(sourceText, visualBasic, Language.VisualBasic);
        return WithNamingStyles(sourceText, common, Language.CSharp | Language.VisualBasic);
    }

    private static SourceText WithNamingStyles(SourceText sourceText, IEnumerable<NamingRule> rules, Language language)
    {
        if (rules.Any())
        {
            var parseResult = NamingStylesParser.Parse(sourceText, null); // file path unnecessary here
            var newNamingStyleSection = new StringBuilder();
            if (parseResult.TryGetSectionForLanguage(language, out var existingSection))
            {
                var span = new TextSpan(existingSection.Span.End, 0);
                EditorConfigFileGenerator.AppendNamingStylePreferencesToEditorConfig(rules, newNamingStyleSection, GetLanguageString(language));
                return WithChanges(sourceText, span, newNamingStyleSection.ToString());
            }
            else
            {
                var span = new TextSpan(sourceText.Length, 0);
                newNamingStyleSection.Append("\r\n");
                newNamingStyleSection.Append(Section.GetHeaderTextForLanguage(language));
                EditorConfigFileGenerator.AppendNamingStylePreferencesToEditorConfig(rules, newNamingStyleSection, GetLanguageString(language));
                return WithChanges(sourceText, span, newNamingStyleSection.ToString());
            }
        }

        return sourceText;

        static SourceText WithChanges(SourceText sourceText, TextSpan span, string newText)
        {
            var textChange = new TextChange(span, newText);
            return sourceText.WithChanges(textChange);
        }

        static string? GetLanguageString(Language language)
        {
            if (language.HasFlag(Language.CSharp) && language.HasFlag(Language.VisualBasic))
            {
                return null;
            }
            else if (language.HasFlag(Language.CSharp))
            {
                return LanguageNames.CSharp;
            }
            else if (language.HasFlag(Language.VisualBasic))
            {
                return LanguageNames.VisualBasic;
            }

            throw new InvalidOperationException("Invalid language specified");
        }
    }

    private static (IEnumerable<NamingRule> Common, IEnumerable<NamingRule> CSharp, IEnumerable<NamingRule> VisualBasic) GetPreferencesForAllLanguages(IGlobalOptionService globalOptions)
    {
        var csharpNamingStylePreferences = globalOptions.GetOption(NamingStyleOptions.NamingPreferences, LanguageNames.CSharp);
        var vbNamingStylePreferences = globalOptions.GetOption(NamingStyleOptions.NamingPreferences, LanguageNames.VisualBasic);

        var commonOptions = GetCommonOptions(csharpNamingStylePreferences, vbNamingStylePreferences);
        var csharpOnlyOptions = GetOptionsUniqueOptions(csharpNamingStylePreferences, commonOptions);
        var vbOnlyOptions = GetOptionsUniqueOptions(vbNamingStylePreferences, commonOptions);
        return (commonOptions, csharpOnlyOptions, vbOnlyOptions);

        static IEnumerable<NamingRule> GetCommonOptions(NamingStylePreferences csharp, NamingStylePreferences visualBasic)
            => csharp.Rules.NamingRules.Intersect(visualBasic.Rules.NamingRules, NamingRuleComparerIgnoreGUIDs.Instance);

        static IEnumerable<NamingRule> GetOptionsUniqueOptions(NamingStylePreferences csharp, IEnumerable<NamingRule> common)
            => csharp.Rules.NamingRules.Except(common, NamingRuleComparerIgnoreGUIDs.Instance);
    }

    private class NamingRuleComparerIgnoreGUIDs : IEqualityComparer<NamingRule>
    {
        private static readonly Lazy<NamingRuleComparerIgnoreGUIDs> s_lazyInstance = new(() => new NamingRuleComparerIgnoreGUIDs());

        public static NamingRuleComparerIgnoreGUIDs Instance => s_lazyInstance.Value;

        public bool Equals(NamingRule left, NamingRule right)
        {
            return left.EnforcementLevel == right.EnforcementLevel &&
                   NamingStyleComparerIgnoreGUIDs.Instance.Equals(left.NamingStyle, right.NamingStyle) &&
                   SymbolSpecificationComparerIgnoreGUIDs.Instance.Equals(left.SymbolSpecification, right.SymbolSpecification);
        }

        public int GetHashCode(NamingRule rule)
        {
            var enforcementLevelHashCode = (int)rule.EnforcementLevel;
            var namingStyleHashCode = NamingStyleComparerIgnoreGUIDs.Instance.GetHashCode(rule.NamingStyle);
            var symbolSpecificationHashCode = SymbolSpecificationComparerIgnoreGUIDs.Instance.GetHashCode(rule.SymbolSpecification);
            return Hash.Combine(enforcementLevelHashCode, Hash.Combine(namingStyleHashCode, symbolSpecificationHashCode));
        }

        private class NamingStyleComparerIgnoreGUIDs : IEqualityComparer<NamingStyle>
        {
            private static readonly Lazy<NamingStyleComparerIgnoreGUIDs> s_lazyInstance = new(() => new NamingStyleComparerIgnoreGUIDs());

            public static NamingStyleComparerIgnoreGUIDs Instance => s_lazyInstance.Value;

            public bool Equals(NamingStyle left, NamingStyle right)
            {
                return StringComparer.OrdinalIgnoreCase.Equals(left.Name, right.Name) &&
                       StringComparer.Ordinal.Equals(left.Prefix, right.Prefix) &&
                       StringComparer.Ordinal.Equals(left.Suffix, right.Suffix) &&
                       StringComparer.Ordinal.Equals(left.WordSeparator, right.WordSeparator) &&
                       left.CapitalizationScheme == right.CapitalizationScheme;
            }

            public int GetHashCode(NamingStyle style)
            {
                return Hash.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(style.Name),
                    Hash.Combine(StringComparer.Ordinal.GetHashCode(style.Prefix),
                        Hash.Combine(StringComparer.Ordinal.GetHashCode(style.Suffix),
                            Hash.Combine(StringComparer.Ordinal.GetHashCode(style.WordSeparator),
                                (int)style.CapitalizationScheme))));
            }
        }

        private class SymbolSpecificationComparerIgnoreGUIDs : IEqualityComparer<SymbolSpecification>
        {
            private static readonly Lazy<SymbolSpecificationComparerIgnoreGUIDs> s_lazyInstance = new(() => new SymbolSpecificationComparerIgnoreGUIDs());

            public static SymbolSpecificationComparerIgnoreGUIDs Instance => s_lazyInstance.Value;

            public bool Equals(SymbolSpecification? left, SymbolSpecification? right)
            {
                if (left is null && right is null)
                {
                    return true;
                }

                if (left is null || right is null)
                {
                    return false;
                }

                return StringComparer.OrdinalIgnoreCase.Equals(left!.Name, right!.Name) &&
                       left.RequiredModifierList.SequenceEqual(right.RequiredModifierList) &&
                       left.ApplicableAccessibilityList.SequenceEqual(right.ApplicableAccessibilityList) &&
                       left.ApplicableSymbolKindList.SequenceEqual(right.ApplicableSymbolKindList);
            }

            public int GetHashCode(SymbolSpecification symbolSpecification)
            {
                return Hash.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(symbolSpecification.Name),
                    Hash.Combine(Hash.CombineValues(symbolSpecification.RequiredModifierList),
                        Hash.Combine(Hash.CombineValues(symbolSpecification.ApplicableAccessibilityList),
                            Hash.CombineValues(symbolSpecification.ApplicableSymbolKindList))));
            }
        }
    }
}
