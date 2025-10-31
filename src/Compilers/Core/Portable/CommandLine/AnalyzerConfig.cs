// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a single EditorConfig file, see https://editorconfig.org for details about the format.
    /// </summary>
    public sealed partial class AnalyzerConfig
    {
        // Matches EditorConfig section header such as "[*.{js,py}]", see https://editorconfig.org for details
        private const string s_sectionMatcherPattern = @"^\s*\[(([^#;]|\\#|\\;)+)\]\s*([#;].*)?$";

#if NET

        [GeneratedRegex(s_sectionMatcherPattern)]
        private static partial Regex GetSectionMatcherRegex();

#else
        private static readonly Regex s_sectionMatcher = new Regex(s_sectionMatcherPattern, RegexOptions.Compiled);

        private static Regex GetSectionMatcherRegex() => s_sectionMatcher;

#endif

        /// <summary>
        /// Key that indicates if this config is a global config
        /// </summary>
        internal const string GlobalKey = "is_global";

        /// <summary>
        /// Key that indicates the precedence of this config when <see cref="IsGlobal"/> is true
        /// </summary>
        internal const string GlobalLevelKey = "global_level";

        /// <summary>
        /// Filename that indicates this file is a user provided global config
        /// </summary>
        internal const string UserGlobalConfigName = ".globalconfig";

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
        internal static ImmutableHashSet<string> ReservedKeys { get; }
            = ImmutableHashSet.CreateRange(Section.PropertiesKeyComparer, new[] {
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
        internal static ImmutableHashSet<string> ReservedValues { get; }
            = ImmutableHashSet.CreateRange(CaseInsensitiveComparison.Comparer, new[] { "unset" });

        internal Section GlobalSection { get; }

        /// <summary>
        /// The directory the editorconfig was contained in, with all directory separators
        /// replaced with '/'.
        /// </summary>
        internal string NormalizedDirectory { get; }

        /// <summary>
        /// The path passed to <see cref="Parse(string, string)"/> during construction.
        /// </summary>
        internal string PathToFile { get; }

        /// <summary>
        /// Comparer for sorting <see cref="AnalyzerConfig"/> files by <see cref="NormalizedDirectory"/> path length.
        /// </summary>
        internal static Comparer<AnalyzerConfig> DirectoryLengthComparer { get; } = Comparer<AnalyzerConfig>.Create(
            (e1, e2) => e1.NormalizedDirectory.Length.CompareTo(e2.NormalizedDirectory.Length));

        internal ImmutableArray<Section> NamedSections { get; }

        /// <summary>
        /// Gets whether this editorconfig is a topmost editorconfig.
        /// </summary>
        internal bool IsRoot => GlobalSection.Properties.TryGetValue("root", out string? val) && val == "true";

        /// <summary>
        /// Gets whether this editorconfig is a global editorconfig.
        /// </summary>
        internal bool IsGlobal => _hasGlobalFileName || GlobalSection.Properties.ContainsKey(GlobalKey);

        /// <summary>
        /// Get the global level of this config, used to resolve conflicting keys
        /// </summary>
        /// <remarks>
        /// A user can explicitly set the global level via the <see cref="GlobalLevelKey"/>.
        /// When no global level is explicitly set, we use a heuristic:
        ///  <list type="bullet">
        ///     <item><description>
        ///     Any file matching the <see cref="UserGlobalConfigName"/> is determined to be a user supplied global config and gets a level of 100
        ///     </description></item>
        ///     <item><description>
        ///     Any other file gets a default level of 0
        ///     </description></item>
        ///  </list>
        ///  
        /// This value is unused when <see cref="IsGlobal"/> is <c>false</c>.
        /// </remarks>
        internal int GlobalLevel
        {
            get
            {
                if (GlobalSection.Properties.TryGetValue(GlobalLevelKey, out string? val) && int.TryParse(val, out int level))
                {
                    return level;
                }
                else if (_hasGlobalFileName)
                {
                    return 100;
                }
                else
                {
                    return 0;
                }
            }
        }

        private readonly bool _hasGlobalFileName;

        private AnalyzerConfig(
            Section globalSection,
            ImmutableArray<Section> namedSections,
            string pathToFile)
        {
            GlobalSection = globalSection;
            NamedSections = namedSections;
            PathToFile = pathToFile;
            _hasGlobalFileName = Path.GetFileName(pathToFile).Equals(UserGlobalConfigName, StringComparison.OrdinalIgnoreCase);

            // Find the containing directory and normalize the path separators
            string directory = Path.GetDirectoryName(pathToFile) ?? pathToFile;
            NormalizedDirectory = PathUtilities.NormalizeWithForwardSlash(directory);
        }

        /// <summary>
        /// Parses an editor config file text located at the given path. No parsing
        /// errors are reported. If any line contains a parse error, it is dropped.
        /// </summary>
        public static AnalyzerConfig Parse(string text, string? pathToFile)
        {
            return Parse(SourceText.From(text), pathToFile);
        }

        /// <summary>
        /// Parses an editor config file text located at the given path. No parsing
        /// errors are reported. If any line contains a parse error, it is dropped.
        /// </summary>
        public static AnalyzerConfig Parse(SourceText text, string? pathToFile)
        {
            if (pathToFile is null || !Path.IsPathRooted(pathToFile) || string.IsNullOrEmpty(Path.GetFileName(pathToFile)))
            {
                throw new ArgumentException("Must be an absolute path to an editorconfig file", nameof(pathToFile));
            }

            Section? globalSection = null;
            var namedSectionBuilder = ImmutableArray.CreateBuilder<Section>();

            // N.B. The editorconfig documentation is quite loose on property interpretation.
            // Specifically, it says:
            //      Currently all properties and values are case-insensitive.
            //      They are lowercased when parsed.
            // To accommodate this, we use a lower case Unicode mapping when adding to the
            // dictionary, but we also use a case-insensitive key comparer when doing lookups
            var activeSectionProperties = ImmutableDictionary.CreateBuilder<string, string>(
                Section.PropertiesKeyComparer);
            string activeSectionName = "";

            foreach (var textLine in text.Lines)
            {
                string line = textLine.ToString();

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (IsComment(line))
                {
                    continue;
                }

                // Section matching
                var sectionMatches = GetSectionMatcherRegex().Matches(line);
                if (sectionMatches.Count > 0 && sectionMatches[0].Groups.Count > 0)
                {
                    addNewSection();

                    var sectionName = sectionMatches[0].Groups[1].Value;
                    Debug.Assert(!string.IsNullOrEmpty(sectionName));

                    activeSectionName = sectionName;
                    activeSectionProperties = ImmutableDictionary.CreateBuilder<string, string>(
                        Section.PropertiesKeyComparer);
                    continue;
                }

                // property matching
                if (ExtractKeyValue(line, activeSectionProperties, out var _))
                {
                    continue;
                }
            }

            // Add the last section
            addNewSection();

            // Normalize the path to file the same way named sections are
            pathToFile = PathUtilities.NormalizeDriveLetter(pathToFile);

            return new AnalyzerConfig(globalSection!, namedSectionBuilder.ToImmutable(), pathToFile);

            void addNewSection()
            {
                var sectionName = PathUtilities.NormalizeDriveLetter(activeSectionName);

                // Close out the previous section
                var previousSection = new Section(sectionName, activeSectionProperties.ToImmutable());
                if (activeSectionName == "")
                {
                    // This is the global section
                    globalSection = previousSection;
                }
                else
                {
                    namedSectionBuilder.Add(previousSection);
                }
            }
        }

        /// <summary>
        /// Extracts a key-value pair from the given line. The line content will be trimmed
        /// </summary>
        /// <param name="line">Line to extract key/value from as an expected form of key{:|=}[value[{#|;}inline comment]]</param>
        /// <param name="activeSectionProperties">Property collection builder</param>
        /// <param name="key">Optional key found; default is "" because nullability is flagged by return</param>
        /// <returns>Actual key found flag</returns>
        private static bool ExtractKeyValue(string line, ImmutableDictionary<string, string>.Builder activeSectionProperties, out string key)
        {
            // Look for a key-value pair
            var trimmedLine = line.TrimStart(); // remove leading whitespace for the key part
            string keyPart;

            // Look for a non-empty key part
            var keyStart = trimmedLine.IndexOfAny(['=', ':']);
            if (keyStart <= 0 || (keyPart = trimmedLine[..keyStart].TrimEnd()).Length == 0) // remove trailing whitespace for the key part, and ensure keyPart has a non-trimmable content (it can't have a content if keyStart is below second character)
            {
                key = "";
                return false;
            }

            key = keyPart.ToLower(); // lower casing the key part
            var valueComment = trimmedLine[(keyStart + 1)..].TrimStart(); // remove leading whitespace for the value part
            string valuePart;

            // Look for an inline comment in the value part
            var commentStart = valueComment.IndexOfAny(['#', ';']);
            if (commentStart > -1)
            {
                // Remove inline comment from the value part
                valuePart = valueComment[..commentStart];
            }
            else
            {
                valuePart = valueComment;
            }

            string value;
            if (ReservedKeys.Contains(key) || ReservedValues.Contains(valuePart))
            {
                // Lower case the value part if the key is reserved
                value = valuePart.ToLower();
            }
            else
            {
                value = valuePart;
            }

            // Add the key-value pair to the dictionary
            activeSectionProperties[key] = value.TrimEnd(); // remove trailing whitespace for the value part, allowing for "" values
            return true;
        }

        /// <summary>
        /// Checks if the line is a comment. A line is considered a comment if its first non-space character is either # or ;
        /// </summary>
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

        /// <summary>
        /// Represents a named section of the editorconfig file, which consists of a name followed by a set
        /// of key-value pairs.
        /// </summary>
        internal sealed class Section
        {
            /// <summary>
            /// Used to compare <see cref="Name"/>s of sections. Specified by editorconfig to
            /// be a case-sensitive comparison.
            /// </summary>
            public static StringComparison NameComparer { get; } = StringComparison.Ordinal;

            /// <summary>
            /// Used to compare <see cref="Name"/>s of sections. Specified by editorconfig to
            /// be a case-sensitive comparison.
            /// </summary>
            public static IEqualityComparer<string> NameEqualityComparer { get; } = StringComparer.Ordinal;

            /// <summary>
            /// Used to compare keys in <see cref="Properties"/>. The editorconfig spec defines property
            /// keys as being compared case-insensitively according to Unicode lower-case rules.
            /// </summary>
            public static StringComparer PropertiesKeyComparer { get; } = CaseInsensitiveComparison.Comparer;

            public Section(string name, ImmutableDictionary<string, string> properties)
            {
                Name = name;
                Properties = properties;
            }

            /// <summary>
            /// For regular files, the name as present directly in the section specification of the editorconfig file. For sections in
            /// global configs, this is the unescaped full file path.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Keys and values for this section. All keys are lower-cased according to the
            /// EditorConfig specification and keys are compared case-insensitively. Values are
            /// lower-cased if the value appears in <see cref="ReservedValues" />
            /// or if the corresponding key is in <see cref="ReservedKeys" />. Otherwise,
            /// the values are the literal values present in the source.
            /// </summary>
            public ImmutableDictionary<string, string> Properties { get; }
        }
    }
}
