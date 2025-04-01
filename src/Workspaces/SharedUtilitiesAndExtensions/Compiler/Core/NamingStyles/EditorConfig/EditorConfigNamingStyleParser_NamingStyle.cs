// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.EditorConfig.Parsing;
using Microsoft.CodeAnalysis.EditorConfig.Parsing.NamingStyles;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

internal static partial class EditorConfigNamingStyleParser
{
    internal static bool TryGetNamingStyle(
        Section section,
        string namingRuleName,
        IReadOnlyDictionary<string, string> entries,
        IReadOnlyDictionary<string, TextLine> lines,
        [NotNullWhen(true)] out NamingScheme? namingScheme)
    {
        if (TryGetStyleProperties(
            namingRuleName,
            entries,
            out var specification,
            out var prefix,
            out var suffix,
            out var wordSeparator,
            out var capitalization) &&
            capitalization.Value.HasValue)
        {
            namingScheme = new NamingScheme(
                OptionName: new(section, specification.GetSpan(lines), specification.Value),
                Prefix: new(section, prefix.GetSpan(lines), prefix.Value),
                Suffix: new(section, suffix.GetSpan(lines), suffix.Value),
                WordSeparator: new(section, wordSeparator.GetSpan(lines), wordSeparator.Value),
                Capitalization: new(section, capitalization.GetSpan(lines), capitalization.Value.Value));

            return true;
        }

        namingScheme = null;
        return false;
    }

    private static bool TryGetNamingStyle(
        string namingRuleName,
        IReadOnlyDictionary<string, string> entries,
        out NamingStyle namingStyle)
    {
        if (TryGetStyleProperties(
            namingRuleName,
            entries,
            out var specification,
            out var prefix,
            out var suffix,
            out var wordSeparator,
            out var capitalization) &&
            capitalization.Value.HasValue)
        {
            namingStyle = new NamingStyle(
                id: Guid.NewGuid(),
                specification.Value,
                prefix.Value,
                suffix.Value,
                wordSeparator.Value,
                capitalization.Value.Value);

            return true;
        }

        namingStyle = default;
        return false;
    }

    private static bool TryGetStyleProperties(
        string namingRuleTitle,
        IReadOnlyDictionary<string, string> entries,
        out Property<string> specification,
        out Property<string?> prefix,
        out Property<string?> suffix,
        out Property<string?> wordSeparator,
        out Property<Capitalization?> capitalization)
    {
        var key = $"dotnet_naming_rule.{namingRuleTitle}.style";
        if (!entries.TryGetValue(key, out var name))
        {
            specification = default;
            prefix = default;
            suffix = default;
            wordSeparator = default;
            capitalization = default;
            return false;
        }

        specification = new Property<string>(key, name);

        const string group = "dotnet_naming_style";
        prefix = GetProperty(entries, group, name, "required_prefix", static s => s, defaultValue: null);
        suffix = GetProperty(entries, group, name, "required_suffix", static s => s, defaultValue: null);
        wordSeparator = GetProperty(entries, group, name, "word_separator", static s => s, defaultValue: null);
        capitalization = GetProperty(entries, group, name, "capitalization", ParseCapitalization, defaultValue: null);
        return true;
    }

    private static Capitalization? ParseCapitalization(string namingStyleCapitalization)
        => namingStyleCapitalization switch
        {
            "pascal_case" => Capitalization.PascalCase,
            "camel_case" => Capitalization.CamelCase,
            "first_word_upper" => Capitalization.FirstUpper,
            "all_upper" => Capitalization.AllUpper,
            "all_lower" => Capitalization.AllLower,
            _ => default,
        };

    public static string ToEditorConfigString(this Capitalization capitalization)
        => capitalization switch
        {
            Capitalization.PascalCase => "pascal_case",
            Capitalization.CamelCase => "camel_case",
            Capitalization.FirstUpper => "first_word_upper",
            Capitalization.AllUpper => "all_upper",
            Capitalization.AllLower => "all_lower",
            _ => throw ExceptionUtilities.UnexpectedValue(capitalization),
        };
}
