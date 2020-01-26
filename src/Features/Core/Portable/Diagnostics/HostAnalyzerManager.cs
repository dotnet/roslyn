// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Provides and caches information about diagnostic analyzers such as <see cref="AnalyzerReference"/>, 
    /// <see cref="DiagnosticAnalyzer"/> instance, <see cref="DiagnosticDescriptor"/>s.
    /// Thread-safe.
    /// </summary>
    internal sealed partial class DiagnosticAnalyzerInfoCache
    {
        /// <summary>
        /// This contains vsix info on where <see cref="HostDiagnosticAnalyzerPackage"/> comes from.
        /// </summary>
        private readonly Lazy<ImmutableArray<HostDiagnosticAnalyzerPackage>> _hostDiagnosticAnalyzerPackages;

        /// <summary>
        /// Key is analyzer reference identity <see cref="GetAnalyzerReferenceIdentity(AnalyzerReference)"/>.
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
        /// Key is analyzer reference identity <see cref="GetAnalyzerReferenceIdentity(AnalyzerReference)"/>.
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

        /// <summary>
        /// Map from host diagnostic analyzer to package name it came from.
        /// </summary>
        /// <remarks>
        /// Instances of <see cref="DiagnosticAnalyzer"/> coming from the host (e.g. VSIX) are never released.
        /// </remarks>
        private ImmutableDictionary<DiagnosticAnalyzer, string> _hostDiagnosticAnalyzerPackageNameMap;

        /// <summary>
        /// Set of diagnostic ids supported by a given compiler diagnostic analyzer.
        /// Allows us to quickly determine whether a diagnostic is a compiler diagnostic.
        /// </summary>
        private ImmutableDictionary<DiagnosticAnalyzer, HashSet<string>> _compilerDiagnosticAnalyzerDescriptorMap;

        /// <summary>
        /// Supported descriptors of each <see cref="DiagnosticAnalyzer"/>. 
        /// </summary>
        /// <remarks>
        /// Holds on <see cref="DiagnosticAnalyzer"/> instances weakly so that we don't keep analyzers coming from package references alive.
        /// They need to be released when the project stops referencing the analyzer.
        /// 
        /// The purpose of this map is to avoid multiple calls to <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> that might return different values
        /// (they should not but we need a guarantee to function correctly).
        /// </remarks>
        private readonly ConditionalWeakTable<DiagnosticAnalyzer, DiagnosticDescriptorsInfo> _descriptorsInfo;

        private sealed class DiagnosticDescriptorsInfo
        {
            public readonly ImmutableArray<DiagnosticDescriptor> SupportedDescriptors;
            public readonly bool TelemetryAllowed;

            public DiagnosticDescriptorsInfo(ImmutableArray<DiagnosticDescriptor> supportedDescriptors, bool telemetryAllowed)
            {
                SupportedDescriptors = supportedDescriptors;
                TelemetryAllowed = telemetryAllowed;
            }
        }

        public DiagnosticAnalyzerInfoCache(Lazy<ImmutableArray<HostDiagnosticAnalyzerPackage>> hostAnalyzerPackages, IAnalyzerAssemblyLoader? hostAnalyzerAssemblyLoader, AbstractHostDiagnosticUpdateSource? hostDiagnosticUpdateSource, PrimaryWorkspace primaryWorkspace)
            : this(new Lazy<ImmutableArray<AnalyzerReference>>(() => CreateAnalyzerReferencesFromPackages(hostAnalyzerPackages.Value, new HostAnalyzerReferenceDiagnosticReporter(hostDiagnosticUpdateSource, primaryWorkspace), hostAnalyzerAssemblyLoader), isThreadSafe: true),
                   hostAnalyzerPackages)
        {
        }

        internal DiagnosticAnalyzerInfoCache(ImmutableArray<AnalyzerReference> hostAnalyzerReferences)
            : this(new Lazy<ImmutableArray<AnalyzerReference>>(() => hostAnalyzerReferences), new Lazy<ImmutableArray<HostDiagnosticAnalyzerPackage>>(() => ImmutableArray<HostDiagnosticAnalyzerPackage>.Empty))
        {
        }

        private DiagnosticAnalyzerInfoCache(
            Lazy<ImmutableArray<AnalyzerReference>> hostAnalyzerReferences, Lazy<ImmutableArray<HostDiagnosticAnalyzerPackage>> hostAnalyzerPackages)
        {
            _hostDiagnosticAnalyzerPackages = hostAnalyzerPackages;

            _hostAnalyzerReferencesMap = new Lazy<ImmutableDictionary<object, AnalyzerReference>>(() => hostAnalyzerReferences.Value.IsDefaultOrEmpty ? ImmutableDictionary<object, AnalyzerReference>.Empty : CreateAnalyzerReferencesMap(hostAnalyzerReferences.Value), isThreadSafe: true);
            _hostDiagnosticAnalyzersPerLanguageMap = new ConcurrentDictionary<string, ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>>>(concurrencyLevel: 2, capacity: 2);
            _lazyHostDiagnosticAnalyzersPerReferenceMap = new Lazy<ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>>>(() => CreateDiagnosticAnalyzersPerReferenceMap(_hostAnalyzerReferencesMap.Value), isThreadSafe: true);

            _compilerDiagnosticAnalyzerMap = ImmutableDictionary<string, DiagnosticAnalyzer>.Empty;
            _compilerDiagnosticAnalyzerDescriptorMap = ImmutableDictionary<DiagnosticAnalyzer, HashSet<string>>.Empty;
            _hostDiagnosticAnalyzerPackageNameMap = ImmutableDictionary<DiagnosticAnalyzer, string>.Empty;
            _descriptorsInfo = new ConditionalWeakTable<DiagnosticAnalyzer, DiagnosticDescriptorsInfo>();
        }

        /// <summary>
        /// It returns a string that can be used as a way to de-duplicate <see cref="AnalyzerReference"/>s.
        /// </summary>
        public object GetAnalyzerReferenceIdentity(AnalyzerReference reference)
            => reference.Id;

        /// <summary>
        /// It returns a map with <see cref="AnalyzerReference.Id"/> as key and <see cref="AnalyzerReference"/> as value
        /// </summary>
        public ImmutableDictionary<object, AnalyzerReference> GetHostAnalyzerReferencesMap()
            => _hostAnalyzerReferencesMap.Value;

        /// <summary>
        /// Returns <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> of given <paramref name="analyzer"/>.
        /// </summary>
        public ImmutableArray<DiagnosticDescriptor> GetDiagnosticDescriptors(DiagnosticAnalyzer analyzer)
            => GetOrCreateDescriptorsInfo(analyzer).SupportedDescriptors;

        /// <summary>
        /// Determine whether collection of telemetry is allowed for given <paramref name="analyzer"/>.
        /// </summary>
        public bool IsTelemetryCollectionAllowed(DiagnosticAnalyzer analyzer)
            => GetOrCreateDescriptorsInfo(analyzer).TelemetryAllowed;

        private DiagnosticDescriptorsInfo GetOrCreateDescriptorsInfo(DiagnosticAnalyzer analyzer)
            => _descriptorsInfo.GetValue(analyzer, CalculateDescriptorsInfo);

        private DiagnosticDescriptorsInfo CalculateDescriptorsInfo(DiagnosticAnalyzer analyzer)
        {
            ImmutableArray<DiagnosticDescriptor> descriptors;
            try
            {
                // SupportedDiagnostics is user code and can throw an exception.
                descriptors = analyzer.SupportedDiagnostics.NullToEmpty();
            }
            catch
            {
                // No need to report the exception to the user.
                // Eventually, when the analyzer runs the compiler analyzer driver will report a diagnostic.
                descriptors = ImmutableArray<DiagnosticDescriptor>.Empty;
            }

            bool telemetryAllowed = IsTelemetryCollectionAllowed(analyzer, descriptors);
            return new DiagnosticDescriptorsInfo(descriptors, telemetryAllowed);
        }

        private static bool IsTelemetryCollectionAllowed(DiagnosticAnalyzer analyzer, ImmutableArray<DiagnosticDescriptor> descriptors)
            => analyzer.IsCompilerAnalyzer() ||
               analyzer is IBuiltInAnalyzer ||
               descriptors.Length > 0 && descriptors[0].CustomTags.Any(t => t == WellKnownDiagnosticTags.Telemetry);

        /// <summary>
        /// Return true if the given <paramref name="analyzer"/> is suppressed for the given project.
        /// NOTE: This API is intended to be used only for performance optimization.
        /// </summary>
        public bool IsAnalyzerSuppressed(DiagnosticAnalyzer analyzer, Project project)
        {
            var options = project.CompilationOptions;
            if (options == null || analyzer == FileContentLoadAnalyzer.Instance || IsCompilerDiagnosticAnalyzer(project.Language, analyzer))
            {
                return false;
            }

            // If user has disabled analyzer execution for this project, we only want to execute required analyzers
            // that report diagnostics with category "Compiler".
            if (!project.State.RunAnalyzers &&
                GetDiagnosticDescriptors(analyzer).All(d => d.Category != DiagnosticCategory.Compiler))
            {
                return true;
            }

            // NOTE: Previously we used to return "CompilationWithAnalyzers.IsDiagnosticAnalyzerSuppressed(options)"
            //       on this code path, which returns true if analyzer is suppressed through compilation options.
            //       However, this check is no longer correct as analyzers can be enabled/disabled for individual
            //       documents through .editorconfig files. So we pessimistically assume analyzer is not suppressed
            //       and let the core analyzer driver in the compiler layer handle skipping redundant analysis callbacks.
            return false;
        }

        /// <summary>
        /// Get <see cref="AnalyzerReference"/> identity and <see cref="DiagnosticAnalyzer"/>s map for given <paramref name="language"/>
        /// </summary> 
        public ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> GetOrCreateHostDiagnosticAnalyzersPerReference(string language)
        {
            return _hostDiagnosticAnalyzersPerLanguageMap.GetOrAdd(language, CreateHostDiagnosticAnalyzersAndBuildMap);
        }

        public ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> GetDiagnosticDescriptorsPerReference()
        {
            return ConvertReferenceIdentityToName(
                CreateDiagnosticDescriptorsPerReference(_lazyHostDiagnosticAnalyzersPerReferenceMap.Value),
                _hostAnalyzerReferencesMap.Value);
        }

        public ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> GetDiagnosticDescriptorsPerReference(Project project)
        {
            var descriptorPerReference = CreateDiagnosticDescriptorsPerReference(CreateDiagnosticAnalyzersPerReference(project));
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
        {
            return CreateDiagnosticAnalyzersPerReferenceMap(CreateProjectAnalyzerReferencesMap(project), project.Language);
        }

        /// <summary>
        /// Check whether given <see cref="DiagnosticData"/> belong to compiler diagnostic analyzer
        /// </summary>
        public bool IsCompilerDiagnostic(string language, DiagnosticData diagnostic)
        {
            _ = GetOrCreateHostDiagnosticAnalyzersPerReference(language);
            if (_compilerDiagnosticAnalyzerMap.TryGetValue(language, out var compilerAnalyzer) &&
                _compilerDiagnosticAnalyzerDescriptorMap.TryGetValue(compilerAnalyzer, out var idMap) &&
                idMap.Contains(diagnostic.Id))
            {
                return true;
            }

            return false;
        }

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

        /// <summary>
        /// Check whether given <see cref="DiagnosticAnalyzer"/> is compiler analyzer for the language or not.
        /// </summary>
        public bool IsCompilerDiagnosticAnalyzer(string language, DiagnosticAnalyzer analyzer)
        {
            _ = GetOrCreateHostDiagnosticAnalyzersPerReference(language);
            return _compilerDiagnosticAnalyzerMap.TryGetValue(language, out var compilerAnalyzer) && compilerAnalyzer == analyzer;
        }

        public bool SupportAnalysisKind(DiagnosticAnalyzer analyzer, string language, AnalysisKind kind)
        {
            // compiler diagnostic analyzer always supports all kinds:
            if (IsCompilerDiagnosticAnalyzer(language, analyzer))
            {
                return true;
            }

            return kind switch
            {
                AnalysisKind.Syntax => analyzer.SupportsSyntaxDiagnosticAnalysis(),
                AnalysisKind.Semantic => analyzer.SupportsSemanticDiagnosticAnalysis(),
                _ => throw ExceptionUtilities.UnexpectedValue(kind)
            };
        }

        /// <summary>
        /// Get Name of Package (vsix) which Host <see cref="DiagnosticAnalyzer"/> is from.
        /// </summary>
        public string? GetDiagnosticAnalyzerPackageName(string language, DiagnosticAnalyzer analyzer)
        {
            _ = GetOrCreateHostDiagnosticAnalyzersPerReference(language);
            if (_hostDiagnosticAnalyzerPackageNameMap.TryGetValue(analyzer, out var name))
            {
                return name;
            }

            return null;
        }

        private ImmutableDictionary<object, AnalyzerReference> CreateProjectAnalyzerReferencesMap(Project project)
        {
            return CreateAnalyzerReferencesMap(project.AnalyzerReferences.Where(reference => !_hostAnalyzerReferencesMap.Value.ContainsKey(reference.Id)));
        }

        private ImmutableDictionary<object, ImmutableArray<DiagnosticDescriptor>> CreateDiagnosticDescriptorsPerReference(
            ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> analyzersMap)
        {
            var builder = ImmutableDictionary.CreateBuilder<object, ImmutableArray<DiagnosticDescriptor>>();
            foreach (var (referenceId, analyzers) in analyzersMap)
            {
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
            foreach (var (referenceIdentity, reference) in _hostAnalyzerReferencesMap.Value)
            {
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
            if (!(reference is AnalyzerFileReference fileReference))
            {
                return;
            }

            if (!nameMap.TryGetValue(fileReference.FullPath, out var name))
            {
                return;
            }

            foreach (var analyzer in analyzers)
            {
                ImmutableInterlocked.GetOrAdd(ref _hostDiagnosticAnalyzerPackageNameMap, analyzer, name);
            }
        }

        private ImmutableDictionary<string, string> CreateAnalyzerPathToPackageNameMap()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var package in _hostDiagnosticAnalyzerPackages.Value)
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

            public void OnAnalyzerLoadFailed(object sender, AnalyzerLoadFailureEventArgs e)
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
