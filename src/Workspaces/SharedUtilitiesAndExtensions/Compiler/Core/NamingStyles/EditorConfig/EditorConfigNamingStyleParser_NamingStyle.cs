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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal static partial class EditorConfigNamingStyleParser
    {
        internal static bool TryGetNamingStyleData(
            Section section,
            string namingRuleName,
            IReadOnlyDictionary<string, (string value, TextLine? line)> properties,
            [NotNullWhen(true)] out NamingScheme? namingScheme)
        {
            return TryGetNamingStyleData(
                namingRuleName,
                properties,
                s => s.value,
                x => x.line,
                s => (s.value, s.line),
                (nameTuple, prefixTuple, suffixTuple, wordSeparatorTuple, capitalizationTuple) =>
                {
                    var (name, nameTextLine) = nameTuple;
                    var (prefix, prefixTextLine) = prefixTuple;
                    var (suffix, suffixTextLine) = suffixTuple;
                    var (wordSeparator, wordSeparatorTextLine) = wordSeparatorTuple;
                    var (capitalization, capitalizationTextLine) = capitalizationTuple;

                    return new NamingScheme(
                        OptionName: (section, nameTextLine?.Span, name),
                        Prefix: (section, prefixTextLine?.Span, prefix),
                        Suffix: (section, suffixTextLine?.Span, suffix),
                        WordSeparator: (section, wordSeparatorTextLine?.Span, wordSeparator),
                        Capitalization: (section, capitalizationTextLine?.Span, capitalization));
                },
                out namingScheme);
        }

        private static bool TryGetNamingStyleData(
            string namingRuleName,
            IReadOnlyDictionary<string, string> rawOptions,
            out NamingStyle namingStyle)
        {
            return TryGetNamingStyleData<string, object?, NamingStyle>(
                namingRuleName,
                rawOptions,
                s => s,
                x => null,
                s => (s ?? string.Empty, null),
                (nameTuple, prefixTuple, suffixTuple, wordSeparatorTuple, capitalizationTuple) =>
                {
                    var (namingStyleName, _) = nameTuple;
                    var (prefix, _) = prefixTuple;
                    var (suffix, _) = suffixTuple;
                    var (wordSeparator, _) = wordSeparatorTuple;
                    var (capitalization, _) = capitalizationTuple;

                    return new NamingStyle(
                        Guid.NewGuid(),
                        namingStyleName,
                        prefix,
                        suffix,
                        wordSeparator,
                        capitalization);
                },
                out namingStyle);
        }

        private static bool TryGetNamingStyleData<T, TData, TResult>(
            string namingRuleName,
            IReadOnlyDictionary<string, T> rawOptions,
            Func<T, string> nameSelector,
            Func<T, TData> dataSelector,
            Func<T?, (string value, TData data)> tupleSelector,
            Func<(string namingStyleName, TData data),
                 (string prefix, TData data),
                 (string suffix, TData data),
                 (string wordSeparator, TData data),
                 (Capitalization capitalization, TData data), TResult> constructor,
            [NotNullWhen(true)] out TResult? namingStyle)
        {
            namingStyle = default;
            if (!TryGetNamingStyleTitle(namingRuleName, rawOptions, nameSelector, dataSelector, out var namingStyleTitle))
            {
                return false;
            }

            var requiredPrefix = GetNamingRequiredPrefix(namingStyleTitle.name, rawOptions, tupleSelector);
            var requiredSuffix = GetNamingRequiredSuffix(namingStyleTitle.name, rawOptions, tupleSelector);
            var wordSeparator = GetNamingWordSeparator(namingStyleTitle.name, rawOptions, tupleSelector);
            if (!TryGetNamingCapitalization(namingStyleTitle.name, rawOptions, tupleSelector, out var capitalization))
            {
                return false;
            }

            namingStyle = constructor(namingStyleTitle, requiredPrefix, requiredSuffix, wordSeparator, capitalization);
            return namingStyle is not null;
        }

        private static bool TryGetNamingStyleTitle<T, TData>(
            string namingRuleName,
            IReadOnlyDictionary<string, T> conventionsDictionary,
            Func<T, string> nameSelector,
            Func<T, TData> dataSelector,
            out (string name, TData data) result)
        {
            if (conventionsDictionary.TryGetValue($"dotnet_naming_rule.{namingRuleName}.style", out var namingStyleName))
            {
                var name = nameSelector(namingStyleName);
                result = (name, dataSelector(namingStyleName));
                return name != null;
            }

            result = default;
            return false;
        }

        private static (string prefix, TData data) GetNamingRequiredPrefix<T, TData>(
            string namingStyleName,
            IReadOnlyDictionary<string, T> properties,
            Func<T?, (string value, TData data)> tupleSelector)
            => GetValueFromDictionary(namingStyleName, "required_prefix", properties, tupleSelector);

        private static (string suffix, TData data) GetNamingRequiredSuffix<T, TData>(
            string namingStyleName,
            IReadOnlyDictionary<string, T> properties,
            Func<T?, (string value, TData data)> tupleSelector)
            => GetValueFromDictionary(namingStyleName, "required_suffix", properties, tupleSelector);

        private static (string wordSeparator, TData data) GetNamingWordSeparator<T, TData>(
            string namingStyleName,
            IReadOnlyDictionary<string, T> properties,
            Func<T?, (string value, TData data)> tupleSelector)
            => GetValueFromDictionary(namingStyleName, "word_separator", properties, tupleSelector);

        private static bool TryGetNamingCapitalization<T, TData>(
            string namingStyleName,
            IReadOnlyDictionary<string, T> properties,
            Func<T?, (string value, TData data)> tupleSelector,
            out (Capitalization capitalization, TData data) result)
        {
            var (value, data) = GetValueFromDictionary(namingStyleName, "capitalization", properties, tupleSelector);
            if (TryParseCapitalizationScheme(value, out var capitalization))
            {
                result = (capitalization.Value, data);
                return true;
            }

            result = default;
            return false;
        }

        private static (string value, TData data) GetValueFromDictionary<T, TData>(
            string namingStyleName,
            string optionName,
            IReadOnlyDictionary<string, T> conventionsDictionary,
            Func<T?, (string value, TData data)> tupleSelector)
        {
            _ = conventionsDictionary.TryGetValue($"dotnet_naming_style.{namingStyleName}.{optionName}", out var result);
            return tupleSelector(result);
        }

        private static bool TryParseCapitalizationScheme(
            string namingStyleCapitalization,
            [NotNullWhen(true)] out Capitalization? capitalization)
        {
            capitalization = namingStyleCapitalization switch
            {
                "pascal_case" => Capitalization.PascalCase,
                "camel_case" => Capitalization.CamelCase,
                "first_word_upper" => Capitalization.FirstUpper,
                "all_upper" => Capitalization.AllUpper,
                "all_lower" => Capitalization.AllLower,
                _ => null,
            };

            return capitalization is not null;
        }

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
}
