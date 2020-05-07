// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.AnalyzerConfig;
using AnalyzerOptions = System.Collections.Immutable.ImmutableDictionary<string, string>;
using TreeOptions = System.Collections.Immutable.ImmutableDictionary<string, Microsoft.CodeAnalysis.ReportDiagnostic>;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a set of <see cref="AnalyzerConfig"/>, and can compute the effective analyzer options for a given source file. This is used to
    /// collect all the <see cref="AnalyzerConfig"/> files for that would apply to a compilation.
    /// </summary>
    public sealed class AnalyzerConfigSet
    {
        /// <summary>
        /// The list of <see cref="AnalyzerConfig" />s in this set. This list has been sorted per <see cref="AnalyzerConfig.DirectoryLengthComparer"/>.
        /// </summary>
        private readonly ImmutableArray<AnalyzerConfig> _analyzerConfigs;

        private readonly GlobalAnalyzerConfig? _globalConfig;

        /// <summary>
        /// <see cref="SectionNameMatcher"/>s for each section. The entries in the outer array correspond to entries in <see cref="_analyzerConfigs"/>, and each inner array
        /// corresponds to each <see cref="AnalyzerConfig.NamedSections"/>.
        /// </summary>
        private readonly ImmutableArray<ImmutableArray<SectionNameMatcher?>> _analyzerMatchers;

        // PERF: diagnostic IDs will appear in the output options for every syntax tree in
        // the solution. We share string instances for each diagnostic ID to avoid creating
        // excess strings
        private readonly ConcurrentDictionary<ReadOnlyMemory<char>, string> _diagnosticIdCache =
            new ConcurrentDictionary<ReadOnlyMemory<char>, string>(CharMemoryEqualityComparer.Instance);

        // PERF: Most files will probably have the same options, so share the dictionary instances
        private readonly ConcurrentCache<List<Section>, AnalyzerConfigOptionsResult> _optionsCache =
            new ConcurrentCache<List<Section>, AnalyzerConfigOptionsResult>(50, SequenceEqualComparer.Instance); // arbitrary size

        private readonly ObjectPool<TreeOptions.Builder> _treeOptionsPool =
            new ObjectPool<TreeOptions.Builder>(() => ImmutableDictionary.CreateBuilder<string, ReportDiagnostic>(Section.PropertiesKeyComparer));

        private readonly ObjectPool<AnalyzerOptions.Builder> _analyzerOptionsPool =
            new ObjectPool<AnalyzerOptions.Builder>(() => ImmutableDictionary.CreateBuilder<string, string>(Section.PropertiesKeyComparer));

        private readonly ObjectPool<List<Section>> _sectionKeyPool = new ObjectPool<List<Section>>(() => new List<Section>());

        private sealed class SequenceEqualComparer : IEqualityComparer<List<Section>>
        {
            public static SequenceEqualComparer Instance { get; } = new SequenceEqualComparer();

            public bool Equals([AllowNull] List<Section> x, [AllowNull] List<Section> y)
            {
                if (x is null || y is null)
                {
                    return x is null && y is null;
                }

                if (x.Count != y.Count)
                {
                    return false;
                }

                for (int i = 0; i < x.Count; i++)
                {
                    if (!ReferenceEquals(x[i], y[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(List<Section> obj) => Hash.CombineValues(obj);
        }

        private readonly static DiagnosticDescriptor InvalidAnalyzerConfigSeverityDescriptor
            = new DiagnosticDescriptor(
                "InvalidSeverityInAnalyzerConfig",
                CodeAnalysisResources.WRN_InvalidSeverityInAnalyzerConfig_Title,
                CodeAnalysisResources.WRN_InvalidSeverityInAnalyzerConfig,
                "AnalyzerConfig",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public static AnalyzerConfigSet Create<TList>(TList analyzerConfigs) where TList : IReadOnlyCollection<AnalyzerConfig>
        {
            return Create(analyzerConfigs, out _);
        }

        public static AnalyzerConfigSet Create<TList>(TList analyzerConfigs, out ImmutableArray<AnalyzerUnsetKey> unsetGlobalKeys) where TList : IReadOnlyCollection<AnalyzerConfig>
        {
            var sortedAnalyzerConfigs = ArrayBuilder<AnalyzerConfig>.GetInstance(analyzerConfigs.Count);
            sortedAnalyzerConfigs.AddRange(analyzerConfigs);
            sortedAnalyzerConfigs.Sort(AnalyzerConfig.DirectoryLengthComparer);

            var globalConfig = GlobalAnalyzerConfigBuilder.MergeGlobalConfigs(sortedAnalyzerConfigs, out unsetGlobalKeys);
            return new AnalyzerConfigSet(sortedAnalyzerConfigs.ToImmutableAndFree(), globalConfig);
        }

        private AnalyzerConfigSet(ImmutableArray<AnalyzerConfig> analyzerConfigs, GlobalAnalyzerConfig? globalConfig)
        {
            _analyzerConfigs = analyzerConfigs;
            _globalConfig = globalConfig;

            var allMatchers = ArrayBuilder<ImmutableArray<SectionNameMatcher?>>.GetInstance(_analyzerConfigs.Length);

            foreach (var config in _analyzerConfigs)
            {
                // Create an array of regexes with each entry corresponding to the same index
                // in <see cref="EditorConfig.NamedSections"/>.
                var builder = ArrayBuilder<SectionNameMatcher?>.GetInstance(config.NamedSections.Length);
                foreach (var section in config.NamedSections)
                {
                    SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher(section.Name);
                    builder.Add(matcher);
                }

                Debug.Assert(builder.Count == config.NamedSections.Length);

                allMatchers.Add(builder.ToImmutableAndFree());
            }

            Debug.Assert(allMatchers.Count == _analyzerConfigs.Length);

            _analyzerMatchers = allMatchers.ToImmutableAndFree();
        }

        /// <summary>
        /// Returns a <see cref="AnalyzerConfigOptionsResult"/> for a source file. This computes which <see cref="AnalyzerConfig"/> rules applies to this file, and correctly applies
        /// precedence rules if there are multiple rules for the same file.
        /// </summary>
        /// <param name="sourcePath">The path to a file such as a source file or additional file. Must be non-null.</param>
        /// <remarks>This method is safe to call from multiple threads.</remarks>
        public AnalyzerConfigOptionsResult GetOptionsForSourcePath(string sourcePath)
        {
            if (sourcePath == null)
            {
                throw new ArgumentNullException(nameof(sourcePath));
            }

            var sectionKey = _sectionKeyPool.Allocate();

            var normalizedPath = PathUtilities.NormalizeWithForwardSlash(sourcePath);

            // If we have a global config, add any sections that match the full path 
            if (_globalConfig is object)
            {
                foreach (var section in _globalConfig.NamedSections)
                {
                    if (normalizedPath.Equals(section.Name, Section.NameComparer))
                    {
                        sectionKey.Add(section);
                    }
                }
            }

            // The editorconfig paths are sorted from shortest to longest, so matches
            // are resolved from most nested to least nested, where last setting wins
            for (int analyzerConfigIndex = 0; analyzerConfigIndex < _analyzerConfigs.Length; analyzerConfigIndex++)
            {
                var config = _analyzerConfigs[analyzerConfigIndex];

                if (normalizedPath.StartsWith(config.NormalizedDirectory, StringComparison.Ordinal))
                {
                    // If this config is a root config, then clear earlier options since they don't apply
                    // to this source file.
                    if (config.IsRoot)
                    {
                        sectionKey.Clear();
                    }

                    int dirLength = config.NormalizedDirectory.Length;
                    // Leave '/' if the normalized directory ends with a '/'. This can happen if
                    // we're in a root directory (e.g. '/' or 'Z:/'). The section matching
                    // always expects that the relative path start with a '/'. 
                    if (config.NormalizedDirectory[dirLength - 1] == '/')
                    {
                        dirLength--;
                    }
                    string relativePath = normalizedPath.Substring(dirLength);

                    ImmutableArray<SectionNameMatcher?> matchers = _analyzerMatchers[analyzerConfigIndex];
                    for (int sectionIndex = 0; sectionIndex < matchers.Length; sectionIndex++)
                    {
                        if (matchers[sectionIndex]?.IsMatch(relativePath) == true)
                        {
                            var section = config.NamedSections[sectionIndex];
                            sectionKey.Add(section);
                        }
                    }
                }
            }

            // Try to avoid creating extra dictionaries if we've already seen an options result with the
            // exact same options
            if (!_optionsCache.TryGetValue(sectionKey, out var result))
            {
                var treeOptionsBuilder = _treeOptionsPool.Allocate();
                var analyzerOptionsBuilder = _analyzerOptionsPool.Allocate();
                var diagnosticBuilder = ArrayBuilder<Diagnostic>.GetInstance();

                int sectionKeyIndex = 0;

                if (_globalConfig is object)
                {
                    addOptions(_globalConfig.GlobalSection,
                                treeOptionsBuilder,
                                analyzerOptionsBuilder,
                                diagnosticBuilder,
                                GlobalAnalyzerConfig.ConfigPath,
                                _diagnosticIdCache);

                    foreach (var configSection in _globalConfig.NamedSections)
                    {
                        if (sectionKey.Count > 0 && configSection == sectionKey[sectionKeyIndex])
                        {
                            addOptions(
                                sectionKey[sectionKeyIndex],
                                treeOptionsBuilder,
                                analyzerOptionsBuilder,
                                diagnosticBuilder,
                                GlobalAnalyzerConfig.ConfigPath,
                                _diagnosticIdCache);
                            sectionKeyIndex++;
                            if (sectionKeyIndex == sectionKey.Count)
                            {
                                break;
                            }
                        }
                    }
                }

                for (int analyzerConfigIndex = 0;
                    analyzerConfigIndex < _analyzerConfigs.Length && sectionKeyIndex < sectionKey.Count;
                    analyzerConfigIndex++)
                {
                    AnalyzerConfig config = _analyzerConfigs[analyzerConfigIndex];
                    ImmutableArray<SectionNameMatcher?> matchers = _analyzerMatchers[analyzerConfigIndex];
                    for (int matcherIndex = 0; matcherIndex < matchers.Length; matcherIndex++)
                    {
                        if (sectionKey[sectionKeyIndex] == config.NamedSections[matcherIndex])
                        {
                            addOptions(
                                sectionKey[sectionKeyIndex],
                                treeOptionsBuilder,
                                analyzerOptionsBuilder,
                                diagnosticBuilder,
                                config.PathToFile,
                                _diagnosticIdCache);
                            sectionKeyIndex++;
                            if (sectionKeyIndex == sectionKey.Count)
                            {
                                // Exit the inner 'for' loop now that work is done. The outer loop is handled by a
                                // top-level condition.
                                break;
                            }
                        }
                    }
                }

                result = new AnalyzerConfigOptionsResult(
                    treeOptionsBuilder.Count > 0 ? treeOptionsBuilder.ToImmutable() : SyntaxTree.EmptyDiagnosticOptions,
                    analyzerOptionsBuilder.Count > 0 ? analyzerOptionsBuilder.ToImmutable() : AnalyzerConfigOptions.EmptyDictionary,
                    diagnosticBuilder.ToImmutableAndFree());

                if (_optionsCache.TryAdd(sectionKey, result))
                {
                    // Release the pooled object to be used as a key
                    _sectionKeyPool.ForgetTrackedObject(sectionKey);
                }
                else
                {
                    freeKey(sectionKey, _sectionKeyPool);
                }

                treeOptionsBuilder.Clear();
                analyzerOptionsBuilder.Clear();
                _treeOptionsPool.Free(treeOptionsBuilder);
                _analyzerOptionsPool.Free(analyzerOptionsBuilder);
            }
            else
            {
                freeKey(sectionKey, _sectionKeyPool);
            }

            return result;

            static void freeKey(List<Section> sectionKey, ObjectPool<List<Section>> pool)
            {
                sectionKey.Clear();
                pool.Free(sectionKey);
            }

            static void addOptions(
                AnalyzerConfig.Section section,
                TreeOptions.Builder treeBuilder,
                AnalyzerOptions.Builder analyzerBuilder,
                ArrayBuilder<Diagnostic> diagnosticBuilder,
                string analyzerConfigPath,
                ConcurrentDictionary<ReadOnlyMemory<char>, string> diagIdCache)
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
                        ReadOnlyMemory<char> idSlice = key.AsMemory().Slice(DiagnosticOptionPrefix.Length, diagIdLength);
                        // PERF: this is similar to a double-checked locking pattern, and trying to fetch the ID first
                        // lets us avoid an allocation if the id has already been added
                        if (!diagIdCache.TryGetValue(idSlice, out var diagId))
                        {
                            // We use ReadOnlyMemory<char> to allow allocation-free lookups in the
                            // dictionary, but the actual keys stored in the dictionary are trimmed
                            // to avoid holding GC references to larger strings than necessary. The
                            // GetOrAdd APIs do not allow the key to be manipulated between lookup
                            // and insertion, so we separate the operations here in code.
                            diagId = idSlice.ToString();
                            diagId = diagIdCache.GetOrAdd(diagId.AsMemory(), diagId);
                        }

                        if (TryParseSeverity(value, out ReportDiagnostic severity))
                        {
                            treeBuilder[diagId] = severity;
                        }
                        else
                        {
                            diagnosticBuilder.Add(Diagnostic.Create(
                                InvalidAnalyzerConfigSeverityDescriptor,
                                Location.None,
                                diagId,
                                value,
                                analyzerConfigPath));
                        }
                    }
                    else
                    {
                        analyzerBuilder[key] = value;
                    }
                }
            }
        }

        internal static bool TryParseSeverity(string value, out ReportDiagnostic severity)
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            if (comparer.Equals(value, "default"))
            {
                severity = ReportDiagnostic.Default;
                return true;
            }
            else if (comparer.Equals(value, "error"))
            {
                severity = ReportDiagnostic.Error;
                return true;
            }
            else if (comparer.Equals(value, "warning"))
            {
                severity = ReportDiagnostic.Warn;
                return true;
            }
            else if (comparer.Equals(value, "suggestion"))
            {
                severity = ReportDiagnostic.Info;
                return true;
            }
            else if (comparer.Equals(value, "silent") || comparer.Equals(value, "refactoring"))
            {
                severity = ReportDiagnostic.Hidden;
                return true;
            }
            else if (comparer.Equals(value, "none"))
            {
                severity = ReportDiagnostic.Suppress;
                return true;
            }

            severity = default;
            return false;
        }
    }

    /// <summary>
    /// Builds a global analyzer config from a series of configs
    /// </summary>
    internal class GlobalAnalyzerConfigBuilder
    {
        internal const string GlobalKey = "is_global";

        private PooledDictionary<string, ImmutableDictionary<string, (string value, string configPath)>.Builder>? _values;
        private PooledDictionary<string, ImmutableDictionary<string, ArrayBuilder<string>>.Builder>? _duplicates;

        /// <summary>
        /// Merge any partial global configs into a single config file
        /// </summary>
        /// <param name="analyzerConfigs">An <see cref="ArrayBuilder{T}"/> of <see cref="AnalyzerConfig"/> containing a mix of regular and unmerged partial global configs</param>
        /// <returns>An <see cref="ImmutableArray{T}"/> of duplicate global keys that were unset</returns>
        internal static GlobalAnalyzerConfig? MergeGlobalConfigs(ArrayBuilder<AnalyzerConfig> analyzerConfigs, out ImmutableArray<AnalyzerUnsetKey> unsetAnalyzerKeys)
        {
            GlobalAnalyzerConfigBuilder globalAnalyzerConfigBuilder = new GlobalAnalyzerConfigBuilder();
            for (int i = 0; i < analyzerConfigs.Count; i++)
            {
                if (IsPartialGlobalConfig(analyzerConfigs[i]))
                {
                    globalAnalyzerConfigBuilder.MergeIntoGlobalConfig(analyzerConfigs[i]);
                    analyzerConfigs.RemoveAt(i);
                    i--;
                }
            }

            var globalConfig = globalAnalyzerConfigBuilder.Build(out unsetAnalyzerKeys);
            return globalConfig;
        }

        internal static bool IsPartialGlobalConfig(AnalyzerConfig config) => config.GlobalSection.Properties.ContainsKey(GlobalKey);

        private void MergeIntoGlobalConfig(AnalyzerConfig config)
        {
            if (_values is null)
            {
                _values = PooledDictionary<string, ImmutableDictionary<string, (string, string)>.Builder>.GetInstance();
                _duplicates = PooledDictionary<string, ImmutableDictionary<string, ArrayBuilder<string>>.Builder>.GetInstance();
            }

            MergeSection(config.PathToFile, config.GlobalSection);
            foreach (var section in config.NamedSections)
            {
                MergeSection(config.PathToFile, section);
            }
        }

        private GlobalAnalyzerConfig? Build(out ImmutableArray<AnalyzerUnsetKey> unsetKeys)
        {
            if (_values is null || _duplicates is null)
            {
                unsetKeys = ImmutableArray<AnalyzerUnsetKey>.Empty;
                return null;
            }

            unsetKeys = getUnsetKeys();

            Section globalSection = getSection(string.Empty);
            _values.Remove(string.Empty);

            ArrayBuilder<Section> namedSectionBuilder = new ArrayBuilder<Section>(_values.Count);
            foreach (var sectionName in _values.Keys)
            {
                namedSectionBuilder.Add(getSection(sectionName));
            }

            GlobalAnalyzerConfig globalConfig = new GlobalAnalyzerConfig(globalSection, namedSectionBuilder.ToImmutableAndFree());
            _values.Free();
            return globalConfig;

            ImmutableArray<AnalyzerUnsetKey> getUnsetKeys()
            {
                ArrayBuilder<AnalyzerUnsetKey> unsetKeys = ArrayBuilder<AnalyzerUnsetKey>.GetInstance();
                foreach ((var section, var keys) in _duplicates)
                {
                    bool isGlobalSection = string.IsNullOrWhiteSpace(section);
                    string sectionName = isGlobalSection ? "Global Section" : section;
                    foreach ((var keyName, var configs) in keys)
                    {
                        unsetKeys.Add(new AnalyzerUnsetKey(keyName, sectionName, isGlobalSection, configs.ToImmutableAndFree()));
                    }
                }
                _duplicates.Free();
                return unsetKeys.ToImmutableAndFree();
            }

            Section getSection(string sectionName)
            {
                var dict = _values[sectionName];
                var result = dict.ToImmutableDictionary(d => d.Key, d => d.Value.value, Section.PropertiesKeyComparer);
                return new Section(sectionName, result);
            }
        }

        private void MergeSection(string configPath, Section section)
        {
            Debug.Assert(_values is object);
            Debug.Assert(_duplicates is object);

            if (!_values.TryGetValue(section.Name, out var sectionDict))
            {
                sectionDict = ImmutableDictionary.CreateBuilder<string, (string, string)>(Section.NameEqualityComparer);
                _values.Add(section.Name, sectionDict);
            }

            var duplicateDict = _duplicates.ContainsKey(section.Name) ? _duplicates[section.Name] : null;
            foreach ((var key, var value) in section.Properties)
            {
                if (Section.PropertiesKeyComparer.Equals(key, GlobalKey))
                {
                    continue;
                }

                bool keyInSection = sectionDict.ContainsKey(key);
                bool keyDuplicated = duplicateDict?.ContainsKey(key) ?? false;

                // if this key is neither already present, or already duplicate, we can add it
                if (!keyInSection && !keyDuplicated)
                {
                    sectionDict.Add(key, (value, configPath));
                }
                else
                {
                    if (duplicateDict is null)
                    {
                        duplicateDict = ImmutableDictionary.CreateBuilder<string, ArrayBuilder<string>>(Section.NameEqualityComparer);
                        _duplicates.Add(section.Name, duplicateDict);
                    }

                    // record that this key is now a duplicate
                    ArrayBuilder<string> configList = keyDuplicated ? duplicateDict[key] : ArrayBuilder<string>.GetInstance();
                    configList.Add(configPath);
                    duplicateDict[key] = configList;

                    // if we'd previously added this key, remove it and remember the extra duplicate location
                    if (keyInSection)
                    {
                        var originalConfigPath = sectionDict[key].configPath;
                        sectionDict.Remove(key);
                        duplicateDict[key].Insert(0, originalConfigPath);
                    }
                }
            }
        }
    }
}
