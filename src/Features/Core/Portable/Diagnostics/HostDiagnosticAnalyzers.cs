// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class HostDiagnosticAnalyzers
    {
        /// <summary>
        /// Key is <see cref="AnalyzerReference.Id"/>.
        /// 
        /// We use the key to de-duplicate analyzer references if they are referenced from multiple places.
        /// </summary>
        private readonly Lazy<ImmutableDictionary<object, AnalyzerReference>> _hostAnalyzerReferencesMap;

        /// <summary>
        /// Key is the language the <see cref="DiagnosticAnalyzer"/> supports and key for the second map is analyzer reference identity and
        /// <see cref="DiagnosticAnalyzer"/> for that assembly reference.
        /// 
        /// Entry will be lazily filled in.
        /// </summary>
        private readonly ConcurrentDictionary<string, ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>>> _hostDiagnosticAnalyzersPerLanguageMap;

        /// <summary>
        /// Key is <see cref="AnalyzerReference.Id"/>.
        /// 
        /// Value is set of <see cref="DiagnosticAnalyzer"/> that belong to the <see cref="AnalyzerReference"/>.
        /// 
        /// We populate it lazily. otherwise, we will bring in all analyzers preemptively
        /// </summary>
        private readonly Lazy<ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>>> _lazyHostDiagnosticAnalyzersPerReferenceMap;

        /// <summary>
        /// Maps <see cref="LanguageNames"/> to compiler diagnostic analyzers.
        /// </summary>
        private ImmutableDictionary<string, DiagnosticAnalyzer> _compilerDiagnosticAnalyzerMap;

        public HostDiagnosticAnalyzers(Lazy<ImmutableArray<HostDiagnosticAnalyzerPackage>> hostAnalyzerPackages, IAnalyzerAssemblyLoader? hostAnalyzerAssemblyLoader, AbstractHostDiagnosticUpdateSource? hostDiagnosticUpdateSource, PrimaryWorkspace primaryWorkspace)
            : this(new Lazy<ImmutableArray<AnalyzerReference>>(() => CreateAnalyzerReferencesFromPackages(hostAnalyzerPackages.Value, new HostAnalyzerReferenceDiagnosticReporter(hostDiagnosticUpdateSource, primaryWorkspace), hostAnalyzerAssemblyLoader), isThreadSafe: true))
        {
        }

        internal HostDiagnosticAnalyzers(ImmutableArray<AnalyzerReference> hostAnalyzerReferences)
            : this(new Lazy<ImmutableArray<AnalyzerReference>>(() => hostAnalyzerReferences))
        {
        }

        private HostDiagnosticAnalyzers(Lazy<ImmutableArray<AnalyzerReference>> hostAnalyzerReferences)
        {
            _hostAnalyzerReferencesMap = new Lazy<ImmutableDictionary<object, AnalyzerReference>>(() => hostAnalyzerReferences.Value.IsDefaultOrEmpty ? ImmutableDictionary<object, AnalyzerReference>.Empty : CreateAnalyzerReferencesMap(hostAnalyzerReferences.Value), isThreadSafe: true);
            _hostDiagnosticAnalyzersPerLanguageMap = new ConcurrentDictionary<string, ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>>>(concurrencyLevel: 2, capacity: 2);
            _lazyHostDiagnosticAnalyzersPerReferenceMap = new Lazy<ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>>>(() => CreateDiagnosticAnalyzersPerReferenceMap(_hostAnalyzerReferencesMap.Value), isThreadSafe: true);

            _compilerDiagnosticAnalyzerMap = ImmutableDictionary<string, DiagnosticAnalyzer>.Empty;
        }

        /// <summary>
        /// It returns a map with <see cref="AnalyzerReference.Id"/> as key and <see cref="AnalyzerReference"/> as value
        /// </summary>
        public ImmutableDictionary<object, AnalyzerReference> GetHostAnalyzerReferencesMap()
            => _hostAnalyzerReferencesMap.Value;

        /// <summary>
        /// Get <see cref="AnalyzerReference"/> identity and <see cref="DiagnosticAnalyzer"/>s map for given <paramref name="language"/>
        /// </summary> 
        public ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> GetOrCreateHostDiagnosticAnalyzersPerReference(string language)
            => _hostDiagnosticAnalyzersPerLanguageMap.GetOrAdd(language, CreateHostDiagnosticAnalyzersAndBuildMap);

        public ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> GetDiagnosticDescriptorsPerReference(DiagnosticAnalyzerInfoCache infoCache)
        {
            return ConvertReferenceIdentityToName(
                CreateDiagnosticDescriptorsPerReference(infoCache, _lazyHostDiagnosticAnalyzersPerReferenceMap.Value),
                _hostAnalyzerReferencesMap.Value);
        }

        public ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> GetDiagnosticDescriptorsPerReference(DiagnosticAnalyzerInfoCache infoCache, Project project)
        {
            var descriptorPerReference = CreateDiagnosticDescriptorsPerReference(infoCache, CreateDiagnosticAnalyzersPerReference(project));
            var map = _hostAnalyzerReferencesMap.Value.AddRange(CreateProjectAnalyzerReferencesMap(project));
            return ConvertReferenceIdentityToName(descriptorPerReference, map);
        }

        private static ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> ConvertReferenceIdentityToName(
            ImmutableDictionary<object, ImmutableArray<DiagnosticDescriptor>> descriptorsPerReference,
            ImmutableDictionary<object, AnalyzerReference> map)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<DiagnosticDescriptor>>();

            foreach (var (id, descriptors) in descriptorsPerReference)
            {
                if (!map.TryGetValue(id, out var reference) || reference == null)
                {
                    continue;
                }

                var displayName = reference.Display ?? FeaturesResources.Unknown;

                // if there are duplicates, merge descriptors
                if (builder.TryGetValue(displayName, out var existing))
                {
                    builder[displayName] = existing.AddRange(descriptors);
                    continue;
                }

                builder.Add(displayName, descriptors);
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Create <see cref="AnalyzerReference"/> identity and <see cref="DiagnosticAnalyzer"/>s map for given <paramref name="project"/> that
        /// includes both host and project analyzers
        /// </summary>
        public ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> CreateDiagnosticAnalyzersPerReference(Project project)
        {
            var hostAnalyzerReferences = GetOrCreateHostDiagnosticAnalyzersPerReference(project.Language);
            var projectAnalyzerReferences = CreateProjectDiagnosticAnalyzersPerReference(project);

            return MergeDiagnosticAnalyzerMap(hostAnalyzerReferences, projectAnalyzerReferences);
        }

        /// <summary>
        /// Create <see cref="AnalyzerReference"/> identity and <see cref="DiagnosticAnalyzer"/>s map for given <paramref name="project"/> that
        /// has only project analyzers
        /// </summary>
        public ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> CreateProjectDiagnosticAnalyzersPerReference(Project project)
            => CreateDiagnosticAnalyzersPerReferenceMap(CreateProjectAnalyzerReferencesMap(project), project.Language);

        /// <summary>
        /// Return compiler <see cref="DiagnosticAnalyzer"/> for the given language.
        /// </summary>
        public DiagnosticAnalyzer? GetCompilerDiagnosticAnalyzer(string language)
        {
            _ = GetOrCreateHostDiagnosticAnalyzersPerReference(language);
            if (_compilerDiagnosticAnalyzerMap.TryGetValue(language, out var compilerAnalyzer))
            {
                return compilerAnalyzer;
            }

            return null;
        }

        private ImmutableDictionary<object, AnalyzerReference> CreateProjectAnalyzerReferencesMap(Project project)
            => CreateAnalyzerReferencesMap(project.AnalyzerReferences.Where(reference => !_hostAnalyzerReferencesMap.Value.ContainsKey(reference.Id)));

        private ImmutableDictionary<object, ImmutableArray<DiagnosticDescriptor>> CreateDiagnosticDescriptorsPerReference(
            DiagnosticAnalyzerInfoCache infoCache,
            ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> analyzersMap)
        {
            var builder = ImmutableDictionary.CreateBuilder<object, ImmutableArray<DiagnosticDescriptor>>();
            foreach (var (referenceId, analyzers) in analyzersMap)
            {
                var descriptors = ImmutableArray.CreateBuilder<DiagnosticDescriptor>();
                foreach (var analyzer in analyzers)
                {
                    // given map should be in good shape. no duplication. no null and etc
                    descriptors.AddRange(infoCache.GetDiagnosticDescriptors(analyzer));
                }

                // there can't be duplication since _hostAnalyzerReferenceMap is already de-duplicated.
                builder.Add(referenceId, descriptors.ToImmutable());
            }

            return builder.ToImmutable();
        }

        private ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> CreateHostDiagnosticAnalyzersAndBuildMap(string language)
        {
            Contract.ThrowIfNull(language);

            var builder = ImmutableDictionary.CreateBuilder<object, ImmutableArray<DiagnosticAnalyzer>>();
            foreach (var (referenceIdentity, reference) in _hostAnalyzerReferencesMap.Value)
            {
                var analyzers = reference.GetAnalyzers(language);
                if (analyzers.Length == 0)
                {
                    continue;
                }

                UpdateCompilerAnalyzerMapIfNeeded(language, analyzers);

                // there can't be duplication since _hostAnalyzerReferenceMap is already de-duplicated.
                builder.Add(referenceIdentity, analyzers);
            }

            return builder.ToImmutable();
        }

        private void UpdateCompilerAnalyzerMapIfNeeded(string language, ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            if (_compilerDiagnosticAnalyzerMap.ContainsKey(language))
            {
                return;
            }

            foreach (var analyzer in analyzers)
            {
                if (analyzer.IsCompilerAnalyzer())
                {
                    ImmutableInterlocked.GetOrAdd(ref _compilerDiagnosticAnalyzerMap, language, analyzer);
                    return;
                }
            }
        }

        private static ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> CreateDiagnosticAnalyzersPerReferenceMap(
            IDictionary<object, AnalyzerReference> analyzerReferencesMap, string? language = null)
        {
            var builder = ImmutableDictionary.CreateBuilder<object, ImmutableArray<DiagnosticAnalyzer>>();

            foreach (var reference in analyzerReferencesMap)
            {
                var analyzers = language == null ? reference.Value.GetAnalyzersForAllLanguages() : reference.Value.GetAnalyzers(language);
                if (analyzers.Length == 0)
                {
                    continue;
                }

                // input "analyzerReferencesMap" is a dictionary, so there will be no duplication here.
                builder.Add(reference.Key, analyzers.WhereNotNull().ToImmutableArray());
            }

            return builder.ToImmutable();
        }

        private static ImmutableDictionary<object, AnalyzerReference> CreateAnalyzerReferencesMap(IEnumerable<AnalyzerReference> analyzerReferences)
        {
            var builder = ImmutableDictionary.CreateBuilder<object, AnalyzerReference>();
            foreach (var reference in analyzerReferences)
            {
                var key = reference.Id;

                // filter out duplicated analyzer reference
                if (builder.ContainsKey(key))
                {
                    continue;
                }

                builder.Add(key, reference);
            }

            return builder.ToImmutable();
        }

        private static ImmutableArray<AnalyzerReference> CreateAnalyzerReferencesFromPackages(
            ImmutableArray<HostDiagnosticAnalyzerPackage> analyzerPackages,
            HostAnalyzerReferenceDiagnosticReporter reporter,
            IAnalyzerAssemblyLoader? hostAnalyzerAssemblyLoader)
        {
            if (analyzerPackages.IsEmpty)
            {
                return ImmutableArray<AnalyzerReference>.Empty;
            }

            Contract.ThrowIfNull(hostAnalyzerAssemblyLoader);

            var analyzerAssemblies = analyzerPackages.SelectMany(p => p.Assemblies);

            var builder = ImmutableArray.CreateBuilder<AnalyzerReference>();
            foreach (var analyzerAssembly in analyzerAssemblies.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var reference = new AnalyzerFileReference(analyzerAssembly, hostAnalyzerAssemblyLoader);
                reference.AnalyzerLoadFailed += reporter.OnAnalyzerLoadFailed;

                builder.Add(reference);
            }

            LogWorkspaceAnalyzerCount(builder.Count);
            return builder.ToImmutable();
        }

        private static void LogWorkspaceAnalyzerCount(int analyzerCount)
        {
            Logger.Log(FunctionId.DiagnosticAnalyzerService_Analyzers, KeyValueLogMessage.Create(m =>
            {
                m["AnalyzerCount"] = analyzerCount;
            }));
        }

        private static ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> MergeDiagnosticAnalyzerMap(
            ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> map1, ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> map2)
        {
            var current = map1;
            var seen = new HashSet<DiagnosticAnalyzer>(map1.Values.SelectMany(v => v));

            foreach (var kv in map2)
            {
                var referenceIdentity = kv.Key;
                var analyzers = kv.Value;

                if (map1.ContainsKey(referenceIdentity))
                {
                    continue;
                }

                current = current.Add(referenceIdentity, analyzers.Where(a => seen.Add(a)).ToImmutableArray());
            }

            return current;
        }

        private class HostAnalyzerReferenceDiagnosticReporter
        {
            private readonly AbstractHostDiagnosticUpdateSource? _hostUpdateSource;
            private readonly PrimaryWorkspace _primaryWorkspace;

            public HostAnalyzerReferenceDiagnosticReporter(AbstractHostDiagnosticUpdateSource? hostUpdateSource, PrimaryWorkspace primaryWorkspace)
            {
                _hostUpdateSource = hostUpdateSource;
                _primaryWorkspace = primaryWorkspace;
            }

            public void OnAnalyzerLoadFailed(object? sender, AnalyzerLoadFailureEventArgs e)
            {
                if (!(sender is AnalyzerFileReference reference))
                {
                    return;
                }

                var diagnostic = AnalyzerHelper.CreateAnalyzerLoadFailureDiagnostic(e, reference.FullPath, projectId: null, language: null);

                // diagnostic from host analyzer can never go away
                var args = DiagnosticsUpdatedArgs.DiagnosticsCreated(
                    id: Tuple.Create(this, reference.FullPath, e.ErrorCode, e.TypeName),
                    workspace: _primaryWorkspace.Workspace,
                    solution: null,
                    projectId: null,
                    documentId: null,
                    diagnostics: ImmutableArray.Create<DiagnosticData>(diagnostic));

                // this can be null in test. but in product code, this should never be null.
                _hostUpdateSource?.RaiseDiagnosticsUpdated(args);
            }
        }
    }
}
