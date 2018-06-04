// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a single EditorConfig file, see http://editorconfig.org for details about the format.
    /// </summary>
    internal sealed partial class EditorConfig
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
        public static ImmutableHashSet<string> ReservedKeys { get; }
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
        public static ImmutableHashSet<string> ReservedValues { get; }
            = ImmutableHashSet.CreateRange(CaseInsensitiveComparison.Comparer, new[] { "unset" });

        public Section GlobalSection { get; }

        public string Directory { get; }

        public ImmutableArray<Section> NamedSections { get; }

        private EditorConfig(Section globalSection, ImmutableArray<Section> namedSections, string directory)
        {
            GlobalSection = globalSection;
            NamedSections = namedSections;
            Directory = directory;
        }

        /// <summary>
        /// Gets whether this editorconfig is a the topmost editorconfig.
        /// </summary>
        public bool IsRoot
            => GlobalSection.Properties.TryGetValue("root", out string val) && val == "true";

        /// <summary>
        /// Parses an editor config file text located within the given parent directory. No parsing
        /// errors are reported. If any line contains a parse error, it is dropped.
        /// </summary>
        public static EditorConfig Parse(string text, string parentDirectory)
        {
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

            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
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
                        // Close out the previous section
                        var section = new Section(activeSectionName, activeSectionProperties.ToImmutable());
                        if (activeSectionName == "")
                        {
                            // This is the global section
                            globalSection = section;
                        }
                        else
                        {
                            namedSectionBuilder.Add(section);
                        }

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
            }

            // Close out the last section
            var lastSection = new Section(activeSectionName, activeSectionProperties.ToImmutable());
            if (activeSectionName == "")
            {
                // This is the global section
                globalSection = lastSection;
            }
            else
            {
                namedSectionBuilder.Add(lastSection);
            }

            return new EditorConfig(globalSection, namedSectionBuilder.ToImmutable(), parentDirectory);
        }

        /// <summary>
        /// Combine an editorconfig with an editorconfig nested in a subdirectory to form a new "effective"
        /// editorconfig. "Nested" is defined as the parent directory being an ordinal prefix of the nested
        /// directory.
        /// </summary>
        /// <remarks>
        /// Editorconfig files are combined by applying all properties from the parent editorconfig, then
        /// applying all properties from the nested editorconfig, with any conflicts resolving in favor
        /// of the nested editorconfig. Any new sections in the nested editorconfig will appear at the
        /// end of <see cref="EditorConfig.NamedSections" />.
        /// </remarks>
        public static EditorConfig Combine(EditorConfig nested, EditorConfig parent)
        {
            Debug.Assert(nested.Directory.StartsWith(parent.Directory, StringComparison.Ordinal));

            // PROTOTYPE(editorconfig): Global properties are not described in the editorconfig
            // spec and are not specified as being inherited or overridden. The only mentioned
            // global property, 'root', should definitely not be inherited. For now, let's assume
            // that the global properties are fixed.
            Section newGlobals = nested.GlobalSection;

            var newSections = ImmutableArray.CreateBuilder<Section>(
                Math.Max(nested.NamedSections.Length, parent.NamedSections.Length));

            // Parent config sections come first
            newSections.AddRange(parent.NamedSections);

            // Now nested config sections
            foreach (Section nestedSection in nested.NamedSections)
            {
                int parentIndex = findSection(newSections, nestedSection.Name);
                if (parentIndex >= 0)
                {
                    // Nested section properties override parent section properties
                    Section parentSection = newSections[parentIndex];
                    ImmutableDictionary<string, string> properties = 
                        parentSection.Properties.SetItems(nestedSection.Properties);
                    newSections[parentIndex] = new Section(nestedSection.Name, properties);
                }
                else
                {
                    newSections.Add(nestedSection);
                }
            }

            return new EditorConfig(newGlobals, newSections.ToImmutable(), nested.Directory);

            int findSection(ImmutableArray<Section>.Builder sections, string name)
            {
                for (int i = 0; i < sections.Count; i++)
                {
                    Section s = sections[i];
                    if (s.Name.Equals(name, Section.NameComparer))
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        private static bool IsComment(string line)
        {
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
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
            public static IEqualityComparer<string> PropertiesKeyComparer { get; } = CaseInsensitiveComparison.Comparer;

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
            /// lower-cased if the value appears in <see cref="EditorConfig.ReservedValues" />
            /// or if the corresponding key is in <see cref="EditorConfig.ReservedKeys" />. Otherwise,
            /// the values are the literal values present in the source.
            /// </summary>
            public ImmutableDictionary<string, string> Properties { get; }
        }
    }
}
