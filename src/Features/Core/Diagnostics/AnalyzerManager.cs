// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// This owns every information about analyzer itself 
    /// such as <see cref="AnalyzerReference"/>, <see cref="DiagnosticAnalyzer"/> and <see cref="DiagnosticDescriptor"/>.
    /// 
    /// people should use this to get <see cref="DiagnosticAnalyzer"/>s or <see cref="DiagnosticDescriptor"/>s of a <see cref="AnalyzerReference"/>.
    /// this will do appropriate de-duplication and cache for those information.
    /// 
    /// this should be alway thread-safe.
    /// </summary>
    internal sealed class AnalyzerManager
    {
        /// <summary>
        /// Key is analyzer reference identity <see cref="GetAnalyzerReferenceIdentity(AnalyzerReference)"/>.
        /// 
        /// We use the key to de-duplicate analyzer references if they are referenced from multiple places.
        /// </summary>
        private readonly ImmutableDictionary<string, AnalyzerReference> _hostAnalyzerReferencesMap;

        /// <summary>
        /// Key is the language the <see cref="DiagnosticAnalyzer"/> supports and key for the second map is analyzer reference identity and
        /// <see cref="DiagnosticAnalyzer"/> for that assembly reference.
        /// 
        /// Entry will be lazily filled in.
        /// </summary>
        private readonly ConcurrentDictionary<string, ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>>> _hostDiagnosticAnalyzersPerLanguageMap;

        /// <summary>
        /// Cache descriptors for each diagnostic analyzer. We do this since <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> is
        /// a property rather than metadata. We expect it to be cheap and immutable, but We can't force them to be so, we cache them
        /// and ask only once.
        /// </summary>
        private readonly ConditionalWeakTable<DiagnosticAnalyzer, IReadOnlyList<DiagnosticDescriptor>> _descriptorCache;

        /// <summary>
        /// Key is analyzer reference identity <see cref="GetAnalyzerReferenceIdentity(AnalyzerReference)"/>.
        /// 
        /// Value is set of <see cref="DiagnosticAnalyzer"/> that belong to the <see cref="AnalyzerReference"/>.
        /// 
        /// We populate it lazily. otherwise, we will bring in all analyzers preemptively
        /// </summary>
        private readonly Lazy<ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>>> _lazyHostDiagnosticAnalyzersPerReferenceMap;

        public AnalyzerManager(IEnumerable<string> hostAnalyzerAssemblies) :
            this(CreateAnalyzerReferencesFromAssemblies(hostAnalyzerAssemblies))
        {
        }

        public AnalyzerManager(ImmutableArray<AnalyzerReference> hostAnalyzerReferences)
        {
            _hostAnalyzerReferencesMap = hostAnalyzerReferences.IsDefault ? ImmutableDictionary<string, AnalyzerReference>.Empty : CreateAnalyzerReferencesMap(hostAnalyzerReferences);

            _hostDiagnosticAnalyzersPerLanguageMap = new ConcurrentDictionary<string, ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>>>(concurrencyLevel: 2, capacity: 2);
            _descriptorCache = new ConditionalWeakTable<DiagnosticAnalyzer, IReadOnlyList<DiagnosticDescriptor>>();

            _lazyHostDiagnosticAnalyzersPerReferenceMap = new Lazy<ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>>>(() => CreateDiagnosticAnalyzersPerReferenceMap(_hostAnalyzerReferencesMap), isThreadSafe: true);

            DiagnosticAnalyzerLogger.LogWorkspaceAnalyzers(hostAnalyzerReferences);
        }

        /// <summary>
        /// It returns a string that can be used as a way to de-duplicate <see cref="AnalyzerReference"/>s.
        /// </summary>
        public string GetAnalyzerReferenceIdentity(AnalyzerReference reference)
        {
            return GetAnalyzerReferenceId(reference);
        }

        /// <summary>
        /// Return <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> of given <paramref name="analyzer"/>.
        /// </summary>
        public ImmutableArray<DiagnosticDescriptor> GetDiagnosticDescriptors(DiagnosticAnalyzer analyzer)
        {
            var descriptors = _descriptorCache.GetValue(analyzer, key =>
            {
                try
                {
                    return analyzer.SupportedDiagnostics;
                }
                catch when (!AnalyzerHelper.IsBuiltInAnalyzer(analyzer))
                {
                    // TODO: here, we should report the issue to host update service
                    // If the SupportedDiagnostics throws an exception, then we don't want to run the analyzer.
                    return ImmutableArray<DiagnosticDescriptor>.Empty;
                }
            });

            return (ImmutableArray<DiagnosticDescriptor>)descriptors;
        }

        /// <summary>
        /// Get <see cref="AnalyzerReference"/> identity and <see cref="DiagnosticAnalyzer"/>s map for given <paramref name="language"/>
        /// </summary>
        public ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> GetHostDiagnosticAnalyzersPerReference(string language)
        {
            return _hostDiagnosticAnalyzersPerLanguageMap.GetOrAdd(language, CreateHostDiagnosticAnalyzers);
        }

        /// <summary>
        /// Create <see cref="AnalyzerReference"/> identity and <see cref="DiagnosticDescriptor"/>s map
        /// </summary>
        public ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> GetHostDiagnosticDescriptorsPerReference()
        {
            return CreateDiagnosticDescriptorsPerReference(_lazyHostDiagnosticAnalyzersPerReferenceMap.Value);
        }

        /// <summary>
        /// Create <see cref="AnalyzerReference"/> identity and <see cref="DiagnosticDescriptor"/>s map for given <paramref name="project"/>
        /// </summary>
        public ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> CreateDiagnosticDescriptorsPerReference(Project project)
        {
            return CreateDiagnosticDescriptorsPerReference(CreateDiagnosticAnalyzersPerReference(project));
        }

        /// <summary>
        /// Create <see cref="AnalyzerReference"/> identity and <see cref="DiagnosticAnalyzer"/>s map for given <paramref name="project"/>
        /// </summary>
        public ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> CreateDiagnosticAnalyzersPerReference(Project project)
        {
            var hostAnalyzerReferences = GetHostDiagnosticAnalyzersPerReference(project.Language);
            var projectAnalyzerReferences = CreateDiagnosticAnalyzersPerReferenceMap(CreateAnalyzerReferencesMap(project.AnalyzerReferences.Where(CheckAnalyzerReferenceIdentity)), project.Language);

            return MergeDiagnosticAnalyzerMap(hostAnalyzerReferences, projectAnalyzerReferences);
        }

        /// <summary>
        /// Create <see cref="DiagnosticAnalyzer"/>s collection for given <paramref name="project"/>
        /// </summary>
        public ImmutableArray<DiagnosticAnalyzer> CreateDiagnosticAnalyzers(Project project)
        {
            var analyzersPerReferences = CreateDiagnosticAnalyzersPerReference(project);
            return analyzersPerReferences.SelectMany(kv => kv.Value).ToImmutableArray();
        }

        private ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> CreateDiagnosticDescriptorsPerReference(
            ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzersMap)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<DiagnosticDescriptor>>();
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

        private ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> CreateHostDiagnosticAnalyzers(string language)
        {
            Contract.ThrowIfNull(language);

            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<DiagnosticAnalyzer>>();
            foreach (var kv in _hostAnalyzerReferencesMap)
            {
                var referenceIdenity = kv.Key;
                var reference = kv.Value;

                var analyzers = reference.GetAnalyzers(language);
                if (analyzers.Length == 0)
                {
                    continue;
                }

                // there can't be duplication since _hostAnalyzerReferenceMap is already de-duplicated.
                builder.Add(referenceIdenity, reference.GetAnalyzers(language));
            }

            return builder.ToImmutable();
        }

        private static string GetAnalyzerReferenceId(AnalyzerReference reference)
        {
            return reference.Display ?? FeaturesResources.Unknown;
        }

        private bool CheckAnalyzerReferenceIdentity(AnalyzerReference reference)
        {
            if (reference == null)
            {
                return false;
            }

            return !_hostAnalyzerReferencesMap.ContainsKey(GetAnalyzerReferenceId(reference));
        }

        private static ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> CreateDiagnosticAnalyzersPerReferenceMap(
            IDictionary<string, AnalyzerReference> analyzerReferencesMap, string languageOpt = null)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<DiagnosticAnalyzer>>();

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

        private static ImmutableDictionary<string, AnalyzerReference> CreateAnalyzerReferencesMap(IEnumerable<AnalyzerReference> analyzerReferences)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, AnalyzerReference>();
            foreach (var reference in analyzerReferences)
            {
                string key = GetAnalyzerReferenceId(reference);

                // filter out duplicated analyzer reference
                if (builder.ContainsKey(key))
                {
                    continue;
                }

                builder.Add(key, reference);
            }

            return builder.ToImmutable();
        }

        private static ImmutableArray<AnalyzerReference> CreateAnalyzerReferencesFromAssemblies(IEnumerable<string> analyzerAssemblies)
        {
            if (analyzerAssemblies == null || analyzerAssemblies.IsEmpty())
            {
                return ImmutableArray<AnalyzerReference>.Empty;
            }

            // We want to load the analyzer assembly assets in default context.
            // Use Assembly.Load instead of Assembly.LoadFrom to ensure that if the assembly is ngen'ed, then the native image gets loaded.
            Func<string, Assembly> getAssembly = (fullPath) => Assembly.Load(AssemblyName.GetAssemblyName(fullPath));

            var builder = ImmutableArray.CreateBuilder<AnalyzerReference>();
            foreach (var analyzerAssembly in analyzerAssemblies.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                builder.Add(new AnalyzerFileReference(analyzerAssembly, getAssembly));
            }

            return builder.ToImmutable();
        }

        private static ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> MergeDiagnosticAnalyzerMap(
            ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> map1, ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> map2)
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
    }
}
