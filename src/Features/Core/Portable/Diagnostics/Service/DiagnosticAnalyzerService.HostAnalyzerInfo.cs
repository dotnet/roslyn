// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    private sealed class HostAnalyzerInfo
    {
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
                DocumentDiagnosticAnalyzer analyzer => analyzer.Priority,
                _ => RegularDiagnosticAnalyzerPriority,
            };
        }
    }

    private static readonly ConditionalWeakTable<Project, StrongBox<ImmutableArray<DiagnosticAnalyzer>>> s_projectToAnalyzers = new();

    /// <summary>
    /// Return <see cref="DiagnosticAnalyzer"/>s for the given <see cref="Project"/>. 
    /// </summary>
    private ImmutableArray<DiagnosticAnalyzer> GetProjectAnalyzers_OnlyCallInProcess(Project project)
    {
        if (!s_projectToAnalyzers.TryGetValue(project, out var lazyAnalyzers))
        {
            lazyAnalyzers = new(ComputeProjectAnalyzers());
#if NET
            s_projectToAnalyzers.TryAdd(project, lazyAnalyzers);
#else
            lock (s_projectToAnalyzers)
            {
                if (!s_projectToAnalyzers.TryGetValue(project, out var existing))
                    s_projectToAnalyzers.Add(project, lazyAnalyzers);
            }
#endif
        }

        return lazyAnalyzers.Value;

        ImmutableArray<DiagnosticAnalyzer> ComputeProjectAnalyzers()
        {
            var hostAnalyzerInfo = GetOrCreateHostAnalyzerInfo_OnlyCallInProcess(project);
            var projectAnalyzerInfo = GetOrCreateProjectAnalyzerInfo_OnlyCallInProcess(project);
            return hostAnalyzerInfo.OrderedAllAnalyzers.AddRange(projectAnalyzerInfo.Analyzers);
        }
    }

    private HostAnalyzerInfo GetOrCreateHostAnalyzerInfo_OnlyCallInProcess(Project project)
    {
        var projectAnalyzerInfo = GetOrCreateProjectAnalyzerInfo_OnlyCallInProcess(project);

        var solution = project.Solution;
        var key = new HostAnalyzerInfoKey(project.Language, project.State.HasSdkCodeStyleAnalyzers, solution.SolutionState.Analyzers.HostAnalyzerReferences);
        // Some Host Analyzers may need to be treated as Project Analyzers so that they do not have access to the
        // Host fallback options. These ids will be used when building up the Host and Project analyzer collections.
        var referenceIdsToRedirect = GetReferenceIdsToRedirectAsProjectAnalyzers(project);
        var hostAnalyzerInfo = ImmutableInterlocked.GetOrAdd(ref _hostAnalyzerStateMap, key, CreateLanguageSpecificAnalyzerMap, (solution.SolutionState.Analyzers, referenceIdsToRedirect));
        return hostAnalyzerInfo.WithExcludedAnalyzers(projectAnalyzerInfo.SkippedAnalyzersInfo.SkippedAnalyzers);

        static HostAnalyzerInfo CreateLanguageSpecificAnalyzerMap(HostAnalyzerInfoKey arg, (HostDiagnosticAnalyzers HostAnalyzers, ImmutableHashSet<object> ReferenceIdsToRedirect) state)
        {
            var language = arg.Language;
            var analyzersPerReference = state.HostAnalyzers.GetOrCreateHostDiagnosticAnalyzersPerReference(language);

            var (hostAnalyzerCollection, projectAnalyzerCollection) = GetAnalyzerCollections(analyzersPerReference, state.ReferenceIdsToRedirect);
            var (hostAnalyzers, allAnalyzers) = PartitionAnalyzers(projectAnalyzerCollection, hostAnalyzerCollection, includeWorkspacePlaceholderAnalyzers: true);

            return new HostAnalyzerInfo(hostAnalyzers, allAnalyzers);
        }

        static (ImmutableArray<ImmutableArray<DiagnosticAnalyzer>> HostAnalyzerCollection, ImmutableArray<ImmutableArray<DiagnosticAnalyzer>> ProjectAnalyzerCollection) GetAnalyzerCollections(
            ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> analyzersPerReference,
            ImmutableHashSet<object> referenceIdsToRedirectAsProjectAnalyzers)
        {
            if (referenceIdsToRedirectAsProjectAnalyzers.IsEmpty)
            {
                return ([.. analyzersPerReference.Values], []);
            }

            using var _1 = ArrayBuilder<ImmutableArray<DiagnosticAnalyzer>>.GetInstance(out var hostAnalyzerCollection);
            using var _2 = ArrayBuilder<ImmutableArray<DiagnosticAnalyzer>>.GetInstance(out var projectAnalyzerCollection);

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

            return (hostAnalyzerCollection.ToImmutableAndClear(), projectAnalyzerCollection.ToImmutableAndClear());
        }
    }

    private static (ImmutableHashSet<DiagnosticAnalyzer> hostAnalyzers, ImmutableHashSet<DiagnosticAnalyzer> allAnalyzers) PartitionAnalyzers(
        ImmutableArray<ImmutableArray<DiagnosticAnalyzer>> projectAnalyzerCollection,
        ImmutableArray<ImmutableArray<DiagnosticAnalyzer>> hostAnalyzerCollection,
        bool includeWorkspacePlaceholderAnalyzers)
    {
        using var _1 = PooledHashSet<DiagnosticAnalyzer>.GetInstance(out var hostAnalyzers);
        using var _2 = PooledHashSet<DiagnosticAnalyzer>.GetInstance(out var allAnalyzers);

        if (includeWorkspacePlaceholderAnalyzers)
        {
            hostAnalyzers.Add(FileContentLoadAnalyzer.Instance);
            hostAnalyzers.Add(GeneratorDiagnosticsPlaceholderAnalyzer.Instance);
            allAnalyzers.Add(FileContentLoadAnalyzer.Instance);
            allAnalyzers.Add(GeneratorDiagnosticsPlaceholderAnalyzer.Instance);
        }

        foreach (var analyzers in projectAnalyzerCollection)
        {
            foreach (var analyzer in analyzers)
            {
                Debug.Assert(analyzer != FileContentLoadAnalyzer.Instance && analyzer != GeneratorDiagnosticsPlaceholderAnalyzer.Instance);
                allAnalyzers.Add(analyzer);
            }
        }

        foreach (var analyzers in hostAnalyzerCollection)
        {
            foreach (var analyzer in analyzers)
            {
                Debug.Assert(analyzer != FileContentLoadAnalyzer.Instance && analyzer != GeneratorDiagnosticsPlaceholderAnalyzer.Instance);
                allAnalyzers.Add(analyzer);
                hostAnalyzers.Add(analyzer);
            }
        }

        return (hostAnalyzers.ToImmutableHashSet(), allAnalyzers.ToImmutableHashSet());
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

    private readonly record struct HostAnalyzerInfoKey(
        string Language, bool HasSdkCodeStyleAnalyzers, IReadOnlyList<AnalyzerReference> AnalyzerReferences);
}
