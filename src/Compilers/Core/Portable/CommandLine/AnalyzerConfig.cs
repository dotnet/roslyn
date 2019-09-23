// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using AnalyzerOptions = System.Collections.Immutable.ImmutableDictionary<string, string>;
using TreeOptions = System.Collections.Immutable.ImmutableDictionary<string, Microsoft.CodeAnalysis.ReportDiagnostic>;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a single EditorConfig file, see http://editorconfig.org for details about the format.
    /// </summary>
    public sealed partial class AnalyzerConfig
    {
        // Matches EditorConfig section header such as "[*.{js,py}]", see http://editorconfig.org for details
        private static readonly Regex s_sectionMatcher = new Regex(@"^\s*\[(([^#;]|\\#|\\;)+)\]\s*([#;].*)?$", RegexOptions.Compiled);
        // Matches EditorConfig property such as "indent_style = space", see http://editorconfig.org for details
        private static readonly Regex s_propertyMatcher = new Regex(@"^\s*([\w\.\-_]+)\s*[=:]\s*(.*?)\s*([#;].*)?$", RegexOptions.Compiled);

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
        internal bool IsRoot => GlobalSection.Properties.TryGetValue("root", out string val) && val == "true";

        private AnalyzerConfig(
            Section globalSection,
            ImmutableArray<Section> namedSections,
            string pathToFile)
        {
            GlobalSection = globalSection;
            NamedSections = namedSections;
            PathToFile = pathToFile;

            // Find the containing directory and normalize the path separators
            string directory = Path.GetDirectoryName(pathToFile) ?? pathToFile;
            NormalizedDirectory = PathUtilities.NormalizeWithForwardSlash(directory);
        }

        /// <summary>
        /// Parses an editor config file text located at the given path. No parsing
        /// errors are reported. If any line contains a parse error, it is dropped.
        /// </summary>
        public static AnalyzerConfig Parse(string text, string pathToFile)
        {
            return Parse(SourceText.From(text), pathToFile);
        }

        /// <summary>
        /// Parses an editor config file text located at the given path. No parsing
        /// errors are reported. If any line contains a parse error, it is dropped.
        /// </summary>
        public static AnalyzerConfig Parse(SourceText text, string pathToFile)
        {
            if (!Path.IsPathRooted(pathToFile) || string.IsNullOrEmpty(Path.GetFileName(pathToFile)))
            {
                throw new ArgumentException("Must be an absolute path to an editorconfig file", nameof(pathToFile));
            }

            Section globalSection = null;
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

                var sectionMatches = s_sectionMatcher.Matches(line);
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

                var propMatches = s_propertyMatcher.Matches(line);
                if (propMatches.Count > 0 && propMatches[0].Groups.Count > 1)
                {
                    var key = propMatches[0].Groups[1].Value;
                    var value = propMatches[0].Groups[2].Value;

                    Debug.Assert(!string.IsNullOrEmpty(key));
                    Debug.Assert(key == key.Trim());
                    Debug.Assert(value == value?.Trim());

                    key = CaseInsensitiveComparison.ToLower(key);
                    if (ReservedKeys.Contains(key) || ReservedValues.Contains(value))
                    {
                        value = CaseInsensitiveComparison.ToLower(value);
                    }

                    activeSectionProperties[key] = value ?? "";
                    continue;
                }
            }

            // Add the last section
            addNewSection();

            return new AnalyzerConfig(globalSection, namedSectionBuilder.ToImmutable(), pathToFile);

            void addNewSection()
            {
                // Close out the previous section
                var previousSection = new Section(activeSectionName, activeSectionProperties.ToImmutable());
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
            /// The name as present directly in the section specification of the editorconfig file.
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
