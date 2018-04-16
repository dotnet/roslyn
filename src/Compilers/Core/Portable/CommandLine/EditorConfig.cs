// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a single EditorConfig file, see http://editorconfig.org for details about the format.
    /// </summary>
    internal class EditorConfig
    {
        // Matches EditorConfig section header such as "[*.{js,py}]", see http://editorconfig.org for details
        private static Regex _sectionMatcher = new Regex(@"^\s*\[(([^#;]|\\#|\\;)+)\]\s*([#;].*)?$", RegexOptions.Compiled);
        // Matches EditorConfig property such as "indent_style = space", see http://editorconfig.org for details
        private static Regex _propertyMatcher = new Regex(@"^\s*([\w\.\-_]+)\s*[=:]\s*(.*?)\s*([#;].*)?$", RegexOptions.Compiled);

        public Section GlobalSection = new Section(string.Empty);

        public string Directory { get; private set; }

        public IList<Section> NamedSections = new List<Section>();

        private EditorConfig()
        {
        }

        /// <summary>
        /// Gets whether this editorconfig is a the topmost editorconfig.
        /// </summary>
        public bool IsRoot
        {
            get
            {
                return this.GlobalSection.Properties.ContainsKey("root") && this.GlobalSection.Properties["root"] == "true";
            }
        }

        public static EditorConfig Parse(string text, string editorConfigParentDirectory)
        {
            var editorConfig = new EditorConfig();
            editorConfig.Directory = editorConfigParentDirectory;
            var activeSection = editorConfig.GlobalSection;
            using(var reader = new StringReader(text))
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
                        var sectionName = sectionMatches[0].Groups[1].Value;
                        if (!string.IsNullOrEmpty(sectionName))
                        {
                            activeSection = new Section(sectionName);
                            editorConfig.NamedSections.Add(activeSection);
                        }
                        continue;
                    }

                    var propMatches = _propertyMatcher.Matches(line);
                    if (propMatches.Count > 0 && propMatches[0].Groups.Count > 1)
                    {
                        var key = propMatches[0].Groups[1].Value;
                        var value = propMatches[0].Groups[2].Value;
                        if (!string.IsNullOrEmpty(key))
                        {
                            activeSection.Properties[key.Trim()] = value?.Trim();
                        }
                        continue;
                    }
                }
            }

            return editorConfig;
        }

        private static bool IsComment(string line)
        {
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (!Char.IsWhiteSpace(c))
                {
                    return c == '#' || c == ';';
                }
            }

            return false;
        }

        internal class Section
        {
            public Section(string name)
            {
                this.Name = name;
                // N.B. The editorconfig documentation is quite loose on property interpretation.
                // Specifically, it says:
                //      Currently all properties and values are case-insensitive.
                //      They are lowercased when parsed.
                // This is not a mandate for case-insensitive parsing, but a strong suggestion.
                // As for *which* case-insensitive comparison, we use CaseInsensitiveComparison.Comparer,
                // which is the recommended Unicode comparison for case-insensitive identifiers. The
                // implementation performs the equivalent of a lower-case comparsion. The
                // editorconfig file is defined as using a UTF-8 encoding, making Unicode comparison
                // relevant.
                this.Properties = new Dictionary<string, string>(CaseInsensitiveComparison.Comparer);
            }

            public string Name { get; }

            public IDictionary<string, string> Properties { get; }
        }
    }
}
