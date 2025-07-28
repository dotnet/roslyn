// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.EditorConfig.Parsing.NamingStyles;
using Microsoft.CodeAnalysis.NamingStyles;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;

internal static class EditorConfigNamingStylesExtensions
{
    extension(EditorConfigNamingStyles editorConfigNamingStyles)
    {
        public bool TryGetParseResultForRule(
        NamingStyleSetting namingStyleSetting,
        [NotNullWhen(true)] out NamingStyleOption? namingStyleOption)
        {
            namingStyleOption = null;
            foreach (var (option, optionAsNamingStyle) in editorConfigNamingStyles.Rules.AsNamingStyleSettings())
            {
                if (AreSameRule(optionAsNamingStyle, namingStyleSetting))
                {
                    namingStyleOption = option;
                    return true;
                }
            }

            return false;

            static bool AreSameRule(NamingStyleSetting left, NamingStyleSetting right)
                => left.Severity == right.Severity &&
                   AreSameSymbolSpec(left.Type, right.Type) &&
                   AreSameNamingStyle(left.Style, right.Style);

            static bool AreSameSymbolSpec(SymbolSpecification? left, SymbolSpecification? right)
            {
                if (left is null && right is null)
                {
                    return true;
                }

                if (left is null || right is null)
                {
                    return false;
                }

                return left.ApplicableSymbolKindList.SequenceEqual(right!.ApplicableSymbolKindList) &&
                       left.ApplicableAccessibilityList.SequenceEqual(right.ApplicableAccessibilityList) &&
                       left.RequiredModifierList.SequenceEqual(right.RequiredModifierList);
            }

            static bool AreSameNamingStyle(NamingStyle left, NamingStyle right)
                => left.Prefix == right.Prefix &&
                   left.Suffix == right.Suffix &&
                   left.WordSeparator == right.WordSeparator &&
                   left.CapitalizationScheme == right.CapitalizationScheme;
        }
    }

    extension(ImmutableArray<NamingStyleOption> namingStyleOptions)
    {
        public ImmutableArray<(NamingStyleOption namingStyleOption, NamingStyleSetting namingStyleSetting)> AsNamingStyleSettings()
        => namingStyleOptions.SelectAsArray(rule => (rule, NamingStyleSetting.FromParseResult(rule)));
    }

    extension(NamingScheme namingScheme)
    {
        public NamingStyle AsNamingStyle()
        => new(
            Guid.NewGuid(),
            namingScheme.OptionName.Value,
            namingScheme.Prefix.Value,
            namingScheme.Suffix.Value,
            namingScheme.WordSeparator.Value,
            namingScheme.Capitalization.Value);
    }

    extension(ApplicableSymbolInfo applicableSymbolInfo)
    {
        public SymbolSpecification AsSymbolSpecification()
        => new(
            Guid.NewGuid(),
            applicableSymbolInfo.OptionName.Value,
            applicableSymbolInfo.SymbolKinds.Value,
            applicableSymbolInfo.Accessibilities.Value,
            applicableSymbolInfo.Modifiers.Value);
    }
}
