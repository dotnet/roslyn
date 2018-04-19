// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a single EditorConfig file, see http://editorconfig.org for details about the format.
    /// </summary>
    internal sealed class EditorConfig
    {
        // Matches EditorConfig section header such as "[*.{js,py}]", see http://editorconfig.org for details
        private static readonly Regex _sectionMatcher = new Regex(@"^\s*\[(([^#;]|\\#|\\;)+)\]\s*([#;].*)?$", RegexOptions.Compiled);
        // Matches EditorConfig property such as "indent_style = space", see http://editorconfig.org for details
        private static readonly Regex _propertyMatcher = new Regex(@"^\s*([\w\.\-_]+)\s*[=:]\s*(.*?)\s*([#;].*)?$", RegexOptions.Compiled);

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
        /// Parses an editor config file text located within the given parent directory.
        /// </summary>
        public static EditorConfig Parse(string text, string parentDirectory)
        {
            Section globalSection = null;
            var namedSectionBuilder = ImmutableArray.CreateBuilder<Section>();

            // N.B. The editorconfig documentation is quite loose on property interpretation.
            // Specifically, it says:
            //      Currently all properties and values are case-insensitive.
            //      They are lowercased when parsed.
            // To accomodate this, we use a lower case Unicode mapping when adding to the
            // dictionary, but we also use a case-insensitve key comparer when doing lookups
            var activeSectionProperties = ImmutableDictionary.CreateBuilder<string, string>(
                CaseInsensitiveComparison.Comparer);
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

                    var sectionMatches = _sectionMatcher.Matches(line);
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
                            CaseInsensitiveComparison.Comparer);
                        continue;
                    }

                    var propMatches = _propertyMatcher.Matches(line);
                    if (propMatches.Count > 0 && propMatches[0].Groups.Count > 1)
                    {
                        var key = propMatches[0].Groups[1].Value;
                        var value = propMatches[0].Groups[2].Value;
                        Debug.Assert(!string.IsNullOrEmpty(key));

                        key = CaseInsensitiveComparison.ToLower(key.Trim());
                        value = CaseInsensitiveComparison.ToLower(value?.Trim());

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

        internal sealed class Section
        {
            public Section(string name, ImmutableDictionary<string, string> properties)
            {
                this.Name = name;
                this.Properties = properties;
            }

            public string Name { get; }

            public ImmutableDictionary<string, string> Properties { get; }
        }
    }
}
