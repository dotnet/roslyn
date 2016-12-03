// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal static partial class EditorConfigNamingStyleParser
    {
        private static bool TryGetNamingStyleData(
            string namingRuleName,
            IReadOnlyDictionary<string, object> allRawConventions,
            out NamingStyle namingStyle)
        {
            namingStyle = null;
            if (!TryGetNamingStyleTitle(namingRuleName, allRawConventions, out string namingStyleTitle))
            {
                return false;
            }

            string requiredPrefix = GetNamingRequiredPrefix(namingStyleTitle, allRawConventions);
            string requiredSuffix = GetNamingRequiredSuffix(namingStyleTitle, allRawConventions);
            string wordSeparator = GetNamingWordSeparator(namingStyleTitle, allRawConventions);
            var capitalization = GetNamingCapitalization(namingStyleTitle, allRawConventions);

            namingStyle = new NamingStyle()
            {
                Name = namingStyleTitle,
                Prefix = requiredPrefix,
                Suffix = requiredSuffix,
                WordSeparator = wordSeparator,
                CapitalizationScheme = capitalization
            };

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
        {
            if (conventionsDictionary.TryGetValue($"dotnet_naming_style.{namingStyleName}.required_prefix", out var result))
            {
                return result as string ?? string.Empty;
            }

            return string.Empty;
        }

        private static string GetNamingRequiredSuffix(string namingStyleName, IReadOnlyDictionary<string, object> conventionsDictionary)
        {
            if (conventionsDictionary.TryGetValue($"dotnet_naming_style.{namingStyleName}.required_suffix", out object result))
            {
                return result as string ?? string.Empty;
            }

            return string.Empty;
        }

        private static string GetNamingWordSeparator(string namingStyleName, IReadOnlyDictionary<string, object> conventionsDictionary)
        {
            if (conventionsDictionary.TryGetValue($"dotnet_naming_style.{namingStyleName}.word_separator", out object result))
            {
                return result as string ?? string.Empty;
            }

            return string.Empty;
        }

        private static Capitalization GetNamingCapitalization(string namingStyleName, IReadOnlyDictionary<string, object> conventionsDictionary)
        {
            if (conventionsDictionary.TryGetValue($"dotnet_naming_style.{namingStyleName}.capitalization", out object result))
            {
                return ParseCapitalizationScheme(result as string ?? string.Empty);
            }

            return default(Capitalization);
        }

        private static Capitalization ParseCapitalizationScheme(string namingStyleCapitalization)
        {
            switch (namingStyleCapitalization)
            {
                case "pascal_case": return Capitalization.PascalCase;
                case "camel_case": return Capitalization.CamelCase;
                case "first_word_upper": return Capitalization.FirstUpper;
                case "all_upper": return Capitalization.AllUpper;
                case "all_lower": return Capitalization.AllLower;
                default: return default(Capitalization);
            }
        }
    }
}
