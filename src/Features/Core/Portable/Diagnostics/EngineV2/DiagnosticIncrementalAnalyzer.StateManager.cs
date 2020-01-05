// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        private const string RoslynLanguageServices = "Roslyn Language Services";

        /// <summary>
        /// This is in charge of anything related to <see cref="StateSet"/>
        /// </summary>
        private partial class StateManager
        {
            private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache;
            private readonly IPersistentStorageService _persistentStorageService;

            /// <summary>
            /// Analyzers supplied by the host (IDE). These are built-in to the IDE, the compiler, or from an installed IDE extension (VSIX). 
            /// Maps language name to the analyzers and their state.
            /// </summary>
            private ImmutableDictionary<string, HostAnalyzerStateSets> _hostAnalyzerStateMap;

            /// <summary>
            /// Analyzers referenced by the project via a PackageReference.
            /// </summary>
            private readonly ConcurrentDictionary<ProjectId, ProjectAnalyzerStateSets> _projectAnalyzerStateMap;

            /// <summary>
            /// This will be raised whenever <see cref="StateManager"/> finds <see cref="Project.AnalyzerReferences"/> change
            /// </summary>
            public event EventHandler<ProjectAnalyzerReferenceChangedEventArgs>? ProjectAnalyzerReferenceChanged;

            public StateManager(DiagnosticAnalyzerInfoCache analyzerInfoCache, IPersistentStorageService persistentStorageService)
            {
                _analyzerInfoCache = analyzerInfoCache;
                _persistentStorageService = persistentStorageService;

                _hostAnalyzerStateMap = ImmutableDictionary<string, HostAnalyzerStateSets>.Empty;
                _projectAnalyzerStateMap = new ConcurrentDictionary<ProjectId, ProjectAnalyzerStateSets>(concurrencyLevel: 2, capacity: 10);
            }

            /// <summary>
            /// Return all <see cref="StateSet"/>.
            /// This will never create new <see cref="StateSet"/> but will return ones already created.
            /// </summary>
            public IEnumerable<StateSet> GetAllStateSets()
            {
                return GetAllHostStateSets().Concat(GetAllProjectStateSets());
            }

            /// <summary>
            /// Return <see cref="StateSet"/>s for the given <see cref="ProjectId"/>. 
            /// This will never create new <see cref="StateSet"/> but will return ones already created.
            /// </summary>
            public IEnumerable<StateSet> GetStateSets(ProjectId projectId)
            {
                var hostStateSets = GetAllHostStateSets();

                return _projectAnalyzerStateMap.TryGetValue(projectId, out var entry) ?
                    hostStateSets.Concat(entry.StateSetMap.Values) :
                    hostStateSets;
            }

            /// <summary>
            /// Return <see cref="StateSet"/>s for the given <see cref="Project"/>.
            /// This will never create new <see cref="StateSet"/> but will return ones already created.
            /// Difference with <see cref="GetStateSets(ProjectId)"/> is that 
            /// this will only return <see cref="StateSet"/>s that have same language as <paramref name="project"/>.
            /// </summary>
            public IEnumerable<StateSet> GetStateSets(Project project)
            {
                return GetStateSets(project.Id).Where(s => s.Language == project.Language);
            }

            /// <summary>
            /// Return <see cref="StateSet"/>s for the given <see cref="Project"/>. 
            /// This will either return already created <see cref="StateSet"/>s for the specific snapshot of <see cref="Project"/> or
            /// It will create new <see cref="StateSet"/>s for the <see cref="Project"/> and update internal state.
            /// 
            /// since this has a side-effect, this should never be called concurrently. and incremental analyzer (solution crawler) should guarantee that.
            /// </summary>
            public IEnumerable<StateSet> GetOrUpdateStateSets(Project project)
            {
                return GetOrCreateHostStateSets(project.Language).OrderedStateSets.Concat(GetOrUpdateProjectAnalyzerMap(project).Values);
            }

            /// <summary>
            /// Return <see cref="StateSet"/>s for the given <see cref="Project"/>. 
            /// This will either return already created <see cref="StateSet"/>s for the specific snapshot of <see cref="Project"/> or
            /// It will create new <see cref="StateSet"/>s for the <see cref="Project"/>.
            /// Unlike <see cref="GetOrUpdateStateSets(Project)"/>, this has no side effect.
            /// </summary>
            public IEnumerable<StateSet> GetOrCreateStateSets(Project project)
            {
                return GetOrCreateHostStateSets(project.Language).OrderedStateSets.Concat(GetOrCreateProjectStateSetMap(project).Values);
            }

            /// <summary>
            /// Return <see cref="StateSet"/> for the given <see cref="DiagnosticAnalyzer"/> in the context of <see cref="Project"/>.
            /// This will either return already created <see cref="StateSet"/> for the specific snapshot of <see cref="Project"/> or
            /// It will create new <see cref="StateSet"/> for the <see cref="Project"/>.
            /// This will not have any side effect.
            /// </summary>
            public StateSet? GetOrCreateStateSet(Project project, DiagnosticAnalyzer analyzer)
            {
                var hostStateSets = GetOrCreateHostStateSets(project.Language).StateSetMap;
                if (hostStateSets.TryGetValue(analyzer, out var stateSet))
                {
                    return stateSet;
                }

                var map = GetOrCreateProjectStateSetMap(project);
                if (map.TryGetValue(analyzer, out var set))
                {
                    return set;
                }

                return null;
            }

            /// <summary>
            /// Return <see cref="StateSet"/>s that are added as the given <see cref="Project"/>'s AnalyzerReferences.
            /// This will never create new <see cref="StateSet"/> but will return ones already created.
            /// </summary>
            public ImmutableArray<StateSet> CreateBuildOnlyProjectStateSet(Project project)
            {
                var hostStateSets = GetOrCreateHostStateSets(project.Language).OrderedStateSets;

                if (!project.SupportsCompilation)
                {
                    // languages which don't use our compilation model but diagnostic framework,
                    // all their analyzer should be host analyzers. return all host analyzers
                    // for the language
                    return hostStateSets;
                }

                // now create analyzer to host stateset map
                var hostStateSetMap = hostStateSets.ToDictionary(s => s.Analyzer, s => s);

                // create project analyzer reference identity map
                var referenceIdentities = project.AnalyzerReferences.Select(r => _analyzerInfoCache.GetAnalyzerReferenceIdentity(r)).ToSet();

                // create build only stateSet array
                var stateSets = ImmutableArray.CreateBuilder<StateSet>();

                // we always include compiler analyzer in build only state
                var compilerAnalyzer = _analyzerInfoCache.GetCompilerDiagnosticAnalyzer(project.Language);
                if (compilerAnalyzer == null)
                {
                    // only way to get here is if MEF is corrupted.
                    FailFast.OnFatalException(new Exception("How can this happen?"));
                }

                if (hostStateSetMap.TryGetValue(compilerAnalyzer, out var compilerStateSet))
                {
                    stateSets.Add(compilerStateSet);
                }

                // now add all project analyzers
                stateSets.AddRange(GetOrUpdateProjectAnalyzerMap(project).Values);

                // now add analyzers that exist in both host and project
                var analyzerMap = _analyzerInfoCache.GetOrCreateHostDiagnosticAnalyzersPerReference(project.Language);
                foreach (var (identity, analyzers) in analyzerMap)
                {
                    if (!referenceIdentities.Contains(identity))
                    {
                        // it is from host analyzer package rather than project analyzer reference
                        // which build doesn't have
                        continue;
                    }

                    // if same analyzer exists both in host (vsix) and in analyzer reference,
                    // we include it in build only analyzer.
                    foreach (var analyzer in analyzers)
                    {
                        if (hostStateSetMap.TryGetValue(analyzer, out var stateSet) && stateSet != compilerStateSet)
                        {
                            stateSets.Add(stateSet);
                        }
                    }
                }

                return stateSets.ToImmutable();
            }

            public bool OnDocumentReset(IEnumerable<StateSet> stateSets, Document document)
            {
                // can not be cancelled
                var removed = false;
                foreach (var stateSet in stateSets)
                {
                    removed |= stateSet.OnDocumentReset(document);
                }

                return removed;
            }

            public async Task<bool> OnDocumentOpenedAsync(IEnumerable<StateSet> stateSets, Document document)
            {
                // can not be cancelled
                var opened = false;
                foreach (var stateSet in stateSets)
                {
                    opened |= await stateSet.OnDocumentOpenedAsync(_persistentStorageService, document).ConfigureAwait(false);
                }

                return opened;
            }

            public async Task<bool> OnDocumentClosedAsync(IEnumerable<StateSet> stateSets, Document document)
            {
                // can not be cancelled
                var removed = false;
                foreach (var stateSet in stateSets)
                {
                    removed |= await stateSet.OnDocumentClosedAsync(_persistentStorageService, document).ConfigureAwait(false);
                }

                return removed;
            }

            public bool OnDocumentRemoved(IEnumerable<StateSet> stateSets, DocumentId documentId)
            {
                var removed = false;
                foreach (var stateSet in stateSets)
                {
                    removed |= stateSet.OnDocumentRemoved(documentId);
                }

                return removed;
            }

            public bool OnProjectRemoved(IEnumerable<StateSet> stateSets, ProjectId projectId)
            {
                var removed = false;
                foreach (var stateSet in stateSets)
                {
                    removed |= stateSet.OnProjectRemoved(projectId);
                }

                _projectAnalyzerStateMap.TryRemove(projectId, out _);
                return removed;
            }

            private void RaiseProjectAnalyzerReferenceChanged(ProjectAnalyzerReferenceChangedEventArgs args)
            {
                ProjectAnalyzerReferenceChanged?.Invoke(this, args);
            }

            private static ImmutableDictionary<DiagnosticAnalyzer, StateSet> CreateStateSetMap(
                DiagnosticAnalyzerInfoCache analyzerInfoCache, string language, IEnumerable<ImmutableArray<DiagnosticAnalyzer>> analyzerCollection)
            {
                var compilerAnalyzer = analyzerInfoCache.GetCompilerDiagnosticAnalyzer(language);

                var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, StateSet>();
                foreach (var analyzers in analyzerCollection)
                {
                    foreach (var analyzer in analyzers)
                    {
                        // TODO: 
                        // #1, all de-duplication should move to DiagnosticAnalyzerInfoCache
                        // #2, not sure whether de-duplication of analyzer itself makes sense. this can only happen
                        //     if user deliberately put same analyzer twice.
                        if (builder.ContainsKey(analyzer))
                        {
                            continue;
                        }

                        var buildToolName = analyzer == compilerAnalyzer ?
                            PredefinedBuildTools.Live : GetBuildToolName(analyzerInfoCache, language, analyzer);

                        builder.Add(analyzer, new StateSet(language, analyzer, buildToolName));
                    }
                }

                return builder.ToImmutable();
            }

            private static string GetBuildToolName(DiagnosticAnalyzerInfoCache analyzerInfoCache, string language, DiagnosticAnalyzer analyzer)
            {
                var packageName = analyzerInfoCache.GetDiagnosticAnalyzerPackageName(language, analyzer);
                if (packageName == null)
                {
                    return analyzer.GetAnalyzerAssemblyName();
                }

                if (packageName == RoslynLanguageServices)
                {
                    return PredefinedBuildTools.Live;
                }

                return $"{analyzer.GetAnalyzerAssemblyName()} [{packageName}]";
            }

            [Conditional("DEBUG")]
            private static void VerifyUniqueStateNames(IEnumerable<StateSet> stateSets)
            {
                // Ensure diagnostic state name is indeed unique.
                var set = new HashSet<ValueTuple<string, string>>();

                foreach (var stateSet in stateSets)
                {
                    Contract.ThrowIfFalse(set.Add((stateSet.Language, stateSet.StateName)));
                }
            }

            [Conditional("DEBUG")]
            private void VerifyProjectDiagnosticStates(IEnumerable<StateSet> stateSets)
            {
                // We do not de-duplicate analyzer instances across host and project analyzers.
                var projectAnalyzers = stateSets.Select(state => state.Analyzer).ToImmutableHashSet();

                var hostStates = GetAllHostStateSets().Where(state => !projectAnalyzers.Contains(state.Analyzer));

                VerifyUniqueStateNames(hostStates.Concat(stateSets));
            }
        }
    }
}
