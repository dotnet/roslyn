// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.NamingStyles;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal static partial class EditorConfigNamingStyleParser
    {
        private static bool TryGetNamingStyleData(
            string namingRuleName,
            IReadOnlyDictionary<string, object> allRawConventions,
            out NamingStyle namingStyle)
        {
            namingStyle = default;
            if (!TryGetNamingStyleTitle(namingRuleName, allRawConventions, out string namingStyleTitle))
            {
                return false;
            }

            var requiredPrefix = GetNamingRequiredPrefix(namingStyleTitle, allRawConventions);
            var requiredSuffix = GetNamingRequiredSuffix(namingStyleTitle, allRawConventions);
            var wordSeparator = GetNamingWordSeparator(namingStyleTitle, allRawConventions);
            if (!TryGetNamingCapitalization(namingStyleTitle, allRawConventions, out var capitalization))
            {
                return false;
            }

            namingStyle = new NamingStyle(
                Guid.NewGuid(),
                name: namingStyleTitle,
                prefix: requiredPrefix,
                suffix: requiredSuffix,
                wordSeparator: wordSeparator,
                capitalizationScheme: capitalization);

            return true;
        }

        private static bool TryGetNamingStyleTitle(
            string namingRuleName,
            IReadOnlyDictionary<string, object> conventionsDictionary,
            out string namingStyleName)
        {
            if (conventionsDictionary.TryGetValue($"dotnet_naming_rule.{namingRuleName}.style", out object result))
            {
                namingStyleName = result as string;
                return namingStyleName != null;
            }

            namingStyleName = null;
            return false;
        }

        private static string GetNamingRequiredPrefix(string namingStyleName, IReadOnlyDictionary<string, object> conventionsDictionary)
            => GetStringFromConventionsDictionary(namingStyleName, "required_prefix", conventionsDictionary);

        private static string GetNamingRequiredSuffix(string namingStyleName, IReadOnlyDictionary<string, object> conventionsDictionary)
            => GetStringFromConventionsDictionary(namingStyleName, "required_suffix", conventionsDictionary);

        private static string GetNamingWordSeparator(string namingStyleName, IReadOnlyDictionary<string, object> conventionsDictionary)
            => GetStringFromConventionsDictionary(namingStyleName, "word_separator", conventionsDictionary);

        private static bool TryGetNamingCapitalization(string namingStyleName, IReadOnlyDictionary<string, object> conventionsDictionary, out Capitalization capitalization)
        {
            var result = GetStringFromConventionsDictionary(namingStyleName, "capitalization", conventionsDictionary);
            return TryParseCapitalizationScheme(result, out capitalization);
        }

        private static string GetStringFromConventionsDictionary(string namingStyleName, string optionName, IReadOnlyDictionary<string, object> conventionsDictionary)
        {
            if (conventionsDictionary.TryGetValue($"dotnet_naming_style.{namingStyleName}.{optionName}", out object result))
            {
                return result as string ?? string.Empty;
            }

            return string.Empty;
        }

        private static bool TryParseCapitalizationScheme(string namingStyleCapitalization, out Capitalization capitalization)
        {
            switch (namingStyleCapitalization)
            {
                case "pascal_case":
                    capitalization = Capitalization.PascalCase;
                    return true;
                case "camel_case":
                    capitalization = Capitalization.CamelCase;
                    return true;
                case "first_word_upper":
                    capitalization = Capitalization.FirstUpper;
                    return true;
                case "all_upper":
                    capitalization = Capitalization.AllUpper;
                    return true;
                case "all_lower":
                    capitalization = Capitalization.AllLower;
                    return true;
                default:
                    capitalization = default;
                    return false;
            }
        }
    }
}
