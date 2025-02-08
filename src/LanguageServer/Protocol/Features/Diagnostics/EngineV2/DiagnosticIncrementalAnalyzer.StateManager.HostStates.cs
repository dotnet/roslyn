// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    private partial class DiagnosticIncrementalAnalyzer
    {
        private partial class StateManager
        {
            private HostAnalyzerStateSets GetOrCreateHostStateSets(Project project, ProjectAnalyzerStateSets projectStateSets)
            {
                var key = new HostAnalyzerStateSetKey(project.Language, project.State.HasSdkCodeStyleAnalyzers, project.Solution.SolutionState.Analyzers.HostAnalyzerReferences);
                // Some Host Analyzers may need to be treated as Project Analyzers so that they do not have access to the
                // Host fallback options. These ids will be used when building up the Host and Project analyzer collections.
                var referenceIdsToRedirect = GetReferenceIdsToRedirectAsProjectAnalyzers(project);
                var hostStateSets = ImmutableInterlocked.GetOrAdd(ref _hostAnalyzerStateMap, key, CreateLanguageSpecificAnalyzerMap, (project.Solution.SolutionState.Analyzers, referenceIdsToRedirect));
                return hostStateSets.WithExcludedAnalyzers(projectStateSets.SkippedAnalyzersInfo.SkippedAnalyzers);

                static HostAnalyzerStateSets CreateLanguageSpecificAnalyzerMap(HostAnalyzerStateSetKey arg, (HostDiagnosticAnalyzers HostAnalyzers, ImmutableHashSet<object> ReferenceIdsToRedirect) state)
                {
                    var language = arg.Language;
                    var analyzersPerReference = state.HostAnalyzers.GetOrCreateHostDiagnosticAnalyzersPerReference(language);

                    var (hostAnalyzerCollection, projectAnalyzerCollection) = GetAnalyzerCollections(analyzersPerReference, state.ReferenceIdsToRedirect);
                    var analyzerMap = CreateStateSetMap(projectAnalyzerCollection, hostAnalyzerCollection, includeWorkspacePlaceholderAnalyzers: true);

                    return new HostAnalyzerStateSets(analyzerMap);
                }

                static (IEnumerable<ImmutableArray<DiagnosticAnalyzer>> HostAnalyzerCollection, IEnumerable<ImmutableArray<DiagnosticAnalyzer>> ProjectAnalyzerCollection) GetAnalyzerCollections(
                    ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> analyzersPerReference,
                    ImmutableHashSet<object> referenceIdsToRedirectAsProjectAnalyzers)
                {
                    if (referenceIdsToRedirectAsProjectAnalyzers.IsEmpty)
                    {
                        return (analyzersPerReference.Values, []);
                    }

                    var hostAnalyzerCollection = ArrayBuilder<ImmutableArray<DiagnosticAnalyzer>>.GetInstance();
                    var projectAnalyzerCollection = ArrayBuilder<ImmutableArray<DiagnosticAnalyzer>>.GetInstance();

                    foreach (var (referenceId, analyzers) in analyzersPerReference)
                    {
                        if (referenceIdsToRedirectAsProjectAnalyzers.Contains(referenceId))
                        {
                            projectAnalyzerCollection.Add(analyzers);
                        }
                        else
                        {
                            hostAnalyzerCollection.Add(analyzers);
                        }
                    }

                    return (hostAnalyzerCollection.ToImmutableAndFree(), projectAnalyzerCollection.ToImmutableAndFree());
                }
            }

            private static ImmutableHashSet<object> GetReferenceIdsToRedirectAsProjectAnalyzers(Project project)
            {
                if (project.State.HasSdkCodeStyleAnalyzers)
                {
                    // When a project uses CodeStyle analyzers added by the SDK, we remove them in favor of the
                    // Features analyzers. We need to then treat the Features analyzers as Project analyzers so
                    // they do not get access to the Host fallback options.
                    return GetFeaturesAnalyzerReferenceIds(project.Solution.SolutionState.Analyzers);
                }

                return [];

                static ImmutableHashSet<object> GetFeaturesAnalyzerReferenceIds(HostDiagnosticAnalyzers hostAnalyzers)
                {
                    var builder = ImmutableHashSet.CreateBuilder<object>();

                    foreach (var analyzerReference in hostAnalyzers.HostAnalyzerReferences)
                    {
                        if (analyzerReference.IsFeaturesAnalyzer())
                            builder.Add(analyzerReference.Id);
                    }

                    return builder.ToImmutable();
                }
            }

            private sealed class HostAnalyzerStateSets
            {
                private const int FileContentLoadAnalyzerPriority = -4;
                private const int GeneratorDiagnosticsPlaceholderAnalyzerPriority = -3;
                private const int BuiltInCompilerPriority = -2;
                private const int RegularDiagnosticAnalyzerPriority = -1;

                // ordered by priority
                public readonly ImmutableArray<StateSet> OrderedStateSets;

                public readonly ImmutableDictionary<DiagnosticAnalyzer, StateSet> StateSetMap;

                private HostAnalyzerStateSets(ImmutableDictionary<DiagnosticAnalyzer, StateSet> stateSetMap, ImmutableArray<StateSet> orderedStateSets)
                {
                    StateSetMap = stateSetMap;
                    OrderedStateSets = orderedStateSets;
                }

                public HostAnalyzerStateSets(ImmutableDictionary<DiagnosticAnalyzer, StateSet> analyzerMap)
                {
                    StateSetMap = analyzerMap;

                    // order statesets
                    // order will be in this order
                    // BuiltIn Compiler Analyzer (C#/VB) < Regular DiagnosticAnalyzers < Document/ProjectDiagnosticAnalyzers
                    OrderedStateSets = [.. StateSetMap.Values.OrderBy(PriorityComparison)];
                }

                public HostAnalyzerStateSets WithExcludedAnalyzers(ImmutableHashSet<DiagnosticAnalyzer> excludedAnalyzers)
                {
                    if (excludedAnalyzers.IsEmpty)
                    {
                        return this;
                    }

                    var stateSetMap = StateSetMap.Where(kvp => !excludedAnalyzers.Contains(kvp.Key)).ToImmutableDictionary();
                    var orderedStateSets = OrderedStateSets.WhereAsArray(stateSet => !excludedAnalyzers.Contains(stateSet.Analyzer));
                    return new HostAnalyzerStateSets(stateSetMap, orderedStateSets);
                }

                private int PriorityComparison(StateSet state1, StateSet state2)
                    => GetPriority(state1) - GetPriority(state2);

                private static int GetPriority(StateSet state)
                {
                    // compiler gets highest priority
                    if (state.Analyzer.IsCompilerAnalyzer())
                    {
                        return BuiltInCompilerPriority;
                    }

                    return state.Analyzer switch
                    {
                        FileContentLoadAnalyzer _ => FileContentLoadAnalyzerPriority,
                        GeneratorDiagnosticsPlaceholderAnalyzer _ => GeneratorDiagnosticsPlaceholderAnalyzerPriority,
                        DocumentDiagnosticAnalyzer analyzer => Math.Max(0, analyzer.Priority),
                        ProjectDiagnosticAnalyzer analyzer => Math.Max(0, analyzer.Priority),
                        _ => RegularDiagnosticAnalyzerPriority,
                    };
                }
            }
        }
    }
}
