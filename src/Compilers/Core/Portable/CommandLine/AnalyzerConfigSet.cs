// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
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
        /// This does not include any of the global configs that were merged into <see cref="_globalConfig"/>.
        /// </summary>
        private readonly ImmutableArray<AnalyzerConfig> _analyzerConfigs;

        private readonly GlobalAnalyzerConfig _globalConfig;

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

        private StrongBox<AnalyzerConfigOptionsResult>? _lazyConfigOptions;

        private sealed class SequenceEqualComparer : IEqualityComparer<List<Section>>
        {
            public static SequenceEqualComparer Instance { get; } = new SequenceEqualComparer();

            public bool Equals(List<Section>? x, List<Section>? y)
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

        private static readonly DiagnosticDescriptor InvalidAnalyzerConfigSeverityDescriptor
            = new DiagnosticDescriptor(
                "InvalidSeverityInAnalyzerConfig",
                CodeAnalysisResources.WRN_InvalidSeverityInAnalyzerConfig_Title,
                CodeAnalysisResources.WRN_InvalidSeverityInAnalyzerConfig,
                "AnalyzerConfig",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor MultipleGlobalAnalyzerKeysDescriptor
            = new DiagnosticDescriptor(
                "MultipleGlobalAnalyzerKeys",
                CodeAnalysisResources.WRN_MultipleGlobalAnalyzerKeys_Title,
                CodeAnalysisResources.WRN_MultipleGlobalAnalyzerKeys,
                "AnalyzerConfig",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor InvalidGlobalAnalyzerSectionDescriptor
            = new DiagnosticDescriptor(
                "InvalidGlobalSectionName",
                CodeAnalysisResources.WRN_InvalidGlobalSectionName_Title,
                CodeAnalysisResources.WRN_InvalidGlobalSectionName,
                "AnalyzerConfig",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public static AnalyzerConfigSet Create<TList>(TList analyzerConfigs) where TList : IReadOnlyCollection<AnalyzerConfig>
        {
            return Create(analyzerConfigs, out _);
        }

        public static AnalyzerConfigSet Create<TList>(TList analyzerConfigs, out ImmutableArray<Diagnostic> diagnostics) where TList : IReadOnlyCollection<AnalyzerConfig>
        {
            var sortedAnalyzerConfigs = ArrayBuilder<AnalyzerConfig>.GetInstance(analyzerConfigs.Count);
            sortedAnalyzerConfigs.AddRange(analyzerConfigs);
            sortedAnalyzerConfigs.Sort(AnalyzerConfig.DirectoryLengthComparer);

            var globalConfig = MergeGlobalConfigs(sortedAnalyzerConfigs, out diagnostics);
            return new AnalyzerConfigSet(sortedAnalyzerConfigs.ToImmutableAndFree(), globalConfig);
        }

        private AnalyzerConfigSet(ImmutableArray<AnalyzerConfig> analyzerConfigs, GlobalAnalyzerConfig globalConfig)
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
        /// Gets an <see cref="AnalyzerConfigOptionsResult"/> that contain the options that apply globally
        /// </summary>
        public AnalyzerConfigOptionsResult GlobalConfigOptions
        {
            get
            {
                if (_lazyConfigOptions is null)
                {
                    Interlocked.CompareExchange(
                        ref _lazyConfigOptions,
                        new StrongBox<AnalyzerConfigOptionsResult>(ParseGlobalConfigOptions()),
                        null);
                }

                return _lazyConfigOptions.Value;
            }
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
            normalizedPath = PathUtilities.ExpandAbsolutePathWithRelativeParts(normalizedPath);

            // If we have a global config, add any sections that match the full path. We can have at most one section since
            // we would have merged them earlier.
            foreach (var section in _globalConfig.NamedSections)
            {
                if (normalizedPath.Equals(section.Name, Section.NameComparer))
                {
                    sectionKey.Add(section);
                    break;
                }
            }
            int globalConfigOptionsCount = sectionKey.Count;

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
                        sectionKey.RemoveRange(globalConfigOptionsCount, sectionKey.Count - globalConfigOptionsCount);
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

                analyzerOptionsBuilder.AddRange(GlobalConfigOptions.AnalyzerOptions);
                foreach (var configSection in _globalConfig.NamedSections)
                {
                    if (sectionKey.Count > 0 && configSection == sectionKey[sectionKeyIndex])
                    {
                        ParseSectionOptions(
                            sectionKey[sectionKeyIndex],
                            treeOptionsBuilder,
                            analyzerOptionsBuilder,
                            diagnosticBuilder,
                            GlobalAnalyzerConfigBuilder.GlobalConfigPath,
                            _diagnosticIdCache);
                        sectionKeyIndex++;
                        if (sectionKeyIndex == sectionKey.Count)
                        {
                            break;
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
                            ParseSectionOptions(
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
                    analyzerOptionsBuilder.Count > 0 ? analyzerOptionsBuilder.ToImmutable() : DictionaryAnalyzerConfigOptions.EmptyDictionary,
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

        private AnalyzerConfigOptionsResult ParseGlobalConfigOptions()
        {
            var treeOptionsBuilder = _treeOptionsPool.Allocate();
            var analyzerOptionsBuilder = _analyzerOptionsPool.Allocate();
            var diagnosticBuilder = ArrayBuilder<Diagnostic>.GetInstance();

            ParseSectionOptions(_globalConfig.GlobalSection,
                        treeOptionsBuilder,
                        analyzerOptionsBuilder,
                        diagnosticBuilder,
                        GlobalAnalyzerConfigBuilder.GlobalConfigPath,
                        _diagnosticIdCache);

            var options = new AnalyzerConfigOptionsResult(
                treeOptionsBuilder.ToImmutable(),
                analyzerOptionsBuilder.ToImmutable(),
                diagnosticBuilder.ToImmutableAndFree());

            treeOptionsBuilder.Clear();
            analyzerOptionsBuilder.Clear();
            _treeOptionsPool.Free(treeOptionsBuilder);
            _analyzerOptionsPool.Free(analyzerOptionsBuilder);

            return options;
        }

        private static void ParseSectionOptions(Section section, TreeOptions.Builder treeBuilder, AnalyzerOptions.Builder analyzerBuilder, ArrayBuilder<Diagnostic> diagnosticBuilder, string analyzerConfigPath, ConcurrentDictionary<ReadOnlyMemory<char>, string> diagIdCache)
        {
            const string diagnosticOptionPrefix = "dotnet_diagnostic.";
            const string diagnosticOptionSuffix = ".severity";

            foreach (var (key, value) in section.Properties)
            {
                // Keys are lowercased in editorconfig parsing
                int diagIdLength = -1;
                if (key.StartsWith(diagnosticOptionPrefix, StringComparison.Ordinal) &&
                    key.EndsWith(diagnosticOptionSuffix, StringComparison.Ordinal))
                {
                    diagIdLength = key.Length - (diagnosticOptionPrefix.Length + diagnosticOptionSuffix.Length);
                }

                if (diagIdLength >= 0)
                {
                    ReadOnlyMemory<char> idSlice = key.AsMemory().Slice(diagnosticOptionPrefix.Length, diagIdLength);
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

        /// <summary>
        /// Merge any partial global configs into a single global config, and remove the partial configs
        /// </summary>
        /// <param name="analyzerConfigs">An <see cref="ArrayBuilder{T}"/> of <see cref="AnalyzerConfig"/> containing a mix of regular and unmerged partial global configs</param>
        /// <param name="diagnostics">Diagnostics produced during merge will be added to this bag</param>
        /// <returns>A <see cref="GlobalAnalyzerConfig" /> that contains the merged partial configs, or <c>null</c> if there were no partial configs</returns>
        internal static GlobalAnalyzerConfig MergeGlobalConfigs(ArrayBuilder<AnalyzerConfig> analyzerConfigs, out ImmutableArray<Diagnostic> diagnostics)
        {
            GlobalAnalyzerConfigBuilder globalAnalyzerConfigBuilder = new GlobalAnalyzerConfigBuilder();
            DiagnosticBag diagnosticBag = DiagnosticBag.GetInstance();
            for (int i = 0; i < analyzerConfigs.Count; i++)
            {
                if (analyzerConfigs[i].IsGlobal)
                {
                    globalAnalyzerConfigBuilder.MergeIntoGlobalConfig(analyzerConfigs[i], diagnosticBag);
                    analyzerConfigs.RemoveAt(i);
                    i--;
                }
            }

            var globalConfig = globalAnalyzerConfigBuilder.Build(diagnosticBag);
            diagnostics = diagnosticBag.ToReadOnlyAndFree();
            return globalConfig;
        }

        /// <summary>
        /// Builds a global analyzer config from a series of partial configs
        /// </summary>
        internal struct GlobalAnalyzerConfigBuilder
        {
            private ImmutableDictionary<string, ImmutableDictionary<string, (string value, string configPath, int globalLevel)>.Builder>.Builder? _values;
            private ImmutableDictionary<string, ImmutableDictionary<string, (int globalLevel, ArrayBuilder<string> configPaths)>.Builder>.Builder? _duplicates;

            internal const string GlobalConfigPath = "<Global Config>";
            internal const string GlobalSectionName = "Global Section";

            internal void MergeIntoGlobalConfig(AnalyzerConfig config, DiagnosticBag diagnostics)
            {
                if (_values is null)
                {
                    _values = ImmutableDictionary.CreateBuilder<string, ImmutableDictionary<string, (string, string, int)>.Builder>(Section.NameEqualityComparer);
                    _duplicates = ImmutableDictionary.CreateBuilder<string, ImmutableDictionary<string, (int, ArrayBuilder<string>)>.Builder>(Section.NameEqualityComparer);
                }

                MergeSection(config.PathToFile, config.GlobalSection, config.GlobalLevel, isGlobalSection: true);
                foreach (var section in config.NamedSections)
                {
                    if (IsAbsoluteEditorConfigPath(section.Name))
                    {
                        // Let's recreate the section with the name unescaped, since we can then properly merge and match it later
                        var unescapedSection = new Section(UnescapeSectionName(section.Name), section.Properties);

                        MergeSection(config.PathToFile, unescapedSection, config.GlobalLevel, isGlobalSection: false);
                    }
                    else
                    {
                        diagnostics.Add(Diagnostic.Create(
                            InvalidGlobalAnalyzerSectionDescriptor,
                            Location.None,
                            section.Name,
                            config.PathToFile));
                    }
                }
            }

            internal GlobalAnalyzerConfig Build(DiagnosticBag diagnostics)
            {
                if (_values is null || _duplicates is null)
                {
                    return new GlobalAnalyzerConfig(new Section(GlobalSectionName, AnalyzerOptions.Empty), ImmutableArray<Section>.Empty);
                }

                // issue diagnostics for any duplicate keys
                foreach ((var section, var keys) in _duplicates)
                {
                    bool isGlobalSection = string.IsNullOrWhiteSpace(section);
                    string sectionName = isGlobalSection ? GlobalSectionName : section;
                    foreach ((var keyName, (_, var configPaths)) in keys)
                    {
                        diagnostics.Add(Diagnostic.Create(
                             MultipleGlobalAnalyzerKeysDescriptor,
                             Location.None,
                             keyName,
                             sectionName,
                             string.Join(", ", configPaths)));
                    }
                }
                _duplicates = null;

                // gather the global and named sections
                Section globalSection = GetSection(string.Empty);
                _values.Remove(string.Empty);

                ArrayBuilder<Section> namedSectionBuilder = new ArrayBuilder<Section>(_values.Count);
                foreach (var sectionName in _values.Keys.Order())
                {
                    namedSectionBuilder.Add(GetSection(sectionName));
                }

                // create the global config
                GlobalAnalyzerConfig globalConfig = new GlobalAnalyzerConfig(globalSection, namedSectionBuilder.ToImmutableAndFree());
                _values = null;
                return globalConfig;
            }

            private Section GetSection(string sectionName)
            {
                Debug.Assert(_values is object);

                var dict = _values[sectionName];
                var result = dict.ToImmutableDictionary(d => d.Key, d => d.Value.value, Section.PropertiesKeyComparer);
                return new Section(sectionName, result);
            }

            private void MergeSection(string configPath, Section section, int globalLevel, bool isGlobalSection)
            {
                Debug.Assert(_values is object);
                Debug.Assert(_duplicates is object);

                if (!_values.TryGetValue(section.Name, out var sectionDict))
                {
                    sectionDict = ImmutableDictionary.CreateBuilder<string, (string, string, int)>(Section.PropertiesKeyComparer);
                    _values.Add(section.Name, sectionDict);
                }

                _duplicates.TryGetValue(section.Name, out var duplicateDict);
                foreach ((var key, var value) in section.Properties)
                {
                    if (isGlobalSection && (Section.PropertiesKeyComparer.Equals(key, GlobalKey) || Section.PropertiesKeyComparer.Equals(key, GlobalLevelKey)))
                    {
                        continue;
                    }

                    bool keyInSection = sectionDict.TryGetValue(key, out var sectionValue);

                    (int globalLevel, ArrayBuilder<string> configPaths) duplicateValue = default;
                    bool keyDuplicated = !keyInSection && duplicateDict?.TryGetValue(key, out duplicateValue) == true;

                    // if this key is neither already present, or already duplicate, we can add it	
                    if (!keyInSection && !keyDuplicated)
                    {
                        sectionDict.Add(key, (value, configPath, globalLevel));
                    }
                    else
                    {
                        int currentGlobalLevel = keyInSection ? sectionValue.globalLevel : duplicateValue.globalLevel;

                        // if this key overrides one we knew about previously, replace it
                        if (currentGlobalLevel < globalLevel)
                        {
                            sectionDict[key] = (value, configPath, globalLevel);
                            if (keyDuplicated)
                            {
                                duplicateDict!.Remove(key);
                            }
                        }
                        // this key conflicts with a previous one
                        else if (currentGlobalLevel == globalLevel)
                        {
                            if (duplicateDict is null)
                            {
                                duplicateDict = ImmutableDictionary.CreateBuilder<string, (int, ArrayBuilder<string>)>(Section.PropertiesKeyComparer);
                                _duplicates.Add(section.Name, duplicateDict);
                            }

                            // record that this key is now a duplicate
                            ArrayBuilder<string> configList = duplicateValue.configPaths ?? ArrayBuilder<string>.GetInstance();
                            configList.Add(configPath);
                            duplicateDict[key] = (globalLevel, configList);

                            // if we'd previously added this key, remove it and remember the extra duplicate location
                            if (keyInSection)
                            {
                                var originalEntry = sectionValue;
                                Debug.Assert(originalEntry.globalLevel == globalLevel);

                                sectionDict.Remove(key);
                                configList.Insert(0, originalEntry.configPath);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Represents a combined global analyzer config.
        /// </summary>
        /// <remarks>
        /// We parse all <see cref="AnalyzerConfig"/>s as individual files, according to the editorconfig spec.
        /// 
        /// However, when viewing the configs as an <see cref="AnalyzerConfigSet"/> if multiple files have the
        /// <c>is_global</c> property set to <c>true</c> we combine those files and treat them as a single 
        /// 'logical' global config file. This type represents that combined file. 
        /// </remarks>
        internal sealed class GlobalAnalyzerConfig
        {
            internal AnalyzerConfig.Section GlobalSection { get; }

            internal ImmutableArray<AnalyzerConfig.Section> NamedSections { get; }

            public GlobalAnalyzerConfig(AnalyzerConfig.Section globalSection, ImmutableArray<AnalyzerConfig.Section> namedSections)
            {
                GlobalSection = globalSection;
                NamedSections = namedSections;
            }
        }
    }
}
