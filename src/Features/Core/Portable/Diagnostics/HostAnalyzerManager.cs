// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Roslyn.Utilities;
using System.Runtime.CompilerServices;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// This owns every information about analyzer itself 
    /// such as <see cref="AnalyzerReference"/>, <see cref="DiagnosticAnalyzer"/> and <see cref="DiagnosticDescriptor"/>.
    /// 
    /// people should use this to get <see cref="DiagnosticAnalyzer"/>s or <see cref="DiagnosticDescriptor"/>s of a <see cref="AnalyzerReference"/>.
    /// this will do appropriate de-duplication and cache for those information.
    /// 
    /// this should be always thread-safe.
    /// </summary>
    internal sealed partial class HostAnalyzerManager
    {
        /// <summary>
        /// This contains vsix info on where <see cref="HostDiagnosticAnalyzerPackage"/> comes from.
        /// </summary>
        private readonly ImmutableArray<HostDiagnosticAnalyzerPackage> _hostDiagnosticAnalyzerPackages;

        /// <summary>
        /// Key is analyzer reference identity <see cref="GetAnalyzerReferenceIdentity(AnalyzerReference)"/>.
        /// 
        /// We use the key to de-duplicate analyzer references if they are referenced from multiple places.
        /// </summary>
        private readonly ImmutableDictionary<object, AnalyzerReference> _hostAnalyzerReferencesMap;

        /// <summary>
        /// Key is the language the <see cref="DiagnosticAnalyzer"/> supports and key for the second map is analyzer reference identity and
        /// <see cref="DiagnosticAnalyzer"/> for that assembly reference.
        /// 
        /// Entry will be lazily filled in.
        /// </summary>
        private readonly ConcurrentDictionary<string, ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>>> _hostDiagnosticAnalyzersPerLanguageMap;

        /// <summary>
        /// Key is analyzer reference identity <see cref="GetAnalyzerReferenceIdentity(AnalyzerReference)"/>.
        /// 
        /// Value is set of <see cref="DiagnosticAnalyzer"/> that belong to the <see cref="AnalyzerReference"/>.
        /// 
        /// We populate it lazily. otherwise, we will bring in all analyzers preemptively
        /// </summary>
        private readonly Lazy<ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>>> _lazyHostDiagnosticAnalyzersPerReferenceMap;

        /// <summary>
        /// Host diagnostic update source for analyzer host specific diagnostics.
        /// </summary>
        private readonly AbstractHostDiagnosticUpdateSource _hostDiagnosticUpdateSource;

        /// <summary>
        /// map to compiler diagnostic analyzer.
        /// </summary>
        private ImmutableDictionary<string, DiagnosticAnalyzer> _compilerDiagnosticAnalyzerMap;

        /// <summary>
        /// map from host diagnostic analyzer to package name it came from
        /// </summary>
        private ImmutableDictionary<DiagnosticAnalyzer, string> _hostDiagnosticAnalyzerPackageNameMap;

        /// <summary>
        /// map to compiler diagnostic analyzer descriptor.
        /// </summary>
        private ImmutableDictionary<DiagnosticAnalyzer, HashSet<string>> _compilerDiagnosticAnalyzerDescriptorMap;

        /// <summary>
        /// Cache from <see cref="DiagnosticAnalyzer"/> instance to its supported desciptors.
        /// </summary>
        private readonly ConditionalWeakTable<DiagnosticAnalyzer, IReadOnlyCollection<DiagnosticDescriptor>> _descriptorCache;

        public HostAnalyzerManager(IEnumerable<HostDiagnosticAnalyzerPackage> hostAnalyzerPackages, IAnalyzerAssemblyLoader hostAnalyzerAssemblyLoader, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource) :
            this(CreateAnalyzerReferencesFromPackages(hostAnalyzerPackages, new HostAnalyzerReferenceDiagnosticReporter(hostDiagnosticUpdateSource), hostAnalyzerAssemblyLoader),
                 hostAnalyzerPackages.ToImmutableArrayOrEmpty(), hostDiagnosticUpdateSource)
        {
        }

        public HostAnalyzerManager(ImmutableArray<AnalyzerReference> hostAnalyzerReferences, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource) :
            this(hostAnalyzerReferences, ImmutableArray<HostDiagnosticAnalyzerPackage>.Empty, hostDiagnosticUpdateSource)
        {
        }

        private HostAnalyzerManager(
            ImmutableArray<AnalyzerReference> hostAnalyzerReferences, ImmutableArray<HostDiagnosticAnalyzerPackage> hostAnalyzerPackages, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
        {
            _hostDiagnosticAnalyzerPackages = hostAnalyzerPackages;
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;

            _hostAnalyzerReferencesMap = hostAnalyzerReferences.IsDefault ? ImmutableDictionary<object, AnalyzerReference>.Empty : CreateAnalyzerReferencesMap(hostAnalyzerReferences);
            _hostDiagnosticAnalyzersPerLanguageMap = new ConcurrentDictionary<string, ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>>>(concurrencyLevel: 2, capacity: 2);
            _lazyHostDiagnosticAnalyzersPerReferenceMap = new Lazy<ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>>>(() => CreateDiagnosticAnalyzersPerReferenceMap(_hostAnalyzerReferencesMap), isThreadSafe: true);

            _compilerDiagnosticAnalyzerMap = ImmutableDictionary<string, DiagnosticAnalyzer>.Empty;
            _compilerDiagnosticAnalyzerDescriptorMap = ImmutableDictionary<DiagnosticAnalyzer, HashSet<string>>.Empty;
            _hostDiagnosticAnalyzerPackageNameMap = ImmutableDictionary<DiagnosticAnalyzer, string>.Empty;
            _descriptorCache = new ConditionalWeakTable<DiagnosticAnalyzer, IReadOnlyCollection<DiagnosticDescriptor>>();

            DiagnosticAnalyzerLogger.LogWorkspaceAnalyzers(hostAnalyzerReferences);
        }

        /// <summary>
        /// It returns a string that can be used as a way to de-duplicate <see cref="AnalyzerReference"/>s.
        /// </summary>
        public object GetAnalyzerReferenceIdentity(AnalyzerReference reference)
        {
            return reference.Id;
        }

        /// <summary>
        /// It returns a map with <see cref="AnalyzerReference.Id"/> as key and <see cref="AnalyzerReference"/> as value
        /// </summary>
        public ImmutableDictionary<object, AnalyzerReference> CreateAnalyzerReferencesMap(Project projectOpt = null)
        {
            if (projectOpt == null)
            {
                return _hostAnalyzerReferencesMap;
            }

            return _hostAnalyzerReferencesMap.AddRange(CreateProjectAnalyzerReferencesMap(projectOpt));
        }

        /// <summary>
        /// Return <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> of given <paramref name="analyzer"/>.
        /// </summary>
        public ImmutableArray<DiagnosticDescriptor> GetDiagnosticDescriptors(DiagnosticAnalyzer analyzer)
        {
            return (ImmutableArray<DiagnosticDescriptor>)_descriptorCache.GetValue(analyzer, a => GetDiagnosticDescriptorsCore(a));
        }

        private ImmutableArray<DiagnosticDescriptor> GetDiagnosticDescriptorsCore(DiagnosticAnalyzer analyzer)
        {
            ImmutableArray<DiagnosticDescriptor> descriptors;
            try
            {
                // SupportedDiagnostics is user code and can throw an exception.
                descriptors = analyzer.SupportedDiagnostics;
                if (descriptors.IsDefault)
                {
                    descriptors = ImmutableArray<DiagnosticDescriptor>.Empty;
                }
            }
            catch (Exception ex)
            {
                AnalyzerHelper.OnAnalyzerExceptionForSupportedDiagnostics(analyzer, ex, _hostDiagnosticUpdateSource);
                descriptors = ImmutableArray<DiagnosticDescriptor>.Empty;
            }

            return descriptors;
        }

        /// <summary>
        /// Return true if the given <paramref name="analyzer"/> is suppressed for the given project.
        /// </summary>
        public bool IsAnalyzerSuppressed(DiagnosticAnalyzer analyzer, Project project)
        {
            var options = project.CompilationOptions;
            if (options == null || analyzer.IsCompilerAnalyzer())
            {
                return false;
            }

            // Skip telemetry logging for supported diagnostics, as that can cause an infinite loop.
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = (ex, a, diagnostic) =>
                    AnalyzerHelper.OnAnalyzerException_NoTelemetryLogging(ex, a, diagnostic, _hostDiagnosticUpdateSource, project.Id);

            return CompilationWithAnalyzers.IsDiagnosticAnalyzerSuppressed(analyzer, options, onAnalyzerException);
        }

        /// <summary>
        /// Get <see cref="AnalyzerReference"/> identity and <see cref="DiagnosticAnalyzer"/>s map for given <paramref name="language"/>
        /// </summary> 
        public ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> GetHostDiagnosticAnalyzersPerReference(string language)
        {
            return _hostDiagnosticAnalyzersPerLanguageMap.GetOrAdd(language, CreateHostDiagnosticAnalyzersAndBuildMap);
        }

        /// <summary>
        /// Create <see cref="AnalyzerReference"/> identity and <see cref="DiagnosticDescriptor"/>s map
        /// </summary>
        public ImmutableDictionary<object, ImmutableArray<DiagnosticDescriptor>> GetHostDiagnosticDescriptorsPerReference()
        {
            return CreateDiagnosticDescriptorsPerReference(_lazyHostDiagnosticAnalyzersPerReferenceMap.Value, projectOpt: null);
        }

        /// <summary>
        /// Create <see cref="AnalyzerReference"/> identity and <see cref="DiagnosticDescriptor"/>s map for given <paramref name="project"/>
        /// </summary>
        public ImmutableDictionary<object, ImmutableArray<DiagnosticDescriptor>> CreateDiagnosticDescriptorsPerReference(Project project)
        {
            return CreateDiagnosticDescriptorsPerReference(CreateDiagnosticAnalyzersPerReference(project), project);
        }

        /// <summary>
        /// Create <see cref="AnalyzerReference"/> identity and <see cref="DiagnosticAnalyzer"/>s map for given <paramref name="project"/> that
        /// includes both host and project analyzers
        /// </summary>
        public ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> CreateDiagnosticAnalyzersPerReference(Project project)
        {
            var hostAnalyzerReferences = GetHostDiagnosticAnalyzersPerReference(project.Language);
            var projectAnalyzerReferences = CreateProjectDiagnosticAnalyzersPerReference(project);

            return MergeDiagnosticAnalyzerMap(hostAnalyzerReferences, projectAnalyzerReferences);
        }

        /// <summary>
        /// Create <see cref="AnalyzerReference"/> identity and <see cref="DiagnosticAnalyzer"/>s map for given <paramref name="project"/> that
        /// has only project analyzers
        /// </summary>
        public ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> CreateProjectDiagnosticAnalyzersPerReference(Project project)
        {
            return CreateDiagnosticAnalyzersPerReferenceMap(CreateProjectAnalyzerReferencesMap(project), project.Language);
        }

        /// <summary>
        /// Create <see cref="DiagnosticAnalyzer"/>s collection for given <paramref name="project"/>
        /// </summary>
        public ImmutableArray<DiagnosticAnalyzer> CreateDiagnosticAnalyzers(Project project)
        {
            var analyzersPerReferences = CreateDiagnosticAnalyzersPerReference(project);
            return analyzersPerReferences.SelectMany(kv => kv.Value).ToImmutableArray();
        }

        /// <summary>
        /// Check whether given <see cref="DiagnosticData"/> belong to compiler diagnostic analyzer
        /// </summary>
        public bool IsCompilerDiagnostic(string language, DiagnosticData diagnostic)
        {
            var map = GetHostDiagnosticAnalyzersPerReference(language);

            HashSet<string> idMap;
            DiagnosticAnalyzer compilerAnalyzer;
            if (_compilerDiagnosticAnalyzerMap.TryGetValue(language, out compilerAnalyzer) &&
                _compilerDiagnosticAnalyzerDescriptorMap.TryGetValue(compilerAnalyzer, out idMap) &&
                idMap.Contains(diagnostic.Id))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Return compiler <see cref="DiagnosticAnalyzer"/> for the given language.
        /// </summary>
        public DiagnosticAnalyzer GetCompilerDiagnosticAnalyzer(string language)
        {
            var map = GetHostDiagnosticAnalyzersPerReference(language);

            DiagnosticAnalyzer compilerAnalyzer;
            if (_compilerDiagnosticAnalyzerMap.TryGetValue(language, out compilerAnalyzer))
            {
                return compilerAnalyzer;
            }

            return null;
        }

        /// <summary>
        /// Check whether given <see cref="DiagnosticAnalyzer"/> is compiler analyzer for the language or not.
        /// </summary>
        public bool IsCompilerDiagnosticAnalyzer(string language, DiagnosticAnalyzer analyzer)
        {
            var map = GetHostDiagnosticAnalyzersPerReference(language);

            DiagnosticAnalyzer compilerAnalyzer;
            return _compilerDiagnosticAnalyzerMap.TryGetValue(language, out compilerAnalyzer) && compilerAnalyzer == analyzer;
        }

        /// <summary>
        /// Get Name of Package (vsix) which Host <see cref="DiagnosticAnalyzer"/> is from.
        /// </summary>
        public string GetDiagnosticAnalyzerPackageName(string language, DiagnosticAnalyzer analyzer)
        {
            var map = GetHostDiagnosticAnalyzersPerReference(language);

            string name;
            if (_hostDiagnosticAnalyzerPackageNameMap.TryGetValue(analyzer, out name))
            {
                return name;
            }

            return null;
        }

        private ImmutableDictionary<object, AnalyzerReference> CreateProjectAnalyzerReferencesMap(Project project)
        {
            return CreateAnalyzerReferencesMap(project.AnalyzerReferences.Where(CheckAnalyzerReferenceIdentity));
        }

        private ImmutableDictionary<object, ImmutableArray<DiagnosticDescriptor>> CreateDiagnosticDescriptorsPerReference(
            ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> analyzersMap,
            Project projectOpt)
        {
            var builder = ImmutableDictionary.CreateBuilder<object, ImmutableArray<DiagnosticDescriptor>>();
            foreach (var kv in analyzersMap)
            {
                var referenceId = kv.Key;
                var analyzers = kv.Value;

                var descriptors = ImmutableArray.CreateBuilder<DiagnosticDescriptor>();
                foreach (var analyzer in analyzers)
                {
                    // given map should be in good shape. no duplication. no null and etc
                    descriptors.AddRange(GetDiagnosticDescriptors(analyzer));
                }

                // there can't be duplication since _hostAnalyzerReferenceMap is already de-duplicated.
                builder.Add(referenceId, descriptors.ToImmutable());
            }

            return builder.ToImmutable();
        }

        private ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> CreateHostDiagnosticAnalyzersAndBuildMap(string language)
        {
            Contract.ThrowIfNull(language);

            var nameMap = CreateAnalyzerPathToPackageNameMap();

            var builder = ImmutableDictionary.CreateBuilder<object, ImmutableArray<DiagnosticAnalyzer>>();
            foreach (var kv in _hostAnalyzerReferencesMap)
            {
                var referenceIdentity = kv.Key;
                var reference = kv.Value;

                var analyzers = reference.GetAnalyzers(language);
                if (analyzers.Length == 0)
                {
                    continue;
                }

                UpdateCompilerAnalyzerMapIfNeeded(language, analyzers);

                UpdateDiagnosticAnalyzerToPackageNameMap(nameMap, reference, analyzers);

                // there can't be duplication since _hostAnalyzerReferenceMap is already de-duplicated.
                builder.Add(referenceIdentity, analyzers);
            }

            return builder.ToImmutable();
        }

        private void UpdateDiagnosticAnalyzerToPackageNameMap(
            ImmutableDictionary<string, string> nameMap,
            AnalyzerReference reference,
            ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            var fileReference = reference as AnalyzerFileReference;
            if (fileReference == null)
            {
                return;
            }

            string name;
            if (!nameMap.TryGetValue(fileReference.FullPath, out name))
            {
                return;
            }

            foreach (var analyzer in analyzers)
            {
                ImmutableInterlocked.GetOrAdd(ref _hostDiagnosticAnalyzerPackageNameMap, analyzer, _ => name);
            }
        }

        private ImmutableDictionary<string, string> CreateAnalyzerPathToPackageNameMap()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var package in _hostDiagnosticAnalyzerPackages)
            {
                if (string.IsNullOrEmpty(package.Name))
                {
                    continue;
                }

                foreach (var assembly in package.Assemblies)
                {
                    if (!builder.ContainsKey(assembly))
                    {
                        builder.Add(assembly, package.Name);
                    }
                }
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
                    ImmutableInterlocked.GetOrAdd(ref _compilerDiagnosticAnalyzerDescriptorMap, analyzer, a => new HashSet<string>(GetDiagnosticDescriptors(a).Select(d => d.Id)));
                    ImmutableInterlocked.GetOrAdd(ref _compilerDiagnosticAnalyzerMap, language, _ => analyzer);
                    return;
                }
            }
        }

        private bool CheckAnalyzerReferenceIdentity(AnalyzerReference reference)
        {
            if (reference == null)
            {
                return false;
            }

            return !_hostAnalyzerReferencesMap.ContainsKey(reference.Id);
        }

        private static ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> CreateDiagnosticAnalyzersPerReferenceMap(
            IDictionary<object, AnalyzerReference> analyzerReferencesMap, string languageOpt = null)
        {
            var builder = ImmutableDictionary.CreateBuilder<object, ImmutableArray<DiagnosticAnalyzer>>();

            foreach (var reference in analyzerReferencesMap)
            {
                var analyzers = languageOpt == null ? reference.Value.GetAnalyzersForAllLanguages() : reference.Value.GetAnalyzers(languageOpt);
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
            IEnumerable<HostDiagnosticAnalyzerPackage> analyzerPackages,
            HostAnalyzerReferenceDiagnosticReporter reporter,
            IAnalyzerAssemblyLoader hostAnalyzerAssemblyLoader)
        {
            if (analyzerPackages == null || analyzerPackages.IsEmpty())
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

            return builder.ToImmutable();
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
            private readonly AbstractHostDiagnosticUpdateSource _hostUpdateSource;

            public HostAnalyzerReferenceDiagnosticReporter(AbstractHostDiagnosticUpdateSource hostUpdateSource)
            {
                _hostUpdateSource = hostUpdateSource;
            }

            public void OnAnalyzerLoadFailed(object sender, AnalyzerLoadFailureEventArgs e)
            {
                var reference = sender as AnalyzerFileReference;
                if (reference == null)
                {
                    return;
                }

                var diagnostic = AnalyzerHelper.CreateAnalyzerLoadFailureDiagnostic(reference.FullPath, e);

                // diagnostic from host analyzer can never go away
                var args = DiagnosticsUpdatedArgs.DiagnosticsCreated(
                    id: Tuple.Create(this, reference.FullPath, e.ErrorCode, e.TypeName),
                    workspace: PrimaryWorkspace.Workspace,
                    solution: null,
                    projectId: null,
                    documentId: null,
                    diagnostics: ImmutableArray.Create<DiagnosticData>(diagnostic));

                _hostUpdateSource.RaiseDiagnosticsUpdated(args);
            }
        }
    }
}
