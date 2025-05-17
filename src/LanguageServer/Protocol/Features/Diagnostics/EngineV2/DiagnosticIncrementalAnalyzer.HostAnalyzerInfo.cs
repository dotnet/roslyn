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
            private HostAnalyzerInfo GetOrCreateHostAnalyzerInfo(
                SolutionState solution, ProjectState project, ProjectAnalyzerInfo projectAnalyzerInfo)
            {
                var key = new HostAnalyzerInfoKey(project.Language, project.HasSdkCodeStyleAnalyzers, solution.Analyzers.HostAnalyzerReferences);
                // Some Host Analyzers may need to be treated as Project Analyzers so that they do not have access to the
                // Host fallback options. These ids will be used when building up the Host and Project analyzer collections.
                var referenceIdsToRedirect = GetReferenceIdsToRedirectAsProjectAnalyzers(solution, project);
                var hostAnalyzerInfo = ImmutableInterlocked.GetOrAdd(ref _hostAnalyzerStateMap, key, CreateLanguageSpecificAnalyzerMap, (solution.Analyzers, referenceIdsToRedirect));
                return hostAnalyzerInfo.WithExcludedAnalyzers(projectAnalyzerInfo.SkippedAnalyzersInfo.SkippedAnalyzers);

                static HostAnalyzerInfo CreateLanguageSpecificAnalyzerMap(HostAnalyzerInfoKey arg, (HostDiagnosticAnalyzers HostAnalyzers, ImmutableHashSet<object> ReferenceIdsToRedirect) state)
                {
                    var language = arg.Language;
                    var analyzersPerReference = state.HostAnalyzers.GetOrCreateHostDiagnosticAnalyzersPerReference(language);

                    var (hostAnalyzerCollection, projectAnalyzerCollection) = GetAnalyzerCollections(analyzersPerReference, state.ReferenceIdsToRedirect);
                    var (hostAnalyzers, allAnalyzers) = PartitionAnalyzers(projectAnalyzerCollection, hostAnalyzerCollection, includeWorkspacePlaceholderAnalyzers: true);

                    return new HostAnalyzerInfo(hostAnalyzers, allAnalyzers);
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

            private static ImmutableHashSet<object> GetReferenceIdsToRedirectAsProjectAnalyzers(
                SolutionState solution, ProjectState project)
            {
                if (project.HasSdkCodeStyleAnalyzers)
                {
                    // When a project uses CodeStyle analyzers added by the SDK, we remove them in favor of the
                    // Features analyzers. We need to then treat the Features analyzers as Project analyzers so
                    // they do not get access to the Host fallback options.
                    return GetFeaturesAnalyzerReferenceIds(solution.Analyzers);
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
        }
    }

    private sealed class HostAnalyzerInfo
    {
        private const int FileContentLoadAnalyzerPriority = -4;
        private const int GeneratorDiagnosticsPlaceholderAnalyzerPriority = -3;
        private const int BuiltInCompilerPriority = -2;
        private const int RegularDiagnosticAnalyzerPriority = -1;

        private readonly ImmutableHashSet<DiagnosticAnalyzer> _hostAnalyzers;
        private readonly ImmutableHashSet<DiagnosticAnalyzer> _allAnalyzers;
        public readonly ImmutableArray<DiagnosticAnalyzer> OrderedAllAnalyzers;

        public HostAnalyzerInfo(
            ImmutableHashSet<DiagnosticAnalyzer> hostAnalyzers,
            ImmutableHashSet<DiagnosticAnalyzer> allAnalyzers)
        {
            _hostAnalyzers = hostAnalyzers;
            _allAnalyzers = allAnalyzers;

            // order analyzers.
            // order will be in this order
            // BuiltIn Compiler Analyzer (C#/VB) < Regular DiagnosticAnalyzers < Document/ProjectDiagnosticAnalyzers
            OrderedAllAnalyzers = [.. _allAnalyzers.OrderBy(PriorityComparison)];
        }

        public bool IsHostAnalyzer(DiagnosticAnalyzer analyzer)
            => _hostAnalyzers.Contains(analyzer);

        public HostAnalyzerInfo WithExcludedAnalyzers(ImmutableHashSet<DiagnosticAnalyzer> excludedAnalyzers)
        {
            if (excludedAnalyzers.IsEmpty)
            {
                return this;
            }

            return new(_hostAnalyzers, _allAnalyzers.Except(excludedAnalyzers));
        }

        private int PriorityComparison(DiagnosticAnalyzer state1, DiagnosticAnalyzer state2)
            => GetPriority(state1) - GetPriority(state2);

        private static int GetPriority(DiagnosticAnalyzer state)
        {
            // compiler gets highest priority
            if (state.IsCompilerAnalyzer())
            {
                return BuiltInCompilerPriority;
            }

            return state switch
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
