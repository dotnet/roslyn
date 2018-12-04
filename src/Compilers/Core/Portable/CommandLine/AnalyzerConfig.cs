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

        private readonly static DiagnosticDescriptor InvalidAnalyzerConfigSeverityDescriptor
            = new DiagnosticDescriptor(
                "InvalidSeverityInAnalyzerConfig",
                CodeAnalysisResources.WRN_InvalidSeverityInAnalyzerConfig_Title,
                CodeAnalysisResources.WRN_InvalidSeverityInAnalyzerConfig,
                "AnalyzerConfig",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public Section GlobalSection { get; }

        /// <summary>
        /// The directory the editorconfig was contained in, with all directory separators
        /// replaced with '/'.
        /// </summary>
        public string NormalizedDirectory { get; }

        /// <summary>
        /// The path passed to <see cref="AnalyzerConfig.Parse(string, string)"/> during construction.
        /// </summary>
        public string PathToFile { get; }

        /// <summary>
        /// Comparer for sorting <see cref="AnalyzerConfig"/> files by <see cref="NormalizedDirectory"/> path length.
        /// </summary>
        internal static Comparer<AnalyzerConfig> DirectoryLengthComparer { get; } = Comparer<AnalyzerConfig>.Create(
            (e1, e2) => e1.NormalizedDirectory.Length.CompareTo(e2.NormalizedDirectory.Length));

        public ImmutableArray<Section> NamedSections { get; }

        /// <summary>
        /// Gets whether this editorconfig is a topmost editorconfig.
        /// </summary>
        public bool IsRoot => GlobalSection.Properties.TryGetValue("root", out string val) && val == "true";

        /// <summary>
        /// Takes a list of paths to source files and a list of AnalyzeConfigs and produces a
        /// resultant dictionary of diagnostic configurations for each of the source paths.
        /// Source paths are matched by checking if they are members of the language recognized by
        /// <see cref="AnalyzerConfig.Section.Name"/>s.
        /// </summary>
        /// <param name="sourcePaths">
        /// Absolute, normalized paths to source files. These paths are expected to be normalized
        /// using the same mechanism used to normalize the path passed to the path parameter of
        /// <see cref="AnalyzerConfig.Parse(string, string)"/>. Source files will only be considered
        /// applicable for a given <see cref="AnalyzerConfig"/> if the config path is an ordinal
        /// prefix of the source path.
        /// </param>
        /// <param name="analyzerConfigs">
        /// Parsed AnalyzerConfig files. The <see cref="AnalyzerConfig.NormalizedDirectory"/>
        /// must be an ordinal prefix of a source file path to be considered applicable.
        /// </param>
        /// <returns>
        /// Diagnostic and analyzer option arrays, where the array indices correspond
        /// to the options for the source path at the same index.
        /// </returns>
        public static AnalyzerConfigOptionsResult GetAnalyzerConfigOptions<TStringList, TACList>(
            TStringList sourcePaths,
            TACList analyzerConfigs)
            where TStringList : IReadOnlyList<string>
            where TACList : IReadOnlyList<AnalyzerConfig>
        {
            // Since paths are compared as ordinal string comparisons, the least nested
            // file will always be the first in sort order
            if (!analyzerConfigs.IsSorted(DirectoryLengthComparer))
            {
                throw new ArgumentException(
                    "Analyzer config files must be sorted from shortest to longest path",
                    nameof(analyzerConfigs));
            }

            var diagnosticBuilder = ArrayBuilder<Diagnostic>.GetInstance();
            var allTreeOptions = ArrayBuilder<TreeOptions>.GetInstance(sourcePaths.Count);
            var allAnalyzerOptions = ArrayBuilder<AnalyzerOptions>.GetInstance(sourcePaths.Count);

            var allRegexes = PooledDictionary<AnalyzerConfig, ImmutableArray<Regex>>.GetInstance();
            foreach (var config in analyzerConfigs)
            {
                // Create an array of regexes with each entry corresponding to the same index
                // in <see cref="EditorConfig.NamedSections"/>.
                var builder = ArrayBuilder<Regex>.GetInstance(config.NamedSections.Length);
                foreach (var section in config.NamedSections)
                {
                    string regex = AnalyzerConfig.TryCompileSectionNameToRegEx(section.Name);
                    builder.Add(new Regex(regex, RegexOptions.Compiled));
                }

                Debug.Assert(builder.Count == config.NamedSections.Length);

                allRegexes.Add(config, builder.ToImmutableAndFree());
            }

            Debug.Assert(allRegexes.Count == analyzerConfigs.Count);

            var treeOptionsBuilder = ImmutableDictionary.CreateBuilder<string, ReportDiagnostic>(
                CaseInsensitiveComparison.Comparer);
            var analyzerOptionsBuilder = ImmutableDictionary.CreateBuilder<string, string>(
                CaseInsensitiveComparison.Comparer);
            foreach (var sourceFile in sourcePaths)
            {
                var normalizedPath = PathUtilities.NormalizeWithForwardSlash(sourceFile);
                // The editorconfig paths are sorted from shortest to longest, so matches
                // are resolved from most nested to least nested, where last setting wins
                foreach (var config in analyzerConfigs)
                {
                    if (normalizedPath.StartsWith(config.NormalizedDirectory, StringComparison.Ordinal))
                    {
                        int dirLength = config.NormalizedDirectory.Length;
                        // Leave '/' if the normalized directory ends with a '/'. This can happen if
                        // we're in a root directory (e.g. '/' or 'Z:/'). The section matching
                        // always expects that the relative path start with a '/'. 
                        if (config.NormalizedDirectory[dirLength - 1] == '/')
                        {
                            dirLength--;
                        }
                        string relativePath = normalizedPath.Substring(dirLength);

                        ImmutableArray<Regex> regexes = allRegexes[config];
                        for (int sectionIndex = 0; sectionIndex < regexes.Length; sectionIndex++)
                        {
                            if (regexes[sectionIndex].IsMatch(relativePath))
                            {
                                var section = config.NamedSections[sectionIndex];
                                addOptions(section, treeOptionsBuilder, analyzerOptionsBuilder, config.PathToFile);
                            }
                        }
                    }
                }

                // Avoid extra allocations by using null.
                allTreeOptions.Add(treeOptionsBuilder.Count > 0 ? treeOptionsBuilder.ToImmutable() : null);
                allAnalyzerOptions.Add(analyzerOptionsBuilder.Count > 0 ? analyzerOptionsBuilder.ToImmutable() : null);
                treeOptionsBuilder.Clear();
                analyzerOptionsBuilder.Clear();
            }

            allRegexes.Free();
            Debug.Assert(allTreeOptions.Count == allAnalyzerOptions.Count);
            Debug.Assert(allTreeOptions.Count == sourcePaths.Count);

            return new AnalyzerConfigOptionsResult(
                allTreeOptions.ToImmutableAndFree(),
                allAnalyzerOptions.ToImmutableAndFree(),
                diagnosticBuilder.ToImmutableAndFree());

            void addOptions(
                AnalyzerConfig.Section section,
                TreeOptions.Builder treeBuilder,
                AnalyzerOptions.Builder analyzerBuilder,
                string analyzerConfigPath)
            {
                const string DiagnosticOptionPrefix = "dotnet_diagnostic.";
                const string DiagnosticOptionSuffix = ".severity";

                foreach (var (key, value) in section.Properties)
                {
                    // Keys are lowercased in editorconfig parsing
                    int diagIdLength = -1;
                    if (key.StartsWith(DiagnosticOptionPrefix, StringComparison.Ordinal) &&
                        key.EndsWith(DiagnosticOptionSuffix, StringComparison.Ordinal))
                    {
                        diagIdLength = key.Length - (DiagnosticOptionPrefix.Length + DiagnosticOptionSuffix.Length);
                    }

                    if (diagIdLength >= 0)
                    {
                        var diagId = key.Substring(
                            DiagnosticOptionPrefix.Length,
                            diagIdLength);

                        ReportDiagnostic? severity;
                        var comparer = StringComparer.OrdinalIgnoreCase;
                        if (comparer.Equals(value, "default"))
                        {
                            severity = ReportDiagnostic.Default;
                        }
                        else if (comparer.Equals(value, "error"))
                        {
                            severity = ReportDiagnostic.Error;
                        }
                        else if (comparer.Equals(value, "warn"))
                        {
                            severity = ReportDiagnostic.Warn;
                        }
                        else if (comparer.Equals(value, "info"))
                        {
                            severity = ReportDiagnostic.Info;
                        }
                        else if (comparer.Equals(value, "hidden"))
                        {
                            severity = ReportDiagnostic.Hidden;
                        }
                        else if (comparer.Equals(value, "suppress"))
                        {
                            severity = ReportDiagnostic.Suppress;
                        }
                        else
                        {
                            severity = null;
                            diagnosticBuilder.Add(Diagnostic.Create(
                                InvalidAnalyzerConfigSeverityDescriptor,
                                Location.None,
                                diagId,
                                value,
                                analyzerConfigPath));
                        }

                        if (severity.HasValue)
                        {
                            treeBuilder[diagId] = severity.GetValueOrDefault();
                        }
                    }
                    else
                    {
                        analyzerBuilder[key] = value;
                    }
                }
            }
        }

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
        public sealed class Section
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
            /// lower-cased if the value appears in <see cref="AnalyzerConfig.ReservedValues" />
            /// or if the corresponding key is in <see cref="AnalyzerConfig.ReservedKeys" />. Otherwise,
            /// the values are the literal values present in the source.
            /// </summary>
            public ImmutableDictionary<string, string> Properties { get; }
        }
    }
}
