// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Parses a given .editorconfig source text into <see cref="CategorizedAnalyzerConfigOptions"/>.
    /// </summary>
    internal static class EditorConfigParser
    {
        // Matches EditorConfig property such as "indent_style = space", see http://editorconfig.org for details
        private static readonly Regex s_propertyMatcher = new Regex(@"^\s*([\w\.\-_]+)\s*[=:]\s*(.*?)\s*([#;].*)?$", RegexOptions.Compiled);

        private static readonly StringComparer s_keyComparer = CaseInsensitiveComparison.Comparer;

        /// <summary>
        /// A set of keys that are reserved for special interpretation for the editorconfig specification.
        /// All values corresponding to reserved keys in a (key,value) property pair are always lowercased
        /// during parsing.
        /// </summary>
        /// <remarks>
        /// This list was retrieved from https://github.com/editorconfig/editorconfig/wiki/EditorConfig-Properties
        /// at 2018-04-21 19:37:05Z. New keys may be added to this list in newer versions, but old ones will
        /// not be removed.
        /// </remarks>
        private static readonly ImmutableHashSet<string> s_reservedKeys
            = ImmutableHashSet.CreateRange(s_keyComparer, new[] {
                "root",
                "indent_style",
                "indent_size",
                "tab_width",
                "end_of_line",
                "charset",
                "trim_trailing_whitespace",
                "insert_final_newline",
            });

        /// <summary>
        /// A set of values that are reserved for special use for the editorconfig specification
        /// and will always be lower-cased by the parser.
        /// </summary>
        private static readonly ImmutableHashSet<string> s_reservedValues
            = ImmutableHashSet.CreateRange(s_keyComparer, new[] { "unset" });

        public static CategorizedAnalyzerConfigOptions Parse(SourceText text)
        {
            var parsedOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var textLine in text.Lines)
            {
                var line = textLine.ToString();

                if (string.IsNullOrWhiteSpace(line) || IsComment(line))
                {
                    continue;
                }

                var propMatches = s_propertyMatcher.Matches(line);
                if (propMatches.Count > 0 && propMatches[0].Groups.Count > 1)
                {
                    var key = propMatches[0].Groups[1].Value;
                    var value = propMatches[0].Groups[2].Value;

                    Debug.Assert(!string.IsNullOrEmpty(key));
                    Debug.Assert(key == key.Trim());
                    Debug.Assert(value == value.Trim());

                    key = CaseInsensitiveComparison.ToLower(key);
                    if (s_reservedKeys.Contains(key) || s_reservedValues.Contains(value))
                    {
                        value = CaseInsensitiveComparison.ToLower(value);
                    }

                    parsedOptions[key] = value;
                    continue;
                }
            }

            return CategorizedAnalyzerConfigOptions.Create(parsedOptions);
        }

        private static bool IsComment(string line)
        {
            foreach (char c in line)
            {
                if (!char.IsWhiteSpace(c))
                {
                    return c == '#' || c == ';';
                }
            }

            return false;
        }
    }
}
