// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.NamingStyles;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal static partial class EditorConfigNamingStyleParser
    {
        private static bool TryGetNamingStyleData(
            string namingRuleName,
            IReadOnlyDictionary<string, string> rawOptions,
            out NamingStyle namingStyle)
        {
            namingStyle = default;
            if (!TryGetNamingStyleTitle(namingRuleName, rawOptions, out var namingStyleTitle))
            {
                return false;
            }

            var requiredPrefix = GetNamingRequiredPrefix(namingStyleTitle, rawOptions);
            var requiredSuffix = GetNamingRequiredSuffix(namingStyleTitle, rawOptions);
            var wordSeparator = GetNamingWordSeparator(namingStyleTitle, rawOptions);
            if (!TryGetNamingCapitalization(namingStyleTitle, rawOptions, out var capitalization))
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
            IReadOnlyDictionary<string, string> conventionsDictionary,
            out string namingStyleName)
        {
            if (conventionsDictionary.TryGetValue($"dotnet_naming_rule.{namingRuleName}.style", out namingStyleName))
            {
                return namingStyleName != null;
            }

            namingStyleName = null;
            return false;
        }

        private static string GetNamingRequiredPrefix(string namingStyleName, IReadOnlyDictionary<string, string> conventionsDictionary)
            => GetStringFromConventionsDictionary(namingStyleName, "required_prefix", conventionsDictionary);

        private static string GetNamingRequiredSuffix(string namingStyleName, IReadOnlyDictionary<string, string> conventionsDictionary)
            => GetStringFromConventionsDictionary(namingStyleName, "required_suffix", conventionsDictionary);

        private static string GetNamingWordSeparator(string namingStyleName, IReadOnlyDictionary<string, string> conventionsDictionary)
            => GetStringFromConventionsDictionary(namingStyleName, "word_separator", conventionsDictionary);

        private static bool TryGetNamingCapitalization(string namingStyleName, IReadOnlyDictionary<string, string> conventionsDictionary, out Capitalization capitalization)
        {
            var result = GetStringFromConventionsDictionary(namingStyleName, "capitalization", conventionsDictionary);
            return TryParseCapitalizationScheme(result, out capitalization);
        }

        private static string GetStringFromConventionsDictionary(string namingStyleName, string optionName, IReadOnlyDictionary<string, string> conventionsDictionary)
        {
            conventionsDictionary.TryGetValue($"dotnet_naming_style.{namingStyleName}.{optionName}", out var result);
            return result ?? string.Empty;
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

        public static string ToEditorConfigString(this Capitalization capitalization)
        {
            switch (capitalization)
            {
                case Capitalization.PascalCase:
                    return "pascal_case";

                case Capitalization.CamelCase:
                    return "camel_case";

                case Capitalization.FirstUpper:
                    return "first_word_upper";

                case Capitalization.AllUpper:
                    return "all_upper";

                case Capitalization.AllLower:
                    return "all_lower";

                default:
                    throw ExceptionUtilities.UnexpectedValue(capitalization);
            }
        }
    }
}
